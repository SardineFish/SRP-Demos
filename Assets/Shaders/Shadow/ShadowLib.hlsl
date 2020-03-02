

float _ShadowBias;
float4x4 _WorldToLight;
Texture2D _ShadowMap;
SamplerState tex_point_clamp_sampler;


float shadowAt(float3 worldPos)
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

    //return float4(shadowDepth, p.z, 0, 1);

    return step(shadowDepth, p.z + _ShadowBias);
}

float4 objectToPSMClipPos(float3 pos)
{
    float4 p = UnityObjectToClipPos(pos);
    p /= p.w;
    p = mul(_WorldToLight, p);
    return p;
}