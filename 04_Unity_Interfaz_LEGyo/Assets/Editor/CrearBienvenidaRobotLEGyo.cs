using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CrearBienvenidaRobotLEGyo
{
    static Font fuente;
    [MenuItem("LEGyo/Crear bienvenida con asistente")]
    public static void Aplicar()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Sal de Play para guardar la bienvenida."); return; }
        var escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (escena.name != "MenuPrincipal") return;
        var raiz = System.Array.Find(escena.GetRootGameObjects(), x => x.name == "CanvasConexion");
        var panel = raiz.transform.Find("PanelFlotante");
        if (panel.Find("BienvenidaAsistente") != null) { Debug.Log("La bienvenida con asistente ya existe."); return; }
        fuente = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Exo2-Regular.ttf");
        const string ruta = "Assets/Resources/AsistenteLEGyo.png";
        var importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        importador.textureType = TextureImporterType.Default;
        importador.alphaIsTransparency = true;
        importador.mipmapEnabled = false;
        importador.maxTextureSize = 2048;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.SaveAndReimport();
        foreach (string nombre in new[]{"TituloConexion","Instrucciones","EstadoSensor"})
        {
            var anterior = panel.Find(nombre);
            if (anterior != null) anterior.gameObject.SetActive(false);
        }
        var subtitulo = panel.Find("Subtitulo").GetComponent<Text>();
        subtitulo.text = "ANATOMÍA DEL MIEMBRO INFERIOR";
        subtitulo.font = fuente;
        subtitulo.fontSize = 26;
        var grupo = Rect("BienvenidaAsistente", panel, Vector2.zero, new Vector2(1440,900));
        var robot = Rect("RobotAsistente", grupo, new Vector2(-425,-15), new Vector2(600,400));
        var imagen = robot.gameObject.AddComponent<RawImage>();
        imagen.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
        imagen.raycastTarget = false;
        var dialogo = Rect("MensajeDelRobot", grupo, new Vector2(190,-35), new Vector2(800,470));
        var fondo = dialogo.gameObject.AddComponent<Image>();
        fondo.color = new Color(.1f,.3f,.4f,.055f);
        fondo.raycastTarget = false;
        Texto("TituloBienvenida",grupo,"¡Bienvenido a LEGyo!",new Vector2(190,160),new Vector2(720,80),43,Color.white);
        Texto("Descripcion",grupo,"Explora la anatomía del miembro inferior de una forma interactiva.",new Vector2(190,65),new Vector2(720,110),29,new Color(.9f,.94f,.97f));
        Texto("NavegacionVoz",grupo,"Navegación por voz",new Vector2(190,-45),new Vector2(720,65),30,new Color32(120,232,239,255));
        Texto("DescripcionVoz",grupo,"Podrás controlar la aplicación mediante comandos de voz.",new Vector2(190,-120),new Vector2(720,90),28,new Color(.9f,.94f,.97f));
        Texto("ComandoContinuar",grupo,"Di «Continuar» para comenzar.",new Vector2(190,-210),new Vector2(720,70),30,new Color32(222,140,203,255));
        var boton = panel.Find("BotonContinuar").GetComponent<RectTransform>();
        boton.anchoredPosition = new Vector2(190,-335);
        boton.sizeDelta = new Vector2(350,76);
        foreach (var texto in boton.GetComponentsInChildren<Text>(true)) { texto.text="Continuar"; texto.font=fuente; texto.fontSize=29; }
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("LEGyo: bienvenida con robot y mensajes guardada. El botón conserva su navegación.");
    }
    static RectTransform Rect(string nombre,Transform padre,Vector2 posicion,Vector2 medida)
    {
        var r=new GameObject(nombre,typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(padre,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;
        r.anchoredPosition=posicion;r.sizeDelta=medida;return r;
    }
    static void Texto(string nombre,Transform padre,string contenido,Vector2 posicion,Vector2 medida,int tamano,Color color)
    {
        var t=Rect(nombre,padre,posicion,medida).gameObject.AddComponent<Text>();
        t.font=fuente;t.text=contenido;t.fontSize=tamano;t.color=color;
        t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;
        t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;
    }
}
