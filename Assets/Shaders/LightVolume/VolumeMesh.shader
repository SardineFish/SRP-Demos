Shader "SarRP/LightVolume" {
    Properties {

    }

    HLSLINCLUDE

    #include "../Lib.hlsl"

    float frag(v2f_light i) : SV_TARGET
    {
        return i.pos.z;
    }

    ENDHLSL

    SubShader {
        // #0 Back face render
        Pass {
            Name "Volume Mesh Back"
            Cull Front
            ZTest Off
            ZWrite Off
            Blend One One
            BlendOp Add

            HLSLPROGRAM

            #pragma vertex light_vert
            #pragma fragment 

            ENDHLSL
        }

        // #1 Front face render
        Pass {
            Name "Volume Mesh Front"
            Cull Back
            ZTest Off
            ZWrite Off
            Blend One One
            BlendOp Sub
        }
    }
}