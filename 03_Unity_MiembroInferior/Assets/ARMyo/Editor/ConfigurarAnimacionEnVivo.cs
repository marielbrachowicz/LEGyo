using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ARMyo.Anatomia;
using ARMyo.Animacion;
using ARMyo.Biomecanica;

namespace ARMyo.EditorTools
{
    /// <summary>
    /// Menú "ARMyo → 3. Añadir animación en vivo".
    /// Abre PruebaPierna y monta la Fase 3:
    ///   · Pierna        → PiernaAnimadaEnVivo (FLEXION_RODILLA según el ángulo real)
    ///                   → MuscleIntensityByAngle (color de los músculos)
    ///                   → PruebaAnimacionesPierna queda DESACTIVADO (chocaría con la animación en vivo)
    ///   · ARMyo_Fase3   → FuenteAngulosPierna + MovementClassifier + PanelMovimientoPierna
    /// Además lista en la Console las mallas del modelo (para comprobar los nombres de los músculos).
    /// Se puede ejecutar varias veces: borra lo anterior y lo vuelve a crear.
    /// </summary>
    public static class ConfigurarAnimacionEnVivo
    {
        const string ScenePath = "Assets/ARMyo/Scenes/PruebaPierna.unity";
        const string FbxPath   = "Assets/ARMyo/Modelos3D/Pierna/PIERNA.fbx";

        [MenuItem("ARMyo/3. Añadir animación en vivo", priority = 3)]
        public static void Configurar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorUtility.DisplayDialog("ARMyo", "No existe la escena PruebaPierna. Ejecuta primero ARMyo → 1.", "OK");
                return;
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // ── Modelo ────────────────────────────────────
            var animator = Object.FindObjectsOfType<Animator>(true)
                .FirstOrDefault(a => a.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0);
            if (animator == null)
            {
                EditorUtility.DisplayDialog("ARMyo", "No se encontró la pierna (Animator) en la escena. Ejecuta ARMyo → 1.", "OK");
                return;
            }
            GameObject pierna = animator.gameObject;

            var clip = AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__") && c.name.ToUpperInvariant().Contains("FLEXION_RODILLA"));
            if (clip == null)
            {
                EditorUtility.DisplayDialog("ARMyo", "No se encontró el clip FLEXION_RODILLA en PIERNA.fbx. Ejecuta ARMyo → 1.", "OK");
                return;
            }

            // Quitar versiones anteriores
            foreach (var c in pierna.GetComponents<PiernaAnimadaEnVivo>()) Object.DestroyImmediate(c);
            foreach (var c in pierna.GetComponents<MuscleIntensityByAngle>()) Object.DestroyImmediate(c);
            foreach (var f in Object.FindObjectsOfType<FuenteAngulosPierna>(true))
                if (f.gameObject.name == "ARMyo_Fase3") Object.DestroyImmediate(f.gameObject);
                else Object.DestroyImmediate(f);
            foreach (var c in Object.FindObjectsOfType<MovementClassifier>(true)) Object.DestroyImmediate(c);
            foreach (var c in Object.FindObjectsOfType<PanelMovimientoPierna>(true)) Object.DestroyImmediate(c);

            // La prueba de animaciones con teclas 0–8 pelearía con la animación en vivo
            if (pierna.TryGetComponent(out PruebaAnimacionesPierna prueba)) prueba.enabled = false;

            // ── Componentes del modelo ────────────────────
            var enVivo = pierna.AddComponent<PiernaAnimadaEnVivo>();
            enVivo.clip = clip;

            var intensidad = pierna.AddComponent<MuscleIntensityByAngle>();
            intensidad.modelo = pierna.transform;
            intensidad.RellenarDesdeAnatomia();

            // ── Lógica + UI ───────────────────────────────
            var go = new GameObject("ARMyo_Fase3");
            var fuente = go.AddComponent<FuenteAngulosPierna>();
            fuente.medidor = Object.FindObjectOfType<MedidorAngulosPierna>(true);
            var clasificador = go.AddComponent<MovementClassifier>();
            clasificador.fuente = fuente;
            clasificador.UsarArticulacion(ArticulacionPierna.Rodilla);
            var panel = go.AddComponent<PanelMovimientoPierna>();
            panel.clasificador = clasificador;
            panel.intensidad = intensidad;
            panel.fuente = fuente;
            panel.pierna = enVivo;

            enVivo.fuente = fuente;
            intensidad.clasificador = clasificador;

            // ── Comprobar los nombres de las mallas ───────
            var renderers = pierna.GetComponentsInChildren<Renderer>(true);
            var faltan = intensidad.musculos.Where(m => !renderers.Any(r => MuscleAnatomyData.CoincideMalla(r.name, m.malla)))
                                            .Select(m => m.malla).ToList();
            var sb = new StringBuilder($"[ARMyo] Mallas del modelo ({renderers.Length}):\n");
            foreach (var r in renderers.OrderBy(r => r.name)) sb.Append("  ").Append(r.name).Append('\n');
            Debug.Log(sb.ToString());

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = go;

            string aviso = "";
            if (fuente.medidor == null)
                aviso += "\n⚠ No hay Kinect en la escena (menú ARMyo → 2). Solo funcionará la simulación con teclado.\n";
            if (faltan.Count > 0)
                aviso += "\n⚠ No se encontraron estas mallas: " + string.Join(", ", faltan) + ". Mira la lista en la Console.\n";

            Debug.Log($"[ARMyo] Animación en vivo añadida a {scene.name}. Clip: {clip.name}. " +
                      $"Músculos: {intensidad.musculos.Count - faltan.Count}/{intensidad.musculos.Count} encontrados.");
            EditorUtility.DisplayDialog("ARMyo",
                "Listo. La pierna sigue ahora el ángulo de rodilla y colorea los músculos.\n" + aviso +
                "\nPulsa ▶ Play.\n  ↑ / ↓ = simular flexión / extensión\n  Espacio = ciclo automático\n  K = volver al Kinect", "OK");
        }
    }
}
