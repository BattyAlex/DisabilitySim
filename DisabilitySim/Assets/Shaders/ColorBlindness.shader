// Full-screen color blindness simulation for URP (used by a Full Screen Pass Renderer Feature).
// Multiplies each pixel's linear RGB by a 3x3 matrix set from ColorBlindnessEffect.
// Works with XR single-pass instanced rendering via the _X texture macros.
Shader "DisabilitySim/ColorBlindness"
{
    Properties
    {
        _Row0 ("Matrix Row 0", Vector) = (1, 0, 0, 0)
        _Row1 ("Matrix Row 1", Vector) = (0, 1, 0, 0)
        _Row2 ("Matrix Row 2", Vector) = (0, 0, 1, 0)
        _Severity ("Severity", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "ColorBlindness"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _Row0;
            float4 _Row1;
            float4 _Row2;
            float _Severity;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, input.texcoord);
                float3 c = color.rgb;
                float3 simulated = float3(dot(_Row0.xyz, c), dot(_Row1.xyz, c), dot(_Row2.xyz, c));
                color.rgb = lerp(c, saturate(simulated), _Severity);
                return color;
            }
            ENDHLSL
        }
    }
}
