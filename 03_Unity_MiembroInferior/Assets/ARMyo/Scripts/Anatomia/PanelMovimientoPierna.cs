using UnityEngine;
using ARMyo.Animacion;
using ARMyo.Biomecanica;

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
        [Tooltip("Dibujar el panel en el lado derecho de la pantalla.")]
        public bool ladoDerechoPantalla;
        [Tooltip("Distancia (px) desde el borde superior.")]
        public float margenSuperior = 10f;
        [Tooltip("Mostrar la línea de ayuda de teclas.")]
        public string ayudaTeclas = "↑/↓ simular ángulo   Espacio ciclo automático   K volver al Kinect";

        GUIStyle _titulo, _fase, _texto, _chico;

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
            if (_titulo == null)
            {
                _titulo = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
                _fase   = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                _texto  = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                _chico  = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            }

            float w = 420, h = 270;
            var r = new Rect(ladoDerechoPantalla ? Screen.width - w - 10 : 10, margenSuperior, w, h);
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
