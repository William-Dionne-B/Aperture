Shader "Unlit/SpaceTimeWarp"
{
    Properties
    {
        _MassCount ("Mass Count", Int) = 0
        _Strength ("Strength", Float) = 1
        _Falloff ("Falloff", Float) = 1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define MAX_MASSES 10

            float4 _MassPositions[MAX_MASSES];
            float _MassValues[MAX_MASSES];
            int _MassCount;

            float _Strength;
            float _Falloff;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;

                float3 pos = v.vertex.xyz;
                float warp = 0;

                for (int i = 0; i < _MassCount; i++)
                {
                    float3 massPos = _MassPositions[i].xyz;
                    float mass = _MassValues[i];

                    float dist = distance(pos, massPos);
                    warp += -_Strength * mass / (pow(dist, _Falloff) + 0.01);
                }

                pos.y += warp;

                o.vertex = UnityObjectToClipPos(float4(pos,1));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(0.1, 0.4, 1.0, 1.0);
            }
            ENDCG
        }
    }
}