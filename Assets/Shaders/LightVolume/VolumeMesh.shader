Shader "SarRP/LightVolume/VolumeMesh" {
    Properties {

    }

    HLSLINCLUDE

    #include "../Lib.hlsl"

    

    float frag(v2f_default i) : SV_TARGET
    {
        return i.worldPos.z;
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

            #pragma vertex vert_default
            #pragma fragment frag

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

            HLSLPROGRAM

            #pragma vertex vert_default
            #pragma fragment frag

            ENDHLSL
        }
    }
}