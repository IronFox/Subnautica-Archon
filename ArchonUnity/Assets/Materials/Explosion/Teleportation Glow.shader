Shader "Unlit/Teleportation Glow"
{
    Properties
    {
        _Noise0 ("Noise0", 2D) = "white" {}
        _Noise1 ("Noise1", 2D) = "white" {}
        _Noise2 ("Noise2", 2D) = "white" {}
        _Seconds ("Seconds", Float) = 0.0
        _Opacity ("Opacity", Float) = 1.0
        _Exponent ("Exponent", Float) = 4.0
        _WorldToUvScale ("World to UV Scale", Float) = 0.01
        _Belt ("Belt", Range (0,1)) = 0.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+800" "IgnoreProjector"="True" "RenderType"="Transparent" }

        Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }
        LOD 100

        
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
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
                float3 world: TEXCOORD2;
                float4 vertex : SV_POSITION;
            };


            sampler2D _Noise0;
            sampler2D _Noise1;
            sampler2D _Noise2;
            float _Seconds;
            float _Opacity;
            float _Exponent;
            float _WorldToUvScale;
            float _Belt;
            float _Seed;
            float4 _Center;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 color = float4(0,0,0,1.0);
                //color.rg = i.uv;
                // // sample the texture
                // fixed4 col = tex2D(_MainTex, i.uv);
                // // apply fog
                // UNITY_APPLY_FOG(i.fogCoord, col);
                float t = _Time.x*2;
                float3 w = (i.world - _Center.xyz)*_WorldToUvScale;
                //return float4(fmod(w*0.1,1.0),1.0);
                float noise = saturate((
                            tex2D(_Noise2, w.xy  + _Seed * float2(2,0.02)*t ).r 
                             + tex2D(_Noise1, w.xz + _Seed + float2(-1,0.01)*t).r
                             + tex2D(_Noise0, w.yz + _Seed * float2(-0.13,-1.8)*t).r
                            
                            
                            )/3);

                float b1 = 1-abs(i.uv.y-0.5)*2;
                float b2 = 1.0 - b1;
                float belt = lerp(b1, b2, _Belt);

                float intensity = pow(
                    saturate(belt + noise * 0.75),
                    _Exponent
                    );

                    // pow(saturate(((abs(i.uv.y-0.5)*2)+noise * 0.5)),_Exponent);
                    //pow(noise, _Exponent);

                color.a = intensity * _Opacity;

                clip(intensity-0.1);


                return color;
            }
            ENDCG
        }

        Pass
        {
            Blend SrcAlpha One
            ZWrite Off 
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
                float3 world: TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _Noise0;
            sampler2D _Noise1;
            sampler2D _Noise2;
            float _Seconds;
            float _Opacity;
            float _Exponent;
            float _WorldToUvScale;
            float _Belt;
            float _Seed;
            float4 _Center;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            float sqr(float f)
            {
                return f * f;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 color = float4(1,1.2,2.0,1.0);
                //color.rg = i.uv;
                // // sample the texture
                // fixed4 col = tex2D(_MainTex, i.uv);
                // // apply fog
                // UNITY_APPLY_FOG(i.fogCoord, col);
                float t = _Time.x*2;
                float3 w = (i.world - _Center.xyz)*_WorldToUvScale;
                //return float4(fmod(w*0.1,1.0),1.0);
                float noise = saturate((
                            tex2D(_Noise2, w.xy  + _Seed * float2(2,0.02)*t ).r 
                             + tex2D(_Noise1, w.xz + _Seed + float2(-1,0.01)*t).r
                             + tex2D(_Noise0, w.yz + _Seed * float2(-0.13,-1.8)*t).r
                            
                            
                            )/3);

                float b1 = 1-abs(i.uv.y-0.5)*2;
                float b2 = 1.0 - b1;
                float belt = lerp(b1, b2, _Belt);

                float intensity = pow(
                    saturate(belt + noise * 0.5),
                    _Exponent
                    );

                    // pow(saturate(((abs(i.uv.y-0.5)*2)+noise * 0.5)),_Exponent);
                    //pow(noise, _Exponent);

                color.a = intensity * _Opacity* 0.75;

                clip(intensity-0.1);


                return color;
            }
            ENDCG
        }
    }
}
