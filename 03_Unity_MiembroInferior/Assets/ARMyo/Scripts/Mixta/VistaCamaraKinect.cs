using UnityEngine;
using UnityEngine.UI;
using com.rfilkov.kinect;

namespace ARMyo.Mixta
{
    /// <summary>
    /// Muestra la imagen en color del Kinect en un RawImage (la "mitad real" del espejo).
    /// Ajusta la proporción (16:9) con un AspectRatioFitter y corrige la orientación con
    /// colorImageScale del sensor (en el Kinect v2 la imagen llega boca abajo).
    ///
    /// Tecla E = invertir izquierda/derecha (por si la imagen no se ve como un espejo).
    /// Necesita que KinectManager tenga "Get Color Frames" = Color Texture (lo pone el menú ARMyo → 4).
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class VistaCamaraKinect : MonoBehaviour
    {
        public int sensorIndex = 0;
        [Tooltip("Invertir izquierda/derecha además de la corrección del sensor.")]
        public bool espejo;
        public KeyCode teclaEspejo = KeyCode.E;

        [Tooltip("Texto mientras no hay imagen.")]
        public Text avisoSinImagen;

        RawImage _img;
        AspectRatioFitter _fitter;
        KinectManager _km;

        void Awake()
        {
            _img = GetComponent<RawImage>();
            _fitter = GetComponent<AspectRatioFitter>();
            _img.color = Color.black;
        }

        void Update()
        {
            if (Input.GetKeyDown(teclaEspejo)) espejo = !espejo;

            if (_km == null) _km = KinectManager.Instance;
            Texture tex = _km != null && _km.IsInitialized() ? _km.GetColorImageTex(sensorIndex) : null;

            bool hay = tex != null && tex.width > 16;
            if (avisoSinImagen != null) avisoSinImagen.enabled = !hay;
            if (!hay) { _img.texture = null; _img.color = Color.black; return; }

            _img.texture = tex;
            _img.color = Color.white;
            if (_fitter != null) _fitter.aspectRatio = (float)tex.width / tex.height;

            Vector3 escala = _km.GetSensorData(sensorIndex)?.colorImageScale ?? Vector3.one;
            float sx = escala.x * (espejo ? -1f : 1f);
            float sy = escala.y;
            _img.uvRect = new Rect(sx < 0f ? 1f : 0f, sy < 0f ? 1f : 0f, sx < 0f ? -1f : 1f, sy < 0f ? -1f : 1f);
        }
    }
}
