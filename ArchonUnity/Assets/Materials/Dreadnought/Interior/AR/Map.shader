Shader "Custom/Map"
{
    Properties
    {
        _Color ("Compatiblity Color", Color) = (1,1,1,1)
        _BaseColor ("Base Color", Color) = (0.1,0.1,0.1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _MapCenterWorldPos ("Map Center", Vector) = (0,0,0,1)
        _FresnelColor ("Fresnel Color", Color) = (0.8,0.8,0.9,1)
        _LineColor ("Line Color", Color) = (0.8,0.8,0.9,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float Face:VFACE;
            float3 viewDir;
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

        half _Glossiness;
        half _Metallic;
        float _FadeRadius;   //for compatiblity
        float _FadeSharpness;   //for compatiblity
        fixed4 _Color;  //for compatiblity
        fixed4 _BaseColor;
        fixed4 _FresnelColor;
        fixed4 _LineColor;
        float3 _MapCenterWorldPos;

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
                o.Normal = float3(0,0,-1);
            }
            else
                o.Normal = float3(0,0,1);

            float3 realWorld = (IN.worldPos - _MapCenterWorldPos)*100.0 + _MapCenterWorldPos;

            float lineEvery = 10;
            float3 rel = fmod(realWorld + 10000 + lineEvery /2, lineEvery) / lineEvery;
            float3 d = max(abs(ddx(realWorld)), abs(ddy(realWorld)))*0.1;
            float l0 = 0.499;
            float l1 = 0.501;
            float3 lo = smoothstep(l0 - d*2, l0, rel) * (1.0 - smoothstep(l1, l1+d*2, rel));
            lo.x *= rel.z > 0.4 && rel.z < 0.6;
            lo.z *= rel.x > 0.4 && rel.x < 0.6;

            float3 view = IN.worldPos - _WorldSpaceCameraPos;
            float3 normal = WorldNormalVector (IN, o.Normal);
            float fresnel = saturate(1+dot(view, normal) / length(view));
            fresnel = pow(fresnel, 2.0);

            //tex2D (_MainTex, IN.uv_MainTex) * _Color;
            if (frontFace)
                o.Emission = fresnel*_FresnelColor.rgb;

            o.Emission += max(lo.y,0.5 * max(lo.x, lo.z))*_LineColor.rgb * (!frontFace ? 0.25 : 1);
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
