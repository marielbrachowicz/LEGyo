using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using com.rfilkov.kinect;
using ARMyo.Biomecanica;
using ARMyo.Sensores;

namespace ARMyo.EditorTools
{
    /// <summary>
    /// Menú "ARMyo → 2. Añadir Kinect v2 a la escena".
    /// Abre PruebaPierna (o usa la escena abierta si no existe) y le añade:
    ///   · KinectController  → KinectManager
    ///       └ KinectV2      → Kinect2Interface en modo "Connected Sensor"
    ///   · ARMyo_Kinect      → KinectBridgePierna + MedidorAngulosPierna
    /// Se puede ejecutar varias veces: borra lo anterior y lo vuelve a crear.
    /// </summary>
    public static class ConfigurarKinect
    {
        const string ScenePath = "Assets/ARMyo/Scenes/PruebaPierna.unity";

        [MenuItem("ARMyo/2. Añadir Kinect v2 a la escena", priority = 2)]
        public static void Configurar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Quitar versiones anteriores para no duplicar
            foreach (var km in Object.FindObjectsOfType<KinectManager>(true))
                Object.DestroyImmediate(km.gameObject);
            foreach (var b in Object.FindObjectsOfType<KinectBridgePierna>(true))
                Object.DestroyImmediate(b.gameObject);

            // ── KinectController + KinectManager ──────────
            var controller = new GameObject("KinectController");
            var manager = controller.AddComponent<KinectManager>();
            manager.getBodyFrames = KinectManager.BodyTextureType.BodyAndBodyIndexData;
            manager.getColorFrames = KinectManager.ColorTextureType.None; // la imagen en color llega en la Fase 4
            manager.consoleLogMessages = true;

            // ── Sensor Kinect v2 ─────────────────────────
#if UNITY_STANDALONE_WIN
            var sensorGo = new GameObject("KinectV2");
            sensorGo.transform.SetParent(controller.transform, false);
            sensorGo.transform.localPosition = new Vector3(0f, 1f, 0f); // altura aprox. del sensor (m), igual que en las demos
            var sensor = sensorGo.AddComponent<Kinect2Interface>();
            sensor.deviceStreamingMode = KinectInterop.DeviceStreamingMode.ConnectedSensor;
#else
            Debug.LogError("[ARMyo] Kinect2Interface solo existe con la plataforma Windows. " +
                           "Cambia a File → Build Settings → Windows, Mac, Linux → Windows.");
#endif

            // ── Nuestro puente + medidor de ángulos ──────
            var armyo = new GameObject("ARMyo_Kinect");
            var bridge = armyo.AddComponent<KinectBridgePierna>();
            bridge.lado = Core.LadoPierna.Derecha;
            var medidor = armyo.AddComponent<MedidorAngulosPierna>();
            medidor.bridge = bridge;

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!string.IsNullOrEmpty(scene.path)) EditorSceneManager.SaveScene(scene);

            Selection.activeGameObject = armyo;
            Debug.Log("[ARMyo] Kinect v2 añadido a la escena " + scene.name);
            EditorUtility.DisplayDialog("ARMyo",
                "Listo. Se añadieron KinectController (Kinect v2) y ARMyo_Kinect.\n\n" +
                "Pulsa ▶ Play, ponte de frente al sensor a 2–3 m con todo el cuerpo visible, " +
                "y mira los ángulos en la esquina superior derecha.", "OK");
        }
    }
}
