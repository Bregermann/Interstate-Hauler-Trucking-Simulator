#ifndef TAXI_ENVIRONMENT_SNOW_INCLUDED
#define TAXI_ENVIRONMENT_SNOW_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// The existing Weatherade volume and trace generator publish these globals.
TEXTURE2D(_SRS_depth);
TEXTURE2D(_SRS_TraceTex);
TEXTURE2D(_TaxiSnowTexture);
SAMPLER(sampler_TaxiSnowLinearClamp);
SAMPLER(sampler_TaxiSnowLinearRepeat);
float4x4 _depthCamMatrix, _SRS_TraceSurfCamMatrix;
float _depthCamFarHeight, _VsmExp;
float _TaxiSnowCoverage, _TaxiSnowTracesReady;
float4 _TaxiSnowExposure;

half TaxiSnowExposure(float3 positionWS)
{
    float3 position = positionWS;
    position.y = (position.y + _depthCamFarHeight) * .5;
    float3 projected = mul(_depthCamMatrix, float4(position, 1)).xyz;
    float2 uv = .5 + projected.xy * .5;
    // Never wrap the depth map onto an unrelated building/interior outside the volume.
    if (_TaxiSnowExposure.w < .5) return 0;
    if (any(uv <= .005) || any(uv >= .995) || projected.z < -1 || projected.z > 1)
    {
        #if defined(TAXI_SNOW_EXTERIOR)
            return 1;
        #else
            return 0;
        #endif
    }
    float2 moments = SAMPLE_TEXTURE2D(_SRS_depth, sampler_TaxiSnowLinearClamp, uv).rg;
    float z = 1 - projected.z;
    float warped = exp(_VsmExp * (-2 * z - 1));
    float bias = _TaxiSnowExposure.x * _VsmExp * warped;
    float variance = max(moments.y - moments.x * moments.x, max(bias * bias, 1e-12));
    float difference = warped - moments.x;
    float bound = variance / (variance + difference * difference);
    bound = saturate((bound - _TaxiSnowExposure.z) / max(1 - _TaxiSnowExposure.z, .001));
    float mask = warped <= moments.x ? 1 : bound;
    return sqrt(saturate(smoothstep(0, max(10 * _TaxiSnowExposure.y, .001), 5 * mask) * 2));
}

void TaxiApplyEnvironmentSnow(InputData input, inout SurfaceData surface)
{
    if (_TaxiSnowCoverage <= .001) return;
    // Derivatives follow wind-deformed geometry; preserve the authored normal-map detail.
    float3 geometric = cross(ddy(input.positionWS), ddx(input.positionWS));
    geometric *= rsqrt(max(dot(geometric, geometric), 1e-12));
    geometric *= dot(geometric, input.normalWS) < 0 ? -1 : 1;
    half upward = smoothstep(.12, .75, geometric.y);
    #if defined(TAXI_SNOW_FOLIAGE)
        upward = max(upward, smoothstep(.15, .8, input.normalWS.y) * .8);
    #endif
    half exposure = TaxiSnowExposure(input.positionWS);
    half4 snow = SAMPLE_TEXTURE2D(_TaxiSnowTexture, sampler_TaxiSnowLinearRepeat, input.positionWS.xz * .3);
    half mask = saturate((_TaxiSnowCoverage * 1.4 - (1 - snow.b) * .5) * 2);
    mask *= upward * exposure;
    half trace = 0;
    #if defined(TAXI_SNOW_TRACK_SURFACE)
        float3 tracePosition = mul(_SRS_TraceSurfCamMatrix, float4(input.positionWS, 1)).xyz;
        float2 traceUV = .5 + tracePosition.xy * .5;
        traceUV.y = 1 - traceUV.y; // Weatherade's trace texture uses the opposite vertical UV.
        if (_TaxiSnowTracesReady > .5 && all(traceUV > .005) && all(traceUV < .995))
            trace = SAMPLE_TEXTURE2D(_SRS_TraceTex, sampler_TaxiSnowLinearClamp, traceUV).b;
    #endif
    half3 snowColor = lerp(half3(.88, .92, .95), half3(.35, .41, .45), trace * .75);
    snowColor *= lerp(.88, 1, snow.b);
    surface.albedo = lerp(surface.albedo, snowColor, mask);
    surface.metallic *= 1 - mask;
    surface.specular = lerp(surface.specular, half3(.04, .04, .04), mask);
    surface.smoothness = lerp(surface.smoothness, .32, mask);
    // Alpha, wind, normal maps, detail, AO and emission remain vendor-owned.
}

half4 TaxiSnowFragmentPBR(InputData input, SurfaceData surface)
{
    TaxiApplyEnvironmentSnow(input, surface);
    return UniversalFragmentPBR(input, surface);
}

half4 TaxiSnowFragmentBlinnPhong(InputData input, SurfaceData surface)
{
    TaxiApplyEnvironmentSnow(input, surface);
    return UniversalFragmentBlinnPhong(input, surface);
}

#define UniversalFragmentPBR TaxiSnowFragmentPBR
#define UniversalFragmentBlinnPhong TaxiSnowFragmentBlinnPhong
#endif
