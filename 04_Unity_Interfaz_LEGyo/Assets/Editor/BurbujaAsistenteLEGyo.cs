using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BurbujaAsistenteLEGyo
{
 [MenuItem("LEGyo/Aplicar burbuja de dialogo y boca")]
 public static void Aplicar()
 {
  if(EditorApplication.isPlaying){Debug.LogWarning("Sal de Play para guardar la burbuja.");return;}
  var escena=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  if(escena.name!="MenuPrincipal")return;
  var raiz=System.Array.Find(escena.GetRootGameObjects(),r=>r.name=="CanvasConexion");
  var grupo=raiz.transform.Find("PanelFlotante/BienvenidaAsistente");
  var anterior=grupo.Find("MensajeDelRobot");
  if(anterior!=null)anterior.gameObject.SetActive(false);
  const int w=1024,h=640;
  var textura=new Texture2D(w,h,TextureFormat.RGBA32,false);
  for(int y=0;y<h;y++)for(int x=0;x<w;x++)
  {
   var p=new Vector2(x+.5f,y+.5f);
   var q=new Vector2(Mathf.Abs(p.x-560)-428,Mathf.Abs(p.y-320)-284);
   float caja=new Vector2(Mathf.Max(q.x,0),Mathf.Max(q.y,0)).magnitude+Mathf.Min(Mathf.Max(q.x,q.y),0)-35;
   var a=new Vector2(4,320);var b=new Vector2(115,260);var c=new Vector2(115,380);
   bool dentro=p.x<=115 && p.x>=4 && Mathf.Abs(p.y-320)<=(p.x-4)*60/111;
   float tri=Mathf.Min(Segmento(p,a,b),Mathf.Min(Segmento(p,b,c),Segmento(p,c,a)))*(dentro?-1:1);
   float d=Mathf.Min(caja,tri);
   float alpha=1-Mathf.SmoothStep(-1,1,d);
   float borde=1-Mathf.SmoothStep(1.5f,4.5f,Mathf.Abs(d));
   var color=Color.Lerp(new Color(.025f,.075f,.105f,.88f),new Color(.47f,.91f,.96f,.75f),borde);
   color.a*=alpha;textura.SetPixel(x,y,color);
  }
  textura.Apply();const string ruta="Assets/Resources/BurbujaRobotLEGyo.png";
  File.WriteAllBytes(ruta,textura.EncodeToPNG());Object.DestroyImmediate(textura);
  AssetDatabase.ImportAsset(ruta);
  var importador=(TextureImporter)AssetImporter.GetAtPath(ruta);
  importador.alphaIsTransparency=true;importador.mipmapEnabled=false;importador.textureCompression=TextureImporterCompression.Uncompressed;importador.SaveAndReimport();
  var existente=grupo.Find("BurbujaDeDialogo");
  var imagen=existente!=null?existente.GetComponent<RawImage>():new GameObject("BurbujaDeDialogo",typeof(RectTransform),typeof(RawImage)).GetComponent<RawImage>();
  var rect=imagen.rectTransform;rect.SetParent(grupo,false);rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;
  rect.anchoredPosition=new Vector2(140,-35);rect.sizeDelta=new Vector2(900,490);rect.SetSiblingIndex(0);
  imagen.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);imagen.color=Color.white;imagen.raycastTarget=false;
  var robot=grupo.Find("RobotAsistente").GetComponent<AsistenteAnimadoLEGyo>();
  robot.bienvenida=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/BienvenidaLEGyo.wav");
  EditorUtility.SetDirty(robot);EditorSceneManager.MarkSceneDirty(escena);EditorSceneManager.SaveScene(escena);
  Debug.Log("LEGyo: burbuja con cola hacia el robot y narracion completa guardadas.");
 }
 static float Segmento(Vector2 p,Vector2 a,Vector2 b){var ab=b-a;return (p-a-ab*Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude)).magnitude;}
}
