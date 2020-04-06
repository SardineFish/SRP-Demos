Shader "SarRP/TAA" {
    Properties {

    }
    HLSLINCLUDE

    #include "./Lib.hlsl"

    sampler2D _PreviousFrameBuffer;
    sampler2D _CurrentFrameBuffer;
    sampler2D _VelocityBuffer;
    float _Alpha;

    float4 blendFrame(v2f_light i) : SV_TARGET
    {
        float4 current = tex2D(_CurrentFrameBuffer, i.uv);
        float2 velocity = tex2D(_VelocityBuffer, i.uv);
        float2 previousUV = i.uv + velocity;
        if(min(previousUV.x, previousUV.y) < 0 || max(previousUV.x, previousUV.y) > 1)
        {
            return current;
        }

        float4 history = tex2D(_PreviousFrameBuffer, previousUV);
        return lerp(history, current, _Alpha);
    }

    ENDHLSL
    SubShader {
        // #0 Exponential TAA Blend
        Pass {

            HLSLPROGRAM

            #pragma vertex light_vert
            #pragma fragment blendFrame

            ENDHLSL
        }
    }
}