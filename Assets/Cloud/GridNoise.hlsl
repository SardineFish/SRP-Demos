#ifndef GRID_NOISE_INCLUDED
#define GRID_NOISE_INCLUDED

#define PHI (1.61803398874989484820459 * 00000.1)
#define PI (3.14159265358979323846264 * 00000.1)
#define SQ2 (1.41421356237309504880169 * 10000.0)
#define E (2.71828182846)
#define BIAS_X (1.31)
#define BIAS_Y (1.17)
#define BIAS_Z (1.57)

// https://www.shadertoy.com/view/wtsSW4
inline float gold_noise(float3 pos, float seed)
{
    return frac(tan(distance(pos * (PHI + seed), float3(PHI, PI, E))) * SQ2) * 2 - 1;
}
inline float gold_noise(float2 pos, float seed)
{
    return frac(tan(distance(pos * (PHI + seed), float2(PHI, PI))) * SQ2) * 2 - 1;
}

inline float fade(float t)
{
    return pow(t, 3) * (t * (t * 6 - 15) + 10);
}

inline float smoothLerp(float t, float a, float b)
{
    return a + t * (b - a);
}

inline float3 gridAt(float3 pos, float seed)
{
    return float3(
        gold_noise(pos, seed * BIAS_X) + 0.00001,
        gold_noise(pos, seed + BIAS_Y) + 0.00001,
        gold_noise(pos, seed / BIAS_Z) + 0.00001
    );
}
inline float2 gridAt(float2 pos, float seed)
{
    return float2(
        gold_noise(pos, seed * BIAS_X) + 0.00001,
        gold_noise(pos, seed + BIAS_Y) + 0.00001
    );
}

inline float grad(float3 v, float3 offset)
{
    return length(v) <= 0 ? 0 : dot(normalize(v), offset);
}
inline float grad(float2 v, float2 offset)
{
    return length(v) <= 0 ? 0 : dot(normalize(v), offset);
}

inline float3 mod(float3 n, float3 m)
{
    return (n + m) % m;
}

#endif