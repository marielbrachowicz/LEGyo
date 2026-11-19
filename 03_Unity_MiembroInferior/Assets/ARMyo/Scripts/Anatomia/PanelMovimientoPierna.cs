using UnityEngine;
using ARMyo.Animacion;
using ARMyo.Biomecanica;

namespace ARMyo.Anatomia
{
    /// <summary>
    /// Panel en pantalla de la Fase 3 (versión simple del AnatomyUIManager del brazo):
    /// fase del movimiento, ángulo, velocidad, músculos activos y teclas de simulación.
    /// Esquina superior izquierda (el panel del Kinect está a la derecha).
    /// </summary>
    public class PanelMovimientoPierna : MonoBehaviour
    {
        public MovementClassifier clasificador;
        public MuscleIntensityByAngle intensidad;
        public FuenteAngulosPierna fuente;
        public PiernaAnimadaEnVivo pierna;
        public bool mostrar = true;

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
                _fase   = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
                _texto  = new GUIStyle(GUI.skin.label) { fontSize = 16 };
                _chico  = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            }

            float w = 420, h = 360;
            var r = new Rect(10, 10, w, h);
            GUI.Box(r, GUIContent.none);
            GUILayout.BeginArea(new Rect(r.x + 12, r.y + 8, w - 24, h - 16));

            string origen = fuente == null ? "—"
                          : fuente.simulando ? (fuente.CicloAutomatico ? "Simulación (ciclo)" : "Simulación (teclado)")
                          : fuente.HayDatos ? "Kinect" : "Kinect (sin datos)";
            GUILayout.Label($"Rodilla en vivo  ·  {origen}", _titulo);

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
                GUILayout.Label($"Modelo 3D: {pierna.AnguloMostrado:F0}°  (máx. del clip {pierna.AnguloMaxClip:F0}°)", _chico);

            GUILayout.Space(6);
            GUILayout.Label("Músculos activos:", _titulo);
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
            GUILayout.Label("↑/↓ simular ángulo   Espacio ciclo automático   K volver al Kinect", _chico);
            GUILayout.EndArea();
        }
    }
}
