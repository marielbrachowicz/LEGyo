using UnityEngine;
using UnityEngine.UI;

public class SeguimientoPiernasLEGyo : MaskableGraphic
{
 public Vector2[] puntos = new Vector2[8];
 public bool[] visibles = new bool[8];
 public bool correcta;
 public void Actualizar(){SetVerticesDirty();}
 public void Limpiar(){for(int i=0;i<8;i++)visibles[i]=false;SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper vh)
 {
  vh.Clear();var r=rectTransform.rect;
  var tenue=new Color(.35f,.85f,.9f,.55f);
  for(float t=.08f;t<.92f;t+=.085f)
  {
   Linea(vh,P(.29f,t,r),P(.29f,t+.045f,r),2.5f,tenue);Linea(vh,P(.71f,t,r),P(.71f,t+.045f,r),2.5f,tenue);
  }
  for(float t=.29f;t<.71f;t+=.07f){Linea(vh,P(t,.08f,r),P(t+.035f,.08f,r),2.5f,tenue);Linea(vh,P(t,.92f,r),P(t+.035f,.92f,r),2.5f,tenue);}
  var c=correcta?new Color(.45f,1,.72f,1):new Color(.47f,.91f,.94f,1);
  int[,] huesos={{0,1},{1,2},{2,3},{4,5},{5,6},{6,7},{0,4}};
  for(int i=0;i<huesos.GetLength(0);i++){int a=huesos[i,0],b=huesos[i,1];if(visibles[a]&&visibles[b])Linea(vh,P(puntos[a].x,puntos[a].y,r),P(puntos[b].x,puntos[b].y,r),5,c);}
  for(int i=0;i<8;i++)if(visibles[i])Circulo(vh,P(puntos[i].x,puntos[i].y,r),7,c);
 }
 Vector2 P(float x,float y,Rect r){return new Vector2(r.xMin+x*r.width,r.yMin+y*r.height);}
 void Linea(VertexHelper vh,Vector2 a,Vector2 b,float ancho,Color c)
 {
  Vector2 n=(b-a).normalized;n=new Vector2(-n.y,n.x)*ancho*.5f;
  int k=vh.currentVertCount;vh.AddVert(a-n,c,Vector2.zero);vh.AddVert(a+n,c,Vector2.zero);vh.AddVert(b+n,c,Vector2.zero);vh.AddVert(b-n,c,Vector2.zero);vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
 }
 void Circulo(VertexHelper vh,Vector2 p,float radio,Color c)
 {
  int k=vh.currentVertCount;vh.AddVert(p,c,Vector2.zero);
  for(int i=0;i<=20;i++){float a=i*Mathf.PI*2/20;vh.AddVert(p+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radio,c,Vector2.zero);if(i>0)vh.AddTriangle(k,k+i,k+i+1);}
 }
}
