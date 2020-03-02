#include "UnityCG.cginc"

float4 _MainLightPosition;
float4 _MainLightColor;
float4x4 _ViewProjectionInverseMatrix;
float3 _WorldCameraPos;

float _ShadowBias;
float4x4 _WorldToLight;
Texture2D _ShadowMap;
SamplerState tex_point_clamp_sampler;


struct v2f_light
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
};

struct v2f
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
	float3 worldPos: TEXCOORD1;
	float3 t2w0: TEXCOORD2;
	float3 t2w1: TEXCOORD3;
	float3 t2w2: TEXCOORD4;
    float4 screenPos : TEXCOORD5;
};

struct v2f_ray
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 ray : TEXCOORD1;
};

v2f_light light_vert(appdata_full i)
{
	v2f_light o;
	o.pos = UnityObjectToClipPos(i.vertex);
	o.uv = i.texcoord;
	return o;
}

v2f default_vert(appdata_full i)
{
    v2f o;
	o.pos = UnityObjectToClipPos(i.vertex);
    o.uv = i.texcoord;
    o.worldPos = mul(unity_ObjectToWorld, i.vertex);
    float3 worldNormal = UnityObjectToWorldNormal(i.normal);
	float3 worldTangent = UnityObjectToWorldDir(i.tangent.xyz);
	float3 worldBinormal = cross(worldNormal, worldTangent) * i.tangent.w;
	o.t2w0 = float3(worldTangent.x, worldBinormal.x, worldNormal.x);
	o.t2w1 = float3(worldTangent.y, worldBinormal.y, worldNormal.y);
	o.t2w2 = float3(worldTangent.z, worldBinormal.z, worldNormal.z);
    o.screenPos = ComputeScreenPos(o.pos);
    return o;
}

v2f_ray vert_ray(appdata_full i)
{
	v2f_ray o;
    o.pos = float4(i.vertex.x, i.vertex.y * _ProjectionParams.x, 1, 1);
    o.uv = i.texcoord;
	o.uv.y = 1 - o.uv.y;
    float4 p = float4(i.vertex.x, i.vertex.y, 1, 1);
    p = p * _ProjectionParams.z;
    float3 worldPos = mul(_ViewProjectionInverseMatrix, float4(p.xyzw));
    o.ray = worldPos - _WorldCameraPos;
	o.ray = normalize(o.ray) * length(o.ray) * _ProjectionParams.w;
	return o;
}

inline float3 tangentSpaceToWorld(v2f i, float3 v)
{
    return float3(dot(v, i.t2w0.xyz), dot(v, i.t2w1.xyz), dot(v, i.t2w2.xyz));
}

inline float3 fresnelFunc(float f0, float nv, float p) {
	return f0 + (1 - f0) * pow(1 - nv, p);
}

void lightAt(float3 worldPos, out float3 lightDir, out float3 lightColor)
{
	lightDir = normalize(_MainLightPosition.xyz - worldPos * _MainLightPosition.w);
	lightColor = _MainLightColor.rgb;
	return;
}

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