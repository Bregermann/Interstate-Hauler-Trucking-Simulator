Shader "LWS/TruckSkeeBall/Arcade Pixels"
{
 Properties { [PerRendererData] _MainTex("Original camera feed",2D)="white" {} _Color("Tint",Color)=(1,1,1,1) }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
  Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   struct Input { float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
   struct Varying { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR; };
   sampler2D _MainTex;float4 _MainTex_TexelSize;float4 _Color;
   Varying vert(Input v){Varying o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   fixed4 frag(Varying i):SV_Target
   {
    float2 pixel=floor(i.uv*_MainTex_TexelSize.zw);float2 uv=(pixel+.5)*_MainTex_TexelSize.xy;
    float4 color=tex2D(_MainTex,uv);float3 palette=saturate(color.rgb);
    // The sRGB camera texture is sampled in linear light in a Linear project.
    // Quantize perceived display levels, then return linear light exactly once
    // for the existing UI/backbuffer encoding. Linear quantization erased dark art.
    #ifndef UNITY_COLORSPACE_GAMMA
    palette=float3(LinearToGammaSpaceExact(palette.r),LinearToGammaSpaceExact(palette.g),LinearToGammaSpaceExact(palette.b));
    #endif
    palette=floor(palette*31+.5)/31;
    #ifndef UNITY_COLORSPACE_GAMMA
    palette=float3(GammaToLinearSpaceExact(palette.r),GammaToLinearSpaceExact(palette.g),GammaToLinearSpaceExact(palette.b));
    #endif
    color.rgb=palette;
    color.rgb*=lerp(.94,1,step(.5,frac(pixel.y*.5)));return fixed4(color.rgb,1)*i.color;
   }
   ENDHLSL
  }
 }
}
