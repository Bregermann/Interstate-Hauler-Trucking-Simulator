Shader "CompassNavigatorPro/RouteOverlay"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_MaskTex ("Minimap Mask", 2D) = "white" {}
		_RouteData ("Route Data (tiling, _, edgeFeather, _)", Vector) = (0.1, 0.5, 1.5, 0.5)
		_RouteStyle ("Route Style (0 solid, 4 texture)", Float) = 1
		_RouteFlow ("Route Flow Speed", Float) = 1
		_ScrollOffset ("Flow Scroll", Float) = 0
		_RouteProgress ("Route Progress", Float) = -1
		_TraveledColor ("Traveled Color", Color) = (0.55,0.55,0.55,0.35)
		_TraveledSolid ("Traveled Solid (0/1)", Float) = 1
		_RouteTex ("Route Texture", 2D) = "white" {}

		_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255

		_ColorMask ("Color Mask", Float) = 15

		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Stencil
		{
			Ref [_Stencil]
			Comp [_StencilComp]
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest [unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass
		{
			Name "Default"
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
			#pragma multi_compile_local _ UNITY_UI_ALPHACLIP

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				float2 maskcoord: TEXCOORD1;
				float2 uv2      : TEXCOORD2;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex        : SV_POSITION;
				fixed4 color         : COLOR;
				float2 texcoord      : TEXCOORD0;
				float2 maskcoord     : TEXCOORD1;
				float4 worldPosition : TEXCOORD2;
				float  routeNorm     : TEXCOORD3;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MaskTex;
			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;
			float4 _RouteData;
			float _RouteStyle;
			float _RouteFlow;
			float _ScrollOffset;
			float _RouteProgress;
			fixed4 _TraveledColor;
			float _TraveledSolid; sampler2D _RouteTex;

			#define TILING       _RouteData.x
			#define EDGE_FEATHER _RouteData.z

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_OUTPUT(v2f, OUT);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				OUT.worldPosition = IN.vertex;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;       // x = arc length, y = cross axis [-1..1]
				OUT.maskcoord = IN.maskcoord;     // normalized minimap position [0..1]
				OUT.routeNorm = IN.uv2.x;         // normalized world arc along the route [0..1]
				OUT.color = IN.color * _Color;
				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 col = IN.color;

				float len = IN.texcoord.x;
				float cr = IN.texcoord.y;

				// Solid line or tiled texture (flow animates from _Time)
				float craa = max(fwidth(cr) * EDGE_FEATHER, 1e-5);
				float scroll = _Time.y * _RouteFlow + _ScrollOffset;
				float edgeA = smoothstep(1.0, 1.0 - craa, abs(cr));   // solid ribbon shape

				float styledA;
				if (_RouteStyle > 3.5) {
					// Texture: tile a custom texture along the route
					float u = len * TILING - scroll;
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

				// Circular / box clip using the active minimap mask (antialiased)
				float m = tex2D(_MaskTex, IN.maskcoord).a;
				float maa = max(fwidth(m), 1e-5);
				col.a *= smoothstep(0.5 - maa, 0.5 + maa, m);

				#ifdef UNITY_UI_CLIP_RECT
					col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
				#endif

				#ifdef UNITY_UI_ALPHACLIP
					clip (col.a - 0.001);
				#endif

				return col;
			}
		ENDCG
		}
	}
}
