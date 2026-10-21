// Fake light shaft from above (owner request, D-287, after the reference build). Drawn on an open cone with no caps
// (MESH_FX_ShaftCone: v = 1 at the narrow top, 0 at the wide bottom). Additive and soft everywhere: it fades at its
// silhouette (view fresnel), at both ends, and near the camera, and is broken into slow-moving streaks so it reads as
// light rays rather than a solid volume. Not a volumetric pass.
Shader "ProfessorSprat/FX/Light Shaft"
{
    Properties
    {
        _Color ("Color", Color) = (0.55, 0.88, 1, 1)
        _Intensity ("Intensity", Range(0, 2)) = 0.22
        _EdgePower ("Edge Softness", Range(0.5, 6)) = 2.6
        _TopFade ("Top Fade (fraction)", Range(0.01, 0.5)) = 0.18
        _BottomPower ("Bottom Falloff", Range(0.2, 4)) = 1.4
        _Streaks ("Streak Count", Float) = 9
        _StreakAmount ("Streak Amount", Range(0, 1)) = 0.65
        _NearFade ("Near Fade (start, end)", Vector) = (2, 9, 0, 0)
        _Speed ("Drift Speed", Float) = 0.12
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Shaft"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _EdgePower;
                half _TopFade;
                half _BottomPower;
                float _Streaks;
                half _StreakAmount;
                float4 _NearFade;
                float _Speed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fog : TEXCOORD3;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fog = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                // Soft silhouette: bright where we look through the cone, fading to nothing at its outline.
                float edge = pow(saturate(abs(dot(normalize(input.normalWS), view))), _EdgePower);
                float v = input.uv.y;
                float ends = smoothstep(0.0, _TopFade, 1.0 - v) * pow(v, _BottomPower);
                // Streaks around the cone, drifting slowly; two frequencies so they don't look regular.
                float u = input.uv.x * 6.28318;
                float t = _Time.y * _Speed;
                float streak = 0.5 + 0.5 * sin(u * _Streaks + t * 6.0) * sin(u * (_Streaks * 0.47) - t * 4.0 + 1.3);
                streak = lerp(1.0, smoothstep(0.15, 0.95, streak), _StreakAmount);
                float distance = length(_WorldSpaceCameraPos - input.positionWS);
                float near = smoothstep(_NearFade.x, _NearFade.y, distance);
                float a = edge * ends * streak * near * _Intensity;
                half3 color = _Color.rgb * a;
                color = MixFogColor(color, half3(0, 0, 0), input.fog);
                return half4(color, a);
            }
            ENDHLSL
        }
    }
}
