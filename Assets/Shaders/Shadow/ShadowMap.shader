Shader "SarRP/Shadow/ShadowMap" {
    Properties {

    }

    HLSLINCLUDE 

    #include "../Lib.hlsl"

    float simpleDepth(v2f i): SV_TARGET
    {
        return 0;//i.pos.z;
    }

    ENDHLSL

    SubShader {
        // #0 Simple shadow map
        Pass {
            Tags { "LightMode"="ShadowCaster" }
            ZTest Less
            ZWrite On
            Cull Back

            HLSLPROGRAM

            #pragma vertex default_vert
            #pragma fragment simpleDepth

            ENDHLSL
        }
    }
}