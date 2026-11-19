using System.Collections.Generic;
using UnityEngine;

namespace ARMyo.Mixta
{
    /// <summary>
    /// Mantiene las dos piernas enteras dentro de su columna de pantalla.
    /// La columna del modelo es estrecha, así que la distancia de la cámara se calcula
    /// teniendo en cuenta el ancho (proporción del viewport), no solo el alto.
    /// Se recalcula cuando cambia el tamaño de la ventana.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class EncuadrarModelo : MonoBehaviour
    {
        public List<GameObject> modelos = new List<GameObject>();
        [Tooltip("Margen alrededor del modelo (1 = justo).")]
        [Range(1f, 2f)] public float margen = 1.1f;

        Camera _cam;
        Bounds _bounds;
        bool _hayBounds;
        float _ultimoAspecto = -1f;

        void Start()
        {
            _cam = GetComponent<Camera>();
            // Bounds en la pose inicial (pierna recta): no se recalculan para que la cámara no se mueva al flexionar
            foreach (var m in modelos)
            {
                if (m == null) continue;
                foreach (var r in m.GetComponentsInChildren<Renderer>())
                {
                    if (!r.enabled) continue;
                    if (!_hayBounds) { _bounds = r.bounds; _hayBounds = true; }
                    else _bounds.Encapsulate(r.bounds);
                }
            }
        }

        void LateUpdate()
        {
            if (!_hayBounds || Mathf.Approximately(_cam.aspect, _ultimoAspecto)) return;
            _ultimoAspecto = _cam.aspect;

            float tanV = Mathf.Tan(_cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float distAlto  = _bounds.extents.y / tanV;
            float distAncho = _bounds.extents.x / (tanV * _cam.aspect);
            float dist = Mathf.Max(distAlto, distAncho) * margen + _bounds.extents.z;

            Vector3 dir = transform.forward.sqrMagnitude > 0f ? transform.forward : Vector3.back;
            transform.position = _bounds.center - dir * dist;
            transform.LookAt(_bounds.center);
        }
    }
}
