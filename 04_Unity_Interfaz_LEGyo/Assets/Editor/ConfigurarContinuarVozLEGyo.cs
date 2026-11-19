using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ConfigurarContinuarVozLEGyo
{
 [MenuItem("LEGyo/Usar solo Continuar por voz")]
 public static void Aplicar()
 {
  if(EditorApplication.isPlaying){Debug.LogWarning("Sal de Play para guardar.");return;}
  var escena=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(escena.name!="MenuPrincipal")return;
  var raices=escena.GetRootGameObjects();
  var actual=System.Array.Find(raices,x=>x.name=="CanvasConexion");
  var siguiente=System.Array.Find(raices,x=>x.name=="CanvasBienvenida");
  var panel=actual.transform.Find("PanelFlotante");
  var boton=panel.Find("BotonContinuar");
  if(boton!=null)Undo.DestroyObjectImmediate(boton.gameObject);
  var controlador=actual.GetComponent<ContinuarPorVozLEGyo>();
  if(controlador==null)controlador=actual.AddComponent<ContinuarPorVozLEGyo>();
  controlador.pantallaActual=actual;controlador.siguientePantalla=siguiente;
  controlador.asistente=panel.Find("BienvenidaAsistente/RobotAsistente").GetComponent<AsistenteAnimadoLEGyo>();
  var existente=panel.Find("EstadoNavegacionVoz");
  var etiqueta=existente!=null?existente.GetComponent<Text>():new GameObject("EstadoNavegacionVoz",typeof(RectTransform),typeof(Text)).GetComponent<Text>();
  var rect=etiqueta.rectTransform;rect.SetParent(panel,false);rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
  rect.anchoredPosition=new Vector2(190,-335);rect.sizeDelta=new Vector2(800,80);
  etiqueta.font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Exo2-Regular.ttf");etiqueta.fontSize=24;
  etiqueta.alignment=TextAnchor.MiddleCenter;etiqueta.raycastTarget=false;
  etiqueta.text="Escucha la bienvenida…";etiqueta.color=new Color32(120,232,239,255);
  controlador.estado=etiqueta;EditorUtility.SetDirty(controlador);
  EditorSceneManager.MarkSceneDirty(escena);EditorSceneManager.SaveScene(escena);
  Debug.Log("LEGyo: botón eliminado. Solo el comando de voz Continuar permite avanzar, tras la narración.");
 }
}
