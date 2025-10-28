// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Custom/Map"
{
    Properties
    {
        _Color ("Compatiblity Color", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (0.1,0.1,0.1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _ArchonCenterWorldPos ("Archon Center", Vector) = (0,0,0,1)
        _FresnelColor ("Fresnel Color", Color) = (0.8,0.8,0.9,1)
        _LineColor ("Line Color", Color) = (0.8,0.8,0.9,1)
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _MapSize ("Map Size", Float) = 4.09
        _DisplayScale ("Display Scale", Float) = 0.01
        _DownClip ("Down Clip", Float) = -2.02
        _UpClip ("Up Clip", Float) = 0.253

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert noshadow

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        struct Input
        {
            float3 local;
            float3 object;
            float2 uv_BumpMap;
            float Face:VFACE;
            float3 viewDir;
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

        sampler2D _BumpMap;

        half _Glossiness;
        half _Metallic;
        float _FadeRadius;   //for compatiblity
        float _FadeSharpness;   //for compatiblity
        fixed4 _Color;  //for compatiblity
        fixed4 _BaseColor;
        fixed4 _FresnelColor;
        fixed4 _LineColor;
        float3 _ArchonCenterWorldPos;
        float _MapSize;
        float _DownClip;
        float _UpClip;
        float _DisplayScale;
        float4x4 _ObjectToDisplay;
        float4x4 _LocalObject;


        void vert (inout appdata_full v, out Input ip) {

            ip = (Input)0;
            UNITY_INITIALIZE_OUTPUT(Input, ip);
            ip.object = mul(_LocalObject, float4(v.vertex.xyz, 1.0)).xyz;
            ip.local =  mul(_ObjectToDisplay, float4(v.vertex.xyz,1.0)).xyz;

        }

        float lineOpacity(float r, float minR, float maxR)
        {
            float3 d = max(abs(ddx(r)), abs(ddy(r)));

            //return saturate((r - minR) / (maxR - minR));
            return smoothstep(minR-d*2, minR, r) * (1 - smoothstep(maxR, maxR + d*2,r));
        }

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            bool frontFace = IN.Face > 0.5;
            if (!frontFace)
            {
                o.Normal = -UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
            }
            else
                o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));

                

            float3 relative = IN.local;
            //0.0 - totally with
            //0.52 - with
            //0.53 - with
            //0.6 - with
            //0.8 - with
            //0.85 - with
            //0.87 - with
            //0.9 - against
            //1.0 - against
            float3 realWorld = (IN.object + _ArchonCenterWorldPos * 0.88);
            relative *= _DisplayScale;
            float r = length(relative.xz);

            float lineEvery = 10;
            float3 mod = fmod(realWorld + 10000 + lineEvery /2, lineEvery) / lineEvery;
            float3 d = max(abs(ddx(realWorld)), abs(ddy(realWorld)))*0.1;
            float l0 = 0.499;
            float l1 = 0.501;
            float3 lo = smoothstep(l0 - d*2, l0, mod) * (1.0 - smoothstep(l1, l1+d*2, mod));
            lo.x *= mod.z > 0.4 && mod.z < 0.6;
            lo.z *= mod.x > 0.4 && mod.x < 0.6;

            float3 view = IN.worldPos - _WorldSpaceCameraPos;
            float3 normal = WorldNormalVector (IN, o.Normal);
            float fresnel = saturate(1+dot(view, normal) / length(view));
            fresnel = pow(fresnel, 2.0);

            //o.Emission = clamp(IN.local*0.01,0,1);/ max(_MapSize,0.01);

            if (frontFace)
                o.Emission = fresnel*_FresnelColor.rgb;

            o.Emission += max(lo.y,0.5 * max(lo.x, lo.z))*_LineColor.rgb * (!frontFace ? 0.25 : 1);
            o.Emission += lineOpacity(r,_MapSize*0.993,_MapSize*1.002);
            float vClipWidth = max(abs(_UpClip), abs(_DownClip)) * 0.005;
            o.Emission += lineOpacity(relative.y,_UpClip - vClipWidth,_UpClip + vClipWidth * 2);
            o.Emission += lineOpacity(relative.y,_DownClip - vClipWidth,_DownClip +vClipWidth * 2);
            if (r > _MapSize || relative.y > _UpClip || relative.y < _DownClip)
                clip(-1);

            o.Albedo = _BaseColor.rgb;
            //_BaseColor.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
            clip(_Color.a -0.5);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
