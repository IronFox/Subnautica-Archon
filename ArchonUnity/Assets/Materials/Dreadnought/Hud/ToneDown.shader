Shader "Unlit/ToneDown"
{
    Properties
    {
        _Opacity ("Opacity", Range(0, 1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent+850" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest Off
        Lighting Off 

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
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float _Opacity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float r = length((i.uv - (float2)0.5)*2.0);
                float a = smoothstep(0.5,1.0,r);
                return float4(0,0,0,(1-a)*_Opacity);
            }
            ENDCG
        }
    }
}
