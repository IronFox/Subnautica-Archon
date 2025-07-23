Shader "Unlit/Elements"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+1001" "IgnoreProjector"="True" "RenderType"="Transparent" }
        ZWrite Off 
        Blend SrcAlpha OneMinusSrcAlpha
        Fog { Color (0,0,0,0) }
        LOD 100
        Lighting Off 

        Pass
        {

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

            float4 _Color;

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
                float fresnel = saturate(1+dot(view, i.normal) / length(view));
                fresnel = pow(fresnel, 2.0);
                return float4(_Color.rgb,fresnel*2);
            }
            ENDCG
        }
    }
}
