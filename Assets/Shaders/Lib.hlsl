#ifndef SAR_RP_SHADER_LIB
#define SAR_RP_SHADER_LIB

#include "UnityCG.cginc"


float4x4 _ViewProjectionInverseMatrix;
float3 _WorldCameraPos;
float2 _DepthParams;


struct v2f_light
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
};

struct v2f_default
{
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
	float4 tangent : TEXCOORD2;
	float3 worldPos : TEXCOORD3;
	float4 screenPos : TEXCOORD4;
};

struct v2f_no_clip_pos
{
	float2 uv : TEXCOORD0;
	float3 normal : TEXCOORD1;
	float3 tangent : TEXCOORD2;
	float3 worldPos : TEXCOORD3;
};

struct v2f_legacy
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
	float3 worldPos: TEXCOORD1;
	float3 t2w0: TEXCOORD2;
	float3 t2w1: TEXCOORD3;
	float3 t2w2: TEXCOORD4;
    float4 screenPos : TEXCOORD5;
	float4 clipPos : TEXCOORD6;
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

v2f_legacy vert_legacy(appdata_full i)
{
    v2f_legacy o;
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
	o.clipPos = o.pos;
    return o;
}

v2f_default vert_default(appdata_full i)
{
	v2f_default o;
	o.pos = UnityObjectToClipPos(i.vertex);
    o.uv = i.texcoord;
	o.normal = UnityObjectToWorldNormal(i.normal);
	o.tangent = float4(UnityObjectToWorldDir(i.tangent.xyz), i.tangent.w);
	//o.binormal = cross(o.normal, o.tangent) * i.tangent.w;
    o.worldPos = mul(unity_ObjectToWorld, i.vertex);
	o.screenPos = ComputeScreenPos(o.pos);
	return o;
}

v2f_default vert_blit_default(appdata_full i)
{
	v2f_default o;
	o.pos = float4(i.vertex.x, i.vertex.y * _ProjectionParams.x, 1, 1);
    o.uv = i.texcoord;
	o.normal = UnityObjectToWorldNormal(i.normal);
	o.tangent = float4(UnityObjectToWorldDir(i.tangent.xyz), i.tangent.w);
	//o.binormal = cross(o.normal, o.tangent) * i.tangent.w;
	float4 p = float4(i.vertex.x, i.vertex.y, 1, 1);
    p = p * _ProjectionParams.z;
    o.worldPos = mul(_ViewProjectionInverseMatrix, float4(p.xyzw));
	o.screenPos = ComputeScreenPos(o.pos);
	return o;
}

v2f_ray vert_ray(appdata_full i)
{
	v2f_ray o;
    o.pos = float4(i.vertex.x, i.vertex.y * _ProjectionParams.x, 1, 1);
    o.uv = i.texcoord;
	o.pos.y *= _ProjectionParams.x;
    float4 p = float4(i.vertex.x, i.vertex.y, 1, 1);
    p = p * _ProjectionParams.z;
    float3 worldPos = mul(_ViewProjectionInverseMatrix, float4(p.xyzw));
    o.ray = worldPos - _WorldCameraPos;
	o.ray = normalize(o.ray) * length(o.ray) * _ProjectionParams.w;
	return o;
}

v2f_no_clip_pos vert_no_clip_pos(appdata_full i, out float4 outpos : SV_POSITION)
{
	v2f_no_clip_pos o;
	outpos = UnityObjectToClipPos(i.vertex);
    o.uv = i.texcoord;
	o.normal = UnityObjectToWorldNormal(i.normal);
	o.tangent = UnityObjectToWorldDir(i.tangent.xyz);
    o.worldPos = mul(unity_ObjectToWorld, i.vertex);
	return o;
}

inline float3 tangentSpaceToWorld(v2f_legacy i, float3 v)
{
    return float3(dot(v, i.t2w0.xyz), dot(v, i.t2w1.xyz), dot(v, i.t2w2.xyz));
}

inline float3 fresnelFunc(float f0, float nv, float p) {
	return f0 + (1 - f0) * pow(1 - nv, p);
}

float3 tangentSpaceToWorld(float3 normal, float4 tangent4, float3 v)
{
	float3 tangent = normalize(tangent4.xyz);
	float3 binormal = cross(normal, tangent.xyz) * tangent4.w;
	float3x3 mat = {
		tangent.xyz,
		binormal.xyz,
		normal.xyz
	};
	mat = transpose(mat);
	return mul(mat, v);
}

float depthToWorldDistance(float2 screenCoord, float depthValue)
{
	float2 p = (screenCoord.xy * 2 - 1) * _DepthParams.xy;
	float3 ray = float3(p.xy, 1);
	return LinearEyeDepth(depthValue) * length(ray);
}

#endif