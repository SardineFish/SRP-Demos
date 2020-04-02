Shader "SarRP/LightVolume/Raymarching" {
    Properties {
        [HideInInspector] _MainTex ("MainTex", 2D) = "white" {}
        _ExtinctionTex ("Extinction", 3D) = "white" {}
        _UVScale ("UV Scale", Vector) = (1, 1, 1, 1)
        _ExtinctionScale ("Extinction Scale", Color) = (1, 1, 1, 1)
        _LightAttenuation ("Light Attenuation", Range(0, 1)) = 1
        _Steps ("Steps", Int) = 64
        _Seed ("Seed", Float) = 1
        _HGFactor ("HG Phase Factor", Range(-1, 1)) = 0
        //_SampleNoise ("Sample Noise", 2D) = "black" {}
    }

    HLSLINCLUDE

    #include "../Lib.hlsl"
    #include "../Light.hlsl"
    #include "../Noise.hlsl"

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
    int _Steps;
    float3 _ExtinctionScale;
    float _LightAttenuation;
    float4 _BoundaryPlanes[16];
    int _BoundaryPlaneCount;
    float _Seed;
    float4 _FrameSize;
    float _HGFactor;
    
    sampler2D _SampleNoise;
    float4 _SampleNoise_TexelSize;

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

    float phaseHG(float3 lightDir, float3 viewDir)
    {
        float g = _HGFactor;
        return (1 - g * g) / (4 * PI * pow(1 + g * g - 2 * g * dot(viewDir, lightDir), 1.5)); 
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
        lightColor *= step(_LightCosHalfAngle, dot(lightDir, _LightDirection));
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

    float sampleOffset(float2 screenPos)
    {
        //return 0;
        // return gold_noise(screenPos, _Seed);
        return tex2D(_SampleNoise, screenPos * _FrameSize.xy * _SampleNoise_TexelSize.xy) * 2 - 1;
    }

    float4 volumeLight(v2f_default i) : SV_TARGET
    {
        i.screenPos /= i.screenPos.w;
        float3 ray = normalize(i.worldPos - _WorldCameraPos);
        float near, far, depth;
        
        float3 nearWorldPos = _WorldCameraPos + ray * near;

        depth = getBoundary(ray, near, far);

        float offset = sampleOffset(i.screenPos.xy) * (far - near) / _Steps;
        far += offset;
        near +=offset;

        float3 transmittance = 1;
        float3 color = 0;
        color = scattering(ray, near, far, transmittance);



        return float4(color, 1);
    }

    sampler2D _CameraDepthTex;
    float _GlobalFogExtinction;

    float4 globalFog(v2f_ray i) : SV_TARGET
    {
        float3 ray = normalize(i.ray);
        float depth = tex2D(_CameraDepthTex, i.uv).r;
        float3 worldPos = _WorldCameraPos + LinearEyeDepth(depth) * i.ray;
        float z = distance(_WorldCameraPos, worldPos);
        float transmittance = exp(-_GlobalFogExtinction * z);

        float3 color = _AmbientLight * (1 - transmittance);
        return float4(color.rgb, 1 - transmittance);
    }

    sampler2D _MainTex;
    float4 _MainTex_TexelSize;

    float randf(float2 pos, float seed)
    {
        return gold_noise(pos, seed) *.5 + .5;
    }

    float4 mixScreen(v2f_default i) : SV_TARGET
    {
        float4 col = 0;
        float R = 8;
        for(int j = 0; j < 4; j++)
        {
            float dist = j * R;
            dist += randf(i.uv.xy, _Seed + j);
            dist = sqrt(dist);
            float2 rot;
            float ang = randf(i.uv.xy, _Seed + j) * 2 * PI;
            sincos(ang, rot.x, rot.y);
            float2 offset = rot * dist * _MainTex_TexelSize.xy;
            col += tex2D(_MainTex, i.uv.xy + offset).rgba;
        }
        //return tex2D(_MainTex, i.uv.xy);
        return col / 4;
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
            ZTest Off
            ZWrite On
            Cull Back
            Blend One One

            HLSLPROGRAM

            #pragma vertex vert_default
            #pragma fragment volumeLight
            #pragma target 5.0
            
            //#pragma enable_d3d11_debug_symbols

            ENDHLSL
        }

        // #2 Blit to Screen Buffer
        Pass {
            Name "Light Volume Scattering"
            ZTest Off
            ZWrite On
            Cull Back
            Blend One One

            HLSLPROGRAM

            #pragma vertex vert_default
            #pragma fragment mixScreen
            #pragma target 5.0
            
            //#pragma enable_d3d11_debug_symbols

            ENDHLSL
        }

        // #3 Global Fog
        Pass {
            Name "Global Fog"
            ZTest Off
            ZWrite On
            Cull Off
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma vertex vert_ray
            #pragma fragment globalFog
            #pragma target 5.0
            
            //#pragma enable_d3d11_debug_symbols

            ENDHLSL
        }
    }
}