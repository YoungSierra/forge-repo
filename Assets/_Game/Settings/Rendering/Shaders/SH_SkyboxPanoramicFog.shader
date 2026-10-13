// Lat-long panoramic skybox that the scene fog reaches (owner request, D-261). Same properties as the built-in
// Skybox/Panoramic (latitude-longitude layout), so the provider's MAT_Skybox keeps its texture, tint, exposure and
// rotation. The sky has no distance, so the fog colour (RenderSettings, as for every other object) is blended by view
// elevation: full at and below the horizon band, thinning to _FogZenith straight up. Off when the scene fog is off.
Shader "ProfessorSprat/Skybox/Panoramic Fog"
{
    Properties
    {
        _Tint ("Tint Color", Color) = (.5, .5, .5, .5)
        [Gamma] _Exposure ("Exposure", Range(0, 8)) = 1.0
        _Rotation ("Rotation", Range(0, 360)) = 0
        [NoScaleOffset] _MainTex ("Spherical (HDR)", 2D) = "grey" {}
        [Header(Fog)]
        _FogHorizon ("Fog At Horizon", Range(0, 1)) = 1.0
        _FogZenith ("Fog At Zenith", Range(0, 1)) = 0.35
        _FogHeightStart ("Fog Fade Start (elevation sin)", Range(-1, 1)) = 0.0
        _FogHeightEnd ("Fog Fade End (elevation sin)", Range(-1, 1)) = 0.6
    }

    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            half4 _MainTex_HDR;
            half4 _Tint;
            half _Exposure;
            float _Rotation;
            half _FogHorizon;
            half _FogZenith;
            float _FogHeightStart;
            float _FogHeightEnd;

            struct appdata_t
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 RotateAroundYInDegrees(float3 vertex, float degrees)
            {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float3(mul(m, vertex.xz), vertex.y).xzy;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 rotated = RotateAroundYInDegrees(v.vertex.xyz, _Rotation);
                o.vertex = UnityObjectToClipPos(rotated);
                o.texcoord = v.vertex.xyz;
                return o;
            }

            inline float2 ToRadialCoords(float3 coords)
            {
                float3 normalizedCoords = normalize(coords);
                float latitude = acos(normalizedCoords.y);
                float longitude = atan2(normalizedCoords.z, normalizedCoords.x);
                float2 sphereCoords = float2(longitude, latitude) * float2(0.5 / UNITY_PI, 1.0 / UNITY_PI);
                return float2(0.5, 1.0) - sphereCoords;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 tc = ToRadialCoords(i.texcoord);
                half4 tex = tex2Dlod(_MainTex, float4(tc, 0, 0));
                half3 c = DecodeHDR(tex, _MainTex_HDR);
                c = c * _Tint.rgb * unity_ColorSpaceDouble.rgb;
                c *= _Exposure;

                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                float elevation = normalize(i.texcoord).y;
                half fog = lerp(_FogHorizon, _FogZenith, smoothstep(_FogHeightStart, _FogHeightEnd, elevation));
                c = lerp(c, unity_FogColor.rgb, saturate(fog));
                #endif

                return half4(c, 1);
            }
            ENDCG
        }
    }

    Fallback Off
}
