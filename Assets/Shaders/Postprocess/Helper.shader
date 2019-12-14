Shader "SarRP/Postprocess/Helper" {
    Properties {
        _MainTex("Main Texture", 2D) = "white" {}
    }
    HLSLINCLUDE

    #include "UnityCG.cginc"
    #include "../Lib.hlsl"

    sampler2D _MainTex;

    float4 grabScreen(v2f_light i) : SV_TARGET
    {
        float2 uv = i.uv;
        uv.y = 1 - uv.y;
        return tex2D(_MainTex, uv.xy).rgba;
    }

    ENDHLSL
    SubShader {
        Tags {
            "RenderPipeline" = "SardineRenderPipeline"
        }
        // #0 Grab screen
        Pass {
            Cull Off

            HLSLPROGRAM
            #pragma vertex light_vert
            #pragma fragment grabScreen
            ENDHLSL
        }
    }
}