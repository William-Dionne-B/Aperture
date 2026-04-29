{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _DistFadeOut ("Dist Fade Out (near)", Float) = 5
        _DistFadeIn ("Dist Fade In (far)", Float) = 20
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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _DistFadeOut;
            float _DistFadeIn;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                // world position for distance fade
                float4 worldP = mul(unity_ObjectToWorld, v.vertex);
                o.worldPos = worldP.xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture (kept for potential detail but color override below)
                fixed4 texCol = tex2D(_MainTex, i.uv);

                // distance to camera in world space
                float dist = distance(i.worldPos, _WorldSpaceCameraPos);

                // compute alpha:
                // - si on est plus proche que _DistFadeOut => alpha = 1
                // - si on est plus loin que _DistFadeIn => alpha = 0
                // - sinon interpolation linéaire entre les deux
                float alpha;
                if (dist <= _DistFadeOut) alpha = 1.0;
                else if (dist >= _DistFadeIn) alpha = 0.0;
                else alpha = 1.0 - (dist - _DistFadeOut) / max(0.0001, (_DistFadeIn - _DistFadeOut));

                // couleur finale : couleur définie par _Color (l'alpha est contrôlé par alpha)
                fixed4 col = _Color;
                col.a *= alpha;

                // appliquer le fog après calcul
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}