Shader "SarRP/LightVolume/Raymarching" {
    Properties {
        _ExtinctionTex ("Extinction", 3D) = "white" {}
        _UVScale ("UV Scale", Vector) = (1, 1, 1, 1)
        _ExtinctionScale ("Extinction Scale", Color) = (1, 1, 1, 1)
        _LightAttenuation ("Light Attenuation", Range(0, 1)) = 1
        _Steps ("Steps", Int) = 64
    }

    HLSLINCLUDE

    #include "../Lib.hlsl"

    float intersectPlane(float4 plane, float3 origin, float3 dir, out bool intersect)
    {
        // t = -(O . P) / (D . P)
        float d = dot(dir, plane.xyz);
        intersect = d != 0;
        return -dot(float4(origin.xyz, 1), plane) / d;
    }

    uint _VolumeIndex;
    RWTexture2DArray<half2> _RWVolumeDepthTexture;
    Texture2DArray<half2> _VolumeDepthTexture;
    sampler3D _ExtinctionTex;
    sampler2D _ShadowMap;
    float4x4 _LightProjectionMatrix;
    int _UseShadow;
    float3 _UVScale;
    float4 _LightPosition;
    float3 _LightDirection;
    float _LightAngle;
    float3 _LightColor;
    int _Steps;
    float3 _ExtinctionScale;
    float _LightAttenuation;
    float4 _BoundaryPlanes[16];
    int _BoundaryPlaneCount;

    float4 volumeDepth(v2f_default i, fixed facing : VFACE) : SV_TARGET
    {
        uint3 coord = uint3(_ScreenParams.xy * i.pos.xy, _VolumeIndex);
        half2 depth = _RWVolumeDepthTexture[coord].rg;
        if(facing > 0)
            depth.r = distance(i.worldPos, _WorldCameraPos);
        else
            depth.g = distance(i.worldPos, _WorldCameraPos);
        _RWVolumeDepthTexture[coord] = depth;
        return float4(depth.rg, 0, 0);
    }

    float3 extinctionAt(float3 pos)
    {
        return tex3D(_ExtinctionTex, pos * _UVScale).rgb * _ExtinctionScale;
    }

    float shadowAt(float3 pos)
    {
        if(_UseShadow == 0)
            return 1;
        float4 p = float4(pos.xyz, 1);
        p = mul(_LightProjectionMatrix, p);
        p /= p.w;
        p.z = 1 - (0.5 * p.z + .5);
        float2 uv = p.xy * .5 + .5;
	    float2 clip = step(0, uv.xy) * (1 - step(1, uv.xy));
        float shadowDepth = tex2D(_ShadowMap, uv) * clip.x * clip.y;
	    if(shadowDepth <= 0)
	    	shadowDepth = -100;
        return step(shadowDepth, p.z);
    }

    float3 lightAt(float3 pos)
    {
	    float3 lightDir = normalize(_LightPosition.xyz - pos * _LightPosition.w);
        float lightDistance = distance(_LightPosition.xyz, pos);
        float3 transmittance = lerp(1, exp(-lightDistance * _ExtinctionScale), _LightAttenuation);

	    float3 lightColor = _LightColor.rgb;
        lightColor *= step(_LightAngle, dot(lightDir, _LightDirection));
        lightColor *= shadowAt(pos);
        lightColor *= transmittance;

        return lightColor;
    }

    float3 scattering(float3 ray, float near, float far, out float3 transmittance) // Alpha is total transmittance
    {
        transmittance = 1;
        float3 totalLight = 0;
        float stepSize = (far - near) / _Steps;
        for(int i = 1; i <= _Steps; i++)
        {
            float3 pos = _WorldCameraPos + ray * (near + stepSize * i);
            transmittance *= exp(-stepSize * extinctionAt(pos));
            totalLight += transmittance * lightAt(pos) * stepSize;
        }
        return totalLight;
    }

    float getBoundary(float3 ray, out float near, out float far)
    {
        float maxNear = -1e100;
        float minFar = 1e100;
        bool intersected = false;
        for(int i = 0; i < _BoundaryPlaneCount; i++)
        {
            float t = intersectPlane(_BoundaryPlanes[i], _WorldCameraPos, ray, intersected);
            if(intersected && dot(ray, _BoundaryPlanes[i].xyz) < 0) // frontface
                maxNear = max(maxNear, t);
            else if(intersected)
                minFar = min(minFar, t);
        }
        near = maxNear;
        far = minFar;
        return minFar - maxNear;
    }

    float4 volumeLight(v2f_default i) : SV_TARGET
    {
        float3 ray = normalize(i.worldPos - _WorldCameraPos);
        uint3 coord = uint3(_ScreenParams.xy * i.pos.xy, _VolumeIndex);
        float near, far, depth;

        depth = getBoundary(ray, near, far);

        float3 transmittance = 1;
        float3 color = 0;
        color = scattering(ray, near, far, transmittance);

        return float4(color.rgb, 1);
    }

    ENDHLSL

    SubShader {
        // #0 Distance between front & backface
        Pass {
            Name "Light Volume Raymarching"
            ZTest Off
            ZWrite Off
            Cull Off
            Blend One Zero

            HLSLPROGRAM

            #pragma vertex vert_default
            #pragma fragment volumeDepth
            #pragma target 5.0

            ENDHLSL
        }
        // #1 Volumetric scattering
        Pass {
            Name "Light Volume Scattering"
            ZTest Less
            ZWrite On
            Cull Back
            Blend One One

            HLSLPROGRAM

            #pragma vertex vert_default
            #pragma fragment volumeLight
            #pragma target 5.0
            
    #pragma enable_d3d11_debug_symbols

            ENDHLSL
        }
    }
}