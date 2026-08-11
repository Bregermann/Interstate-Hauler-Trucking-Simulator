Shader "CompassNavigatorPro/RouteWorld"
{
	Properties
	{
		_Color ("Tint", Color) = (1,1,1,1)
		_RouteGradientColor ("Gradient Color (destination)", Color) = (1,1,1,1)
		_RouteData ("Route Data (_, _, edgeFeather, _)", Vector) = (0.1, 0.5, 1.5, 0.5)
		_RouteStyle ("Route Style (0 solid, 4 texture)", Float) = 1
		_RouteFlow ("Route Flow Speed", Float) = 1
		_ScrollOffset ("Flow Scroll", Float) = 0
		_RouteProgress ("Route Progress", Float) = -1
		_TraveledColor ("Traveled Color", Color) = (0.55,0.55,0.55,0.35)
		_TraveledSolid ("Traveled Solid (0/1)", Float) = 1
		_ZTest ("ZTest", Float) = 4
		_RouteFadeBand ("Fade Band (m)", Float) = 0
		_RouteTex ("Route Texture", 2D) = "white" {}
		_RouteTexLen ("Texture Tile Length (m)", Float) = 4
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
			Offset -1, -1
			Blend SrcAlpha OneMinusSrcAlpha

		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;    // x = arc length (m), y = cross axis [-1..1]
				float2 texcoord1: TEXCOORD1;    // x = normalized world arc along the route [0..1]
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex    : SV_POSITION;
				fixed4 color     : COLOR;
				float2 texcoord  : TEXCOORD0;
				float  routeNorm : TEXCOORD1;
				float3 worldPos  : TEXCOORD2;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			fixed4 _Color;
			fixed4 _RouteGradientColor;
			float4 _RouteData;
			float _RouteStyle;
			float _RouteFlow;
			float _ScrollOffset;
			float _RouteProgress;
			fixed4 _TraveledColor;
			float _TraveledSolid;
			float4 _RouteFade;
			float _RouteFadeBand;
			sampler2D _RouteTex;
			float _RouteTexLen;

			#define EDGE_FEATHER _RouteData.z

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_OUTPUT(v2f, OUT);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.routeNorm = IN.texcoord1.x;
				OUT.worldPos = IN.vertex.xyz;       // mesh is built in world space (identity matrix)
				// Gradient start->destination along the normalized route arc (gradient color = base color when disabled)
				OUT.color = lerp(_Color, _RouteGradientColor, IN.texcoord1.x);
				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 col = IN.color;

				float len = IN.texcoord.x;
				float cr = IN.texcoord.y;

				float craa = max(fwidth(cr) * EDGE_FEATHER, 1e-5);
				float scroll = _Time.y * _RouteFlow + _ScrollOffset;
				float edgeA = smoothstep(1.0, 1.0 - craa, abs(cr));

				float styledA;
				if (_RouteStyle > 3.5) {
					// Texture: tile a custom texture along the route (U = arc, V = across the width)
					float u = len / max(_RouteTexLen, 0.01) - scroll;
					float vtex = cr * 0.5 + 0.5;
					fixed4 t = tex2D(_RouteTex, float2(u, vtex));
					col.rgb *= t.rgb;
					styledA = t.a;
				} else {
					// Solid line
					styledA = edgeA;
				}

				// Traveled portion: optionally solid (no pattern) + traveled color (-1 disables)
				float finalA = styledA;
				if (_RouteProgress >= 0.0) {
					float remaining = smoothstep(_RouteProgress - 0.006, _RouteProgress + 0.006, IN.routeNorm);
					float travelledA = _TraveledSolid > 0.5 ? edgeA : styledA;
					finalA = lerp(travelledA, styledA, remaining);
					col.rgb = lerp(_TraveledColor.rgb, col.rgb, remaining);
					col.a *= lerp(_TraveledColor.a, 1.0, remaining);
				}
				col.a *= finalA;

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
