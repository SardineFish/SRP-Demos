Shader "SarRP/Postprocess/Fog" {
    HLSLINCLUDE

    #include "UnityCG.cginc"
    #include "../Lib.hlsl"

    float4 _FogDistance; // (near, far, (far - near))
    float _Density;
    float _Scale;
    float3 _Color;
    sampler2D _CameraDepthTex;
    sampler2D _MainTex;


    float4 renderFog(v2f_ray i) : SV_TARGET
    {
        float3 ray = normalize(i.ray);
        float depth = tex2D(_CameraDepthTex, i.uv).r;
        float3 worldPos = _WorldCameraPos + LinearEyeDepth(0) * i.ray;
        float z = distance(_WorldCameraPos, worldPos);
        z = smoothstep(_FogDistance.x, _FogDistance.y, z);
        float f = 1 - exp(-_Density * z);
        f *= _Scale;
        float3 color = tex2D(_MainTex, i.uv).rgb;
        color = f * _Color + (1 - f) * color;
        return float4(color.rgb, 1);
    }

    ENDHLSL
    SubShader {
        Tags {
            "RenderPipeline" = "SardineRenderPipeline"
        }
        // #0 
        Pass {
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert_ray
            #pragma fragment renderFog
            ENDHLSL
        }
    }
}