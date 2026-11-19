using UnityEngine;
using ARMyo.Animacion;
using ARMyo.Biomecanica;
using ARMyo.Core;

namespace ARMyo.Anatomia
{
    /// <summary>
    /// Panel en pantalla de la Fase 3 (versión simple del AnatomyUIManager del brazo):
    /// fase del movimiento, ángulo, velocidad, músculos activos y teclas de simulación.
    /// Con dos piernas hay un panel por pierna, cada uno en el lado de la pantalla donde se ve
    /// esa pierna: la derecha arriba a la izquierda y la izquierda a la derecha, debajo del panel del Kinect.
    /// </summary>
    public class PanelMovimientoPierna : MonoBehaviour
    {
        public MovementClassifier clasificador;
        public MuscleIntensityByAngle intensidad;
        public FuenteAngulosPierna fuente;
        public PiernaAnimadaEnVivo pierna;
        public bool mostrar = true;
        [Tooltip("Texto del título, p. ej. \"Rodilla derecha\".")]
        public string titulo = "Rodilla";
        [Header("Posición")]
        [Tooltip("Punto de la pantalla (0–1) donde se coloca el panel. (0,0) = arriba a la izquierda, (1,1) = abajo a la derecha.")]
        public Vector2 anclaPantalla = Vector2.zero;
        [Tooltip("Esquina del panel (0–1) que se pone en el ancla. (1,0) = su esquina superior derecha.")]
        public Vector2 pivote = Vector2.zero;
        [Tooltip("Desplazamiento desde el ancla, en píxeles de una pantalla 1920×1080 (se escala con la ventana).")]
        public Vector2 desplazamiento = new Vector2(10f, 10f);

        /// <summary>Coloca el panel: ancla y pivote en fracciones (0–1), desplazamiento en píxeles.</summary>
        public void Colocar(Vector2 ancla, Vector2 piv, Vector2 desp)
        {
            anclaPantalla = ancla; pivote = piv; desplazamiento = desp;
        }
        [Tooltip("Mostrar la línea de ayuda de teclas.")]
        public string ayudaTeclas = "↑/↓ simular ángulo   Espacio ciclo automático   K volver al Kinect";

        [Header("Modo grande (para leer de lejos)")]
        [Tooltip("Solo la fase y los músculos activos, con letra grande. Sin ángulos ni ayuda de teclas.")]
        public bool modoGrande;
        public Vector2 tamanoGrande = new Vector2(580f, 460f);
        [Range(1, 9)] public int maxMusculosGrande = 4;

        GUIStyle _titulo, _fase, _texto, _chico;
        GUIStyle _gTitulo, _gFase, _gMusculo;

        void DibujarGrande()
        {
            if (_gTitulo == null)
            {
                _gTitulo  = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold };
                _gFase    = new GUIStyle(GUI.skin.label) { fontSize = 68, fontStyle = FontStyle.Bold, wordWrap = true };
                _gMusculo = new GUIStyle(GUI.skin.label) { fontSize = 32, wordWrap = true };
            }

            EscalaUI.Aplicar();
            var r = EscalaUI.Colocar(anclaPantalla, pivote, desplazamiento, tamanoGrande);
            GUI.Box(r, GUIContent.none);
            GUI.Box(r, GUIContent.none); // dos veces = fondo más opaco
            GUILayout.BeginArea(new Rect(r.x + 16, r.y + 10, r.width - 32, r.height - 20));

            GUI.color = new Color(1f, 1f, 1f, 0.8f);
            string sinDatos = fuente != null && !fuente.HayDatos ? "  (sin datos)" : "";
            GUILayout.Label(titulo + sinDatos, _gTitulo);

            var fase = clasificador.Fase;
            GUI.color = ColorFase(fase);
            GUILayout.Label(MuscleAnatomyData.NombreFase(fase).ToUpperInvariant(), _gFase);
            GUI.color = Color.white;

            if (intensidad != null)
            {
                var activos = intensidad.MusculosActivos();
                for (int i = 0; i < activos.Count && i < maxMusculosGrande; i++)
                {
                    var (info, val) = activos[i];
                    GUI.color = Color.Lerp(Color.white, new Color(1f, 0.4f, 0.35f), val);
                    GUILayout.Label("• " + info.nombre, _gMusculo);
                }
                GUI.color = Color.white;
            }
            GUILayout.EndArea();
        }

        static Color ColorFase(FaseMovimiento fase) => fase switch
        {
            FaseMovimiento.Flexion   => new Color(1f, 0.55f, 0.3f),
            FaseMovimiento.Extension => new Color(0.4f, 0.8f, 1f),
            FaseMovimiento.Sostenido => new Color(1f, 0.9f, 0.4f),
            _                        => new Color(0.75f, 0.75f, 0.75f),
        };

        void Awake()
        {
            if (clasificador == null) clasificador = FindObjectOfType<MovementClassifier>();
            if (intensidad == null) intensidad = FindObjectOfType<MuscleIntensityByAngle>();
            if (fuente == null) fuente = FindObjectOfType<FuenteAngulosPierna>();
            if (pierna == null) pierna = FindObjectOfType<PiernaAnimadaEnVivo>();
        }

        void OnGUI()
        {
            if (!mostrar || clasificador == null) return;
            if (modoGrande) { DibujarGrande(); return; }
            if (_titulo == null)
            {
                _titulo = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
                _fase   = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                _texto  = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                _chico  = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            }

            float w = 420, h = 270;
            EscalaUI.Aplicar();
            var r = EscalaUI.Colocar(anclaPantalla, pivote, desplazamiento, new Vector2(w, h));
            GUI.Box(r, GUIContent.none);
            GUILayout.BeginArea(new Rect(r.x + 12, r.y + 8, w - 24, h - 16));

            string origen = fuente == null ? "—"
                          : fuente.simulando ? (fuente.CicloAutomatico ? "Simulación (ciclo)" : "Simulación (teclado)")
                          : fuente.HayDatos ? "Kinect" : "Kinect (sin datos)";
            GUILayout.Label($"{titulo}  ·  {origen}", _titulo);

            var fase = clasificador.Fase;
            GUI.color = fase switch
            {
                FaseMovimiento.Flexion   => new Color(1f, 0.55f, 0.3f),
                FaseMovimiento.Extension => new Color(0.4f, 0.8f, 1f),
                FaseMovimiento.Sostenido => new Color(1f, 0.9f, 0.4f),
                _                        => new Color(0.75f, 0.75f, 0.75f),
            };
            GUILayout.Label(MuscleAnatomyData.NombreFase(fase).ToUpperInvariant(), _fase);
            GUI.color = Color.white;

            GUILayout.Label($"Ángulo rodilla: {clasificador.Angulo,6:F1}°    Velocidad: {clasificador.Velocidad,6:F0} °/s", _texto);
            if (pierna != null)
                GUILayout.Label($"Modelo 3D: {Mathf.Min(pierna.AnguloMostrado, pierna.AnguloMaxAlcanzable):F0}°  " +
                                $"(clip hasta {pierna.AnguloMaxClip:F0}°, modelo hasta {pierna.AnguloMaxAlcanzable:F0}°)", _chico);

            GUILayout.Space(4);
            GUILayout.Label("Músculos activos:", _texto);
            if (intensidad != null)
            {
                var activos = intensidad.MusculosActivos();
                if (activos.Count == 0) GUILayout.Label("  ninguno", _texto);
                foreach (var (info, val) in activos)
                {
                    GUI.color = Color.Lerp(Color.white, new Color(1f, 0.35f, 0.3f), val);
                    GUILayout.Label($"  {info.nombre}  {val * 100f:F0}%", _texto);
                }
                GUI.color = Color.white;
            }

            GUILayout.FlexibleSpace();
            if (!string.IsNullOrEmpty(ayudaTeclas)) GUILayout.Label(ayudaTeclas, _chico);
            GUILayout.EndArea();
        }
    }
}
