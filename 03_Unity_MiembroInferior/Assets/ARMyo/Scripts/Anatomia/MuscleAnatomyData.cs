using System;

namespace ARMyo.Anatomia
{
    /// <summary>Fase del movimiento en la que un músculo trabaja como agonista.</summary>
    public enum FaseMovimiento { Reposo, Flexion, Extension, Sostenido }

    /// <summary>Datos de un músculo: malla del FBX, nombre en pantalla y acción.</summary>
    [Serializable]
    public class MusculoInfo
    {
        public string malla;     // nombre exacto del objeto en PIERNA.fbx
        public string nombre;    // nombre en español para la pantalla
        public string accion;    // descripción corta
        public FaseMovimiento fase;

        public MusculoInfo(string malla, string nombre, string accion, FaseMovimiento fase)
        {
            this.malla = malla; this.nombre = nombre; this.accion = accion; this.fase = fase;
        }
    }

    /// <summary>
    /// Versión pierna del MuscleAnatomyData del brazo: nombre y acción de cada músculo.
    /// Los nombres son los nodos de PIERNA.fbx (sufijo _R). En Unity la malla pintable de cada
    /// uno se llama "X.001" (p. ej. Rectus_Femoris.001); CoincideMalla() acepta las dos formas.
    /// Semitendinosus no tiene malla propia en el modelo: sale en pantalla pero no se colorea.
    /// </summary>
    public static class MuscleAnatomyData
    {
        /// <summary>Músculos de la rodilla (FLEXION_RODILLA / EXTENSION_RODILLA).</summary>
        public static readonly MusculoInfo[] Rodilla =
        {
            // ── Flexores (isquiotibiales + gastrocnemio) ──
            new MusculoInfo("Biceps_Femoris_Long_Head_R",  "Bíceps femoral (cabeza larga)", "Flexiona la rodilla y extiende la cadera", FaseMovimiento.Flexion),
            new MusculoInfo("Biceps_Femoris_Short_Head_R", "Bíceps femoral (cabeza corta)", "Flexiona la rodilla",                      FaseMovimiento.Flexion),
            new MusculoInfo("Semitendinosus_R",            "Semitendinoso",                 "Flexiona la rodilla y extiende la cadera", FaseMovimiento.Flexion),
            new MusculoInfo("Semimembranosus_R",           "Semimembranoso",                "Flexiona la rodilla y extiende la cadera", FaseMovimiento.Flexion),
            new MusculoInfo("Gastrocnemius_Lateral_Medial_R", "Gastrocnemio",               "Ayuda a flexionar la rodilla; flexión plantar", FaseMovimiento.Flexion),

            // ── Extensores (cuádriceps) ──
            new MusculoInfo("Rectus_Femoris_R",     "Recto femoral",   "Extiende la rodilla y flexiona la cadera", FaseMovimiento.Extension),
            new MusculoInfo("Vastus_Lateralis_R",   "Vasto lateral",   "Extiende la rodilla",                      FaseMovimiento.Extension),
            new MusculoInfo("Vastus_Medialis_R",    "Vasto medial",    "Extiende la rodilla",                      FaseMovimiento.Extension),
            new MusculoInfo("Vastus_Intermedius_R", "Vasto intermedio","Extiende la rodilla",                      FaseMovimiento.Extension),
        };

        public static MusculoInfo Buscar(string malla)
        {
            foreach (var m in Rodilla)
                if (CoincideMalla(m.malla, malla)) return m;
            return null;
        }

        /// <summary>
        /// En Unity las mallas pintables se llaman "Rectus_Femoris.001" y los nodos "_R" no tienen
        /// Renderer. Se comparan sin el sufijo ".001" / ".003" ni "_R" / "_L".
        /// </summary>
        public static bool CoincideMalla(string nombreObjeto, string malla) =>
            NombreBase(nombreObjeto) == NombreBase(malla);

        public static string NombreBase(string n)
        {
            n = System.Text.RegularExpressions.Regex.Replace(n, @"\.\d+$", "");
            return System.Text.RegularExpressions.Regex.Replace(n, @"_[RL]$", "");
        }

        public static string NombreFase(FaseMovimiento f) => f switch
        {
            FaseMovimiento.Flexion   => "Flexión",
            FaseMovimiento.Extension => "Extensión",
            FaseMovimiento.Sostenido => "Mantiene la posición",
            _                        => "Reposo",
        };
    }
}
