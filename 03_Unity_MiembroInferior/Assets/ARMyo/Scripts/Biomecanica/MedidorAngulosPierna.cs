using System;
using UnityEngine;
using ARMyo.Core;
using ARMyo.Sensores;

namespace ARMyo.Biomecanica
{
    /// <summary>
    /// Versión pierna del JointAngleMeasurer del brazo.
    /// Calcula EN VIVO, a partir del KinectBridgePierna:
    ///   · Flexión de rodilla        (0° = recta)
    ///   · Flexión/extensión cadera  (+ adelante / − atrás)
    ///   · Abducción/aducción cadera (+ afuera / − adentro)
    /// Suaviza con un promedio móvil y los muestra en pantalla (Fase 2: validación).
    /// </summary>
    public class MedidorAngulosPierna : MonoBehaviour
    {
        [Header("Fuente de datos")]
        [Tooltip("Dejar vacío: se busca solo el KinectBridgePierna de la escena.")]
        public KinectBridgePierna bridge;

        [Header("Suavizado")]
        [Range(1, 30)] public int smoothingFrames = 8;
        [Tooltip("Cambios menores que esto (grados) se ignoran para que el número no tiemble.")]
        [Range(0f, 10f)] public float angleDeadZone = 1.5f;

        [Header("Pantalla")]
        public bool mostrarEnPantalla = true;

        [Header("Resultado (solo lectura)")]
        [SerializeField] private float flexionRodilla;
        [SerializeField] private float flexionCadera;
        [SerializeField] private float abduccionCadera;
        [SerializeField] private bool trackingActivo;

        public float FlexionRodilla => flexionRodilla;
        public float FlexionCadera => flexionCadera;
        public float AbduccionCadera => abduccionCadera;
        public bool TrackingActivo => trackingActivo;

        /// <summary>(rodilla, flexión cadera, abducción cadera) cada vez que cambian.</summary>
        public event Action<float, float, float> OnAngulosCambiados;
        public event Action<bool> OnTrackingChanged;

        private CircularBuffer _buffer;
        private float _maxRodilla;

        void Awake()
        {
            _buffer = new CircularBuffer(smoothingFrames);
            if (bridge == null) bridge = KinectBridgePierna.Instance;
            if (bridge == null) bridge = FindObjectOfType<KinectBridgePierna>();
            if (bridge == null)
            {
                Debug.LogError("[MedidorAngulosPierna] No hay KinectBridgePierna en la escena. " +
                               "Usa el menú ARMyo → 2. Añadir Kinect v2 a la escena.", this);
                enabled = false;
            }
        }

        void OnEnable()  { if (bridge != null) bridge.OnFrameUpdated += AlRecibirFrame; }
        void OnDisable() { if (bridge != null) bridge.OnFrameUpdated -= AlRecibirFrame; }

        void AlRecibirFrame(LegBodyFrame f)
        {
            if (!f.IsValid)
            {
                _buffer.Clear();
                SetTracking(false);
                return;
            }

            Vector3 crudo = new Vector3(f.GetFlexionRodilla(), f.GetFlexionCadera(), f.GetAbduccionCadera());
            Vector3 suave = _buffer.Add(crudo);

            bool cambio = Mathf.Abs(suave.x - flexionRodilla) > angleDeadZone ||
                          Mathf.Abs(suave.y - flexionCadera) > angleDeadZone ||
                          Mathf.Abs(suave.z - abduccionCadera) > angleDeadZone;

            if (cambio || !trackingActivo)
            {
                flexionRodilla = suave.x;
                flexionCadera = suave.y;
                abduccionCadera = suave.z;
                _maxRodilla = Mathf.Max(_maxRodilla, flexionRodilla);
                OnAngulosCambiados?.Invoke(flexionRodilla, flexionCadera, abduccionCadera);
            }
            SetTracking(true);
        }

        void SetTracking(bool activo)
        {
            if (activo == trackingActivo) return;
            trackingActivo = activo;
            OnTrackingChanged?.Invoke(activo);
        }

        void Update()
        {
            // R = reiniciar el máximo de flexión de rodilla
            if (UnityEngine.Input.GetKeyDown(KeyCode.R)) _maxRodilla = 0f;

            // P = volcar posiciones crudas a la Console (para diagnosticar ángulos raros)
            if (UnityEngine.Input.GetKeyDown(KeyCode.P) && bridge != null)
            {
                var f = bridge.LastFrame;
                if (!f.IsValid) { Debug.Log("[MedidorAngulosPierna] Sin frame válido ahora mismo."); return; }
                Debug.Log("[MedidorAngulosPierna] Datos crudos (m)\n" +
                          $"Pecho   {f.Pecho:F2}\nPelvis  {f.Pelvis:F2}\n" +
                          $"Cadera  {f.Cadera:F2}\nRodilla {f.Rodilla:F2}\n" +
                          $"Tobillo {f.Tobillo:F2}\nPie     {f.Pie:F2}\n" +
                          $"Crudo → rodilla {f.GetFlexionRodilla():F1}°, cadera {f.GetFlexionCadera():F1}°, " +
                          $"abducción {f.GetAbduccionCadera():F1}°");
            }
        }

        // ─────────────────────────────────────────────
        // Panel de validación (esquina superior derecha)
        // ─────────────────────────────────────────────
        GUIStyle _titulo, _texto;

        void OnGUI()
        {
            if (!mostrarEnPantalla || bridge == null) return;
            if (_titulo == null)
            {
                _titulo = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
                _texto  = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            }

            float w = 420, h = 200;
            var r = new Rect(Screen.width - w - 10, 10, w, h);
            GUI.Box(r, GUIContent.none);
            GUILayout.BeginArea(new Rect(r.x + 12, r.y + 8, w - 24, h - 16));

            GUILayout.Label($"Kinect – pierna {bridge.lado.ToString().ToLower()}", _titulo);

            string estado;
            Color color;
            if (!bridge.KinectListo)      { estado = "Sensor no iniciado";                           color = Color.red; }
            else if (!bridge.HayUsuario)  { estado = "Sin usuario: colócate frente al sensor";       color = Color.yellow; }
            else if (!trackingActivo)     { estado = $"Pierna no visible ({bridge.JointFaltante})";  color = Color.yellow; }
            else                          { estado = "Midiendo";                                     color = Color.green; }

            GUI.color = color;
            GUILayout.Label("● " + estado, _texto);
            GUI.color = Color.white;

            // Sin tracking no se muestran números viejos: se veían valores absurdos
            // del último frame antes de perder al usuario.
            GUI.color = trackingActivo ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            string Num(float v) => trackingActivo ? $"{v,6:F1}°" : "   —  ";
            GUILayout.Space(4);
            GUILayout.Label($"Flexión rodilla:   {Num(flexionRodilla)}   (máx {_maxRodilla:F0}°)", _texto);
            GUILayout.Label($"Flexión cadera:    {Num(flexionCadera)}", _texto);
            GUILayout.Label($"Abducción cadera:  {Num(abduccionCadera)}", _texto);
            GUI.color = Color.white;
            GUILayout.Space(4);
            GUILayout.Label("R = reiniciar máximo    P = copiar datos crudos a la Console", GUI.skin.label);

            GUILayout.EndArea();
        }
    }
}
