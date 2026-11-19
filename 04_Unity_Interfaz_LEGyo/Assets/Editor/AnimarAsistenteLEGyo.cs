using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AnimarAsistenteLEGyo
{
 [MenuItem("LEGyo/Animar robot y activar narracion")]
 public static void Aplicar()
 {
  if(EditorApplication.isPlaying){Debug.LogWarning("Sal de Play antes de guardar.");return;}
  var escena=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(escena.name!="MenuPrincipal")return;
  var raiz=System.Array.Find(escena.GetRootGameObjects(),r=>r.name=="CanvasConexion");
  var panel=raiz.transform.Find("PanelFlotante");
  var robot=panel.Find("BienvenidaAsistente/RobotAsistente");
  var fuente=robot.GetComponent<AudioSource>();
  if(fuente==null)fuente=robot.gameObject.AddComponent<AudioSource>();
  fuente.playOnAwake=false;fuente.loop=false;fuente.spatialBlend=0;fuente.volume=.85f;
  var animacion=robot.GetComponent<AsistenteAnimadoLEGyo>();
  if(animacion==null)animacion=robot.gameObject.AddComponent<AsistenteAnimadoLEGyo>();
  animacion.bienvenida=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/BienvenidaLEGyo.wav");
  if(animacion.bienvenida==null)throw new System.InvalidOperationException("Falta el audio de bienvenida.");
  var boton=panel.Find("BotonContinuar").GetComponent<Button>();
  bool agregado=false;
  for(int i=0;i<boton.onClick.GetPersistentEventCount();i++)if(boton.onClick.GetPersistentTarget(i)==fuente && boton.onClick.GetPersistentMethodName(i)=="Stop")agregado=true;
  if(!agregado)UnityEventTools.AddPersistentListener(boton.onClick,fuente.Stop);
  // Keep the shader available in player builds, not only in the editor preview.
  var shader=Shader.Find("LEGyo/Robot con parpadeo");
  if(shader==null)throw new System.InvalidOperationException("Falta el shader de ojos.");
  const string ruta="Assets/Resources/RobotParpadeoLEGyo.mat";
  var material=AssetDatabase.LoadAssetAtPath<Material>(ruta);
  if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,ruta);}
  robot.GetComponent<RawImage>().material=material;
  EditorUtility.SetDirty(animacion);EditorUtility.SetDirty(boton);
  EditorSceneManager.MarkSceneDirty(escena);EditorSceneManager.SaveScene(escena);AssetDatabase.SaveAssets();
  Debug.Log("LEGyo: parpadeo, movimiento de cabeza y narracion en español guardados.");
 }
}
