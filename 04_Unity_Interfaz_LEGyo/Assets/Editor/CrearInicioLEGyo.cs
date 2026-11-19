using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public static class CrearInicioLEGyo
{
    [MenuItem("LEGyo/Crear pantalla de conexión")]
    public static void Crear()
    {
        var scene = EditorSceneManager.OpenScene("Assets/MenuPrincipal.unity");
        var canvasObject = GameObject.Find("CanvasConexion");
        if (canvasObject != null)
            throw new System.InvalidOperationException("La interfaz ya existe.");

        canvasObject = new GameObject("CanvasConexion", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var background = Element("Fondo", canvasObject.transform, Vector2.zero, new Vector2(1920,1080));
        background.gameObject.AddComponent<Image>().color = new Color32(16,28,39,255);
        Texto("Marca", canvasObject.transform, "LEGyo", new Vector2(0,370), 62, new Color32(76,215,192,255));
        Texto("Subtitulo", canvasObject.transform, "Anatomia del miembro inferior", new Vector2(0,292), 28, Color.white);
        Texto("TituloConexion", canvasObject.transform, "Conecta tu Kinect", new Vector2(0,120), 52, Color.white);
        Texto("Instrucciones", canvasObject.transform, "Conecta el sensor a la corriente y a un puerto USB 3.0.\nDespués comprobaremos que se vean tus piernas y tus pies.", new Vector2(0,10), 27, new Color32(190,205,216,255));
        Texto("EstadoSensor", canvasObject.transform, "Estado del sensor pendiente", new Vector2(0,-125), 25, new Color32(235,197,112,255));
        var buttonRect = Element("BotonContinuar", canvasObject.transform, new Vector2(0,-255),new Vector2(350,76));
        var image = buttonRect.gameObject.AddComponent<Image>();
        image.color = new Color32(76,215,192,255);
        var button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.interactable = false;
        Texto("TextoContinuar",buttonRect,"Continuar",Vector2.zero,28,new Color32(16,28,39,255));
        if (Object.FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = canvasObject;
        Debug.Log("LEGyo: pantalla de conexion guardada. Deteccion Kinect pendiente.");
    }

    private static RectTransform Element(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var item = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        item.SetParent(parent,false);
        item.anchorMin = item.anchorMax = new Vector2(0.5f,0.5f);
        item.anchoredPosition = position;
        item.sizeDelta = size;
        return item;
    }

    private static void Texto(string name, Transform parent, string content, Vector2 position, int size, Color color)
    {
        var rect = Element(name,parent,position,new Vector2(1200,130));
        var text = rect.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
    }
}

