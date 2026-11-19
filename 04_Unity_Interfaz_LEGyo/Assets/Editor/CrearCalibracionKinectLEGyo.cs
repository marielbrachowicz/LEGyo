using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CrearCalibracionKinectLEGyo
{
 static Font fuente;
 [MenuItem("LEGyo/Crear pantalla de calibracion Kinect")]
 public static void Crear()
 {
  if(EditorApplication.isPlaying){Debug.LogWarning("Sal de Play para guardar.");return;}
  var escena=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(escena.name!="MenuPrincipal")return;
  var raices=escena.GetRootGameObjects();var bienvenida=System.Array.Find(raices,r=>r.name=="CanvasConexion");
  var calibracion=System.Array.Find(raices,r=>r.name=="CanvasBienvenida");var panel=calibracion.transform.Find("PanelFlotante");
  if(panel.Find("CalibracionKinect")!=null){Debug.Log("La pantalla de calibración ya existe.");return;}
  fuente=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Exo2-Regular.ttf");
  foreach(string nombre in new[]{"TituloConexion","Instrucciones","EstadoSensor"}){var viejo=panel.Find(nombre);if(viejo!=null)viejo.gameObject.SetActive(false);}
  var subtitulo=panel.Find("Subtitulo").GetComponent<Text>();subtitulo.font=fuente;subtitulo.text="Calibremos tu posición";subtitulo.fontSize=40;subtitulo.rectTransform.anchoredPosition=new Vector2(0,282);
  var grupo=Rect("CalibracionKinect",panel,Vector2.zero,new Vector2(1440,900));
  Texto("InstruccionPrincipal",grupo,"Colócate frente al Kinect hasta que ambas piernas y pies sean visibles.",new Vector2(0,210),new Vector2(1300,65),25,Color.white);
  var original=bienvenida.transform.Find("PanelFlotante/BienvenidaAsistente/RobotAsistente");
  var robot=Object.Instantiate(original.gameObject,grupo,false);robot.name="RobotCalibracion";
  var r=robot.GetComponent<RectTransform>();r.anchoredPosition=new Vector2(-470,-25);r.sizeDelta=new Vector2(520,346.6667f);r.localRotation=Quaternion.identity;r.localScale=Vector3.one;
  var animacion=robot.GetComponent<AsistenteAnimadoLEGyo>();animacion.bienvenida=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/CalibracionLEGyo.wav");
  var vista=Rect("VistaKinect",grupo,new Vector2(205,-40),new Vector2(740,416.25f));
  var fondo=vista.gameObject.AddComponent<RawImage>();fondo.color=new Color(.012f,.045f,.065f,1);fondo.raycastTarget=false;
  var video=Rect("CamaraKinect",vista,Vector2.zero,new Vector2(740,416.25f)).gameObject.AddComponent<RawImage>();video.color=Color.clear;video.raycastTarget=false;
  var guia=Rect("SeguimientoPiernas",vista,Vector2.zero,new Vector2(740,416.25f)).gameObject.AddComponent<SeguimientoPiernasLEGyo>();guia.raycastTarget=false;
  Texto("AreaDeteccion",vista,"ÁREA DE DETECCIÓN",new Vector2(0,-180),new Vector2(450,35),18,new Color(.55f,.85f,.9f,.75f));
  for(int lado=-1;lado<=1;lado+=2){Linea(vista,new Vector2(0,lado*208.125f),new Vector2(740,2));Linea(vista,new Vector2(lado*370,0),new Vector2(2,416.25f));}
  var conexion=Texto("ConexionKinect",grupo,"Esperando cámara del Kinect…",new Vector2(205,181),new Vector2(740,35),20,new Color32(120,232,239,255));
  Texto("TituloVistaSeguimiento",grupo,"Vista de seguimiento de piernas · cámara Kinect",new Vector2(205,-278),new Vector2(780,45),22,new Color(.7f,.8f,.85f));
  var indicacion=Texto("IndicacionPosicion",grupo,"Da un paso hacia atrás",new Vector2(-470,-245),new Vector2(410,100),28,new Color32(120,232,239,255));
  Texto("AyudaPosicion",grupo,"Ajusta tu posición siguiendo las indicaciones.",new Vector2(0,-330),new Vector2(1250,55),24,new Color(.75f,.83f,.87f));
  var estado=Texto("EstadoCalibracion",grupo,"Esperando posición correcta…",new Vector2(0,-390),new Vector2(1250,65),27,new Color32(120,232,239,255));
  var controlador=calibracion.GetComponent<CalibracionKinectLEGyo>();if(controlador==null)controlador=calibracion.AddComponent<CalibracionKinectLEGyo>();
  controlador.camara=video;controlador.seguimiento=guia;controlador.indicacion=indicacion;controlador.estado=estado;controlador.conexion=conexion;
  // Ensure the native Kinect v2 plugin is available in the Windows editor and builds.
  const string ruta="Assets/Plugins/x86_64/KinectUnityAddin.dll";
  var plugin=AssetImporter.GetAtPath(ruta) as PluginImporter;
  if(plugin!=null){plugin.SetCompatibleWithAnyPlatform(false);plugin.SetCompatibleWithEditor(true);plugin.SetEditorData("CPU","x86_64");plugin.SetEditorData("OS","Windows");plugin.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64,true);plugin.SaveAndReimport();}
  EditorUtility.SetDirty(controlador);EditorSceneManager.MarkSceneDirty(escena);EditorSceneManager.SaveScene(escena);
  Debug.Log("LEGyo: calibración Kinect guardada, con vídeo real, guía y detección de piernas y pies.");
 }
 [MenuItem("LEGyo/Ver calibracion en Play")]
 static void Ver()
 {
  if(!EditorApplication.isPlaying){Debug.LogWarning("Activa Play para ver la calibración.");return;}
  var raices=UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
  var a=System.Array.Find(raices,r=>r.name=="CanvasConexion");var b=System.Array.Find(raices,r=>r.name=="CanvasBienvenida");
  b.SetActive(true);a.SetActive(false);
 }
 static RectTransform Rect(string nombre,Transform padre,Vector2 posicion,Vector2 medida){var r=new GameObject(nombre,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(padre,false);r.anchorMin=r.anchorMax=r.pivot=Vector2.one*.5f;r.anchoredPosition=posicion;r.sizeDelta=medida;return r;}
 static Text Texto(string nombre,Transform padre,string contenido,Vector2 posicion,Vector2 medida,int tamano,Color color){var t=Rect(nombre,padre,posicion,medida).gameObject.AddComponent<Text>();t.font=fuente;t.text=contenido;t.fontSize=tamano;t.color=color;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
 static void Linea(Transform padre,Vector2 posicion,Vector2 medida){var i=Rect("BordeVista",padre,posicion,medida).gameObject.AddComponent<Image>();i.color=new Color(.3f,.8f,.9f,.35f);i.raycastTarget=false;}
}
