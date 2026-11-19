Shader "LEGyo/Robot con parpadeo"
{
 Properties
 {
  [PerRendererData] _MainTex ("Robot",2D)="white" {}
  _Color ("Tint",Color)=(1,1,1,1)
  _Blink ("Parpadeo",Range(0,1))=0
  _Talk ("Boca",Range(0,1))=0
  _StencilComp ("Stencil Comparison",Float)=8
  _Stencil ("Stencil ID",Float)=0
  _StencilOp ("Stencil Operation",Float)=0
  _StencilWriteMask ("Stencil Write Mask",Float)=255
  _StencilReadMask ("Stencil Read Mask",Float)=255
  _ColorMask ("Color Mask",Float)=15
  [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip",Float)=0
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"}
  Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha ColorMask [_ColorMask]
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
   struct v2f {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float4 local:TEXCOORD1;};
   sampler2D _MainTex;fixed4 _Color;float _Blink;float _Talk;float4 _ClipRect;
   v2f vert(appdata v){v2f o;o.local=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    float2 uv=i.uv;fixed4 c=tex2D(_MainTex,uv);
    float dx=min(abs(uv.x-.386),abs(uv.x-.614));
    float dy=uv.y-.51;
    float mascara=(1-smoothstep(.044,.068,dx))*(1-smoothstep(.105,.145,abs(dy)));
    float apertura=max(.035,1-_Blink*.965);
    float remapeado=dy/apertura;
    fixed4 fondo=tex2D(_MainTex,float2(uv.x,.635));
    fixed4 ojo=tex2D(_MainTex,float2(uv.x,.51+clamp(remapeado,-.11,.11)));
    ojo=lerp(ojo,fondo,smoothstep(.095,.115,abs(remapeado)));
    c=lerp(c,ojo,mascara*saturate(_Blink*12));
    // The illuminated mouth opens with the actual audio amplitude.
    float2 boca=uv-float2(.5,.432);
    float zona=(1-smoothstep(.065,.09,abs(boca.x)))*(1-smoothstep(.065,.095,abs(boca.y)));
    fixed4 visor=tex2D(_MainTex,float2(.5,.515));
    float radioY=.012+_Talk*.031;
    float distancia=length(boca/float2(.043,radioY));
    float trazo=1-smoothstep(.07,.18,abs(distancia-1));
    float halo=exp(-abs(distancia-1)*7)*.22;
    fixed4 hablando=visor;
    hablando.rgb+=float3(.25,.9,1)*halo;
    hablando.rgb=lerp(hablando.rgb,float3(.48,1,1),trazo);
    c=lerp(c,hablando,zona*smoothstep(.005,.03,_Talk));
    c*=i.color;
    #ifdef UNITY_UI_CLIP_RECT
    c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
    clip(c.a-.001);
    #endif
    return c;
   }
   ENDCG
  }
 }
}
