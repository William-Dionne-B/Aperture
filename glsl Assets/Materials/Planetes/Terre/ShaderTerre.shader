Shader "Custom/ShaderTerre"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo Day (RGB)", 2D) = "white" {}
        _NightTex ("Albedo Night (RGB)", 2D) = "black" {}
        _CloudsTex ("Clouds (RGB)", 2D) = "white" {}
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _SpecularMap ("Specular Map (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _CloudsIntensity ("Clouds Intensity", Range(0,1)) = 0.5
        _DayNightBlend ("Day/Night Blend", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _NightTex;
        sampler2D _CloudsTex;
        sampler2D _BumpMap;
        sampler2D _SpecularMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NightTex;
            float2 uv_CloudsTex;
            float2 uv_BumpMap;
            float2 uv_SpecularMap;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        half _CloudsIntensity;
        half _DayNightBlend;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from day and night textures blended
            fixed4 dayColor = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            fixed4 nightColor = tex2D(_NightTex, IN.uv_NightTex) * _Color;
            fixed4 albedo = lerp(dayColor, nightColor, _DayNightBlend);

            // Add clouds on top
            fixed4 clouds = tex2D(_CloudsTex, IN.uv_CloudsTex);
            albedo.rgb = lerp(albedo.rgb, clouds.rgb, clouds.a * _CloudsIntensity);

            o.Albedo = albedo.rgb;

            // Normal map
            o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));

            // Specular map influences metallic and smoothness
            fixed4 specular = tex2D(_SpecularMap, IN.uv_SpecularMap);
            o.Metallic = _Metallic * specular.r;
            o.Smoothness = _Glossiness * specular.a;

            o.Alpha = albedo.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}   