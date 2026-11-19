using UnityEngine;
using ARMyo.Anatomia;
using ARMyo.Animacion;
using ARMyo.Biomecanica;

namespace ARMyo.Mixta
{
    /// <summary>
    /// Decide qué pierna 3D sigue a cada pierna real.
    /// Junto a la imagen de la cámara (en espejo o no), puede hacer falta que la pierna real
    /// derecha mueva el modelo que se ve a la derecha de la pantalla y no el "anatómicamente"
    /// derecho. Tecla I = intercambiar en Play.
    ///
    /// Sin intercambiar: pierna real derecha → modeloA (Pierna_Derecha), izquierda → modeloB (Pierna_Izquierda).
    /// Intercambiado:    pierna real derecha → modeloB, izquierda → modeloA.
    /// Los paneles y la ventana AR siguen siempre a la pierna REAL (sus títulos no cambian).
    /// </summary>
    public class IntercambioPiernas : MonoBehaviour
    {
        public bool intercambiado;
        public KeyCode tecla = KeyCode.I;

        [Header("Pierna real (datos)")]
        public FuenteAngulosPierna fuenteDer, fuenteIzq;
        public MovementClassifier clasificadorDer, clasificadorIzq;
        public PanelMovimientoPierna panelDer, panelIzq;

        [Header("Modelos 3D")]
        public PiernaAnimadaEnVivo animA, animB;          // A = Pierna_Derecha, B = Pierna_Izquierda
        public MuscleIntensityByAngle musculosA, musculosB;
        public Transform rodillaA, musloA, rodillaB, musloB;

        [Header("Ventana AR")]
        public VentanaARRodilla ventanaAR;

        void Start() => Aplicar();

        void Update()
        {
            if (!Input.GetKeyDown(tecla)) return;
            intercambiado = !intercambiado;
            Aplicar();
            Debug.Log($"[IntercambioPiernas] {(intercambiado ? "Intercambiadas" : "Normales")}: " +
                      $"la pierna real derecha mueve {(intercambiado ? "Pierna_Izquierda" : "Pierna_Derecha")}.", this);
        }

        public void Aplicar()
        {
            // Modelo que sigue a la pierna real derecha / izquierda
            var animDer = intercambiado ? animB : animA;
            var animIzq = intercambiado ? animA : animB;
            var musDer  = intercambiado ? musculosB : musculosA;
            var musIzq  = intercambiado ? musculosA : musculosB;

            if (animDer != null) animDer.fuente = fuenteDer;
            if (animIzq != null) animIzq.fuente = fuenteIzq;
            if (musDer != null) musDer.clasificador = clasificadorDer;
            if (musIzq != null) musIzq.clasificador = clasificadorIzq;
            if (panelDer != null) panelDer.intensidad = musDer;
            if (panelIzq != null) panelIzq.intensidad = musIzq;

            if (ventanaAR != null)
            {
                ventanaAR.rodillaDer = intercambiado ? rodillaB : rodillaA;
                ventanaAR.musloDer   = intercambiado ? musloB   : musloA;
                ventanaAR.rodillaIzq = intercambiado ? rodillaA : rodillaB;
                ventanaAR.musloIzq   = intercambiado ? musloA   : musloB;
            }
        }
    }
}
