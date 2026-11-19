using UnityEngine;

namespace ARMyo
{
    /// <summary>
    /// Script de PRUEBA: permite revisar las animaciones de la pierna con el teclado.
    ///   Teclas 0–8      → reproduce la animación correspondiente.
    ///   Arrastrar ratón → gira el modelo para verlo desde otros ángulos.
    /// No forma parte del sistema final; solo sirve para comprobar el modelo.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PruebaAnimacionesPierna : MonoBehaviour
    {
        [Tooltip("Nombres de los estados del Animator (los rellena el menú ARMyo → 1).")]
        public string[] estados;

        [Range(0f, 1f)] public float transicion = 0.25f;
        public float velocidadGiro = 0.4f;

        Animator _anim;
        int _actual;

        void Awake() => _anim = GetComponent<Animator>();

        void Update()
        {
            for (int i = 0; i < estados.Length && i < 10; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                {
                    _actual = i;
                    _anim.CrossFadeInFixedTime(estados[i], transicion);
                }
            }

            if (Input.GetMouseButton(0))
                transform.Rotate(Vector3.up, -Input.GetAxis("Mouse X") * velocidadGiro * 10f, Space.World);
        }

        void OnGUI()
        {
            GUI.Box(new Rect(10, 10, 300, 30 + estados.Length * 22), "Animaciones (teclas 0–8)");
            for (int i = 0; i < estados.Length; i++)
            {
                GUI.color = i == _actual ? Color.yellow : Color.white;
                GUI.Label(new Rect(20, 35 + i * 22, 280, 22), $"[{i}]  {estados[i]}");
            }
            GUI.color = Color.white;
            GUI.Label(new Rect(10, 40 + estados.Length * 22, 400, 22), "Arrastra con el ratón para girar el modelo");
        }
    }
}
