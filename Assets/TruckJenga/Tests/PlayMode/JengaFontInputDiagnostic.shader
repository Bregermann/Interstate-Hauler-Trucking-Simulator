Shader "Hidden/LWS/Jenga Font Input Diagnostic"
{
 Properties { _MainTex("Actual atlas",2D)="white"{} _Color("Actual tint",Color)=(1,1,1,1) _ProbeMode("Read-only input stage",Float)=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Tags { "LightMode"="SRPDefaultUnlit" }
   Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off ZTest LEqual
   HLSLPROGRAM
   #pragma vertex Vertex
   #pragma fragment Fragment
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   CBUFFER_START(UnityPerMaterial)
    half4 _Color; float _ProbeMode;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
   Varyings Vertex(Attributes i) { Varyings o; o.positionCS=TransformObjectToHClip(i.positionOS.xyz); o.uv=i.uv; o.color=i.color; return o; }
   half4 Fragment(Varyings i):SV_Target
   {
    half4 atlas=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
    if(_ProbeMode<.5) return half4(1,1,1,1);
    if(_ProbeMode<1.5) return half4(i.color.rgb,1);
    if(_ProbeMode<2.5) return half4(_Color.rgb,1);
    if(_ProbeMode<3.5) return half4(atlas.rgb,1);
    if(_ProbeMode<4.5) return half4(atlas.aaa,1);
    if(_ProbeMode<5.5) return half4(i.color.rgb*_Color.rgb,i.color.a*_Color.a*atlas.a);
    return i.color*_Color*atlas;
   }
   ENDHLSL
  }
 }
}
