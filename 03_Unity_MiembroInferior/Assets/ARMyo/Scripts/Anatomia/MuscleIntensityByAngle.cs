using System;
using System.Collections.Generic;
using UnityEngine;
using ARMyo.Biomecanica;

namespace ARMyo.Anatomia
{
    /// <summary>
    /// Versión pierna del MuscleIntensityByAngle del brazo.
    /// Tiñe cada músculo de blanco (inactivo) a rojo (máxima actividad) con un MaterialPropertyBlock,
    /// sin crear materiales nuevos ni tocar Pierna_Musculos.mat.
    ///
    /// Intensidad de un músculo =
    ///   · su fase (Flexión / Extensión) coincide con la del MovementClassifier → entre
    ///     intensidadMinimaActiva y 1 según el ángulo (rangoAngulo);
    ///   · el usuario mantiene la posición → los músculos de la última fase, a intensidadSostenido;
    ///   · si no → 0.
    /// </summary>
    public class MuscleIntensityByAngle : MonoBehaviour
    {
        [Serializable]
        public class Musculo
        {
            public string malla;
            public FaseMovimiento fase;
            [Tooltip("Ángulo (°) con intensidad mínima → ángulo con intensidad máxima. " +
                     "Si x > y, el músculo se pone más rojo al acercarse a y.")]
            public Vector2 rangoAngulo = new Vector2(0f, 120f);

            [NonSerialized] public Renderer[] renderers;
            [NonSerialized] public float intensidad;
            [NonSerialized] public MusculoInfo info;
        }

        [Tooltip("Dejar vacío: se busca solo en la escena.")]
        public MovementClassifier clasificador;

        [Tooltip("Raíz del modelo donde buscar las mallas. Vacío = este objeto.")]
        public Transform modelo;

        [Tooltip("Si está vacía se rellena con MuscleAnatomyData.Rodilla.")]
        public List<Musculo> musculos = new List<Musculo>();

        [Header("Color")]
        public Color colorReposo = Color.white;
        public Color colorActivo = new Color(1f, 0.08f, 0.05f);
        [Range(0f, 1f)] public float intensidadMinimaActiva = 0.4f;
        [Range(0f, 1f)] public float intensidadSostenido = 0.45f;
        [Tooltip("Velocidad del cambio de color (más alto = más rápido).")]
        public float suavizado = 6f;
        [Tooltip("Por encima de esto un músculo se considera activo y su nombre sale en pantalla.")]
        [Range(0f, 1f)] public float umbralNombre = 0.15f;

        static readonly int ColorId = Shader.PropertyToID("_Color");
        MaterialPropertyBlock _mpb;

        void Awake()
        {
            if (clasificador == null) clasificador = FindObjectOfType<MovementClassifier>();
            if (modelo == null) modelo = transform;
            if (musculos.Count == 0) RellenarDesdeAnatomia();

            _mpb = new MaterialPropertyBlock();
            var todos = modelo.GetComponentsInChildren<Renderer>(true);
            var faltan = new List<string>();
            foreach (var m in musculos)
            {
                m.info = MuscleAnatomyData.Buscar(m.malla);
                m.renderers = Array.FindAll(todos, r => MuscleAnatomyData.CoincideMalla(r.name, m.malla));
                if (m.renderers.Length == 0) faltan.Add(m.malla);
            }

            if (faltan.Count > 0)
                Debug.LogWarning("[MuscleIntensityByAngle] No se encontraron estas mallas en el modelo: " +
                                 string.Join(", ", faltan), this);
            else
                Debug.Log($"[MuscleIntensityByAngle] {musculos.Count} músculos enlazados.", this);
        }

        public void RellenarDesdeAnatomia()
        {
            musculos.Clear();
            foreach (var info in MuscleAnatomyData.Rodilla)
            {
                musculos.Add(new Musculo
                {
                    malla = info.malla,
                    fase = info.fase,
                    // Flexores: más rojo cuanto más flexionada la rodilla.
                    // Cuádriceps: más rojo al acercarse a la extensión completa (máximo esfuerzo al "bloquear").
                    rangoAngulo = info.fase == FaseMovimiento.Flexion ? new Vector2(0f, 110f) : new Vector2(110f, 0f),
                });
            }
        }

        void Update()
        {
            if (clasificador == null) return;
            FaseMovimiento fase = clasificador.Fase;
            float angulo = clasificador.Angulo;
            float k = 1f - Mathf.Exp(-suavizado * Time.deltaTime);

            foreach (var m in musculos)
            {
                float objetivo = 0f;
                if (fase == m.fase)
                {
                    float t = Mathf.InverseLerp(m.rangoAngulo.x, m.rangoAngulo.y, angulo);
                    objetivo = Mathf.Lerp(intensidadMinimaActiva, 1f, t);
                }
                else if (fase == FaseMovimiento.Sostenido && clasificador.UltimaFaseActiva == m.fase)
                {
                    objetivo = intensidadSostenido;
                }

                m.intensidad = Mathf.Lerp(m.intensidad, objetivo, k);
                Aplicar(m);
            }
        }

        void Aplicar(Musculo m)
        {
            if (m.renderers == null) return;
            Color c = Color.Lerp(colorReposo, colorActivo, m.intensidad);
            foreach (var r in m.renderers)
            {
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(ColorId, c);
                r.SetPropertyBlock(_mpb);
            }
        }

        void OnDisable()
        {
            // Dejar el modelo con su color original
            if (musculos == null) return;
            foreach (var m in musculos)
                if (m.renderers != null)
                    foreach (var r in m.renderers)
                        if (r != null) r.SetPropertyBlock(null);
        }

        /// <summary>Músculos con intensidad por encima de umbralNombre, de más a menos activo.</summary>
        public List<(MusculoInfo info, float intensidad)> MusculosActivos()
        {
            var lista = new List<(MusculoInfo, float)>();
            foreach (var m in musculos)
                if (m.intensidad >= umbralNombre && m.info != null)
                    lista.Add((m.info, m.intensidad));
            lista.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            return lista;
        }
    }
}
