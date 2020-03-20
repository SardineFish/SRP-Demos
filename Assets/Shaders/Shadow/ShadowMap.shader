Shader "SarRP/Shadow/ShadowMap" {
    Properties {

    }

    HLSLINCLUDE 

    #include "../Lib.hlsl"
    
    #pragma enable_d3d11_debug_symbols

    

    

    ENDHLSL

    SubShader {
        // #0 Simple shadow map
        Pass {
            Tags { "LightMode"="ShadowCaster" }
            Name "Standard Shadow Map"
            ZTest Less
            ZWrite On
            Cull Back

            HLSLPROGRAM

            #pragma vertex vert_legacy
            #pragma fragment frag

            float frag(v2f_legacy i): SV_TARGET
            {
                return 0;//i.pos.z;
            }

            ENDHLSL
        }

        // #1 PSM shadow map
        Pass {
            Tags { "LightMode"="ShadowCaster" }
            Name "PSM Shadow Map"
            ZTest Less
            ZWrite On
            Cull Off

            HLSLPROGRAM

            #pragma vertex psmVert
            #pragma fragment psmFrag

            float4x4 _LightViewProjection;
            float4 _ShadowParameters;
        
            v2f_light psmVert(appdata_base i)
            {
                v2f_light o;
                float4 pClip = UnityObjectToClipPos(i.vertex);
                
                float4 pNDC = pClip / pClip.w;
                pNDC.y *= _ProjectionParams.x;
                pNDC.w = 1;
                
                o.pos = mul(_LightViewProjection, pNDC);
                
                o.uv = 0;
                return o;
            }
        
            float psmFrag(v2f_light i, out float depth : SV_DEPTH) : SV_TARGET
            {
                if(_ShadowParameters.x > 0)
                    depth = 1 - i.pos.z;
                else
                    depth = i.pos.z;
                return 0;
            }

            ENDHLSL
        }

        // #2 TSM shadow map
        Pass {
            Tags { "LightMode"="ShadowCaster" }
            Name "TSM Shadow Map"
            ZTest Less
            ZWrite On
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma enable_d3d11_debug_symbols

            struct v2f_tsm
            {
                float4 pos : SV_POSITION;
                float4 ndcPos : TEXCOORD0;
            };

            float4x4 _ShadowPostTransform;

            v2f_tsm vert(appdata_base i)
            {
                v2f_tsm o;
                float4 p = UnityObjectToClipPos(i.vertex);
                o.ndcPos = p;
                p /= p.w;
                p.w = 1;
                p = mul(_ShadowPostTransform, p);
                o.pos = p;
                return o;
            }

            float _SlopeDepthBias;
            float _DepthBias;
            
            float frag(v2f_tsm i, out float depth : SV_DEPTH) : SV_TARGET
            {
                i.ndcPos /= i.ndcPos.w;
                float2 depthDelta = float2(ddx(i.ndcPos.z), ddy(i.ndcPos.z));
                float maxDepthSlope = max(depthDelta.x , depthDelta.y);
                float exponent = log2(i.ndcPos.z);
                exponent -= 23;
                float factor = pow(2, exponent);
                float bias = _DepthBias * factor + _SlopeDepthBias * maxDepthSlope;
                depth = i.ndcPos.z + bias;
                return i.ndcPos.z;
            }

            ENDHLSL
        }
    }
}