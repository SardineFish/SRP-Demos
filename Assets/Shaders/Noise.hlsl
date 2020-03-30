#ifndef SAR_RP_NOISE
#define SAR_RP_NOISE

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

#endif