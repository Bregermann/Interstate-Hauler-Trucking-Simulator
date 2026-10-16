Shader "Hidden/TruckSports/BogCameraUnderwater" {
 SubShader { Tags {"RenderPipeline"="UniversalPipeline"} ZWrite Off ZTest Always Cull Off
 Pass { Name "CameraLocalDepthFog" HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
 float _BogSubmersion,_BogClock;float4 _BogViewport;
 half4 Frag(Varyings input):SV_Target {
 float2 uv=input.texcoord;half3 original=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv).rgb;if(any(uv<_BogViewport.xy)||any(uv>_BogViewport.xy+_BogViewport.zw))return half4(original,1);
 float raw=SampleSceneDepth(uv);float distance=LinearEyeDepth(raw,_ZBufferParams);
 float2 bend=float2(sin(uv.y*26+_BogClock*1.8),cos(uv.x*21-_BogClock*1.3))*.0017*saturate(_BogSubmersion*2);
 half3 scene=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,clamp(uv+bend,.001,.999)).rgb;
 float density=1-exp(-min(distance,120)*(.047+min(_BogSubmersion,4)*.004));
 half3 fog=half3(.09,.23,.21);float shaft=.92+.08*sin(uv.x*38+_BogClock*.25);
 return half4(lerp(scene*half3(.86,.96,.92)*shaft,fog,min(.91,density)),1);
 }
 ENDHLSL }
 }
}
