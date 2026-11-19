// Draws a soft, irregular grey blind spot (scotoma) in the middle of each eye's view.
// Meant for a quad parented to the camera. The spot is placed by viewing angle per eye,
// so it lines up in VR even though the quad sits between the two eyes.
Shader "DisabilitySim/CentralVisionLoss"
{
    Properties
    {
        _ScotomaColor ("Scotoma Color", Color) = (0.16, 0.16, 0.16, 1)
        _Radius ("Radius (degrees)", Range(0, 40)) = 10
        _Softness ("Edge Softness (degrees)", Range(0.1, 20)) = 4
        _Irregularity ("Edge Irregularity", Range(0, 0.5)) = 0.2
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+100" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "CentralVisionLoss"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ScotomaColor;
                float _Radius;
                float _Softness;
                float _Irregularity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Direction from this eye to the pixel, in this eye's view space (forward is -Z).
                // Done in float rather than TransformWorldToViewDir (real3), since half precision blurs small angles on Quest.
                float3 dirVS = normalize(mul((float3x3)UNITY_MATRIX_V, input.positionWS - _WorldSpaceCameraPos));
                float angleFromCenter = degrees(acos(saturate(-dirVS.z)));

                // Wobble the edge around the circle so it doesn't look like a perfect disc.
                float around = atan2(dirVS.y, dirVS.x);
                float wobble = 0.5 * sin(3.0 * around + 1.3) + 0.3 * sin(5.0 * around + 0.4) + 0.2 * sin(8.0 * around + 2.1);
                float radius = _Radius * (1.0 + _Irregularity * wobble);

                float alpha = 1.0 - smoothstep(radius - _Softness, radius + _Softness, angleFromCenter);
                // Fade the whole spot out as the radius approaches 0.
                alpha *= saturate(_Radius / _Softness);

                return half4(_ScotomaColor.rgb, alpha * _ScotomaColor.a);
            }
            ENDHLSL
        }
    }
}
