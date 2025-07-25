Shader "Custom/StructuredGlass"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        [HDR] _EmissionColor ("Emission", Color) = (1,1,1,1)
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _Structure ("Structure", 2D) = "white" {}
        //_MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Noise1 ("Noise1", 2D) = "white" {}
        _Noise2 ("Noise2", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _LowerNoiseThreshold ("Lower Noise Threshold", Range(0,1)) = 0.2
        _UpperNoiseThreshold ("Upper Noise Threshold", Range(0,1)) = 0.8
        _LowerNormalThreshold ("Lower Normal Threshold", Range(0,1)) = 0.1
        _UpperNormalThreshold ("Upper Normal Threshold", Range(0,1)) = 0.3
        _NoiseSpeed1 ("Noise Speed 1", Range(0,1)) = 0.25
        _NoiseSpeed2 ("Noise Speed 2", Range(0,1)) = 0.25
    }
    SubShader
    {
        Tags { "Queue"="Transparent+900" "IgnoreProjector"="True" "RenderType"="Transparent" }
        ZWrite Off 
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows keepalpha

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _BumpMap;
        sampler2D _Structure;
        sampler2D _Noise1;
        sampler2D _Noise2;
        float _NoiseSpeed1;
        float _NoiseSpeed2;

        float _LowerNoiseThreshold;
        float _UpperNoiseThreshold;
        float _LowerNormalThreshold;
        float _UpperNormalThreshold;

        struct Input
        {
            float2 uv_BumpMap;
            float2 uv_Noise1;
            float2 uv_Noise2;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float3 _EmissionColor;


        float noiseSample(sampler2D noise, float2 uv, float time)
        {
             float n0 = tex2D(noise, uv + float2(time,-time)).r;
             float n1 = tex2D(noise, uv + float2(-time + 0.31248791 ,time)).r;
             return (n0 + n1) * 0.5;
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
            float n0 = noiseSample(_Noise1, IN.uv_Noise1, _Time.x*_NoiseSpeed1);
            float n1 = noiseSample(_Noise2, IN.uv_Noise2, _Time.x*_NoiseSpeed2);
            float n = n0 * n1;
            float s = tex2D(_Structure, IN.uv_BumpMap).r;
            fixed4 c =  _Color;
            float intensity = smoothstep(_LowerNoiseThreshold,_UpperNoiseThreshold, n);
            float3 normal = UnpackNormal(tex2D (_BumpMap, IN.uv_BumpMap));
            //normal.xy *= smoothstep(_LowerNormalThreshold, _UpperNormalThreshold, n);
            //normal = normalize(normal);
            o.Normal = normal;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
            float d = max(0.1,max(abs(ddx(s)), abs(ddy(s))));
            o.Emission = smoothstep(0.7-d,0.7+d, s) * intensity * _EmissionColor;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
