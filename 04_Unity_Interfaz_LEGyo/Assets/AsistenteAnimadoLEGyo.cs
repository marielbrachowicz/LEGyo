using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage), typeof(AudioSource))]
public class AsistenteAnimadoLEGyo : MonoBehaviour
{
    public AudioClip bienvenida;
    RectTransform cabeza;
    RawImage imagen;
    AudioSource voz;
    Material materialOjos;
    Vector2 posicionInicial;
    Vector3 escalaInicial;
    Quaternion rotacionInicial;
    float inicio, siguienteParpadeo, inicioParpadeo = -100;
    bool inicializado;
    readonly float[] muestras = new float[256];
    float aperturaBoca;

    void Awake()
    {
        cabeza = GetComponent<RectTransform>();
        imagen = GetComponent<RawImage>();
        voz = GetComponent<AudioSource>();
        posicionInicial = cabeza.anchoredPosition;
        escalaInicial = cabeza.localScale;
        rotacionInicial = cabeza.localRotation;
        var shader = Shader.Find("LEGyo/Robot con parpadeo");
        if (shader != null) { materialOjos = new Material(shader); imagen.material = materialOjos; }
        voz.playOnAwake = false;
        voz.loop = false;
        voz.spatialBlend = 0;
        voz.volume = .85f;
        inicializado = true;
    }

    void OnEnable()
    {
        if (!inicializado) return;
        inicio = Time.unscaledTime;
        inicioParpadeo = -100;
        siguienteParpadeo = inicio + 1.8f;
        voz.clip = bienvenida;
        if (bienvenida != null) voz.PlayDelayed(.6f);
    }

    void Update()
    {
        float ahora = Time.unscaledTime, t = ahora - inicio;
        cabeza.anchoredPosition = posicionInicial + new Vector2(Mathf.Sin(t*.65f)*3f, Mathf.Sin(t*1.25f)*9f);
        cabeza.localRotation = rotacionInicial * Quaternion.Euler(0,0,Mathf.Sin(t*.85f)*3.5f);
        float asentir = voz.isPlaying ? Mathf.Sin(t*2.2f)*.014f : Mathf.Sin(t*.75f)*.006f;
        cabeza.localScale = Vector3.Scale(escalaInicial,new Vector3(1,1+asentir,1));
        if (ahora >= siguienteParpadeo)
        {
            inicioParpadeo = ahora;
            siguienteParpadeo = ahora + Random.Range(2.7f,5.1f);
        }
        float fase = (ahora-inicioParpadeo)/.24f;
        float parpadeo = fase>=0 && fase<=1 ? Mathf.Sin(fase*Mathf.PI) : 0;
        if (materialOjos != null) materialOjos.SetFloat("_Blink",parpadeo);
        float nivel = 0;
        if (voz.isPlaying)
        {
            voz.GetOutputData(muestras,0);
            for (int i=0;i<muestras.Length;i++) nivel += muestras[i]*muestras[i];
            nivel = Mathf.Clamp01(Mathf.Sqrt(nivel/muestras.Length)*12f);
        }
        aperturaBoca = Mathf.MoveTowards(aperturaBoca,nivel,Time.unscaledDeltaTime*7f);
        if(materialOjos!=null)materialOjos.SetFloat("_Talk",aperturaBoca);
    }

    void OnDisable()
    {
        if (!inicializado) return;
        voz.Stop();
        cabeza.anchoredPosition = posicionInicial;
        cabeza.localRotation = rotacionInicial;
        cabeza.localScale = escalaInicial;
        if(materialOjos!=null)materialOjos.SetFloat("_Blink",0);
        if(materialOjos!=null)materialOjos.SetFloat("_Talk",0);
        aperturaBoca = 0;
    }
    void OnDestroy() { if(materialOjos!=null)Destroy(materialOjos); }
}
