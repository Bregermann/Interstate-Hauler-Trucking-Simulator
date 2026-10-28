// Unity URP 17.4.0 package sample: Samples~/URPPackageSamples/SharedAssets/Shaders/3DText.shader.
// Shader body is unchanged; only the project-owned shader name differs.
// com.unity.render-pipelines.universal copyright (c) 2020 Unity Technologies ApS.
// Licensed under the Unity Companion License for Unity-dependent projects:
// https://unity.com/legal/licenses/unity-companion-license
// Unless expressly provided otherwise, supplied AS IS, without warranties or conditions.
// The font atlas is Alpha8: primary RGB is preserved; texture supplies alpha only.
Shader "LWS/Truck Sports/Depth Tested World Font" {
    Properties {
        _MainTex ("Font Texture", 2D) = "white" {}
        _Color ("Text Color", Color) = (1,1,1,1)
    }

    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Lighting Off Cull Back ZWrite Off Fog { Mode Off }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass {
            Color [_Color]
            SetTexture [_MainTex] {
                combine primary, texture * primary
            }
        }
    }
}
