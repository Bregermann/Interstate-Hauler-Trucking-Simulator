Shader "Hidden/LWS/TruckTaxi/MushroomWorldWave"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            float _Strength;
            float _UVAmplitude;
            float _WaveHertz;
            half4 Frag(Varyings input) : SV_Target0
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord.xy;
                float phase = _Time.y * _WaveHertz * 6.2831853;
                float2 wave = float2(
                    sin(uv.y * 12.0 + phase) * .58 + sin(uv.y * 25.0 - phase * .67) * .28,
                    sin(uv.x * 10.0 - phase * .81) * .48 + sin(uv.x * 19.0 + phase * .53) * .28);
                uv = saturate(uv + wave * (_UVAmplitude * _Strength));
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, _BlitMipLevel);
            }
            ENDHLSL
        }
    }
}
