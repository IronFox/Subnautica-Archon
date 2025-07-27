Shader "Custom/FresnelGlass"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        [HDR] _FresnelEmission ("Fresnel Emission", Color) = (1,1,1,1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Exponent ("Fresnel Exponent", Range(0,10)) = 2.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+900" "IgnoreProjector"="True" "RenderType"="Transparent" "ForceNoShadowCasting"="True" }
        ZWrite Off 
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100


        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard keepalpha

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0


        struct Input
        {
            float2 uv_MainTex;
            float Face:VFACE;
            float3 viewDir;
            float3 worldPos;
            float3 worldNormal;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float4 _FresnelEmission;
        float _Exponent;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            float3 view = IN.worldPos - _WorldSpaceCameraPos;
            float3 normal = WorldNormalVector (IN, o.Normal);
            float fresnel = saturate(1+dot(view, normal) / length(view));
            fresnel = pow(fresnel, _Exponent);

            fixed4 c = _Color;
            o.Albedo = c.rgb;
            o.Emission = _FresnelEmission.rgb * fresnel * _FresnelEmission.a;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a + fresnel;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
