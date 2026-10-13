// Neon Fly readability outline (owner request, D-273). Drawn as the fly renderer's second material: an additive
// inverted hull whose width is set in screen pixels, so a 0.27 m fly still shows as a cyan glow at any distance. It
// pulses softly and, every few seconds, flashes brighter. Each fly can get its own phase (_Phase, per-renderer
// property block) so the level's flies do not blink together. Emissive only: no real-time light (TDD INV-11).
Shader "ProfessorSprat/FX/Fly Glow Outline"
{
    Properties
    {
        [HDR] _GlowColor ("Glow Color", Color) = (0.25, 0.85, 1.0, 1.0)
        _Intensity ("Intensity", Range(0, 8)) = 2.5
        _WidthPixels ("Width (pixels)", Range(0, 12)) = 3.0
        _WidthWorld ("Extra Width (metres)", Range(0, 0.05)) = 0.006
        _PulseSpeed ("Pulse Speed (cycles/s)", Range(0, 4)) = 0.8
        _PulseMin ("Pulse Minimum", Range(0, 1)) = 0.45
        _FlashPeriod ("Flash Period (s)", Range(0.5, 10)) = 3.0
        _FlashBoost ("Flash Boost", Range(0, 4)) = 1.6
        _FlashWidthBoost ("Flash Extra Width (pixels)", Range(0, 8)) = 2.0
        _Phase ("Phase (s)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Pass
        {
            Name "FlyOutline"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend One One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                half _Intensity;
                float _WidthPixels;
                float _WidthWorld;
                float _PulseSpeed;
                half _PulseMin;
                float _FlashPeriod;
                half _FlashBoost;
                float _FlashWidthBoost;
                float _Phase;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half glow : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // 0..1 soft pulse and a short flash once per period (sharp rise, quick fall).
            void Pulse(out half pulse, out half flash)
            {
                float t = _Time.y + _Phase;
                pulse = lerp(_PulseMin, 1.0h, 0.5h + 0.5h * sin(t * _PulseSpeed * TWO_PI));
                float cycle = frac(t / _FlashPeriod);
                flash = (half)(exp(-cycle * 18.0) * step(0.0, cycle));
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                half pulse, flash;
                Pulse(pulse, flash);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz) + normalWS * _WidthWorld;
                float4 positionCS = TransformWorldToHClip(positionWS);

                // Screen-space extrusion: constant pixel width regardless of distance.
                float2 normalCS = mul((float3x3)UNITY_MATRIX_VP, normalWS).xy;
                float lengthCS = length(normalCS);
                if (lengthCS > 1e-5)
                {
                    float pixels = _WidthPixels + _FlashWidthBoost * flash;
                    positionCS.xy += (normalCS / lengthCS) * (pixels * 2.0 / _ScreenParams.xy) * positionCS.w;
                }

                output.positionCS = positionCS;
                output.glow = pulse + _FlashBoost * flash;
                output.fogFactor = ComputeFogFactor(positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 color = _GlowColor.rgb * _Intensity * input.glow;
                // Additive: fog fades the glow towards black rather than the fog colour, but never fully, so a far
                // fly still reads through the underwater fog.
                color *= lerp(0.4h, 1.0h, ComputeFogIntensity(input.fogFactor));
                return half4(color, 1.0h);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
