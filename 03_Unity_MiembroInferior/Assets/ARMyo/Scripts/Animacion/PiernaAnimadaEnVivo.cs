using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using ARMyo.Biomecanica;

namespace ARMyo.Animacion
{
    /// <summary>
    /// La pierna 3D imita EN VIVO el ángulo de rodilla del usuario.
    /// En vez de reproducir FLEXION_RODILLA sola, se fija su tiempo en cada frame según
    /// FuenteAngulosPierna.FlexionRodilla (0° = recta, ~120° = flexión máxima).
    ///
    /// Al arrancar se "mide" el clip: se muestrea en N puntos y se calcula el ángulo real de la
    /// rodilla del modelo en cada uno (hueso del muslo vs. hueso de la pierna). Así se sabe qué
    /// tiempo de la animación corresponde a cada ángulo, aunque el clip no sea lineal o
    /// flexione y vuelva. Si no se encuentran los huesos, se usa un mapeo lineal.
    ///
    /// El clip solo flexiona la rodilla hasta ~88°. Por encima, la rodilla del modelo se sigue
    /// doblando por código (girando el hueso de la pierna sobre el mismo eje que usa el clip),
    /// hasta anguloMaxModelo. Así no hace falta retocar la animación en Blender.
    ///
    /// Usa un PlayableGraph en modo manual: no depende del Animator Controller ni de los
    /// nombres de sus estados. El controller se desactiva mientras este script está activo.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PiernaAnimadaEnVivo : MonoBehaviour
    {
        [Tooltip("Dejar vacío: se busca sola en la escena.")]
        public FuenteAngulosPierna fuente;

        [Tooltip("Clip de flexión de rodilla. Vacío = se busca un clip cuyo nombre contenga 'FLEXION_RODILLA'.")]
        public AnimationClip clip;

        [Header("Huesos para medir el clip")]
        public string huesoMuslo = "Leg_Upper.R";
        public string huesoPierna = "Leg_Knee.R";

        [Header("Mapeo ángulo → tiempo")]
        [Tooltip("true: la pierna 3D muestra el MISMO ángulo que el usuario (si el clip llega a él).\n" +
                 "false: 0..anguloUsuarioMax se reparte linealmente en el tramo de flexión del clip.")]
        public bool usarAnguloReal = true;
        [Tooltip("Ángulo del usuario que corresponde a la flexión máxima del clip (solo modo lineal).")]
        public float anguloUsuarioMax = 120f;
        [Tooltip("Muestras para medir el clip.")]
        [Range(16, 256)] public int muestras = 96;

        [Header("Más allá del clip")]
        [Tooltip("Seguir doblando la rodilla por código cuando el ángulo supera el máximo del clip.")]
        public bool extenderMasAllaDelClip = true;
        [Tooltip("Flexión máxima que puede mostrar el modelo (°).")]
        [Range(90f, 160f)] public float anguloMaxModelo = 135f;

        [Header("Suavizado visual")]
        [Tooltip("Tiempo (s) para alcanzar el ángulo medido. 0 = sin suavizado.")]
        [Range(0f, 0.5f)] public float suavizado = 0.08f;

        [Header("Resultado (solo lectura)")]
        [SerializeField] float anguloMostrado;
        [SerializeField] float tiempoNormalizado;
        [SerializeField] float anguloMaxClip;
        [SerializeField] float tFinFlexion = 1f;
        [SerializeField] bool mapeoMedido;

        public float AnguloMostrado => anguloMostrado;
        public float AnguloMaxClip => anguloMaxClip;
        /// <summary>Flexión máxima que el modelo puede mostrar (clip + extensión por código).</summary>
        public float AnguloMaxAlcanzable => mapeoMedido && extenderMasAllaDelClip ? Mathf.Max(anguloMaxClip, anguloMaxModelo) : anguloMaxClip;

        Animator _animator;
        RuntimeAnimatorController _controllerOriginal;
        PlayableGraph _graph;
        AnimationClipPlayable _playable;
        float[] _tablaAngulo;   // ángulo del modelo (monótono creciente) en cada muestra del tramo de flexión
        float[] _tablaTiempo;   // tiempo normalizado de esa muestra
        float _vel;
        Transform _pierna;          // hueso de la pierna (se gira por código más allá del clip)
        Vector3 _ejeFlexionLocal;   // eje de flexión de la rodilla en el espacio local de ese hueso

        void Awake()
        {
            _animator = GetComponent<Animator>();
            if (fuente == null) fuente = FindObjectOfType<FuenteAngulosPierna>();
            if (clip == null && _animator.runtimeAnimatorController != null)
                clip = _animator.runtimeAnimatorController.animationClips
                    .FirstOrDefault(c => c.name.ToUpperInvariant().Contains("FLEXION_RODILLA"));

            if (clip == null)
            {
                Debug.LogError("[PiernaAnimadaEnVivo] No se encontró el clip FLEXION_RODILLA. " +
                               "Asígnalo en el Inspector o ejecuta ARMyo → 1.", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            if (clip == null) return;

            _controllerOriginal = _animator.runtimeAnimatorController;
            _animator.runtimeAnimatorController = null;      // que no reproduzca IDLE por encima
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            _graph = PlayableGraph.Create("ARMyo_PiernaEnVivo");
            _graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            _playable = AnimationClipPlayable.Create(_graph, clip);
            _playable.SetApplyFootIK(false);
            var salida = AnimationPlayableOutput.Create(_graph, "Pierna", _animator);
            salida.SetSourcePlayable(_playable);

            MedirClip();
            Aplicar(0f);
        }

        void OnDisable()
        {
            if (_graph.IsValid()) _graph.Destroy();
            if (_animator != null && _controllerOriginal != null)
                _animator.runtimeAnimatorController = _controllerOriginal;
        }

        void LateUpdate()
        {
            if (!_graph.IsValid()) return;

            float objetivo = fuente != null && fuente.HayDatos ? Mathf.Max(0f, fuente.FlexionRodilla) : anguloMostrado;
            anguloMostrado = suavizado > 0f
                ? Mathf.SmoothDamp(anguloMostrado, objetivo, ref _vel, suavizado)
                : objetivo;

            Aplicar(anguloMostrado);
        }

        void Aplicar(float angulo)
        {
            tiempoNormalizado = AnguloATiempo(angulo);
            EvaluarEn(tiempoNormalizado);

            // Más allá del clip: girar el hueso de la pierna sobre el eje de flexión
            if (extenderMasAllaDelClip && mapeoMedido && _pierna != null && angulo > anguloMaxClip)
            {
                float extra = Mathf.Min(angulo, anguloMaxModelo) - anguloMaxClip;
                if (extra > 0f)
                    _pierna.localRotation = _pierna.localRotation * Quaternion.AngleAxis(extra, _ejeFlexionLocal);
            }
        }

        void EvaluarEn(float tNorm)
        {
            _playable.SetTime(tNorm * clip.length);
            _graph.Evaluate(0f);
        }

        // ─────────────────────────────────────────────
        // Ángulo → tiempo
        // ─────────────────────────────────────────────
        public float AnguloATiempo(float angulo)
        {
            if (!usarAnguloReal || !mapeoMedido)
                return Mathf.Clamp01(angulo / Mathf.Max(1f, anguloUsuarioMax)) * tFinFlexion;

            // Tabla monótona: buscar el tramo que contiene el ángulo e interpolar
            if (angulo <= _tablaAngulo[0]) return _tablaTiempo[0];
            int n = _tablaAngulo.Length;
            if (angulo >= _tablaAngulo[n - 1]) return _tablaTiempo[n - 1];
            for (int i = 1; i < n; i++)
            {
                if (angulo <= _tablaAngulo[i])
                {
                    float t = Mathf.InverseLerp(_tablaAngulo[i - 1], _tablaAngulo[i], angulo);
                    return Mathf.Lerp(_tablaTiempo[i - 1], _tablaTiempo[i], t);
                }
            }
            return _tablaTiempo[n - 1];
        }

        void MedirClip()
        {
            Transform muslo = BuscarHueso(huesoMuslo);
            Transform pierna = BuscarHueso(huesoPierna);
            if (muslo == null || pierna == null)
            {
                Debug.LogWarning($"[PiernaAnimadaEnVivo] No se encontraron los huesos '{huesoMuslo}' / '{huesoPierna}'. " +
                                 "Se usa un mapeo lineal del clip completo.", this);
                mapeoMedido = false;
                tFinFlexion = 1f;
                return;
            }

            var angulos = new float[muestras + 1];
            Quaternion relInicial = Quaternion.identity;
            for (int i = 0; i <= muestras; i++)
            {
                EvaluarEn((float)i / muestras);
                Quaternion rel = Quaternion.Inverse(muslo.rotation) * pierna.rotation;
                if (i == 0) relInicial = rel;
                angulos[i] = Quaternion.Angle(relInicial, rel); // flexión respecto al primer fotograma
            }

            int iMax = 0;
            for (int i = 1; i <= muestras; i++) if (angulos[i] > angulos[iMax]) iMax = i;
            anguloMaxClip = angulos[iMax];
            tFinFlexion = (float)iMax / muestras;

            if (anguloMaxClip < 15f || iMax == 0)
            {
                Debug.LogWarning($"[PiernaAnimadaEnVivo] El clip apenas mueve la rodilla según los huesos " +
                                 $"({anguloMaxClip:F1}°). Se usa un mapeo lineal del clip completo.", this);
                mapeoMedido = false;
                tFinFlexion = 1f;
                return;
            }

            // Tramo de flexión 0..iMax, forzado a monótono (máximo acumulado)
            _tablaAngulo = new float[iMax + 1];
            _tablaTiempo = new float[iMax + 1];
            float acum = 0f;
            for (int i = 0; i <= iMax; i++)
            {
                acum = Mathf.Max(acum, angulos[i]);
                _tablaAngulo[i] = acum;
                _tablaTiempo[i] = (float)i / muestras;
            }
            mapeoMedido = true;

            // Eje de flexión: el giro local del hueso de la pierna entre el inicio y la flexión máxima
            EvaluarEn(0f);
            Quaternion local0 = pierna.localRotation;
            EvaluarEn(tFinFlexion);
            Quaternion localMax = pierna.localRotation;
            (Quaternion.Inverse(local0) * localMax).ToAngleAxis(out float giro, out _ejeFlexionLocal);
            _pierna = giro > 1f ? pierna : null;
            if (_pierna == null)
                Debug.LogWarning("[PiernaAnimadaEnVivo] No se pudo calcular el eje de la rodilla: " +
                                 "el modelo no pasará del máximo del clip.", this);

            Debug.Log($"[PiernaAnimadaEnVivo] Clip '{clip.name}' medido: {clip.length:F2} s, flexión máxima del modelo " +
                      $"{anguloMaxClip:F1}° en t = {tFinFlexion:F2}. Ángulos por cuartos: " +
                      $"0%={angulos[0]:F0}°, 25%={angulos[muestras / 4]:F0}°, 50%={angulos[muestras / 2]:F0}°, " +
                      $"75%={angulos[muestras * 3 / 4]:F0}°, 100%={angulos[muestras]:F0}°", this);
        }

        Transform BuscarHueso(string nombre)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == nombre) return t;
            return null;
        }
    }
}
