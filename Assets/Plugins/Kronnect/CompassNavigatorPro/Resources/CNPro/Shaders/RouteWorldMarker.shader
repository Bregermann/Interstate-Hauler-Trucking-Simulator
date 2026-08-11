Shader "CompassNavigatorPro/RouteWorldMarker"
{
	Properties
	{
		_MainTex ("Icon", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_ZTest ("ZTest", Float) = 4
		_RouteFade ("Fade (centerX, centerZ, maxDistance, minDistance)", Vector) = (0,0,0,0)
		_RouteFadeBand ("Fade Band (m)", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"DisableBatching"="True"
		}

		Pass
		{
			Name "Default"
			Cull Off
			Lighting Off
			ZWrite Off
			ZTest [_ZTest]
			Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float3 worldPos : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			fixed4 _Color;
			float4 _RouteFade;
			float _RouteFadeBand;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_OUTPUT(v2f, OUT);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				// Billboard: place the quad center in view space, then offset by the quad corner scaled by the per-object matrix scale (faces every camera)
				float3 center = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
				float sx = length(float3(unity_ObjectToWorld._m00, unity_ObjectToWorld._m10, unity_ObjectToWorld._m20));
				float sy = length(float3(unity_ObjectToWorld._m01, unity_ObjectToWorld._m11, unity_ObjectToWorld._m21));
				float4 viewCenter = mul(UNITY_MATRIX_V, float4(center, 1.0));
				viewCenter.xy += IN.vertex.xy * float2(sx, sy);
				OUT.vertex = mul(UNITY_MATRIX_P, viewCenter);
				OUT.texcoord = TRANSFORM_TEX(IN.texcoord, _MainTex);
				OUT.worldPos = center;
				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, IN.texcoord) * _Color;

				// Distance fade around the follow target (max in .z fades out far; min in .w hides it nearer than minDistance, with an optional fade band; 0 = off)
				if (_RouteFade.z > 0.0 || _RouteFade.w > 0.0) {
					float dC = distance(IN.worldPos.xz, _RouteFade.xy);
					if (_RouteFade.z > 0.0) col.a *= 1.0 - smoothstep(_RouteFade.z * 0.8, _RouteFade.z, dC);
					if (_RouteFade.w > 0.0) col.a *= (_RouteFadeBand > 0.0001) ? smoothstep(_RouteFade.w, _RouteFade.w + _RouteFadeBand, dC) : step(_RouteFade.w, dC);
				}

				clip(col.a - 0.001);
				return col;
			}
		ENDCG
		}
	}
}
