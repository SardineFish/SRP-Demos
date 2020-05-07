Shader "SarRP/TAA" {
    Properties {

    }
    HLSLINCLUDE

    #include "./Lib.hlsl"

    sampler2D _PreviousFrameBuffer;
    sampler2D _CurrentFrameBuffer;
    float4 _CurrentFrameBuffer_TexelSize;
    sampler2D _VelocityBuffer;
    float _Alpha;

    float3 RGBToYCoCg(float3 color)
    {
        float3x3 mat = {
            .25,  .5, .25,
            .5,   0,  -.5,
            -.25, .5, -.25
        };
        return mul(mat, color);
    }

    float3 YCoCgToRGB(float3 color)
    {
        float3x3 mat = {
            1,  1,  -1,
            1,  0,   1,
            1, -1,  -1
        };
        return mul(mat, color);
    }

    float4 blendFrame(v2f_light i) : SV_TARGET
    {
        float3 current = tex2D(_CurrentFrameBuffer, i.uv);
        float2 velocity = tex2D(_VelocityBuffer, i.uv);
        float2 previousUV = i.uv - velocity;

        float3 history;
        if(min(previousUV.x, previousUV.y) < 0 || max(previousUV.x, previousUV.y) > 1)
            history = current;
        else
            history = tex2D(_PreviousFrameBuffer, previousUV);

        float3 minNeighborhood = RGBToYCoCg(current);
        float3 maxNeighborhood = minNeighborhood;
        for(int y = -1; y <= 1; y++)
        {
            for(int x = -1; x <= 1; x++)
            {
                float3 neighborhoodColor = tex2D(_CurrentFrameBuffer, i.uv + float2(x, y) * _CurrentFrameBuffer_TexelSize.xy);
                neighborhoodColor = RGBToYCoCg(neighborhoodColor);
                minNeighborhood = min(minNeighborhood, neighborhoodColor);
                maxNeighborhood = max(maxNeighborhood, neighborhoodColor);
            }
        }
        history = RGBToYCoCg(history);
        history = clamp(history, minNeighborhood, maxNeighborhood);
        history = YCoCgToRGB(history);

        return float4(lerp(history, current, _Alpha), 1);
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