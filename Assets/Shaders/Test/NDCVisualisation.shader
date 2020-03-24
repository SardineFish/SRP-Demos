Shader "SarRP/Test/NDCVisualisation" {
    Properties { 
        _MainTex("Main Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
		_Normal ("Normal Textire", 2D) = "bump"{}
        _BumpScale ("Bump Scale", Float) = 1.0
    }

    HLSLINCLUDE

    #include "../Lib.hlsl"
    #include "../Light.hlsl"
    
    #pragma enable_d3d11_debug_symbols

    float4x4 _CameraViewProjection;
    float4x4 _LightViewProjection;
    int _EnableLightTransform;

    v2f_legacy vert(appdata_full i)
    {
        v2f_legacy o;
	    float3 p = mul(unity_ObjectToWorld, i.vertex);
        o.pos = mul(_CameraViewProjection, float4(p.xyz, 1));
	    o.pos /= o.pos.w;
	    o.pos.w = 1;
	    o.pos = UnityWorldToClipPos(o.pos);
        o.uv = i.texcoord;
        o.worldPos = mul(unity_ObjectToWorld, i.vertex);
        float3 worldNormal = UnityObjectToWorldNormal(i.normal);
	    float3 worldTangent = UnityObjectToWorldDir(i.tangent.xyz);
	    float3 worldBinormal = cross(worldNormal, worldTangent) * i.tangent.w;
	    o.t2w0 = float3(worldTangent.x, worldBinormal.x, worldNormal.x);
	    o.t2w1 = float3(worldTangent.y, worldBinormal.y, worldNormal.y);
	    o.t2w2 = float3(worldTangent.z, worldBinormal.z, worldNormal.z);
        o.screenPos = ComputeScreenPos(o.pos);
        return o;
    }


    float4 _Color;
	sampler2D _MainTex;
	float4 _MainTex_ST;
	sampler2D _Normal;
	float4 _Normal_ST;
    float _BumpScale;

    float4 frag(v2f_legacy i) : SV_TARGET
    {
        float4 albedo = tex2D(_MainTex, i.uv) * _Color;
		float3 ambient = _AmbientLight.rgb * albedo.rgb;
		float4 packNormal = tex2D(_Normal, i.uv); 
        // float shadow = SHADOW_ATTENUATION(i);
        // UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
		float3 normal = UnpackNormal(packNormal);
        normal.xy *= _BumpScale;
        normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
        
		normal = normalize(float3(dot(normal, i.t2w0.xyz), dot(normal, i.t2w1.xyz), dot(normal, i.t2w2.xyz)));

		float3 lightDir, lightColor;
        lightAt(i.worldPos, lightDir, lightColor);

		float nl = saturate(dot(normal, lightDir));
        
        float3 light = lightColor;// * atten;
		float3 diffuseTerm = albedo.rgb * light * nl + ambient;
		float3 color = diffuseTerm;

		return float4(color, albedo.a); 
    }

    ENDHLSL

    SubShader {
        Tags {
            "RenderType" = "Opaque"
            "RenderPipeline" = "SardineRenderPipeline"
            "IgnoreProjector" = "true"
        }

        Pass {
            Tags { "LightMode"="ForwardLit" }

            Cull Off

            HLSLPROGRAM
    
            #pragma vertex vert
            #pragma fragment frag
    
            ENDHLSL
        }
    }
}