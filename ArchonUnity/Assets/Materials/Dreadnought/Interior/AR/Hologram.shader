Shader "Unlit/Hologram"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        LOD 100
        
        Pass
        {
            Tags { "Queue"="Transparent+9999" }
            //Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            Blend One Zero
            CGPROGRAM


            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return float4(0,0,0,1);
            }
            ENDCG
        }

        Pass
        {
            Blend SrcAlpha One
            //ZWrite Off
            Tags { "Queue"="Transparent+10000" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float3 world : TEXCOORD0;
                float3 view : TEXCOORD1;
                float3 normal: NORMAL;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.view = mul(UNITY_MATRIX_V, float4(o.world, 1.0)).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal.xyz);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 view = i.world - _WorldSpaceCameraPos;
                float fresnel = 1+dot(view, i.normal) / length(view);
                fresnel = pow(fresnel, 2.0);
                return float4((float3)fresnel,1);
            }
            ENDCG
        }
    }
}
