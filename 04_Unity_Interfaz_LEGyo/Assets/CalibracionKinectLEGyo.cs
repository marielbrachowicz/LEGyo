using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using Windows.Kinect;

public class CalibracionKinectLEGyo : MonoBehaviour
{
 public RawImage camara;
 public SeguimientoPiernasLEGyo seguimiento;
 public Text indicacion,estado,conexion;
 KinectSensor sensor;
 ColorFrameReader color;
 BodyFrameReader cuerpos;
 Windows.Kinect.Body[] personas;
 Texture2D textura;
 byte[] pixeles;
 GCHandle memoria;
 int ancho,alto;
 float reintentar,proximoColor,ultimaImagen,ultimoCuerpo,estable;
 float posicionCorrectaDesde=-1;
 ulong personaActual;
 readonly JointType[] articulaciones={JointType.HipLeft,JointType.KneeLeft,JointType.AnkleLeft,JointType.FootLeft,JointType.HipRight,JointType.KneeRight,JointType.AnkleRight,JointType.FootRight};
 void OnEnable(){reintentar=0;ultimaImagen=ultimoCuerpo=-100;estable=0;personaActual=0;camara.color=Color.clear;Estado("Esperando conexión del Kinect…","Conecta tu Kinect a corriente y USB 3.0.",false);}
 void Update()
 {
  try
  {
   if(sensor==null){if(Time.unscaledTime>=reintentar){reintentar=Time.unscaledTime+3;Abrir();}return;}
   if(!sensor.IsAvailable){seguimiento.Limpiar();camara.color=Color.clear;estable=0;Estado("Esperando conexión del Kinect…","Conecta tu Kinect a corriente y USB 3.0.",false);return;}
   conexion.text="Kinect conectado · cámara en directo";
   if(Time.unscaledTime>=proximoColor)
   {
    proximoColor=Time.unscaledTime+.066f;
    using(var frame=color.AcquireLatestFrame())if(frame!=null){frame.CopyConvertedFrameDataToIntPtr(memoria.AddrOfPinnedObject(),(uint)pixeles.Length,ColorImageFormat.Rgba);textura.LoadRawTextureData(pixeles);textura.Apply(false);camara.color=Color.white;ultimaImagen=Time.unscaledTime;}
   }
   if(Time.unscaledTime-ultimaImagen>2){estable=0;seguimiento.Limpiar();Estado("Esperando imagen del Kinect…","Comprueba que la cámara del Kinect esté disponible.",false);return;}
   using(var frame=cuerpos.AcquireLatestFrame())
   {
    if(frame!=null){frame.GetAndRefreshBodyData(personas);ultimoCuerpo=Time.unscaledTime;Comprobar();}
    else if(Time.unscaledTime-ultimoCuerpo>1){estable=0;seguimiento.Limpiar();Estado("Esperando posición correcta…","Colócate frente al Kinect.",false);}
   }
  }
  catch(Exception error){Debug.LogWarning("LEGyo Kinect: "+error.Message);Cerrar();reintentar=Time.unscaledTime+3;Estado("No se pudo abrir el Kinect.","Revisa la conexión y el SDK de Kinect v2.",false);}
 }
 void Abrir()
 {
  sensor=KinectSensor.GetDefault();
  if(sensor==null)return;
  var desc=sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);ancho=desc.Width;alto=desc.Height;
  pixeles=new byte[ancho*alto*4];memoria=GCHandle.Alloc(pixeles,GCHandleType.Pinned);
  textura=new Texture2D(ancho,alto,TextureFormat.RGBA32,false);camara.texture=textura;camara.uvRect=new Rect(1,1,-1,-1);
  color=sensor.ColorFrameSource.OpenReader();cuerpos=sensor.BodyFrameSource.OpenReader();personas=new Windows.Kinect.Body[sensor.BodyFrameSource.BodyCount];
  sensor.Open();Debug.Log("LEGyo: lector de cámara y seguimiento de Kinect v2 abiertos.");
 }
 void Comprobar()
 {
  Windows.Kinect.Body persona=null;float cerca=float.MaxValue;
  foreach(var p in personas)if(p!=null && p.IsTracked){if(p.TrackingId==personaActual){persona=p;break;}float z=p.Joints[JointType.SpineBase].Position.Z;if(z<cerca){cerca=z;persona=p;}}
  if(persona==null){personaActual=0;estable=0;seguimiento.Limpiar();Estado("Esperando posición correcta…","Colócate frente al Kinect hasta ver ambas piernas y pies.",false);return;}
  if(personaActual!=persona.TrackingId){estable=0;personaActual=persona.TrackingId;}
  bool completa=true;float minimoX=1,maximoX=0;
  for(int i=0;i<8;i++)
  {
   var j=persona.Joints[articulaciones[i]];var p=sensor.CoordinateMapper.MapCameraPointToColorSpace(j.Position);
   float x=1-p.X/ancho,y=1-p.Y/alto;
   bool dentro=!float.IsNaN(x)&&!float.IsInfinity(x)&&!float.IsNaN(y)&&!float.IsInfinity(y)&&x>.035f&&x<.965f&&y>.035f&&y<.965f;
   seguimiento.puntos[i]=new Vector2(x,y);seguimiento.visibles[i]=dentro && j.TrackingState!=TrackingState.NotTracked;
   completa &= dentro && j.TrackingState==TrackingState.Tracked;
   if(dentro){minimoX=Mathf.Min(minimoX,x);maximoX=Mathf.Max(maximoX,x);}
  }
  float distancia=persona.Joints[JointType.SpineBase].Position.Z;
  bool centrada=(minimoX+maximoX)*.5f>.28f && (minimoX+maximoX)*.5f<.72f;
  bool lista=completa && centrada && distancia>=1.6f && distancia<=3.5f;
  if(lista){if(posicionCorrectaDesde<0)posicionCorrectaDesde=Time.unscaledTime;estable=Time.unscaledTime-posicionCorrectaDesde;}
  else{estable=0;posicionCorrectaDesde=-1;}
  seguimiento.correcta=estable>=2;seguimiento.Actualizar();
  if(estable>=2)Estado("Posición correcta · calibración lista","¡Listo! Ambas piernas y pies son visibles.",true);
  else if(lista)Estado("Comprobando posición…","Mantén tu posición un momento.",false);
  else if(distancia>3.5f)Estado("Esperando posición correcta…","Da un paso hacia adelante.",false);
  else if(!centrada)Estado("Esperando posición correcta…","Muévete al centro del área de detección.",false);
  else Estado("Esperando posición correcta…","Da un paso hacia atrás",false);
 }
 void Estado(string mensaje,string guia,bool lista){estado.text=mensaje;indicacion.text=guia;estado.color=lista?new Color(.45f,1,.72f):new Color32(120,232,239,255);if(sensor==null||!sensor.IsAvailable)conexion.text="Kinect sin conexión";}
 void Cerrar(){if(color!=null){color.Dispose();color=null;}if(cuerpos!=null){cuerpos.Dispose();cuerpos=null;}if(sensor!=null){sensor.Close();sensor=null;}if(memoria.IsAllocated)memoria.Free();if(textura!=null){Destroy(textura);textura=null;}if(camara!=null){camara.texture=null;camara.color=Color.clear;}posicionCorrectaDesde=-1;}
 void OnDisable(){Cerrar();}
 void OnDestroy(){Cerrar();}
}
