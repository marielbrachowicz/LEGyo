using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ARMyo.Anatomia;
using ARMyo.Animacion;
using ARMyo.Biomecanica;
using ARMyo.Core;
using ARMyo.Sensores;

namespace ARMyo.EditorTools
{
    /// <summary>
    /// Menú "ARMyo → 3. Añadir animación en vivo (dos piernas)".
    /// Abre PruebaPierna y monta las DOS piernas, juntas como en un cuerpo completo:
    ///   · Pierna_Derecha   → el modelo PIERNA.fbx (es una pierna derecha)
    ///   · Pierna_Izquierda → copia del mismo modelo en ESPEJO respecto a la línea media (sacro)
    ///       Cada una lleva PiernaAnimadaEnVivo + MuscleIntensityByAngle.
    ///   · ARMyo_Fase3_Derecha / _Izquierda → FuenteAngulosPierna + MovementClassifier + PanelMovimientoPierna
    ///   · ARMyo_Kinect_Izquierda → segundo KinectBridgePierna + MedidorAngulosPierna (pierna izquierda),
    ///       si ya existe el de la derecha (menú ARMyo → 2).
    /// Teclas sin Kinect: derecha ↑/↓, izquierda W/S, Espacio = ciclo con las piernas alternadas, K = Kinect.
    /// Se puede ejecutar varias veces: borra lo anterior y lo vuelve a crear.
    /// </summary>
    public static class ConfigurarAnimacionEnVivo
    {
        const string ScenePath = "Assets/ARMyo/Scenes/PruebaPierna.unity";
        const string FbxPath   = "Assets/ARMyo/Modelos3D/Pierna/PIERNA.fbx";

        const string NombreDer = "Pierna_Derecha";
        const string NombreIzq = "Pierna_Izquierda";

        /// <summary>Mallas de la línea media (columna, sacro, suelo pélvico): en la copia se ocultan
        /// para que no se dibujen dos veces una encima de otra.</summary>
        static readonly string[] MallasLineaMedia =
        {
            "Sacrum", "Coccyx", "L1", "L2", "L3", "L4", "L5", "T11", "T12",
            "Anal_Sphincter_External", "Anal_Sphincter_External_Superficial", "Coccygeus", "Iliococygeus",
            "Pubococcygeus", "Puborectalis", "Deep_Transverse_Perineal", "Superficial_Transverse_Perineal",
        };

        [MenuItem("ARMyo/3. Añadir animación en vivo (dos piernas)", priority = 3)]
        public static void Configurar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorUtility.DisplayDialog("ARMyo", "No existe la escena PruebaPierna. Ejecuta primero ARMyo → 1.", "OK");
                return;
            }
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var clip = AssetDatabase.LoadAllAssetsAtPath(FbxPath).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__") && c.name.ToUpperInvariant().Contains("FLEXION_RODILLA"));
            if (clip == null)
            {
                EditorUtility.DisplayDialog("ARMyo", "No se encontró el clip FLEXION_RODILLA en PIERNA.fbx. Ejecuta ARMyo → 1.", "OK");
                return;
            }

            // ── Limpiar lo anterior ───────────────────────
            foreach (var go in Object.FindObjectsOfType<Transform>(true).Select(t => t.gameObject)
                         .Where(g => g.name == NombreIzq || g.name.StartsWith("ARMyo_Fase3")).ToList())
                Object.DestroyImmediate(go);
            foreach (var c in Object.FindObjectsOfType<FuenteAngulosPierna>(true)) Object.DestroyImmediate(c);
            foreach (var c in Object.FindObjectsOfType<MovementClassifier>(true)) Object.DestroyImmediate(c);
            foreach (var c in Object.FindObjectsOfType<PanelMovimientoPierna>(true)) Object.DestroyImmediate(c);
            foreach (var c in Object.FindObjectsOfType<PiernaAnimadaEnVivo>(true)) Object.DestroyImmediate(c);
            foreach (var c in Object.FindObjectsOfType<MuscleIntensityByAngle>(true)) Object.DestroyImmediate(c);

            // ── Pierna derecha (el modelo original) ───────
            var animator = Object.FindObjectsOfType<Animator>(true)
                .FirstOrDefault(a => a.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0);
            if (animator == null)
            {
                EditorUtility.DisplayDialog("ARMyo", "No se encontró la pierna (Animator) en la escena. Ejecuta ARMyo → 1.", "OK");
                return;
            }
            GameObject der = animator.gameObject;
            der.name = NombreDer;
            // La prueba de animaciones con teclas 0–8 pelearía con la animación en vivo
            if (der.TryGetComponent(out PruebaAnimacionesPierna prueba)) prueba.enabled = false;

            // ── Pierna izquierda (copia en espejo) ────────
            GameObject izq = CrearCopiaEspejo(der, out string avisoEspejo);

            // ── Kinect: un medidor por pierna ─────────────
            var bridges = Object.FindObjectsOfType<KinectBridgePierna>(true);
            var bridgeDer = bridges.FirstOrDefault(b => b.lado == LadoPierna.Derecha);
            var bridgeIzq = bridges.FirstOrDefault(b => b.lado == LadoPierna.Izquierda);
            if (bridgeDer != null && bridgeIzq == null)
            {
                var goK = new GameObject("ARMyo_Kinect_Izquierda");
                bridgeIzq = goK.AddComponent<KinectBridgePierna>();
                bridgeIzq.lado = LadoPierna.Izquierda;
                bridgeIzq.logUserEvents = false; // el de la derecha ya avisa de los usuarios
                var medIzq = goK.AddComponent<MedidorAngulosPierna>();
                medIzq.bridge = bridgeIzq;
                medIzq.mostrarEnPantalla = false; // el panel del Kinect lo dibuja el de la derecha
            }
            var medidorDer = bridgeDer != null ? bridgeDer.GetComponent<MedidorAngulosPierna>() : null;
            var medidorIzq = bridgeIzq != null ? bridgeIzq.GetComponent<MedidorAngulosPierna>() : null;

            // ── Montar cada pierna ────────────────────────
            var intensidadDer = Montar(der, clip, medidorDer, "Derecha", "Rodilla derecha",
                                       KeyCode.UpArrow, KeyCode.DownArrow, 0f, "↑/↓");
            var intensidadIzq = Montar(izq, clip, medidorIzq, "Izquierda", "Rodilla izquierda",
                                       KeyCode.W, KeyCode.S, 0.5f, "W/S");
            // Derecha: arriba a la izquierda. Izquierda: a la derecha, debajo del panel del Kinect.
            PanelDe(intensidadDer).Colocar(Vector2.zero, Vector2.zero, new Vector2(10f, 10f));
            PanelDe(intensidadIzq).Colocar(new Vector2(1f, 0f), new Vector2(1f, 0f),
                                           new Vector2(-10f, medidorDer != null ? 220f : 10f));

            EncuadrarCamara(der, izq);

            // ── Comprobar los nombres de las mallas ───────
            var renderers = der.GetComponentsInChildren<Renderer>(true);
            var faltan = intensidadDer.musculos
                .Where(m => !renderers.Any(r => MuscleAnatomyData.CoincideMalla(r.name, m.malla)))
                .Select(m => m.malla).ToList();
            var sb = new StringBuilder($"[ARMyo] Mallas del modelo ({renderers.Length}):\n");
            foreach (var r in renderers.OrderBy(r => r.name)) sb.Append("  ").Append(r.name).Append('\n');
            Debug.Log(sb.ToString());

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = izq;

            string aviso = avisoEspejo;
            if (medidorDer == null)
                aviso += "\n⚠ No hay Kinect en la escena (menú ARMyo → 2). Solo funcionará la simulación con teclado.\n";
            if (faltan.Count > 0)
                aviso += "\nSin malla propia (no cambian de color): " + string.Join(", ", faltan) + ".\n";

            Debug.Log($"[ARMyo] Dos piernas en vivo añadidas a {scene.name}. Clip: {clip.name}.");
            EditorUtility.DisplayDialog("ARMyo",
                "Listo. Las dos piernas siguen ahora cada rodilla y colorean sus músculos.\n" + aviso +
                "\nPulsa ▶ Play.\n  ↑ / ↓ = pierna derecha\n  W / S = pierna izquierda\n" +
                "  Espacio = ciclo automático (piernas alternadas)\n  K = volver al Kinect", "OK");
        }

        // ─────────────────────────────────────────────
        // Copia en espejo
        // ─────────────────────────────────────────────
        static GameObject CrearCopiaEspejo(GameObject der, out string aviso)
        {
            aviso = "";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            var izq = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            izq.name = NombreIzq;

            if (!izq.TryGetComponent(out Animator anim)) anim = izq.AddComponent<Animator>();
            anim.runtimeAnimatorController = der.GetComponent<Animator>().runtimeAnimatorController;
            anim.applyRootMotion = false;

            // Línea media = centro del sacro de la pierna derecha
            var sacro = der.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name == "Sacrum");
            float medioX;
            if (sacro != null) medioX = sacro.bounds.center.x;
            else
            {
                medioX = der.transform.position.x;
                aviso += "\n⚠ No se encontró el sacro: la pierna izquierda puede quedar mal colocada.\n";
            }

            // Reflejo respecto al plano x = medioX:
            //   posición  x → 2·medioX − x
            //   rotación  (x, y, z, w) → (x, −y, −z, w)
            //   escala    x → −x
            Transform t = der.transform, ti = izq.transform;
            Vector3 p = t.position;
            Quaternion q = t.rotation;
            ti.SetParent(t.parent, false);
            ti.position = new Vector3(2f * medioX - p.x, p.y, p.z);
            ti.rotation = new Quaternion(q.x, -q.y, -q.z, q.w);
            Vector3 s = t.localScale;
            ti.localScale = new Vector3(-s.x, s.y, s.z);

            // No dibujar dos veces la columna y el suelo pélvico
            foreach (var r in izq.GetComponentsInChildren<Renderer>(true))
                if (MallasLineaMedia.Contains(r.name)) r.enabled = false;

            return izq;
        }

        // ─────────────────────────────────────────────
        // Componentes de una pierna
        // ─────────────────────────────────────────────
        static MuscleIntensityByAngle Montar(GameObject pierna, AnimationClip clip, MedidorAngulosPierna medidor,
                                             string sufijo, string titulo, KeyCode flex, KeyCode ext, float desfase,
                                             string teclas)
        {
            var enVivo = pierna.AddComponent<PiernaAnimadaEnVivo>();
            enVivo.clip = clip;

            var intensidad = pierna.AddComponent<MuscleIntensityByAngle>();
            intensidad.modelo = pierna.transform;
            intensidad.RellenarDesdeAnatomia();

            var go = new GameObject("ARMyo_Fase3_" + sufijo);
            var fuente = go.AddComponent<FuenteAngulosPierna>();
            fuente.medidor = medidor;
            fuente.teclaFlexionar = flex;
            fuente.teclaExtender = ext;
            fuente.desfaseCiclo = desfase;

            var clasificador = go.AddComponent<MovementClassifier>();
            clasificador.fuente = fuente;
            clasificador.UsarArticulacion(ArticulacionPierna.Rodilla);

            var panel = go.AddComponent<PanelMovimientoPierna>();
            panel.clasificador = clasificador;
            panel.intensidad = intensidad;
            panel.fuente = fuente;
            panel.pierna = enVivo;
            panel.titulo = titulo;
            panel.ayudaTeclas = $"{teclas} simular   Espacio ciclo   K volver al Kinect";

            enVivo.fuente = fuente;
            intensidad.clasificador = clasificador;
            return intensidad;
        }

        static PanelMovimientoPierna PanelDe(MuscleIntensityByAngle intensidad) =>
            Object.FindObjectsOfType<PanelMovimientoPierna>(true).First(p => p.intensidad == intensidad);

        static void EncuadrarCamara(params GameObject[] modelos)
        {
            var cam = UnityEngine.Camera.main;
            var renderers = modelos.SelectMany(m => m.GetComponentsInChildren<Renderer>()).Where(r => r.enabled).ToArray();
            if (cam == null || renderers.Length == 0) return;

            Bounds b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            float dist = Mathf.Max(b.extents.y, b.extents.x) / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.2f;
            cam.transform.position = b.center + Vector3.forward * dist;
            cam.transform.LookAt(b.center);
        }
    }
}
