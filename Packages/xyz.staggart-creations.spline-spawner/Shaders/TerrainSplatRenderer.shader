Shader "Hidden/TerrainSplatRenderer"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        
        ZWrite Off
        Cull Off


        Pass
        {
            Name "Output terrain splats"
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 input = tex2D(_MainTex, i.uv);

                return input;
            }
            
            ENDHLSL
        }
    }
}