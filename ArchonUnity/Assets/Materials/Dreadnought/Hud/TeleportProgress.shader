Shader "Unlit/TeleportProgress"
{
    Properties
    {
        _Progress ("Progress", Vector) = (0.5,1,1,0)
        _Scale("Scale", Range(0.1,10)) = 1
        _FadeIn("FadeIn", Range(0,1)) = 1
        _Flash("Flash", Range(0,1)) = 0
        [HDR]_Color("Color", Color) = (1,1,1,1)
        
    }
    SubShader
    {
        Tags { "Queue"="Transparent+1000" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Lighting Off 
        Cull Off
        Lighting Off 
        ZWrite Off 
        ZTest Off
        Fog { Color (0,0,0,0) }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            #define M_PI 3.14159265359

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal: NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv: TEXCOORD0;

            };

            float4 _Progress;
            float4 _Color;
            float _Scale;
            float _FadeIn;
            float _Flash;

            v2f vert (appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);
                //mul(UNITY_MATRIX_VP,float4(world,1));
                o.uv = v.uv;


                return o;
            }

            float dd(float v)
            {
                return max(abs(ddx(v)),abs(ddy(v)));
            }

            float hardRange(float begin, float end, float value, float valueDD)
            {
                return (1.0 - smoothstep(end-valueDD*2, end,value)) * smoothstep(begin, begin+valueDD*2, value);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 c = (float4)1;
                c.rg = i.uv;

                float alpha = 1;
                
                float2 xy = i.uv * 2 - 1;
                float r = length(xy);
                float rd = dd(r);
                
                alpha = hardRange(0.8,0.99,r, rd);


                float circularAngle = atan2(xy.y,xy.x);

                float radial = 0.5 + 0.5 * sin(circularAngle * 3 + _Time.w);
                float radialDD = dd(radial);

                if (_Progress.z < 0.5)
                {

                }
                else
                {
                    float circularAngleOne = (circularAngle + M_PI) / (2* M_PI);
                    float circular2Fmod = fmod(circularAngleOne*2,1);
                    float circular2FmodDD = dd(circular2Fmod);
                    float flash = _Flash;
                    float relHealth = _Progress.x / _Progress.y;
                    float radialH = hardRange(0, 0.1 + 0.9 * relHealth, circular2Fmod, circular2FmodDD);
                    //float d2 = max(abs(ddx(radial)),abs(ddy(radial)));
                    alpha *= radialH;
                    c.rgb = _Color.rgb
                            * hardRange(0.85,0.94, r, rd)
                            * hardRange(0.02, 0.1 + 0.9 * relHealth-0.02, circular2Fmod, circular2FmodDD)
                            * ((1 - flash) + (cos(_Time.z* /* flash* */5)*0.5 + 0.5)*flash)
                             ;

                    
                }

                c.a = alpha * _FadeIn;
                return c;
               
            }
            ENDCG
        }
    }
}
