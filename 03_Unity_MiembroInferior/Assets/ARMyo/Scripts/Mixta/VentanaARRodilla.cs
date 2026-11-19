using UnityEngine;
using UnityEngine.UI;
using com.rfilkov.kinect;
using ARMyo.Anatomia;
using ARMyo.Biomecanica;

namespace ARMyo.Mixta
{
    /// <summary>
    /// Ventana AR sobre la rodilla: un recuadro que sigue la rodilla del usuario en la imagen
    /// del Kinect y muestra encima la rodilla VIRTUAL (músculos coloreados), como si se viera
    /// dentro de la pierna.
    ///
    /// · Una cámara aparte (camaraRodilla) enfoca la rodilla del modelo y dibuja en un RenderTexture
    ///   con fondo transparente; ese RenderTexture se muestra en el RawImage de la ventana.
    /// · La posición sale de KinectManager.GetJointPosColorOverlay (rodilla) y el tamaño de la
    ///   distancia cadera–rodilla en la imagen, así que crece al acercarse al sensor.
    /// · Sigue la pierna que se esté moviendo (la última que flexionó o extendió).
    /// </summary>
    public class VentanaARRodilla : MonoBehaviour
    {
        [Header("Imagen del Kinect")]
        public VistaCamaraKinect vista;
        [Tooltip("RectTransform de la imagen del Kinect (la ventana es hija suya).")]
        public RectTransform imagen;
        public int sensorIndex = 0;

        [Header("Ventana")]
        public RectTransform ventana;
        public RawImage contenido;
        public Text etiqueta;
        public CanvasGroup grupo;
        [Tooltip("Tamaño de la ventana respecto a la distancia cadera–rodilla en la imagen.")]
        [Range(0.5f, 2f)] public float factorTamano = 1.1f;
        [Range(0f, 1f)] public float opacidad = 0.92f;
        [Tooltip("Suavizado del movimiento de la ventana (más alto = sigue más rápido).")]
        public float suavizado = 14f;

        [Header("Cámara de la rodilla virtual")]
        public Camera camaraRodilla;
        [Tooltip("Cámara principal: la de la rodilla mira en la misma dirección.")]
        public Camera camaraPrincipal;
        [Range(10f, 60f)] public float fov = 30f;
        [Tooltip("Cuánto muslo/pierna entra en la ventana virtual (× longitud del muslo del modelo).")]
        [Range(0.5f, 2f)] public float zoom = 1.1f;

        [Header("Piernas del modelo")]
        public Transform rodillaDer, musloDer, rodillaIzq, musloIzq;
        public MovementClassifier clasificadorDer, clasificadorIzq;

        bool _derecha = true;
        RenderTexture _rt;
        KinectManager _km;
        Vector2 _pos, _tam;
        bool _colocada;

        void Awake()
        {
            _rt = new RenderTexture(512, 512, 24, RenderTextureFormat.ARGB32) { name = "ARMyo_RodillaAR", antiAliasing = 2 };
            if (camaraRodilla != null)
            {
                camaraRodilla.targetTexture = _rt;
                camaraRodilla.clearFlags = CameraClearFlags.SolidColor;
                camaraRodilla.backgroundColor = new Color(0f, 0f, 0f, 0f); // transparente: se ve la pierna real alrededor
                camaraRodilla.fieldOfView = fov;
                camaraRodilla.nearClipPlane = 0.01f;
                camaraRodilla.enabled = false;
            }
            if (contenido != null) contenido.texture = _rt;
            if (camaraPrincipal == null) camaraPrincipal = Camera.main;
            Ocultar(true);
        }

        void OnDestroy()
        {
            if (_rt != null) _rt.Release();
        }

        void LateUpdate()
        {
            if (_km == null) _km = KinectManager.Instance;
            ulong userId = _km != null && _km.IsInitialized() ? _km.GetPrimaryUserID() : 0;
            if (userId == 0 || imagen == null || ventana == null) { Ocultar(false); return; }

            ElegirPierna();

            var jRodilla = _derecha ? KinectInterop.JointType.KneeRight : KinectInterop.JointType.KneeLeft;
            var jCadera  = _derecha ? KinectInterop.JointType.HipRight  : KinectInterop.JointType.HipLeft;
            if (!_km.IsJointTracked(userId, jRodilla) || !_km.IsJointTracked(userId, jCadera)) { Ocultar(false); return; }

            // Posición normalizada (0–1, y hacia arriba) sobre la imagen
            var unidad = new Rect(0f, 0f, 1f, 1f);
            Vector3 r = _km.GetJointPosColorOverlay(userId, jRodilla, sensorIndex, unidad);
            Vector3 c = _km.GetJointPosColorOverlay(userId, jCadera, sensorIndex, unidad);
            if (r == Vector3.zero || c == Vector3.zero) { Ocultar(false); return; }
            bool espejo = vista != null && vista.espejo;
            if (espejo) { r.x = 1f - r.x; c.x = 1f - c.x; }

            Vector2 tamImagen = imagen.rect.size;
            Vector2 pRodilla = new Vector2((r.x - 0.5f) * tamImagen.x, (r.y - 0.5f) * tamImagen.y);
            Vector2 pCadera  = new Vector2((c.x - 0.5f) * tamImagen.x, (c.y - 0.5f) * tamImagen.y);
            float lado = Mathf.Max(60f, Vector2.Distance(pRodilla, pCadera) * factorTamano);

            float k = _colocada ? 1f - Mathf.Exp(-suavizado * Time.deltaTime) : 1f;
            _pos = Vector2.Lerp(_pos, pRodilla, k);
            _tam = Vector2.Lerp(_tam, new Vector2(lado, lado), k);
            _colocada = true;

            ventana.anchorMin = ventana.anchorMax = new Vector2(0.5f, 0.5f);
            ventana.anchoredPosition = _pos;
            ventana.sizeDelta = _tam;

            if (contenido != null)
            {
                // La imagen real está en espejo: la rodilla virtual también, para que coincidan los lados
                contenido.uvRect = espejo ? new Rect(1f, 0f, -1f, 1f) : new Rect(0f, 0f, 1f, 1f);
                contenido.color = new Color(1f, 1f, 1f, opacidad);
            }
            if (etiqueta != null) etiqueta.text = _derecha ? "Rodilla derecha" : "Rodilla izquierda";

            ApuntarCamara();
            Mostrar();
        }

        void ElegirPierna()
        {
            bool activaDer = clasificadorDer != null && Activa(clasificadorDer.Fase);
            bool activaIzq = clasificadorIzq != null && Activa(clasificadorIzq.Fase);
            if (activaDer && activaIzq)
                _derecha = Mathf.Abs(clasificadorDer.Velocidad) >= Mathf.Abs(clasificadorIzq.Velocidad);
            else if (activaDer) _derecha = true;
            else if (activaIzq) _derecha = false;
            // si ninguna se mueve, se queda la última
        }

        static bool Activa(FaseMovimiento f) => f == FaseMovimiento.Flexion || f == FaseMovimiento.Extension;

        void ApuntarCamara()
        {
            if (camaraRodilla == null) return;
            Transform rodilla = _derecha ? rodillaDer : rodillaIzq;
            Transform muslo   = _derecha ? musloDer   : musloIzq;
            if (rodilla == null) return;

            float largoMuslo = muslo != null ? Vector3.Distance(muslo.position, rodilla.position) : 0.45f;
            float alto = Mathf.Max(0.05f, largoMuslo * zoom);
            float dist = (alto * 0.5f) / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);

            Vector3 dir = camaraPrincipal != null ? camaraPrincipal.transform.forward : Vector3.back;
            Quaternion rot = camaraPrincipal != null ? camaraPrincipal.transform.rotation : Quaternion.LookRotation(dir);
            camaraRodilla.fieldOfView = fov;
            camaraRodilla.transform.SetPositionAndRotation(rodilla.position - dir * dist, rot);
        }

        void Mostrar()
        {
            if (camaraRodilla != null) camaraRodilla.enabled = true;
            if (grupo != null) grupo.alpha = Mathf.MoveTowards(grupo.alpha, 1f, Time.deltaTime * 4f);
        }

        void Ocultar(bool inmediato)
        {
            if (grupo != null)
            {
                grupo.alpha = inmediato ? 0f : Mathf.MoveTowards(grupo.alpha, 0f, Time.deltaTime * 4f);
                if (grupo.alpha > 0f) return;
            }
            if (camaraRodilla != null) camaraRodilla.enabled = false;
            _colocada = false;
        }
    }
}
