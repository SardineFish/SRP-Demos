Shader "SarRP/ForwardDefault" {
    Properties {
        _MainTex("Main Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
		_Normal ("Normal Textire", 2D) = "bump"{}
        _BumpScale ("Bump Scale", Float) = 1.0
    }

    HLSLINCLUDE

    #include "UnityCG.cginc"
    #include "./Lib.hlsl"
    #include "./Light.hlsl"
    #include "./Shadow/ShadowLib.hlsl"

    float4 _Color;
	sampler2D _MainTex;
	float4 _MainTex_ST;
	sampler2D _Normal;
	float4 _Normal_ST;
    float _BumpScale;

    float4 light(v2f_default i, float3 ambient)
    {
        float4 albedo = tex2D(_MainTex, i.uv) * _Color;
        float4 packNormal = tex2D(_Normal, i.uv); 
        float3 normal = UnpackNormal(packNormal);
        normal.xy *= _BumpScale;
        normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
        normal = tangentSpaceToWorld(i.normal, i.tangent, normal);
        

        float3 lightDir, lightColor;
        lightAt(i.worldPos, lightDir, lightColor);
        lightColor *= shadowAt(i);

        float3 diffuse = brdf_lambertian(albedo);
        float3 color = pbr_light(diffuse, lightColor, lightDir, normal) + ambient * albedo;
        color = color;

        return float4(color.rgb, albedo.a);
    }

    float4 forwardBase(v2f_default i) : SV_TARGET
    {
        return light(i, _AmbientLight);
    }

    float4 forwardAdd(v2f_default i) : SV_TARGET
    {
        return light(i, 0);
    }

    ENDHLSL

    SubShader {
        Tags {
            "RenderType" = "Opaque"
            "RenderPipeline" = "SardineRenderPipeline"
            "IgnoreProjector" = "true"
        }

        Pass {
            Tags {"LightMode" = "ForwardBase"}

            Cull Back
            ZWrite On
            ZTest Less

            HLSLPROGRAM

            #pragma vertex vert_default
            #pragma fragment forwardBase

            #pragma enable_d3d11_debug_symbols

            ENDHLSL

        }

        Pass {
            Tags {"LightMode" = "ForwardAdd"}

            Cull Back
            Blend One One
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex vert_default
            #pragma fragment forwardAdd

            #pragma enable_d3d11_debug_symbols

            ENDHLSL
        }
    }
}