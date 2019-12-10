#include "GridNoise.hlsl"

inline float worleyNoise(float3 pos, float3 repeatSize, float seed)
{
    pos = pos % repeatSize;
    uint3 g = floor(pos);
    pos -= g;

    float minDist = 1024;
    for(int i = -1; i <= 1; i++)
    {
        for(int j = -1; j <= 1; j++)
        {
            for(int k = -1; k <= 1 ; k++)
            {
                float3 offset = abs(gridAt(mod(g + float3(i, j, k), repeatSize), seed));
                offset += float3(i, j, k);
                minDist = min(minDist, distance(pos, offset));
            }
        }
    }

    return minDist / sqrt(2);
    //return perm[AB+1] / 255.0f;

                                       
}

inline float worleyNoiseFBM(float3 pos, int iteration, float3 repeatSize, float seed)
{
    float value = 0;
    float amplitude = .5f;
    for(int i = 0; i < iteration; i++)
    {
        value += amplitude * worleyNoise(pos, repeatSize, seed);
        pos *= 2;
        repeatSize *= 2;
        amplitude *= .5;
    }
    
    value /= 1 - pow(.5, iteration);
    return 1 - 2 * value;
}