using UnityEngine;

namespace ARMyo.Biomecanica
{
    /// <summary>
    /// Punto único de lectura de ángulos para la Fase 3.
    /// · Con Kinect: copia los valores de MedidorAngulosPierna (evento OnAngulosCambiados).
    /// · Sin Kinect: simula el ángulo de rodilla con el teclado.
    ///
    /// Teclas (configurables; por defecto pierna derecha ↑/↓, izquierda W/S):
    ///   ↑ / ↓   flexionar / extender a mano (activa la simulación)
    ///   Espacio ciclo automático flexión ↔ extensión en las dos piernas (la izquierda desfasada)
    ///   K       volver a los datos del Kinect
    /// </summary>
    public class FuenteAngulosPierna : MonoBehaviour
    {
        [Tooltip("Dejar vacío: se busca solo en la escena.")]
        public MedidorAngulosPierna medidor;

        [Header("Simulación con teclado")]
        public bool simulando;
        [Tooltip("Grados por segundo al mantener ↑ / ↓.")]
        public float velocidadTeclas = 70f;
        [Tooltip("Duración (s) de un ciclo completo flexión + extensión con Espacio.")]
        public float periodoCiclo = 4f;
        public float anguloMaxSimulado = 120f;
        public KeyCode teclaFlexionar = KeyCode.UpArrow;
        public KeyCode teclaExtender = KeyCode.DownArrow;
        [Tooltip("Desfase del ciclo automático (0–1). 0.5 en la pierna izquierda = piernas alternadas.")]
        [Range(0f, 1f)] public float desfaseCiclo;

        [Header("Resultado (solo lectura)")]
        [SerializeField] float flexionRodilla;
        [SerializeField] float flexionCadera;
        [SerializeField] float abduccionCadera;

        public float FlexionRodilla => flexionRodilla;
        public float FlexionCadera => flexionCadera;
        public float AbduccionCadera => abduccionCadera;

        /// <summary>true si hay un ángulo utilizable (Kinect midiendo o simulación).</summary>
        public bool HayDatos => simulando || (medidor != null && medidor.isActiveAndEnabled && medidor.TrackingActivo);
        public bool CicloAutomatico => _ciclo;

        bool _ciclo;
        float _faseCiclo;

        void Awake()
        {
            if (medidor == null) medidor = FindObjectOfType<MedidorAngulosPierna>();
            if (medidor == null)
            {
                Debug.LogWarning("[FuenteAngulosPierna] No hay MedidorAngulosPierna: solo funciona la simulación con teclado.", this);
                simulando = true;
            }
        }

        void OnEnable()  { if (medidor != null) medidor.OnAngulosCambiados += AlCambiarAngulos; }
        void OnDisable() { if (medidor != null) medidor.OnAngulosCambiados -= AlCambiarAngulos; }

        void AlCambiarAngulos(float rodilla, float cadera, float abduccion)
        {
            if (simulando) return;
            flexionRodilla = rodilla;
            flexionCadera = cadera;
            abduccionCadera = abduccion;
        }

        void Update()
        {
            bool arriba = Input.GetKey(teclaFlexionar);
            bool abajo  = Input.GetKey(teclaExtender);

            if (arriba || abajo)
            {
                simulando = true;
                _ciclo = false;
                float dir = (arriba ? 1f : 0f) - (abajo ? 1f : 0f);
                flexionRodilla = Mathf.Clamp(flexionRodilla + dir * velocidadTeclas * Time.deltaTime, 0f, anguloMaxSimulado);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                simulando = true;
                _ciclo = !_ciclo;
                // Arrancar el ciclo desde el ángulo actual para que no salte (o con el desfase fijo)
                _faseCiclo = desfaseCiclo > 0f
                    ? desfaseCiclo
                    : Mathf.Acos(Mathf.Clamp(1f - 2f * flexionRodilla / anguloMaxSimulado, -1f, 1f)) / (2f * Mathf.PI);
            }

            if (Input.GetKeyDown(KeyCode.K) && medidor != null)
            {
                simulando = false;
                _ciclo = false;
                flexionRodilla = medidor.FlexionRodilla;
                flexionCadera = medidor.FlexionCadera;
                abduccionCadera = medidor.AbduccionCadera;
            }

            if (_ciclo)
            {
                _faseCiclo = (_faseCiclo + Time.deltaTime / Mathf.Max(0.5f, periodoCiclo)) % 1f;
                // 0 → max → 0 con forma de coseno (más lento en los extremos, como un movimiento real)
                flexionRodilla = anguloMaxSimulado * 0.5f * (1f - Mathf.Cos(_faseCiclo * 2f * Mathf.PI));
            }
        }
    }
}
