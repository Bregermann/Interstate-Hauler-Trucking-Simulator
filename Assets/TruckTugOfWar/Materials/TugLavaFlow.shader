Shader "LWS/TruckSports/Volcanic Lava Flow" {
Properties{_BaseColor("Crust",Color)=(0.055,0.012,0.008,1) [HDR]_EmissionColor("Molten channels",Color)=(3.2,0.45,0.015,1) _FlowSpeed("Flow",Float)=0.16 [HideInInspector]_Surface("Exclude molten surface from snow",Float)=1}
SubShader{Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"} Pass{ Name "Molten channels" Tags{"LightMode"="UniversalForward"} HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor,_EmissionColor;float _FlowSpeed,_Surface;
CBUFFER_END
struct Attributes{float4 positionOS:POSITION;};struct Varyings{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;};
Varyings vert(Attributes i){Varyings o;o.world=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);return o;}
float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
half4 frag(Varyings i):SV_Target{float t=_Time.y*_FlowSpeed;float2 uv=i.world.xz*.075+float2(t*.15,t*.05);float n=noise(uv)*.65+noise(uv*2.3+t*.09)*.25+noise(uv*5.7)*.1;float seam=1-smoothstep(.05,.19,abs(n-.48));float pulse=.8+.2*sin(t+uv.x*.4);float3 color=_BaseColor.rgb*(.65+n*.8)+_EmissionColor.rgb*seam*pulse;return half4(color,1);}
ENDHLSL}}
}
