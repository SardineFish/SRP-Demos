#pragma enable_d3d11_debug_symbols

#ifndef SHADOW_LIB
#define SHADOW_LIB

int _UseShadow;
float _ShadowBias;
float4x4 _WorldToLight;
Texture2D _ShadowMap;
SamplerState tex_point_clamp_sampler;
int _ShadowType; // 1.Standard  2.PSM  3.TSM
float4 _ShadowParameters;
float4x4 _ShadowPostTransform;

float shadowAt(v2f_legacy i)
{
    if(_ShadowType == 0)
    {
	    float4 p = float4(i.worldPos.xyz, 1);
        p = mul(_WorldToLight, p);
        p /= p.w;
        p.z = 1 - (0.5 * p.z + .5);
        float2 uv = p.xy * .5 + .5;
	    float2 clip = step(0, uv.xy) * (1 - step(1, uv.xy));
        float shadowDepth = _ShadowMap.Sample(tex_point_clamp_sampler, uv) * clip.x * clip.y;
	    if(shadowDepth <= 0)
	    	shadowDepth = -100;
        return step(shadowDepth, p.z + _ShadowBias);
    }
    else if (_ShadowType == 1)
    {
        float4 p = i.clipPos.xyzw;
        p /= p.w;
        p.w = 1;
        p.y *= _ProjectionParams.x;
        p = mul(_WorldToLight, p);
        p /= p.w;
        #if UNITY_UV_STARTS_AT_TOP
            p.y *= -1;
        #endif
        // Handle z reverse
        if(_ShadowParameters.x > 0)
            p.z = 1 - p.z;
            
        float2 uv = p.xy * .5 + .5;
	    float2 clip = step(0, uv.xy) * (1 - step(1, uv.xy));
        float shadowDepth = _ShadowMap.Sample(tex_point_clamp_sampler, uv) * clip.x * clip.y;
        if(shadowDepth <= 0)
            shadowDepth = -100;
        return step(shadowDepth, p.z + _ShadowBias);
    }
    else if (_ShadowType == 2)
    {
	    float4 p = float4(i.worldPos.xyz, 1);
        float4 pClip = mul(_WorldToLight, p);
        pClip /= p.w;
        pClip.w = 1;

        p = mul(_ShadowPostTransform, pClip);
        p /= p.w;
        p.y *= -1;
        
        float2 uv = p.xy * .5 + .5;
	    float2 clip = step(0, uv.xy) * (1 - step(1, uv.xy));
        float shadowDepth = _ShadowMap.Sample(tex_point_clamp_sampler, uv) * clip.x * clip.y;
	    if(shadowDepth <= 0)
	    	shadowDepth = -100;
        return step(shadowDepth, pClip.z + _ShadowBias);
    }

    //return float4(shadowDepth, p.z, 0, 1);
    return 1;
}

float shadowAt(float3 worldPos)
{
    if(_UseShadow == 0)
        return 1;
    if(_ShadowType == 0)
    {
	    float4 p = float4(worldPos.xyz, 1);
        p = mul(_WorldToLight, p);
        p /= p.w;
        p.z = 1 - (0.5 * p.z + .5);
        float2 uv = p.xy * .5 + .5;
	    float2 clip = step(0, uv.xy) * (1 - step(1, uv.xy));
        float shadowDepth = _ShadowMap.Sample(tex_point_clamp_sampler, uv) * clip.x * clip.y;
	    if(shadowDepth <= 0)
	    	shadowDepth = -100;
        return step(shadowDepth, p.z + _ShadowBias);
    }
    else if (_ShadowType == 2)
    {
	    float4 p = float4(worldPos.xyz, 1);
        float4 pClip = mul(_WorldToLight, p);
        pClip /= p.w;
        pClip.w = 1;

        p = mul(_ShadowPostTransform, pClip);
        p /= p.w;
        p.y *= -1;
        
        float2 uv = p.xy * .5 + .5;
	    float2 clip = step(0, uv.xy) * (1 - step(1, uv.xy));
        float shadowDepth = _ShadowMap.Sample(tex_point_clamp_sampler, uv) * clip.x * clip.y;
	    if(shadowDepth <= 0)
	    	shadowDepth = -100;
        return step(shadowDepth, pClip.z + _ShadowBias);
    }

    //return float4(shadowDepth, p.z, 0, 1);
    return 1;
}


float shadowAt(v2f_default i)
{
    return shadowAt(i.worldPos.xyz);
}

float4 objectToPSMClipPos(float3 pos)
{
    float4 p = UnityObjectToClipPos(pos);
    p /= p.w;
    p = mul(_WorldToLight, p);
    return p;
}

#endif