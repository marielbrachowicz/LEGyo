using System;
using UnityEngine;
using ARMyo.Anatomia;

namespace ARMyo.Biomecanica
{
    /// <summary>Qué ángulo se clasifica.</summary>
    public enum ArticulacionPierna { Rodilla, FlexionCadera, AbduccionCadera }

    /// <summary>
    /// Versión pierna del MovementClassifier del brazo.
    /// Decide si el usuario FLEXIONA, EXTIENDE, MANTIENE o está en REPOSO
    /// a partir de la velocidad del ángulo (grados/s).
    ///
    /// El ángulo se lee en cada frame y la velocidad se calcula sobre una ventana de tiempo,
    /// porque MedidorAngulosPierna solo publica cambios mayores que su zona muerta (saltos de ~1,5°).
    /// Hay histéresis para que la fase no parpadee.
    /// </summary>
    public class MovementClassifier : MonoBehaviour
    {
        [Serializable]
        public struct Preset
        {
            [Tooltip("Velocidad (°/s) para empezar a considerar que hay movimiento.")]
            public float umbralEntrar;
            [Tooltip("Velocidad (°/s) por debajo de la cual el movimiento se da por terminado.")]
            public float umbralSalir;
            [Tooltip("Por debajo de este ángulo, quieto = Reposo. Por encima, quieto = Mantiene.")]
            public float anguloReposo;
        }

        public static readonly Preset PresetRodilla        = new Preset { umbralEntrar = 25f, umbralSalir = 10f, anguloReposo = 12f };
        public static readonly Preset PresetFlexionCadera  = new Preset { umbralEntrar = 20f, umbralSalir = 8f,  anguloReposo = 10f };
        public static readonly Preset PresetAbduccionCadera = new Preset { umbralEntrar = 15f, umbralSalir = 6f,  anguloReposo = 8f };

        [Tooltip("Dejar vacío: se busca sola en la escena.")]
        public FuenteAngulosPierna fuente;
        public ArticulacionPierna articulacion = ArticulacionPierna.Rodilla;
        public Preset preset = PresetRodilla;

        [Tooltip("Ventana (s) para medir la velocidad.")]
        [Range(0.05f, 1f)] public float ventanaVelocidad = 0.3f;
        [Tooltip("Tiempo (s) quieto antes de pasar a Mantiene / Reposo.")]
        [Range(0f, 1f)] public float tiempoQuieto = 0.35f;

        [Header("Resultado (solo lectura)")]
        [SerializeField] FaseMovimiento fase = FaseMovimiento.Reposo;
        [SerializeField] float velocidad;
        [SerializeField] float angulo;

        public FaseMovimiento Fase => fase;
        /// <summary>Velocidad suavizada en °/s (+ flexiona, − extiende).</summary>
        public float Velocidad => velocidad;
        public float Angulo => angulo;
        /// <summary>Última fase con movimiento (Flexion o Extension). Útil en Sostenido.</summary>
        public FaseMovimiento UltimaFaseActiva { get; private set; } = FaseMovimiento.Flexion;

        public event Action<FaseMovimiento> OnFaseCambiada;

        // Historial (tiempo, ángulo) para la ventana de velocidad
        const int N = 64;
        readonly float[] _t = new float[N];
        readonly float[] _a = new float[N];
        int _head, _count;
        float _tiempoSinMovimiento;

        void Awake()
        {
            if (fuente == null) fuente = FindObjectOfType<FuenteAngulosPierna>();
            if (fuente == null)
            {
                Debug.LogError("[MovementClassifier] No hay FuenteAngulosPierna en la escena. " +
                               "Usa el menú ARMyo → 3. Añadir animación en vivo.", this);
                enabled = false;
            }
        }

        /// <summary>Cambia de articulación y aplica su preset.</summary>
        public void UsarArticulacion(ArticulacionPierna a)
        {
            articulacion = a;
            preset = a switch
            {
                ArticulacionPierna.FlexionCadera   => PresetFlexionCadera,
                ArticulacionPierna.AbduccionCadera => PresetAbduccionCadera,
                _                                  => PresetRodilla,
            };
            _count = 0;
        }

        float LeerAngulo() => articulacion switch
        {
            ArticulacionPierna.FlexionCadera   => fuente.FlexionCadera,
            ArticulacionPierna.AbduccionCadera => fuente.AbduccionCadera,
            _                                  => fuente.FlexionRodilla,
        };

        void Update()
        {
            if (!fuente.HayDatos)
            {
                _count = 0;
                velocidad = 0f;
                CambiarFase(FaseMovimiento.Reposo);
                return;
            }

            float ahora = Time.time;
            angulo = LeerAngulo();
            _t[_head] = ahora; _a[_head] = angulo;
            _head = (_head + 1) % N;
            if (_count < N) _count++;

            // Muestra más antigua dentro de la ventana
            int idxViejo = -1;
            for (int i = _count - 1; i >= 1; i--)
            {
                int idx = (_head - 1 - i + N * 2) % N;
                if (ahora - _t[idx] <= ventanaVelocidad) { idxViejo = idx; break; }
            }
            float vInst = 0f;
            if (idxViejo >= 0)
            {
                float dt = ahora - _t[idxViejo];
                if (dt > 1e-3f) vInst = (angulo - _a[idxViejo]) / dt;
            }
            velocidad = Mathf.Lerp(velocidad, vInst, 1f - Mathf.Exp(-12f * Time.deltaTime));

            // ── Clasificación con histéresis ──
            float v = velocidad;
            bool moviendo = fase == FaseMovimiento.Flexion || fase == FaseMovimiento.Extension;
            float umbral = moviendo ? preset.umbralSalir : preset.umbralEntrar;

            if (v > umbral && !(fase == FaseMovimiento.Extension && v < preset.umbralEntrar))
            {
                _tiempoSinMovimiento = 0f;
                CambiarFase(FaseMovimiento.Flexion);
            }
            else if (v < -umbral && !(fase == FaseMovimiento.Flexion && v > -preset.umbralEntrar))
            {
                _tiempoSinMovimiento = 0f;
                CambiarFase(FaseMovimiento.Extension);
            }
            else if (Mathf.Abs(v) < preset.umbralSalir)
            {
                _tiempoSinMovimiento += Time.deltaTime;
                if (_tiempoSinMovimiento >= tiempoQuieto)
                    CambiarFase(Mathf.Abs(angulo) < preset.anguloReposo ? FaseMovimiento.Reposo : FaseMovimiento.Sostenido);
            }
        }

        void CambiarFase(FaseMovimiento nueva)
        {
            if (nueva == fase) return;
            fase = nueva;
            if (nueva == FaseMovimiento.Flexion || nueva == FaseMovimiento.Extension)
                UltimaFaseActiva = nueva;
            OnFaseCambiada?.Invoke(nueva);
        }
    }
}
