using UnityEngine;

namespace ARMyo.Core
{
    /// <summary>Qué pierna del usuario se mide.</summary>
    public enum LadoPierna { Derecha, Izquierda }

    /// <summary>
    /// Foto inmutable de los joints de la pierna en un frame del Kinect.
    /// Equivale a ArmBodyFrame del proyecto del brazo, pero con
    /// cadera, rodilla, tobillo y pie.
    ///
    /// Coordenadas del Kinect (metros): X a los lados, Y hacia arriba,
    /// Z hacia delante, alejándose del sensor. El usuario está DE FRENTE al sensor.
    /// </summary>
    public readonly struct LegBodyFrame
    {
        // ── Pierna medida ─────────────────────────────
        public readonly Vector3 Cadera;
        public readonly Vector3 Rodilla;
        public readonly Vector3 Tobillo;
        public readonly Vector3 Pie;

        // ── Referencia de tronco ──────────────────────
        public readonly Vector3 Pelvis;
        public readonly Vector3 Pecho;      // SpineChest (SpineShoulder en Kinect v2)

        // ── Mano contraria (para el gesto de la Fase 5) ──
        public readonly Vector3 ManoContraria;

        // ── Metadatos ─────────────────────────────────
        public readonly LadoPierna Lado;
        public readonly ulong UserId;
        public readonly float Timestamp;
        public readonly bool IsValid;

        public LegBodyFrame(ulong userId, LadoPierna lado,
            Vector3 cadera, Vector3 rodilla, Vector3 tobillo, Vector3 pie,
            Vector3 pelvis, Vector3 pecho, Vector3 manoContraria,
            float timestamp, bool isValid = true)
        {
            UserId = userId; Lado = lado;
            Cadera = cadera; Rodilla = rodilla; Tobillo = tobillo; Pie = pie;
            Pelvis = pelvis; Pecho = pecho; ManoContraria = manoContraria;
            Timestamp = timestamp; IsValid = isValid;
        }

        public static readonly LegBodyFrame Invalid = new LegBodyFrame(
            0, LadoPierna.Derecha, Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero,
            Vector3.zero, Vector3.zero, Vector3.zero, 0f, false);

        // ─────────────────────────────────────────────
        // Ángulos (convención de goniometría: 0° = posición anatómica)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Flexión de rodilla en 3D: 0° = pierna recta, aumenta al doblarla.
        /// Se usa el ángulo 3D (no proyectado) para que funcione aunque el usuario
        /// no esté perfectamente de frente.
        /// </summary>
        public float GetFlexionRodilla()
        {
            Vector3 muslo = Cadera - Rodilla;
            Vector3 pierna = Tobillo - Rodilla;
            if (muslo.sqrMagnitude < 1e-6f || pierna.sqrMagnitude < 1e-6f) return 0f;
            return 180f - Vector3.Angle(muslo, pierna);
        }

        /// <summary>
        /// Flexión (+) / extensión (−) de cadera en el plano sagital (YZ).
        /// Ángulo entre "tronco hacia abajo" y el muslo.
        /// + = muslo hacia delante (hacia el sensor). − = muslo hacia atrás.
        /// </summary>
        public float GetFlexionCadera()
        {
            Vector3 troncoAbajo = SoloYZ(Pelvis - Pecho);
            Vector3 muslo = SoloYZ(Rodilla - Cadera);
            if (troncoAbajo.sqrMagnitude < 1e-6f || muslo.sqrMagnitude < 1e-6f) return 0f;
            // Con eje +X: el muslo yendo hacia −Z (hacia el sensor) da ángulo positivo.
            return Vector3.SignedAngle(troncoAbajo, muslo, Vector3.right);
        }

        /// <summary>
        /// Abducción (+) / aducción (−) de cadera en el plano frontal (XY).
        /// + = el muslo se aleja de la línea media (hacia afuera).
        /// El signo se saca de la dirección pelvis→cadera, así no depende
        /// de si la imagen está en espejo ni de qué pierna se mide.
        /// </summary>
        public float GetAbduccionCadera()
        {
            Vector3 troncoAbajo = SoloXY(Pelvis - Pecho);
            Vector3 muslo = SoloXY(Rodilla - Cadera);
            if (troncoAbajo.sqrMagnitude < 1e-6f || muslo.sqrMagnitude < 1e-6f) return 0f;

            float angulo = Vector3.Angle(troncoAbajo, muslo);

            float haciaAfuera = Cadera.x - Pelvis.x;                // lado de esta pierna
            float lateralMuslo = Vector3.Cross(troncoAbajo, muslo).z; // hacia qué lado gira el muslo
            float lateralAfuera = Vector3.Cross(troncoAbajo, new Vector3(haciaAfuera, 0f, 0f)).z;
            return (lateralMuslo * lateralAfuera >= 0f) ? angulo : -angulo;
        }

        static Vector3 SoloYZ(Vector3 v) => new Vector3(0f, v.y, v.z);
        static Vector3 SoloXY(Vector3 v) => new Vector3(v.x, v.y, 0f);
    }
}
