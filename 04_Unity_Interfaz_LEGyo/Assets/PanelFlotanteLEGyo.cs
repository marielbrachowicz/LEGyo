using UnityEngine;
using UnityEngine.UI;
public class PanelFlotanteLEGyo : MonoBehaviour {
 RectTransform panel,halo; Vector2 origen; Vector3 escala; Quaternion giro; float inicio; bool creado;
 void OnEnable(){panel=GetComponent<RectTransform>();origen=panel.anchoredPosition;escala=panel.localScale;giro=panel.localRotation;inicio=Time.unscaledTime;if(!creado)Crear();}
 Image Pieza(string nombre,Transform padre,Vector2 pos,Vector2 medida,Color color){var g=new GameObject(nombre,typeof(RectTransform),typeof(Image));var r=g.GetComponent<RectTransform>();r.SetParent(padre,false);r.anchoredPosition=pos;r.sizeDelta=medida;var im=g.GetComponent<Image>();im.color=color;im.raycastTarget=false;return im;}
 void Crear(){
 creado=true;
 var luz=Pieza("Luz bajo el cristal",transform.parent,new Vector2(0,-420),new Vector2(1100,160),Color.white);halo=luz.rectTransform;halo.SetSiblingIndex(transform.GetSiblingIndex());
 var tex=new Texture2D(128,128,TextureFormat.RGBA32,false);
 for(int y=0;y<128;y++)for(int x=0;x<128;x++){float d=new Vector2((x-63.5f)/63.5f,(y-63.5f)/63.5f).magnitude;tex.SetPixel(x,y,new Color(.12f,.8f,1,Mathf.Pow(Mathf.Clamp01(1-d),2)*.7f));}tex.Apply();luz.sprite=Sprite.Create(tex,new Rect(0,0,128,128),Vector2.one*.5f);
 var atras=Pieza("Cristal posterior",transform,new Vector2(36,-28),new Vector2(1440,900),new Color(.025f,.12f,.17f,.85f));atras.transform.SetAsFirstSibling();var b=atras.gameObject.AddComponent<Outline>();b.effectColor=new Color(.15f,.8f,1,.5f);b.effectDistance=new Vector2(2,-2);
 var frente=transform.Find("Fondo (1)");if(frente==null){frente=Pieza("Superficie de cristal",transform,Vector2.zero,new Vector2(1440,900),Color.white).transform;frente.SetSiblingIndex(1);}
    frente.GetComponent<Image>().color=new Color(.12f,.45f,.55f,.006f);
 foreach(var s in frente.GetComponents<Shadow>())if(!(s is Outline))s.enabled=false;
 for(int i=6;i>=1;i--){var a=Pieza("Halo del borde",transform,Vector2.zero,new Vector2(1440+i*8,900+i*8),new Color(.1f,.75f,1,.018f));a.transform.SetSiblingIndex(1);}
 var contorno=frente.GetComponent<Outline>();if(contorno==null)contorno=frente.gameObject.AddComponent<Outline>();contorno.effectColor=new Color(.3f,.95f,1,.9f);contorno.effectDistance=new Vector2(2,-2);
 Pieza("Reflejo superior",transform,new Vector2(0,447),new Vector2(1430,3),new Color(.7f,1,1,.9f));
 Pieza("Reflejo lateral",transform,new Vector2(-717,0),new Vector2(3,890),new Color(.4f,1,1,.7f));
 for(int lado=-1;lado<=1;lado+=2){Pieza("Esquina",transform,new Vector2(lado*675,450),new Vector2(90,7),new Color(.6f,1,1,.95f));Pieza("Esquina",transform,new Vector2(lado*720,410),new Vector2(7,80),new Color(.6f,1,1,.95f));}
 var reflejo=Pieza("Reflejo de vidrio",transform,new Vector2(-380,0),new Vector2(160,880),new Color(.5f,.9f,1,.025f));reflejo.rectTransform.localRotation=Quaternion.Euler(0,0,-18);reflejo.transform.SetSiblingIndex(frente.GetSiblingIndex()+1);
  // Alpha-feathered edges remain smooth while the glass rotates.
 var suave=new Texture2D(32,32,TextureFormat.RGBA32,false);
 suave.wrapMode=TextureWrapMode.Clamp;suave.filterMode=FilterMode.Bilinear;
 for(int y=0;y<32;y++)for(int x=0;x<32;x++){float d=Mathf.Min(Mathf.Min(x,31-x),Mathf.Min(y,31-y));suave.SetPixel(x,y,new Color(1,1,1,Mathf.SmoothStep(0,1,d/3f)));}suave.Apply();
 var spriteSuave=Sprite.Create(suave,new Rect(0,0,32,32),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(5,5,5,5));
 var marco=Pieza("Borde luminoso suavizado",transform,Vector2.zero,new Vector2(1448,908),new Color(.35f,.95f,1,.85f));marco.transform.SetSiblingIndex(frente.GetSiblingIndex());
 foreach(var im in GetComponentsInChildren<Image>(true)){im.sprite=spriteSuave;im.type=Image.Type.Sliced;foreach(var efecto in im.GetComponents<Shadow>())efecto.enabled=false;}
  // Hollow luminous rims let the background show through the glass.
 var anillo=new Texture2D(32,32,TextureFormat.RGBA32,false);anillo.wrapMode=TextureWrapMode.Clamp;anillo.filterMode=FilterMode.Bilinear;
 for(int y=0;y<32;y++)for(int x=0;x<32;x++){float d=Mathf.Min(Mathf.Min(x,31-x),Mathf.Min(y,31-y));float a=Mathf.SmoothStep(0,1,d/1.5f)*(1-Mathf.SmoothStep(2,5,d));anillo.SetPixel(x,y,new Color(1,1,1,a));}anillo.Apply();
 var bordeHueco=Sprite.Create(anillo,new Rect(0,0,32,32),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,new Vector4(6,6,6,6));
 marco.sprite=bordeHueco;atras.sprite=bordeHueco;atras.color=new Color(.12f,.7f,.85f,.3f);
 foreach(var im in GetComponentsInChildren<Image>(true))if(im.name=="Halo del borde")im.sprite=bordeHueco;
   reflejo.color=Color.clear;
 var puntos=transform.parent.Find("Fondo/PuntosFondo");
 if(puntos!=null){int indice=0;foreach(var punto in puntos.GetComponentsInChildren<Image>(true)){punto.color=new Color(.65f,.85f,.92f,.32f+(indice%4)*.07f);indice++;}}
 // A soft rim gives glass its silhouette without filling the centre.
 for(int lado=-1;lado<=1;lado+=2){
 var luzH=Pieza("Resplandor suave del canto",transform,new Vector2(0,lado*450),new Vector2(1450,40),new Color(.2f,.85f,1,.045f));luzH.sprite=spriteSuave;luzH.type=Image.Type.Sliced;
 var luzV=Pieza("Resplandor suave del canto",transform,new Vector2(lado*720,0),new Vector2(40,900),new Color(.2f,.85f,1,.045f));luzV.sprite=spriteSuave;luzV.type=Image.Type.Sliced;
 }

  marco.color=Color.clear;atras.color=Color.clear;
 foreach(var im in GetComponentsInChildren<Image>(true))if(im.name=="Reflejo superior"||im.name=="Reflejo lateral"||im.name=="Esquina")im.color=Color.clear;
 for(int lado=-1;lado<=1;lado+=2){
 var h=Pieza("Contorno de vidrio suave",transform,new Vector2(0,lado*450),new Vector2(1440,12),new Color(.3f,.95f,1,.7f));h.sprite=spriteSuave;h.type=Image.Type.Sliced;
 var v=Pieza("Contorno de vidrio suave",transform,new Vector2(lado*720,0),new Vector2(12,900),new Color(.3f,.95f,1,.7f));v.sprite=spriteSuave;v.type=Image.Type.Sliced;
 }
  // Diffuse light falloff instead of flat neon strips.
 var difusa=new Texture2D(128,128,TextureFormat.RGBA32,false);difusa.wrapMode=TextureWrapMode.Clamp;difusa.filterMode=FilterMode.Bilinear;
 for(int y=0;y<128;y++)for(int x=0;x<128;x++){float dx=(x-63.5f)/63.5f,dy=(y-63.5f)/63.5f;float a=Mathf.Exp(-4*(dx*dx+dy*dy))*Mathf.Clamp01((1-Mathf.Abs(dx))*8)*Mathf.Clamp01((1-Mathf.Abs(dy))*8);difusa.SetPixel(x,y,new Color(1,1,1,a));}difusa.Apply();
 var luzDifusa=Sprite.Create(difusa,new Rect(0,0,128,128),Vector2.one*.5f);
 foreach(var im in GetComponentsInChildren<Image>(true)){
 if(im.name=="Halo del borde")im.color=Color.clear;
 if(im.name=="Resplandor suave del canto"){im.sprite=luzDifusa;im.type=Image.Type.Simple;im.color=new Color(.3f,.85f,1,.12f);}
 if(im.name=="Contorno de vidrio suave"){
 var r=im.rectTransform;bool horizontal=r.sizeDelta.x>r.sizeDelta.y;
 r.sizeDelta=horizontal?new Vector2(1440,6):new Vector2(6,900);
 bool iluminado=horizontal?r.anchoredPosition.y>0:r.anchoredPosition.x<0;
 im.color=iluminado?new Color(.65f,.95f,1,.65f):new Color(.25f,.7f,.8f,.3f);
 }
 }
 luz.color=new Color(1,1,1,.38f);halo.sizeDelta=new Vector2(1000,110);
 reflejo.sprite=luzDifusa;reflejo.type=Image.Type.Simple;reflejo.color=new Color(.7f,.9f,1,.018f);reflejo.rectTransform.sizeDelta=new Vector2(280,720);
 }




   void Update(){float t=Time.unscaledTime-inicio;panel.anchoredPosition=new Vector2(0,Mathf.Sin(t*.9f)*18);panel.localRotation=Quaternion.identity;panel.localScale=escala*.86f;if(halo!=null)halo.localScale=new Vector3(1-Mathf.Sin(t*.9f)*.08f,1,1);}
 void OnDisable(){if(panel==null)return;panel.anchoredPosition=origen;panel.localRotation=giro;panel.localScale=escala;}
}
