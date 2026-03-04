Shader "Custom/BlackHoleShad"
{
    Properties {}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        struct Input {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = float3(0,0,0);
            o.Emission = float3(0,0,0);
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
