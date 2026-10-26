// Extends the existing owned TugLavaFlow world-space channel foundation.
// URP owns rendering; heat refraction uses its existing particle distortion.
Shader "LWS/TruckSports/Carnival Molten Surface"
{
    Properties
    {
        _BaseColor("Cooling basalt", Color) = (.045,.009,.006,1)
        [HDR] _EmissionColor("Deep molten red", Color) = (4.2,.3,.012,1)
        [HDR] _HotColor("Hot yellow currents", Color) = (7,2.4,.08,1)
        _FlowSpeed("Flow speed", Float) = .45
        _PatternScale("World pattern scale", Float) = .065
        _ChannelWidth("Molten channel width", Range(.02,.3)) = .16
        [HideInInspector] _LavaTime("Presentation time", Float) = 0
        [HideInInspector] _Surface("Exclude molten surface from snow", Float) = 1
        [HideInInspector] _Impact0("Impact 0", Vector) = (0,0,-100,0)
        [HideInInspector] _Impact1("Impact 1", Vector) = (0,0,-100,0)
        [HideInInspector] _Impact2("Impact 2", Vector) = (0,0,-100,0)
        [HideInInspector] _Impact3("Impact 3", Vector) = (0,0,-100,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "Advected currents, churning cells and impact rings"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor, _EmissionColor, _HotColor, _Impact0, _Impact1, _Impact2, _Impact3;
            float _LavaTime, _FlowSpeed, _PatternScale, _ChannelWidth, _Surface;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            Varyings vert(Attributes input)
            {
                Varyings output; UNITY_SETUP_INSTANCE_ID(input); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.world=TransformObjectToWorld(input.positionOS.xyz); output.positionCS=TransformWorldToHClip(output.world); return output;
            }
            float hash21(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise21(float2 p)
            {
                float2 cell=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(hash21(cell),hash21(cell+float2(1,0)),f.x),lerp(hash21(cell+float2(0,1)),hash21(cell+1),f.x),f.y);
            }
            float ripple(float2 world, float4 impact)
            {
                float age=_LavaTime-impact.z;
                if (impact.w < .5 || age < 0 || age > 3.5) return 0;
                float valid=step(0,age)*(1-step(3.5,age))*impact.w;
                float distance=length(world-impact.xy), ring=abs(distance-age*6.5);
                return valid*(1-smoothstep(.25,1.1,ring))*saturate(1-age/3.5);
            }
            half4 frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float t=_LavaTime*_FlowSpeed;
                float2 uv=input.world.xz*_PatternScale;
                float2 warp=float2(noise21(uv*.63+t*.07),noise21(uv*.63-t*.055+19.3))-.5;
                float2 currentA=uv+warp*.9+float2(t*.16,t*.055);
                float2 currentB=uv*2.17-warp*.65+float2(-t*.09,t*.12);
                float molten=noise21(currentA)*.7+noise21(currentB)*.3;
                float channels=1-smoothstep(_ChannelWidth*.19,_ChannelWidth,abs(molten-.49));
                float temperature=saturate(noise21(currentA*.73-t*.08)*1.45-.23);
                float2 cells=uv*.65, cell=floor(cells), local=frac(cells)-.5;
                float seed=hash21(cell), phase=frac(t*.22+seed*11);
                float radius=.035+.22*smoothstep(.15,.9,phase);
                float bubble=(1-smoothstep(.012,.045,abs(length(local)-radius)))*smoothstep(.03,.2,phase)*(1-smoothstep(.82,.99,phase));
                float burst=step(.87,seed)*pow(saturate(1-abs(phase-.86)*14),2);
                float impacts=ripple(input.world.xz,_Impact0)+ripple(input.world.xz,_Impact1)+ripple(input.world.xz,_Impact2)+ripple(input.world.xz,_Impact3);
                float pulse=.88+.12*sin(t*.7+uv.x*.4-uv.y*.3);
                float3 crust=_BaseColor.rgb*(.65+molten*.65);
                float3 heat=lerp(_EmissionColor.rgb,_HotColor.rgb,temperature);
                float3 color=crust+heat*channels*pulse+_HotColor.rgb*(bubble*.28+burst*.8+impacts*.8);
                return half4(color,1);
            }
            ENDHLSL
        }
    }
}
