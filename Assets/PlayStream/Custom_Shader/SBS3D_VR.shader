Shader "Custom/SBS3D_VR"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} // Input SBS 3D texture
        _IsSBS ("Is Side-by-Side?", Float) = 1 // 1 = SBS, 0 = Normal
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        Cull Off    // Disable face culling (good for VR)
        ZWrite Off  // UI/Quad doesn't need depth writing

        Pass
        {
            CGPROGRAM
            #pragma multi_compile_instancing  // Enable VR multi-instance rendering
            #pragma multi_compile __ XR_SINGLE_PASS_INSTANCED  // Enable XR instancing
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float _IsSBS;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // Get the eye index in XR (0 = Left Eye, 1 = Right Eye)
                #if defined(UNITY_SINGLE_PASS_STEREO) || defined(XR_SINGLE_PASS_INSTANCED)
                    int eyeIndex = unity_StereoEyeIndex;
                #else
                    int eyeIndex = 0;  // Assume left eye in non-XR mode
                #endif

                // Adjust UVs for SBS 3D
                if (_IsSBS > 0.5)
                {
                    if (eyeIndex == 1)  // Right Eye
                        o.uv.x = 0.5 + (o.uv.x * 0.5);
                    else  // Left Eye
                        o.uv.x = o.uv.x * 0.5;
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
