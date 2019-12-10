#include "GridNoise.hlsl"

inline float perlinNoise(float3 pos, float3 repeatSize, float seed)
{
    pos = pos % repeatSize;
    uint3 p = floor(pos);
    
    pos -= floor(pos);
    float u = fade(pos.x);
    float v = fade(pos.y);
    float w = fade(pos.z);

    return smoothLerp(w, smoothLerp(v, smoothLerp(u, grad(gridAt((p + float3(0,0,0)) % repeatSize, seed), pos - float3(0,0,0)), grad(gridAt((p + float3(1,0,0)) % repeatSize, seed), pos - float3(1,0,0))),
                                       smoothLerp(u, grad(gridAt((p + float3(0,1,0)) % repeatSize, seed), pos - float3(0,1,0)), grad(gridAt((p + float3(1,1,0)) % repeatSize, seed), pos - float3(1,1,0)))),
                         smoothLerp(v, smoothLerp(u, grad(gridAt((p + float3(0,0,1)) % repeatSize, seed), pos - float3(0,0,1)), grad(gridAt((p + float3(1,0,1)) % repeatSize, seed), pos - float3(1,0,1))),
                                       smoothLerp(u, grad(gridAt((p + float3(0,1,1)) % repeatSize, seed), pos - float3(0,1,1)), grad(gridAt((p + float3(1,1,1)) % repeatSize, seed), pos - float3(1,1,1)))));
                                       
}

inline float perlinNoiseFBM(float3 pos, int iteration, float3 repeatSize, float seed)
{
    float value = 0;
    float amplitude = .5f;
    for(int i = 0; i < iteration; i++)
    {
        value += amplitude * perlinNoise(pos, repeatSize, seed);
        pos *= 2;
        repeatSize *= 2;
        amplitude *= .5;
    }
    value /= 1 - pow(.5, iteration);
    return value;
}

inline float perlinNoise(float2 pos, float2 repeatSize, float seed)
{
    pos = pos % repeatSize;
    uint2 p = floor(pos);
    pos -= floor(pos);

    float u = fade(pos.x);
    float v = fade(pos.y);

    return smoothLerp(v, 
        smoothLerp(u, grad(gridAt((p + float2(0, 0)) % repeatSize, seed), pos - float2(0, 0)), grad(gridAt((p + float2(1, 0)) % repeatSize, seed), pos - float2(1, 0))), 
        smoothLerp(u, grad(gridAt((p + float2(0, 1)) % repeatSize, seed), pos - float2(0, 1)), grad(gridAt((p + float2(1, 1)) % repeatSize, seed), pos - float2(1, 1))));
}
inline float perlinNoiseFBM(float2 pos, int octave, float2 repeatSize, float seed)
{
    float value = 0;
    float amplitude = .5;
    for(int i = 0; i < octave; i++)
    {
        value += amplitude * perlinNoise(pos, repeatSize, seed);
        pos *= 2;
        repeatSize *= 2;
        amplitude *= .5;
    }
    value /= 1 - pow(.5, octave);
    return value;
}