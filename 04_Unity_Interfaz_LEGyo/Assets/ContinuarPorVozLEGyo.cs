using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using Vosk;

public class ContinuarPorVozLEGyo : MonoBehaviour
{
 public GameObject pantallaActual, siguientePantalla;
 public AsistenteAnimadoLEGyo asistente;
 public Text estado;
 float escucharDesde, actualizarEstado, ultimaSenal;
 bool iniciado, avanzado;
 Model modelo;
 VoskRecognizer reconocedor;
 AudioClip captura;
 int ultimaPosicion;
 float[] bloque;
 short[] pcm;
 float nivel;
 const int Tamano=1024;
 [Serializable] class Resultado { public string text; }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
 [DllImport("kernel32",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr LoadLibrary(string ruta);
#endif
 void OnEnable()
 {
  iniciado=avanzado=false;nivel=0;
  float duracion=asistente!=null && asistente.bienvenida!=null?asistente.bienvenida.length:0;
  escucharDesde=Time.unscaledTime+duracion+1.4f;
  Mostrar("Escucha la bienvenida…",false);
 }
 void Update()
 {
  if(avanzado)return;
  if(!iniciado && Time.unscaledTime>=escucharDesde){iniciado=true;Escuchar();}
  if(captura==null || reconocedor==null)return;
  int posicion=Microphone.GetPosition(null);
  if(posicion<0){Mostrar("El micrófono se desconectó. Revisa la entrada de sonido.",true);Liberar();return;}
  int disponibles=(posicion-ultimaPosicion+captura.samples)%captura.samples;
  int pasos=0;
  while(disponibles>=Tamano && pasos++<10)
  {
   captura.GetData(bloque,ultimaPosicion);
   float energia=0;
   for(int i=0;i<Tamano;i++)
   {
    float muestra=0;
    for(int c=0;c<captura.channels;c++)muestra+=bloque[i*captura.channels+c];
    muestra/=captura.channels;
    pcm[i]=(short)Mathf.RoundToInt(Mathf.Clamp(muestra,-1,1)*32767);
    energia+=muestra*muestra;
   }
   nivel=Mathf.Sqrt(energia/Tamano);
   if(nivel>.003f)ultimaSenal=Time.unscaledTime;
   ultimaPosicion=(ultimaPosicion+Tamano)%captura.samples;
   disponibles-=Tamano;
   if(reconocedor.AcceptWaveform(pcm,pcm.Length))
   {
    var resultado=JsonUtility.FromJson<Resultado>(reconocedor.Result());
    if(resultado!=null && string.Equals(resultado.text.Trim(),"continuar",StringComparison.OrdinalIgnoreCase)){Avanzar();return;}
   }
  }
  if(Time.unscaledTime>=actualizarEstado)
  {
   actualizarEstado=Time.unscaledTime+.25f;
   if(Time.unscaledTime-ultimaSenal>7)Mostrar("Micrófono sin señal. Revisa la entrada de sonido de Windows.",true);
   else Mostrar("Escuchando en español… di «Continuar» · Mic: "+Mathf.RoundToInt(Mathf.Clamp01(nivel*12)*100)+"%",false);
  }
 }
 void Escuchar()
 {
  try
  {
   if(Microphone.devices.Length==0){Mostrar("No se detecta un micrófono. Conéctalo y vuelve a abrir la bienvenida.",true);return;}
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
   string carpeta=Path.Combine(Application.dataPath,"Plugins/x86_64");
   if(!Directory.Exists(carpeta))carpeta=Path.Combine(Application.dataPath,"Plugins");
   foreach(var dll in new[]{"libwinpthread-1.dll","libgcc_s_seh-1.dll","libstdc++-6.dll","libvosk.dll"})
    if(LoadLibrary(Path.Combine(carpeta,dll))==IntPtr.Zero)throw new Exception("No se pudo cargar "+dll+" ("+Marshal.GetLastWin32Error()+")");
#endif
   Vosk.Vosk.SetLogLevel(-1);
   modelo=new Model(Path.Combine(Application.streamingAssetsPath,"vosk-model-small-es-0.42"));
   captura=Microphone.Start(null,true,10,16000);
   if(captura==null)throw new Exception("Windows no permitió abrir el micrófono predeterminado.");
   reconocedor=new VoskRecognizer(modelo,captura.frequency,"[\"continuar\", \"[unk]\"]");
   bloque=new float[Tamano*captura.channels];pcm=new short[Tamano];ultimaPosicion=0;
   ultimaSenal=Time.unscaledTime;actualizarEstado=0;
   Mostrar("Escuchando en español… di «Continuar»",false);
   Debug.Log("LEGyo: reconocimiento LOCAL EN ESPAÑOL activo, con indicador de micrófono.");
  }
  catch(Exception error){Mostrar("No se pudo activar el micrófono en español. Revisa la consola de Unity.",true);Debug.LogError("LEGyo voz: "+error);Liberar();}
 }
 void Avanzar()
 {
  if(avanzado || siguientePantalla==null)return;
  avanzado=true;Liberar();
  if(asistente!=null)asistente.GetComponent<AudioSource>().Stop();
  siguientePantalla.SetActive(true);
  if(pantallaActual!=null)pantallaActual.SetActive(false);
  Debug.Log("LEGyo: cambio de pantalla por el comando de voz Continuar reconocido en español.");
 }
 void Mostrar(string mensaje,bool error){if(estado!=null){estado.text=mensaje;estado.color=error?new Color32(222,140,203,255):new Color32(120,232,239,255);}}
 void Liberar()
 {
  if(captura!=null){Microphone.End(null);Destroy(captura);captura=null;}
  if(reconocedor!=null){reconocedor.Dispose();reconocedor=null;}
  if(modelo!=null){modelo.Dispose();modelo=null;}
 }
 void OnDisable(){Liberar();}
 void OnDestroy(){Liberar();}
}
