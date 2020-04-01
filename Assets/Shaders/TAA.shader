Shader "SarRP/TAA" {
    Properties {

    }
    HLSLINCLUDE

    #include "./Lib.hlsl"

    sampler2D _PreviousFrameBuffer;
    sampler2D _CurrentFrameBuffer;
    float _Alpha;

    float4 blendFrame(v2f_light i) : SV_TARGET
    {
        float4 history = tex2D(_PreviousFrameBuffer, i.uv);
        float4 current = tex2D(_CurrentFrameBuffer, i.uv);
        return lerp(history, current, _Alpha);
        return  _Alpha * tex2D(_CurrentFrameBuffer, i.uv) + (1 - _Alpha) * tex2D(_PreviousFrameBuffer, i.uv);
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