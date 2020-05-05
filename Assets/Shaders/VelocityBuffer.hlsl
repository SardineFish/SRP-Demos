#ifndef SAR_VELOCITY_BUFFER
#define SAR_VELOCITY_BUFFER

#include "./Lib.hlsl"

float4x4 _PreviousGPUViewProjection;
float2 _PreviousJitterOffset;
float2 _CurrentJutterOffset;

float4 unity_MotionVectorsParams;
float4x4 unity_MatrixPreviousM;

struct v2f_velocity
{
    float4 pos : SV_POSITION;
    float3 vertexPos : TEXCOORD0;
    float3 worldPos : TEXCOORD1;
    float4 screenPos : TEXCOORD2;
};
v2f_velocity vert_velocity(appdata_full i)
{
    v2f_velocity output;
    output.pos = UnityObjectToClipPos(i.vertex);
    output.vertexPos = i.vertex;
    output.worldPos = mul(unity_ObjectToWorld, float4(i.vertex.xyz, 1));
    output.screenPos = ComputeScreenPos(output.pos);
    return output;
}
float2 frag_velocity(v2f_velocity i) : SV_TARGET
{
    i.screenPos /= i.screenPos.w;
    //float depth = tex2D(_CameraDepthTex, i.uv.xy).r;
    float3 worldPos = mul(unity_MatrixPreviousM, float4(i.vertexPos.xyz, 1));
    if(unity_MotionVectorsParams.y == 0)
        worldPos = i.worldPos;
    float4 pClip = mul(_PreviousGPUViewProjection, float4(worldPos.xyz, 1));
    pClip /= pClip.w;
    float2 currentScreenPos = i.screenPos;
    float2 previousScreenPos = pClip * .5 + .5;
    //float2 jitterOffset = (_CurrentJutterOffset - _PreviousJitterOffset) * (_ScreenParams.zw - 1);
    //_PreviousJitterOffset.y *= -1;
    //_CurrentJutterOffset.y *= -1;
    previousScreenPos += _PreviousJitterOffset * float2(1, 1) * (_ScreenParams.zw - 1);
    currentScreenPos += _CurrentJutterOffset * float2(1, 1) * (_ScreenParams.zw - 1);
    return (currentScreenPos - previousScreenPos);
}

#endif