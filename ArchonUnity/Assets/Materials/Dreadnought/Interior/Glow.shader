Shader "Unlit/Glow"
{
    Properties
    {
        _EmissionColor ("Emission Color", Color) = (1,1,1,1)

    }
    SubShader
    {
        Tags { "RenderType"="Opaque+1000" }
        LOD 100

        Pass
        {
            //Blend One One
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

            float4 _EmissionColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return float4(_EmissionColor.rgb,1);
            }
            ENDCG
        }
    }
}
