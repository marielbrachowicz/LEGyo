using UnityEngine;

namespace ARMyo.Core
{
    /// <summary>
    /// Escala común para los paneles OnGUI: se diseñan para 1920×1080 y se encogen o agrandan
    /// con la ventana, para que no se monten unos sobre otros en un Game view pequeño.
    /// </summary>
    public static class EscalaUI
    {
        public const float AnchoRef = 1920f, AltoRef = 1080f;

        public static float Factor => Mathf.Max(0.25f, Mathf.Min(Screen.width / AnchoRef, Screen.height / AltoRef));

        /// <summary>Llamar al principio de OnGUI: a partir de aquí se dibuja en coordenadas escaladas.</summary>
        public static void Aplicar()
        {
            float f = Factor;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(f, f, 1f));
        }

        /// <summary>
        /// Rect (en coordenadas escaladas) de un panel de tamaño fijo.
        /// ancla: punto de la pantalla (0–1). pivote: esquina del panel (0–1). desplazamiento: píxeles de diseño.
        /// </summary>
        public static Rect Colocar(Vector2 ancla, Vector2 pivote, Vector2 desplazamiento, Vector2 tam)
        {
            float f = Factor;
            float w = Screen.width / f, h = Screen.height / f;
            return new Rect(ancla.x * w + desplazamiento.x - pivote.x * tam.x,
                            ancla.y * h + desplazamiento.y - pivote.y * tam.y,
                            tam.x, tam.y);
        }
    }
}
