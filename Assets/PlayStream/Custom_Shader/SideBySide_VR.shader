Shader "Custom/SideBySide_VR"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _IsSBS ("Is Side-by-Side?", Float) = 1
        _DebugEye("Debug Eye (0 = Left, 1 = Right)", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile __ UNITY_SINGLE_PASS_STEREO XR_SINGLE_PASS_INSTANCED

            #include "UnityCG.cginc"
            #include "UnityInstancing.cginc"

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
            float _DebugEye;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // SBS UV logic per eye
                if (_IsSBS > 0.5)
                {
                    uint eyeIndex = 1;
                    
                    // Manually fetch eye index (use stereo context)
                    #if defined(UNITY_SINGLE_PASS_STEREO) || defined(XR_SINGLE_PASS_INSTANCED)
                        eyeIndex = unity_StereoEyeIndex;
                    #endif

                    // Modify UV.x for left or right half
                    if (eyeIndex == 0) // Left Eye
                    {
                        o.uv.x = o.uv.x * 0.5;  // Scale UV for left side
                    }
                    else // Right Eye
                    {
                        o.uv.x = 0.5 + o.uv.x * 0.5;  // Scale UV for right side
                    }
                }

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Debugging: Show different color per eye
                uint eyeIndex = unity_StereoEyeIndex;
                if (eyeIndex == 0)
                {
                    return fixed4(1, 0, 0, 1); // Red for Left Eye
                }
                else
                {
                    return fixed4(0, 0, 1, 1); // Blue for Right Eye
                }
            }
            ENDCG
        }
    }
}
