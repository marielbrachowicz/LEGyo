using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using com.rfilkov.kinect;
using ARMyo.Anatomia;
using ARMyo.Biomecanica;
using ARMyo.Mixta;

namespace ARMyo.EditorTools
{
    /// <summary>
    /// Menú "ARMyo → 4. Crear escena mixta (cámara | modelo)".
    /// Copia PruebaPierna (ya con las dos piernas del menú 3) en EscenaMixta y la divide en tres columnas:
    ///   · 0–40 %   → imagen en color del Kinect, en espejo (la "mitad real")
    ///   · 40–68 %  → las dos piernas virtuales con sus músculos
    ///   · 68–100 % → los paneles, en letra grande (Kinect, rodilla derecha, rodilla izquierda), uno debajo de otro
    /// Activa la imagen en color en el KinectManager.
    /// PruebaPierna no se toca. Se puede ejecutar varias veces: rehace EscenaMixta desde PruebaPierna.
    /// </summary>
    public static class ConfigurarEscenaMixta
    {
        const string Origen  = "Assets/ARMyo/Scenes/PruebaPierna.unity";
        const string Destino = "Assets/ARMyo/Scenes/EscenaMixta.unity";

        [MenuItem("ARMyo/4. Crear escena mixta (cámara | modelo)", priority = 4)]
        public static void Configurar()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Origen) == null)
            {
                EditorUtility.DisplayDialog("ARMyo", "No existe PruebaPierna. Ejecuta los menús ARMyo → 1, 2 y 3.", "OK");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(Destino) != null) AssetDatabase.DeleteAsset(Destino);
            AssetDatabase.CopyAsset(Origen, Destino);
            var scene = EditorSceneManager.OpenScene(Destino, OpenSceneMode.Single);

            string aviso = "";
            var paneles = Object.FindObjectsOfType<PanelMovimientoPierna>(true);
            if (paneles.Length < 2)
                aviso += "\n⚠ PruebaPierna no tiene las dos piernas. Ejecuta antes ARMyo → 3 y repite este menú.\n";

            // ── Kinect: pedir la imagen en color ──────────
            var km = Object.FindObjectOfType<KinectManager>(true);
            if (km != null) km.getColorFrames = KinectManager.ColorTextureType.ColorTexture;
            else aviso += "\n⚠ No hay KinectController en la escena (menú ARMyo → 2): la mitad de la cámara se verá en negro.\n";

            // ── Columnas ──────────────────────────────────
            const float finCamara = 0.40f, finModelo = 0.68f;

            // Cámara 3D: solo la columna del modelo, encuadrando las dos piernas
            var cam = UnityEngine.Camera.main;
            if (cam != null)
            {
                cam.rect = new Rect(finCamara, 0f, finModelo - finCamara, 1f);
                if (!cam.TryGetComponent(out EncuadrarModelo encuadre))
                    encuadre = cam.gameObject.AddComponent<EncuadrarModelo>();
                encuadre.modelos.Clear();
                foreach (var n in new[] { "Pierna_Derecha", "Pierna_Izquierda" })
                {
                    var go = GameObject.Find(n);
                    if (go != null) encuadre.modelos.Add(go);
                }
            }

            // ── Mitad real: imagen del Kinect ─────────────
            var canvasGo = new GameObject("ARMyo_VistaMixta", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;

            var mitad = NuevoUI("MitadReal", canvasGo.transform);
            Estirar(mitad, new Vector2(0f, 0f), new Vector2(finCamara, 1f));
            mitad.gameObject.AddComponent<Image>().color = Color.black;
            mitad.gameObject.AddComponent<RectMask2D>();

            var imgRt = NuevoUI("ImagenKinect", mitad);
            Estirar(imgRt, Vector2.zero, Vector2.one);
            imgRt.gameObject.AddComponent<RawImage>();
            var fitter = imgRt.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16f / 9f;
            var vista = imgRt.gameObject.AddComponent<VistaCamaraKinect>();
            vista.espejo = true; // probado: sin esto la imagen salía al revés

            var avisoRt = NuevoUI("Aviso", mitad);
            Estirar(avisoRt, Vector2.zero, Vector2.one);
            var txt = avisoRt.gameObject.AddComponent<Text>();
            txt.text = "Esperando la imagen del Kinect…";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 36;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1f, 1f, 1f, 0.6f);
            vista.avisoSinImagen = txt;

            // ── Paneles: columna de la derecha, uno debajo de otro ──
            var arribaDerecha = new Vector2(1f, 0f);
            foreach (var m in Object.FindObjectsOfType<MedidorAngulosPierna>(true))
            {
                m.anclaPantalla = arribaDerecha; m.pivote = arribaDerecha; m.desplazamiento = new Vector2(-20f, 10f);
                m.modoCompacto = true;
                EditorUtility.SetDirty(m);
            }
            foreach (var p in paneles)
            {
                bool derecha = p.gameObject.name.EndsWith("Derecha");
                p.Colocar(arribaDerecha, arribaDerecha, new Vector2(-20f, derecha ? 112f : 582f));
                p.modoGrande = true; // se lee a 2–3 m: solo la fase y los músculos, con letra grande
                EditorUtility.SetDirty(p);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AgregarABuild(Destino);
            Selection.activeGameObject = canvasGo;

            Debug.Log("[ARMyo] Escena mixta creada: " + Destino);
            EditorUtility.DisplayDialog("ARMyo",
                "Listo. Se creó la escena EscenaMixta:\n  izquierda = tu imagen del Kinect (en espejo)\n  centro = las dos piernas virtuales\n  derecha = los paneles\n" + aviso +
                "\nPulsa ▶ Play (mejor con 'Play Maximized').\n  E = invertir la imagen si no se ve como un espejo\n" +
                "  ↑/↓, W/S, Espacio, K = igual que antes", "OK");
        }

        static RectTransform NuevoUI(string nombre, Transform padre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            return (RectTransform)go.transform;
        }

        static void Estirar(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min; rt.anchorMax = max;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static void AgregarABuild(string path)
        {
            var escenas = EditorBuildSettings.scenes.ToList();
            if (escenas.Any(e => e.path == path)) return;
            escenas.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }
    }
}
