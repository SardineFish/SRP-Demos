Shader "SarRP/VelocityBuffer" {
    HLSLINCLUDE

    #include "./Lib.hlsl"

    sampler2D _CameraDepthTex;
    float4x4 _PreviousGPUViewProjection;

    float2 velocityBuffer(v2f_ray i) : SV_TARGET
    {
        float depth = tex2D(_CameraDepthTex, i.uv.xy).r;
        float3 worldPos = _WorldCameraPos + LinearEyeDepth(depth) * i.ray;
        float4 pClip = mul(_PreviousGPUViewProjection, float4(worldPos.xyz, 1));
        pClip /= pClip.w;
        float2 screenPos = pClip * .5 + .5;
        return screenPos - i.uv.xy;
    }

    ENDHLSL

    SubShader {
        Pass {
            Cull Off
            
            HLSLPROGRAM

            #pragma vertex vert_ray
            #pragma fragment velocityBuffer

            #pragma enable_d3d11_debug_symbols

            ENDHLSL

        }
    }
}