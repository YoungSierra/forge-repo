#ifndef PROFESSOR_SPRAT_ALGAE_SWAY_INCLUDED
#define PROFESSOR_SPRAT_ALGAE_SWAY_INCLUDED

// Underwater sway for algae (D-263). Displaces vertices in world space, so every pass that uses it (lit, shadow, depth,
// depth-normals, GBuffer) moves the same way and the shadows follow the plant.
//
// Bend weight / phase come from one of two sources:
//  - _SWAY_VERTEXCOLOR (provider data, preferred): R = bend weight (0 at the base → 1 at the tip), G = phase per
//    strand/leaf (constant per strand), B = flutter mask (leaf edges).
//  - default (works with meshes that only have a pivot at the base): weight from the object-space height
//    (posOS.y / _SwayHeight, curved by _SwayStiffness); phase from the object's world origin plus a little from the
//    vertex position, so clumps and strands do not move in lockstep.
// The mesh must not be statically batched (that bakes the object space into world space).

// Set by AlgaePush (Gameplay): xyz = the Professor's centre, w = push radius (0 = no pusher).
float4 _AlgaePusher;

float3 AlgaeSwayOffsetWS(float3 positionOS, float3 positionWS, half4 color)
{
    float3 originWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));

#if defined(_SWAY_VERTEXCOLOR)
    float weight = color.r;
    float phase = color.g * 6.2831853;
    float flutterMask = color.b;
#else
    float height01 = saturate(positionOS.y / max(_SwayHeight, 0.001));
    float weight = pow(height01, max(_SwayStiffness, 0.01));
    float clumpPhase = frac(sin(dot(originWS.xz, float2(12.9898, 78.233))) * 43758.5453) * 6.2831853;
    float phase = clumpPhase + dot(positionWS.xz, float2(0.9, 1.3));
    float flutterMask = weight;
#endif

    float2 direction = _SwayDirection.xz;
    direction = dot(direction, direction) > 1e-6 ? normalize(direction) : float2(1.0, 0.0);
    float3 along = float3(direction.x, 0.0, direction.y);
    float3 across = float3(-direction.y, 0.0, direction.x);

    // Current: a slow wave travelling along the current direction (neighbours follow each other), plus a slower
    // secondary swell so the motion never looks like a metronome.
    float travel = dot(positionWS.xz, direction) * _SwayWaveScale * 6.2831853;
    float t = _Time.y * _SwaySpeed;
    float sway = sin(t - travel + phase) * 0.7 + sin(t * 0.53 + phase * 1.31) * 0.3;
    float3 offset = along * (sway * _SwayStrength * weight);

    // Flutter: small, fast, sideways, strongest at the tips / leaf edges.
    float flutter = sin(_Time.y * _FlutterSpeed + phase * 3.1 + positionWS.y * 5.0);
    offset += across * (flutter * _FlutterStrength * flutterMask);

    // The Professor pushes the plants aside as he walks through them.
    if (_AlgaePusher.w > 0.0)
    {
        float3 away = positionWS - _AlgaePusher.xyz;
        away.y = 0.0;
        float dist = length(away);
        float push = saturate(1.0 - dist / _AlgaePusher.w);
        offset += (away / max(dist, 0.001)) * (push * push * _PushStrength * weight);
    }

    // Bend instead of shear: the tip drops a little as it leans, so blades do not stretch.
    offset.y -= dot(offset.xz, offset.xz) * 0.5;
    return offset;
}

float3 AlgaeSwayPositionOS(float3 positionOS, half4 color)
{
    float3 positionWS = TransformObjectToWorld(positionOS);
    positionWS += AlgaeSwayOffsetWS(positionOS, positionWS, color);
    return TransformWorldToObject(positionWS);
}

#endif
