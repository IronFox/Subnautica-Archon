Shader "Unlit/ArElement"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent+900" }
        Blend SrcAlpha One // Additive blending
        LOD 100

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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float3 normal : NORMAL;
                float4 vertex : SV_POSITION;
            };

            float4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float fresnel = i.normal.z;
                //dot(i.normal, normalize(_WorldSpaceCameraPos - i.vertex.xyz));
                // sample the texture
                fixed4 col = _Color;
                col.a =0.1 + 0.9 * pow(fresnel,10);
                return col;
            }
            ENDCG
        }
    }
}
