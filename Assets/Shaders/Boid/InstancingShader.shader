Shader "SarRP/Boid/Instancing" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
    }

    HLSLINCLUDE

    ENDHLSL

    SubShader {
        // #0 Instancing
        Pass {
            Cull Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 4.5
            #pragma enable_d3d11_debug_symbols

    #include "UnityCG.cginc"

    struct EntityData
    {
        float3 position;
        float3 velocity;
        float3 up;
        float4x4 rotation;
    };
    struct v2f_legacy
    {
        float4 pos : SV_POSITION;
        float3 color : TEXCOORD0;
        float3 worldPos : TEXCOORD1;
        float3 normal : TEXCOORD2;
    };

    StructuredBuffer<EntityData> boidBuffer;

    float4 _MainLightPosition;
    float4 _MainLightColor;
    float4 _AmbientLight;
    float3 _WorldCameraPos;

    v2f_legacy vert(appdata_full i, uint instanceID : SV_INSTANCEID)
    {
        EntityData data = boidBuffer[instanceID];
        v2f_legacy o;
        float4 p = float4(i.vertex.xyz, 1);
        p = mul(data.rotation, p);
        p.xyz += data.position.xyz;
        o.pos = UnityWorldToClipPos(p.xyz);
        o.worldPos = p.xyz;
        o.color = 1;
        float3 viewDir = normalize(_WorldCameraPos - o.worldPos);
        float3 normal = mul(data.rotation, float4(i.normal.xyz, 1)).xyz;
        o.normal = normalize(normal);
        
        return o;
    }

    void lightAt(float3 worldPos, out float3 lightDir, out float3 lightColor)
    {
    	lightDir = normalize(_MainLightPosition.xyz - worldPos * _MainLightPosition.w);
    	lightColor = _MainLightColor.rgb;
    	return;
    }

    float4 frag(v2f_legacy i) : SV_TARGET
    {
        float3 lightDir;
        float3 lightColor;
        float3 viewDir = normalize(_WorldCameraPos - i.worldPos);
        float3 normal = normalize(i.normal);
        if(dot(normal, viewDir) <= 0)
            normal = -normal;
        lightAt(i.worldPos, lightDir, lightColor);

        float3 color = saturate(dot(normal, lightDir)) * lightColor;
        color += saturate(dot(normal, viewDir)) * lightColor * 0.3;
        color += _AmbientLight;
        color *= i.color;

        return float4(color.xyz, 1);
    }
    


            ENDHLSL
        }
    }
}