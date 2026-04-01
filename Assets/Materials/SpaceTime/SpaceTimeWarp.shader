Shader "Unlit/SpaceTimeWarp"
{
    Properties
    {
        _Strength ("Gravity Strength", Float) = 55
        _Falloff ("Gravity Falloff", Float) = 1.5
        _WarpMultiplier ("Warp Multiplier", Float) = 4
        _MinDistance ("Min Distance", Float) = 0.5
        _WellSoftening ("Well Softening", Float) = 30

        _GridScale ("Grid Scale", Float) = 0.02
        _LineWidth ("Line Width", Float) = 0.0006

        _LineColor ("Line Color", Color) = (0.45,0.45,0.45,0.28)
        _FadeStartDistance ("Fade Start Distance", Float) = 350
        _FadeEndDistance ("Fade End Distance", Float) = 1600
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #define MAX_MASSES 10

            float _Strength;
            float _Falloff;
            float _WarpMultiplier;
            float _MinDistance;
            float _WellSoftening;

            float _GridScale;
            float _LineWidth;
            float _FadeStartDistance;
            float _FadeEndDistance;

            float4 _LineColor;

            float4 _MassPositions[MAX_MASSES];
            float _MassValues[MAX_MASSES];
            int _MassCount;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;

                float3 localPos = v.vertex.xyz;
                float warp = 0;
                float3 worldPosTemp = mul(unity_ObjectToWorld, float4(localPos, 1)).xyz;
                int massCount = clamp(_MassCount, 0, MAX_MASSES);

                // Apply gravity wells
                for (int i = 0; i < massCount; i++)
                {
                    float3 massPos = _MassPositions[i].xyz;
                    float mass = _MassValues[i];

                    float dist = distance(worldPosTemp, massPos);
                    float minDist = max(_MinDistance, 0.0001);
                    float softening = max(_WellSoftening, minDist);
                    float distSq = max(dist * dist, minDist * minDist);
                    float denom = pow(distSq + softening * softening, _Falloff * 0.5);

                    warp += -_Strength * _WarpMultiplier * mass / denom;
                }

                localPos.y += warp;

                float4 world = mul(unity_ObjectToWorld, float4(localPos,1));
                o.worldPos = world.xyz;

                o.vertex = UnityWorldToClipPos(world);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Grid calculation in world space (XZ plane)
                float2 uv = i.worldPos.xz * _GridScale;

                float lineX = abs(frac(uv.x) - 0.5);
                float lineY = abs(frac(uv.y) - 0.5);
                float lineDistance = min(lineX, lineY);
                float antialias = max(fwidth(lineDistance), 0.0001);
                float lineMask = 1.0 - smoothstep(_LineWidth, _LineWidth + antialias, lineDistance);

                float cameraDistance = distance(_WorldSpaceCameraPos.xyz, i.worldPos);
                float fadeStart = max(_FadeStartDistance, 0.0001);
                float fadeEnd = max(_FadeEndDistance, fadeStart + 0.0001);
                float fade = 1.0 - smoothstep(fadeStart, fadeEnd, cameraDistance);

                return float4(_LineColor.rgb, _LineColor.a * lineMask * fade);
            }

            ENDCG
        }
    }
}