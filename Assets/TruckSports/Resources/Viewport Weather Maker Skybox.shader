Shader "LWS/TruckSports/Weather Maker Viewport Skybox"
{
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #define WEATHER_MAKER_ENABLE_TEXTURE_DEFINES
            #define WEATHER_MAKER_SKY_BOX_SHADER
            #include "Assets/WeatherMaker/Prefab/Shaders/WeatherMakerSkyBoxShaderInclude.cginc"

            // Unity owns viewport coverage. Installed Weather Maker remains
            // authoritative for atmospheric scattering, textures, stars and tint.
            struct Attributes { float4 position : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 position : SV_POSITION; float3 direction : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.position = UnityObjectToClipPos(input.position);
                output.direction = mul((float3x3)unity_ObjectToWorld, input.position.xyz);
                return output;
            }
            fixed4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float3 direction = normalize(input.direction);
                float2 uv = float2(frac(atan2(direction.x, direction.z) * -.1591549431), asin(clamp(direction.y, -1, 1)) * .3183098862 + .5);
                return ComputeSkySphereColorFullScreen(direction, uv, float2(0, 0), 1, false, true);
            }
            ENDCG
        }
    }
    Fallback Off
}
