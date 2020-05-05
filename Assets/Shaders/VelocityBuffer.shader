Shader "SarRP/VelocityBuffer" {
    Properties {}
    HLSLINCLUDE

    #include "./Lib.hlsl"

    sampler2D _CameraDepthTex;
    
    float4x4 _PreviousGPUViewProjection;
    float2 _PreviousJitterOffset;
    float2 _CurrentJutterOffset;
    
    
    float2 backgroundVelocityBuffer(v2f_ray i) : SV_TARGET
    {
        float3 ray = normalize(i.ray);
        float4 previousP = mul(_PreviousGPUViewProjection, float4(ray.xyz, 0));
        float4 currentP = mul(UNITY_MATRIX_VP, float4(ray.xyz, 0));
        previousP/= previousP.w;
        currentP /= currentP.w;
        currentP.y *= _ProjectionParams.x;
        float2 previousScreenPos = previousP.xy * .5 + .5;
        float2 currentScreenPos = currentP.xy * .5 + .5;
        previousScreenPos += _PreviousJitterOffset * (_ScreenParams.zw - 1);
        currentScreenPos += _CurrentJutterOffset * (_ScreenParams.zw - 1);

        return currentScreenPos - previousScreenPos;
    }

    ENDHLSL

    SubShader {
        // #0 Background Velocity Buffer Pass
        Pass {
            Name "Skybox Velocity Pass"

            Cull Off
            ZWrite Off
            ZTest Off

            HLSLPROGRAM

            #pragma vertex vert_ray
            #pragma fragment backgroundVelocityBuffer

            #pragma enable_d3d11_debug_symbols

            ENDHLSL
        }
    }
}