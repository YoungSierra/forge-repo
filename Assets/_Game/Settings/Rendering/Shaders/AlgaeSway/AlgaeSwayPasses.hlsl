// Vertex entry points for the Algae Sway shader (D-263). Each wraps the URP Lit pass's own vertex function: same inputs
// plus the vertex colour, the position swayed, then the untouched URP function. Include after the URP pass file, with
// one ALGAE_PASS_* defined.

#include "AlgaeSway.hlsl"

#if defined(ALGAE_PASS_FORWARD) || defined(ALGAE_PASS_GBUFFER)
struct AlgaeAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 texcoord : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;
    float2 dynamicLightmapUV : TEXCOORD2;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Attributes AlgaeToAttributes(AlgaeAttributes input)
{
    Attributes output = (Attributes)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionOS = float4(AlgaeSwayPositionOS(input.positionOS.xyz, input.color), input.positionOS.w);
    output.normalOS = input.normalOS;
    output.tangentOS = input.tangentOS;
    output.texcoord = input.texcoord;
    output.staticLightmapUV = input.staticLightmapUV;
    output.dynamicLightmapUV = input.dynamicLightmapUV;
    return output;
}
#endif

#if defined(ALGAE_PASS_FORWARD)
Varyings AlgaeLitPassVertex(AlgaeAttributes input)
{
    return LitPassVertex(AlgaeToAttributes(input));
}
#endif

#if defined(ALGAE_PASS_GBUFFER)
Varyings AlgaeGBufferPassVertex(AlgaeAttributes input)
{
    return LitGBufferPassVertex(AlgaeToAttributes(input));
}
#endif

#if defined(ALGAE_PASS_SHADOW)
struct AlgaeAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 texcoord : TEXCOORD0;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings AlgaeShadowPassVertex(AlgaeAttributes input)
{
    Attributes output = (Attributes)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionOS = float4(AlgaeSwayPositionOS(input.positionOS.xyz, input.color), input.positionOS.w);
    output.normalOS = input.normalOS;
    output.texcoord = input.texcoord;
    return ShadowPassVertex(output);
}
#endif

#if defined(ALGAE_PASS_DEPTHONLY)
struct AlgaeAttributes
{
    float4 position : POSITION;
    float2 texcoord : TEXCOORD0;
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
#endif
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings AlgaeDepthOnlyVertex(AlgaeAttributes input)
{
    Attributes output = (Attributes)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.position = float4(AlgaeSwayPositionOS(input.position.xyz, input.color), input.position.w);
    output.texcoord = input.texcoord;
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    output.normalOS = input.normalOS;
    output.tangentOS = input.tangentOS;
#endif
    return DepthOnlyVertex(output);
}
#endif

#if defined(ALGAE_PASS_DEPTHNORMALS)
struct AlgaeAttributes
{
    float4 positionOS : POSITION;
    float4 tangentOS : TANGENT;
    float2 texcoord : TEXCOORD0;
    float3 normal : NORMAL;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings AlgaeDepthNormalsVertex(AlgaeAttributes input)
{
    Attributes output = (Attributes)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    output.positionOS = float4(AlgaeSwayPositionOS(input.positionOS.xyz, input.color), input.positionOS.w);
    output.tangentOS = input.tangentOS;
    output.texcoord = input.texcoord;
    output.normal = input.normal;
    return DepthNormalsVertex(output);
}
#endif
