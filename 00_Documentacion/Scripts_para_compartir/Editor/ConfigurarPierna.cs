using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ARMyo.EditorTools
{
    /// <summary>
    /// Menú "ARMyo → 1. Configurar modelo de pierna".
    /// Deja listo PIERNA.fbx en un solo paso:
    ///   1. Crea los materiales de músculos y huesos con las texturas Ecorche.
    ///   2. Configura el FBX (animación Generic, blend shapes, nombres de clips limpios).
    ///   3. Crea un Animator Controller con las 9 animaciones.
    ///   4. Crea la escena de prueba Assets/ARMyo/Scenes/PruebaPierna.unity.
    /// Se puede ejecutar varias veces: rehace todo sin duplicar.
    /// </summary>
    public static class ConfigurarPierna
    {
        const string Raiz       = "Assets/ARMyo";
        const string FbxPath    = Raiz + "/Modelos3D/Pierna/PIERNA.fbx";
        const string TexDir     = Raiz + "/Modelos3D/Pierna/Texturas";
        const string MatDir     = Raiz + "/Materiales";
        const string AnimDir    = Raiz + "/Animaciones";
        const string CtrlPath   = AnimDir + "/Pierna.controller";
        const string ScenePath  = Raiz + "/Scenes/PruebaPierna.unity";

        [MenuItem("ARMyo/1. Configurar modelo de pierna", priority = 1)]
        public static void Configurar()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath) == null)
            {
                EditorUtility.DisplayDialog("ARMyo", $"No se encontró {FbxPath}", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar("ARMyo", "Creando materiales...", 0.1f);
                Material musculos = CrearMaterial("Pierna_Musculos", TexDir + "/Ecorche_Muscles.png");
                Material huesos   = CrearMaterial("Pierna_Huesos",   TexDir + "/Ecorche_Bones.png");

                EditorUtility.DisplayProgressBar("ARMyo", "Configurando PIERNA.fbx (puede tardar)...", 0.3f);
                ConfigurarFbx(musculos, huesos);

                EditorUtility.DisplayProgressBar("ARMyo", "Creando Animator Controller...", 0.7f);
                AnimatorController ctrl = CrearController(out string[] estados);

                EditorUtility.DisplayProgressBar("ARMyo", "Creando escena de prueba...", 0.9f);
                CrearEscena(ctrl, estados);

                Debug.Log($"[ARMyo] Modelo de pierna configurado. {estados.Length} animaciones: {string.Join(", ", estados)}");
                EditorUtility.DisplayDialog("ARMyo",
                    $"Listo.\n\nSe creó la escena PruebaPierna con {estados.Length} animaciones.\n\n" +
                    "Pulsa ▶ Play y usa las teclas 0–8 para cambiar de animación.", "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        // ─────────────────────────────────────────────
        // 1. MATERIALES
        // ─────────────────────────────────────────────
        static Material CrearMaterial(string nombre, string texPath)
        {
            CrearCarpeta(MatDir);
            string path = $"{MatDir}/{nombre}.mat";

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(mat, path);
            }

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null) Debug.LogWarning($"[ARMyo] No se encontró la textura {texPath}");

            mat.mainTexture = tex;
            mat.color = Color.white;
            mat.SetFloat("_Glossiness", 0.4f);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ─────────────────────────────────────────────
        // 2. IMPORTACIÓN DEL FBX
        // ─────────────────────────────────────────────
        static void ConfigurarFbx(Material musculos, Material huesos)
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(FbxPath);

            // Reasignar los materiales internos del FBX a los nuestros, según su nombre.
            foreach (var m in AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<Material>())
            {
                string n = m.name.ToLowerInvariant();
                Material destino = (n.Contains("skel") || n.Contains("bone") || n.Contains("hueso")) ? huesos
                                 : (n.Contains("muscle") || n.Contains("musculo")) ? musculos
                                 : null;
                if (destino != null)
                    mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), destino);
            }

            mi.animationType    = ModelImporterAnimationType.Generic; // igual que el proyecto del brazo
            mi.importAnimation  = true;
            mi.importBlendShapes = true;

            // Clips con nombres limpios: "Armature_Ecorche|5.FLEXION_RODILLA" → "5.FLEXION_RODILLA"
            var clips = mi.defaultClipAnimations;
            foreach (var c in clips)
            {
                string limpio = c.takeName.Contains("|") ? c.takeName.Substring(c.takeName.LastIndexOf('|') + 1) : c.takeName;
                c.name = limpio;
                c.loopTime = limpio.Contains("IDLE");
            }
            mi.clipAnimations = clips;

            mi.SaveAndReimport();
        }

        // ─────────────────────────────────────────────
        // 3. ANIMATOR CONTROLLER
        // ─────────────────────────────────────────────
        static AnimatorController CrearController(out string[] estados)
        {
            CrearCarpeta(AnimDir);
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CtrlPath) != null)
                AssetDatabase.DeleteAsset(CtrlPath);

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(CtrlPath);
            var sm = ctrl.layers[0].stateMachine;

            var clips = AssetDatabase.LoadAllAssetsAtPath(FbxPath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .OrderBy(c => c.name)
                .ToArray();

            // Unity cambia los "." por "_" en los nombres de estado,
            // así que guardamos el nombre REAL de cada estado creado.
            var nombres = new System.Collections.Generic.List<string>();
            foreach (var clip in clips)
            {
                var st = sm.AddState(clip.name);
                st.motion = clip;
                if (clip.name.Contains("IDLE")) sm.defaultState = st;
                nombres.Add(st.name);
            }

            estados = nombres.ToArray();
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        // ─────────────────────────────────────────────
        // 4. ESCENA DE PRUEBA
        // ─────────────────────────────────────────────
        static void CrearEscena(AnimatorController ctrl, string[] estados)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            var pierna = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            pierna.name = "Pierna";

            // Ojo: en Unity no se puede usar "??" con componentes (un componente
            // que falta no es null de C#). Por eso se comprueba con TryGetComponent.
            if (!pierna.TryGetComponent(out Animator animator))
                animator = pierna.AddComponent<Animator>();
            animator.runtimeAnimatorController = ctrl;
            animator.applyRootMotion = false;

            var prueba = pierna.AddComponent<ARMyo.PruebaAnimacionesPierna>();
            prueba.estados = estados;

            // Encuadrar la cámara sobre el modelo
            var renderers = pierna.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0 && UnityEngine.Camera.main != null)
            {
                Bounds b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);

                var cam = UnityEngine.Camera.main;
                float dist = Mathf.Max(b.extents.y, b.extents.x) / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.2f;
                cam.transform.position = b.center + Vector3.forward * dist;
                cam.transform.LookAt(b.center);
                cam.nearClipPlane = 0.01f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
            }

            CrearCarpeta(Raiz + "/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = pierna;
        }

        static void CrearCarpeta(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string padre = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            CrearCarpeta(padre);
            AssetDatabase.CreateFolder(padre, System.IO.Path.GetFileName(path));
        }
    }
}
