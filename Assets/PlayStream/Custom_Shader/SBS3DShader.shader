Shader "Custom/SBS3DShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _IsSBS ("Is Side-by-Side?", Float) = 1 // 1 = SBS, 0 = Normal
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off  // No face culling, useful for UI
        ZWrite Off  // No depth writing, since it's a UI element

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float _IsSBS;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // Adjust UVs to sample only the left or right half for each eye
                o.uv = v.uv;
                
                // If SBS is enabled, adjust UVs
                if (_IsSBS > 0.5)
                {
                    if (o.uv.x > 0.5)  // Right side
                        o.uv.x = (o.uv.x - 0.5) * 2.0;  
                    else  // Left side
                        o.uv.x *= 2.0;
                }

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
