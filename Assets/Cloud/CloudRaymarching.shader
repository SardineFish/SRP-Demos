Shader "Cloud/Raymarching" {
    Properties {
        _NoiseTex("Noise Texture", 3D) = "white" {}
        _DetailNoiseTex("Detail Noise Texture", 3D) = "white" {}
        _HeightGradient("Height Gradient", 2D) = "white" {}
        _CoverageTex("Coverage Texture", 2D) = "white" {}
        _CoverageScale("Coverage Scale", Float) = 1
        _Color("Color", Color) = (1, 1, 1, 1)
        _LightColor("Color", Color) = (1, 1, 1, 1)
        _DarkColor("Color", Color) = (.5, .5, .5, 1)
        _AmbientStrength("AmbientStrength", Float) = 1
        _Scale("Scale", Float) = 1
        _HeightScale("Height Scale", Float) = 1
        _HeightOffset("Height Offset", Range(0, 1)) = 0
        _NoiseAmplitude("Cloud Noise Amplitude", Vector) = (.5, .25, .125, .0625)
        _DetailAmplitude("Detail Noise Amplitude", Vector) = (.5, .25, .125, .0625)
        _DetailScale("Detail Scale", Float) = 1
        _DetailStrength("Detail Strength", Float) = 1
        _Near("Near", Float) = 0
        _Far("Far", Float) = 20
        _Step("Step", Float) = 0.5
        _Samples("Sample Count", Int) = 64
        _CloudThreshold("Cloud Threshold", Range(0, 1)) = .5
        _DensityScale("Density Scale", Range(0, 2)) = .5
        _LightScale("Light Scale", Float) = 64
        _GroundRadius("Ground Radius", Float) = 200
        _CloudBottom("Min Cloud Altitude", Float) = 80
        _CloudTop("Max Cloud Altitude", Float) = 90
        _FBMOctave("FBM Octave", Int) = 4
        _CloudType("Cloud Type", Range(0, 1)) = 0
        _CloudShapeExponent("Cloud Shape Exponent", Float) = 1
        _ScatterFactor("Scatter Factor", Range(-1, 1)) = 0
        _ScatterDistanceMultiply("Scatter Distance Multiply", Float) = .5
        _ScatterExtend("Scatter Extend", Float) = 1
        _OcclusionSampleDistance("Occlusion Distance", Float) = 1
        _Absorption("Cloud Absorption", Float) = 1
        _AbsorptionToLight("Absorption To Light", Float) = 1
        _PowderEffectScale("Powder Effect", Float) = 1
        _MotionSpeed("MotionSpeed", Float) = 1
        _NoiseMotionVelocity("Noise Velocity", Vector) = (0, 0, 0, 0)
        _DetailMotionVelocity("Detail Velocity", Vector) = (0, 0, 0, 0)
        _CoverageMotionVelocity("Coverage Velocity", Vector) = (0, 0, 0, 0)
        _CurlTexScale("Curl Noise Motion Scale", Float) = 1
        _CurlMotionStrength("Curl Noise Motion Strength", Float) = 1
        _DetailCurlScale("Detail Curl Motion  Scale", Float) = 1
        _DetailCurlStrength("Detail Curl Motion Scale", Float) = 1
    }

    HLSLINCLUDE

    #include "UnityCG.cginc"

    struct v2f
    {
        float4 pos : SV_POSITION;
        float2 uv : TEXCOORD0;
        float3 ray : TEXCOORD1;
        float3 worldPos : TEXCOORD2;
    };

    #define PI (3.14159265358979323846264338327950288419716939937510)

    
    float3 _WorldCameraPos;
    float4 _CameraClipPlane;
    float4x4 _ViewProjectionInverseMatrix;
    float _Step;
    float _Near;
    float _Far;
    
    float4 _MainLightDirection;
    float4 _MainLightColor;
    float4 _AmbientSkyColor;
    sampler2D _CurlNoiseMotionTex;

    Texture3D _NoiseTex;
    Texture3D _DetailNoiseTex;
    SamplerState detail_linear_repeat_sampler;
    SamplerState noise_linear_repeat_sampler;
    sampler2D _HeightGradient;
    sampler2D _CoverageTex;
    float _CoverageScale;
    float _CoverageThreshold;
    float4 _Color;
    float4 _LightColor;
    float4 _DarkColor;
    float _AmbientStrength;
    float _Scale;
    float _HeightScale;
    float _DetailScale;
    float4 _NoiseAmplitude;
    float _CloudThreshold;
    float _DensityScale;
    float4 _DetailAmplitude;
    float _DetailStrength;
    float _LightScale;
    float _CloudBottom;
    float _CloudTop;
    float _GroundRadius;
    float _Samples;
    int _FBMOctave;
    float _BeerLawScale;
    float _CloudType;
    float _CloudShapeExponent;
    float _ScatterFactor;
    float _ScatterDistanceMultiply;
    float _ScatterDistance;
    float _ScatterExtend;
    float _OcclusionSampleDistance;
    float _Absorption;
    float _AbsorptionToLight;
    float _PowderEffectScale;

    float _MotionSpeed;
    float3 _NoiseMotionVelocity;
    float3 _DetailMotionVelocity;
    float2 _CoverageMotionVelocity;
    float _CurlTexScale;
    float _CurlMotionStrength;
    float _DetailCurlScale;
    float _DetailCurlStrength;
    

    v2f cloudCubeVert(appdata_full i)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(i.vertex);
        o.uv = i.texcoord;
        o.worldPos = mul(unity_ObjectToWorld, i.vertex);
        o.ray = o.worldPos - _WorldCameraPos;
        return o;
    }

    v2f cloudSkyVert(appdata_full i)
    {
        v2f o;
        o.pos = float4(i.vertex.x, i.vertex.y * _ProjectionParams.x, 1, 1);
        o.uv = i.texcoord;
        float4 p = float4(i.vertex.x, i.vertex.y, 1, 1);
        p = p * _ProjectionParams.z;
        o.worldPos = mul(_ViewProjectionInverseMatrix, float4(p.xyzw));
        o.ray = normalize(o.worldPos - _WorldCameraPos);
        return o;
    }

    inline float2 curlNoiseMotion(float2 uv, float uvScale, float strength)
    {
        return tex2D(_CurlNoiseMotionTex, uv.xy / uvScale).xy * strength;
    }

    inline float sampleNoise(float3 uv)
    {
        float3 detailUV = uv.xyz / _DetailScale;
        float2 coverageUV = uv.xy / _CoverageScale;
        float3 cloudUV = uv.xyz;

        // Apply motion
        detailUV.xyz += _Time.y * _MotionSpeed * _DetailMotionVelocity;
        //detailUV.xy += curlNoiseMotion(uv.xy, _DetailCurlScale, _DetailCurlStrength);
        coverageUV += _Time.y * _MotionSpeed * _CoverageMotionVelocity;
        cloudUV.xyz += _Time.y * _MotionSpeed * _NoiseMotionVelocity;
        //cloudUV.xy += curlNoiseMotion(uv.xy, _CurlTexScale, _CurlMotionStrength);

        float coverage = tex2D(_CoverageTex, coverageUV).r;
        float4 noise = _NoiseTex.Sample(noise_linear_repeat_sampler, cloudUV).rgba;
        float4 detailNoise = _DetailNoiseTex.Sample(detail_linear_repeat_sampler, detailUV).rgba;
        float n = dot(noise, _NoiseAmplitude);
        float dn = dot(detailNoise, _DetailAmplitude);
        dn = lerp(-dn, dn, saturate(cloudUV.z * _HeightScale * 4));
        n -= dn * _DetailStrength;
        return n * coverage;
    }

    inline float fbm(float3 pos)
    {
        float value = 0;
        float amplitude = .5f;
        if(_FBMOctave < 2)
            return sampleNoise(pos) * .5 + .5;
        [loop]
        for(int i = 0; i < _FBMOctave; i++)
        {
            value += amplitude * sampleNoise(pos);
            pos *= 2;
            amplitude *= .5;
        }
        return value * .5 + .5;
    }

    inline float cloudShape(float height)
    {
        float4 heightGradient = tex2D(_HeightGradient, float2(.5, height));
        return pow(
            lerp(
                lerp(heightGradient.r, heightGradient.g, smoothstep(0, .5, _CloudType)), 
                heightGradient.b, 
                smoothstep(.5, 1, _CloudType)),
            _CloudShapeExponent) * pow(heightGradient.a, _CloudShapeExponent);
    }

    inline float cloudDensity(float noise)
    {
        return saturate((noise - _CloudThreshold) / (1 - _CloudThreshold)) * _DensityScale;
    }

    inline float raymarchingMinDistance(float3 ray)
    {
        float t = (_CloudBottom - _WorldCameraPos.y) / ray.y;
        return t;
    }
    inline float raymarchingMaxDistance(float3 ray)
    {
        float t = (_CloudTop - _WorldCameraPos.y) / ray.y;
        return t;
    }
    inline float pow2(float3 v)
    {
        return pow(length(v), 2);
    }
    inline bool rayHitSphere(float3 o, float3 p, float3 ray, float r, out float t)
    {
        if(4*pow(dot(ray, o-p), 2) - 4*pow2(ray)*(pow2(o - p) - pow(r, 2)) < 0)
        {
            t = 0;
            return false;
        }
        t = length(p - o) >= r
            ? (1/pow2(ray) * (
                dot(ray, o) 
                - dot(ray, p)
                - .5*sqrt(
                    4*pow(dot(ray, o-p), 2)
                    - 4*pow2(ray)*(
                        pow2(o - p) - 
                        pow(r, 2)
                    )
                )))
            : (1/pow2(ray) * (
                dot(ray, o) 
                - dot(ray, p)
                + .5*sqrt(
                    4*pow(dot(ray, o-p), 2)
                    - 4*pow2(ray)*(
                        pow2(o - p) - 
                        pow(r, 2)
                    )
                )
            ));
        if (t < 0)
            return false;
        return true;
        /*
        1/Norm[dir]^2 * (
            Dot[dir, o] 
            - Dot[dir, p] 
            + 0.5*Sqrt[
                4*Dot[dir, o - p]^2 
                - 4*Norm[dir]^2*(
                    Norm[o - p]^2 
                    - r^2
                )
            ]
        )
        */

    }
    inline float3 toSphereCoord(float3 p)
    {
        float2 u = normalize(float2(p.y, p.z));
        float2 v = normalize(p);
        return float3(atan2(u.y, u.x), asin(v.x), length(p));
    }

    inline float cloudDensityAt(float3 pos)
    {
        float3 earthCenter = float3(0, -_GroundRadius, 0);
        float3 coord = toSphereCoord(pos - earthCenter);
        float2 coverageUV = coord.xy / _CoverageScale;
        coord.xy /= _Scale;
        float height = smoothstep(_GroundRadius + _CloudBottom, _GroundRadius + _CloudTop, coord.z);//
        coord.z = height / _HeightScale;
        return cloudDensity(sampleNoise(coord) * cloudShape(height));// * smoothstep(_CoverageThreshold, 1, tex2D(_CoverageTex, coverageUV).r * .5 + .5);
    }

    inline float attenuation(float d)
    {
        d *= _BeerLawScale;
        return exp(-d) * (1 - exp(-d * 2));
    }

    inline float phaseHG(float VdotL)
    {
        return 1 / (4*PI) * (1 - pow(_ScatterFactor, 2)) / pow(1 + pow(_ScatterFactor, 2) - 2 * _ScatterFactor * VdotL, 3/2);
    }

    inline float3 sampleLightScattered(float3 pos, float3 ray, float dist, float near, float far)
    {
        float occlusion = 0;
        float lightDir = -_MainLightDirection;

        // occlusion += cloudDensityAt(pos + (lightDir * _ScatterDistance + normalize(float3(1, -1, 1)) * _ScatterExtend) * 0.2);
        // occlusion += cloudDensityAt(pos + (lightDir * _ScatterDistance + normalize(float3(-1, -1, 1)) * _ScatterExtend) * 0.2);
        // occlusion += cloudDensityAt(pos + (lightDir * _ScatterDistance + normalize(float3(1, 1, -1)) * _ScatterExtend) * 0.2);
        // occlusion += cloudDensityAt(pos + (lightDir * _ScatterDistance + normalize(float3(-1, -1, -1)) * _ScatterExtend) * 0.2);
        // occlusion += cloudDensityAt(pos + (lightDir * _ScatterDistance + normalize(float3(1, 1, 1)) * _ScatterExtend) * 0.2);
        // occlusion += cloudDensityAt(pos +(lightDir * _OcclusionSampleDistance));
        
        for(float i = 1;i<6;i++)
        {
            occlusion += cloudDensityAt(pos + (lightDir *_ScatterDistance * (i / 5))) * .2;
        }
        
        float atten = exp(_Absorption * -occlusion) * (1 - exp(-(dist - near) * _BeerLawScale * 2));// * phaseHG(dot(lightDir, -ray));
        return 1- occlusion;
        return atten;// _MainLightColor * atten;
    }

    inline float raymarchOcclusion(float3 pos)
    {
        float occlusion = 0;
        float scatterDistance = (_CloudTop - _CloudBottom) * _ScatterDistanceMultiply;
        [unroll]
        for(float i = 1; i < 6; i++)
        {
            occlusion += cloudDensityAt(pos + (-_MainLightDirection * scatterDistance * (i / 5)))* (scatterDistance * .2);
        }
        return occlusion;
    }

    #define MAX_ITERATION 128
    #define IGNORE_DENSITY_THRESHOLD (0.00001) // Will not perform scatter lighting raymarch less than this value

    inline float raymarchingCloud(float3 ray, out float3 light)
    {
        light = 0;
        float3 earthCenter = float3(0, -_GroundRadius, 0);
        float3 cameraCoord = toSphereCoord(_WorldCameraPos - earthCenter);
        float near = 0, far = 0;
        // Under cloud
        float r = distance(_WorldCameraPos, earthCenter);
        if(r < _GroundRadius + _CloudBottom)
        {
            rayHitSphere(earthCenter, _WorldCameraPos, ray, _GroundRadius + _CloudBottom, near);
            rayHitSphere(earthCenter, _WorldCameraPos, ray, _GroundRadius + _CloudTop, far);
            if((_WorldCameraPos + ray * far).y < 0)
                far = 0;
        }
        // In cloud
        else if (r < _GroundRadius + _CloudTop)
        {
            rayHitSphere(earthCenter, _WorldCameraPos, ray, _GroundRadius + _CloudTop, far);
            if(rayHitSphere(earthCenter, _WorldCameraPos, ray, _GroundRadius + _CloudBottom, near))
            {
                far = near;
                near = 0;
            }
            else
                near = 0;
        }
        // Upon cloud
        else
        {
            rayHitSphere(earthCenter, _WorldCameraPos, ray, _GroundRadius + _CloudTop, near);
            if(!rayHitSphere(earthCenter, _WorldCameraPos, ray, _GroundRadius + _CloudBottom, far))
                far = 0;
        }
        if(far <= 0)
            return 1;

        float stepSize = (far - near) / _Samples;
        float transmittance = 1;

        [loop]
        for(float i = 1; i <= _Samples; i++)
        {
            float dist = lerp(near, far, i / _Samples);

            float3 pos = _WorldCameraPos + dist * ray;
            float density = cloudDensityAt(pos);

            transmittance *= exp(-density * stepSize * _Absorption);
            if(density < IGNORE_DENSITY_THRESHOLD)
                continue;

            float occlusion = raymarchOcclusion(pos);

            float lightTransmittance = exp(-occlusion * _AbsorptionToLight) * (1 - exp(-(occlusion + 0.01) * 2 * _PowderEffectScale));

            light += _Color * _MainLightColor * _LightScale * lightTransmittance * density * transmittance * stepSize * phaseHG(dot(-ray, -_MainLightDirection));

            if(transmittance < 0.01)
                break;
        }
        return transmittance;
    }


    float3 _CubeSize;
    float3 _CubePos;
    inline float densityAt(float3 pos)
    {
        float3 d = abs(pos - _CubePos) - (_CubeSize / 2);
        if(length(saturate(d)) > 0)
            return 0;
        float3 coord = pos;
        float3 height = (pos.y - _CubePos.y + .5 * _CubeSize.y) / _CubeSize.y;
        coord.xz /= _Scale;
        coord.y /= _HeightScale;
        return cloudDensity(sampleNoise(coord.xzy) * cloudShape(height));
    }

    inline float intersect(float3 origin, float3 ray, float3 pos, float3 normal)
    {
        float t = (dot(normal, pos) - dot(normal, origin)) / dot(normal, ray);
        
        return t;
    }

    float4 cloudOnSky(v2f i) : SV_TARGET
    {
        float3 ray = normalize(i.ray);
        float3 earthCenter = float3(0, -_GroundRadius, 0);
        float d = 0;
        float hit = rayHitSphere(earthCenter, _WorldCameraPos, ray, _GroundRadius + _CloudBottom, d);
        float3 pos = _WorldCameraPos + ray * d;
        float3 coord = toSphereCoord(pos - earthCenter);

        // if(!hit)
        //     return 0;
        // return float4(frac(coord.xy), 0, 1);
        float3 light;
        float transmittance;
        transmittance = raymarchingCloud(ray, light);

        light = light / saturate(1 - transmittance + 0.0001);
        light += _AmbientSkyColor * _AmbientStrength;
        float3 color = lerp(_DarkColor, _LightColor, light) * saturate(1 - transmittance);

        return float4(color, saturate(1 - transmittance));
    }

    float4 cloudCube(v2f i) : SV_TARGET
    {
        float3 ray = normalize(i.worldPos - _WorldCameraPos);
        float3 intersectA = float3(
            intersect(_WorldCameraPos, ray, _CubePos + float3(.5,0,0) * _CubeSize, float3(1, 0, 0)),
            intersect(_WorldCameraPos, ray, _CubePos + float3(0,.5,0) * _CubeSize, float3(0, 1, 0)),
            intersect(_WorldCameraPos, ray, _CubePos + float3(0,0,.5) * _CubeSize, float3(0, 0, 1))
        );
        float3 intersectB = float3(
            intersect(_WorldCameraPos, ray, _CubePos - float3(.5,0,0) * _CubeSize, float3(-1, 0, 0)),
            intersect(_WorldCameraPos, ray, _CubePos - float3(0,.5,0) * _CubeSize, float3(0, -1, 0)),
            intersect(_WorldCameraPos, ray, _CubePos - float3(0,0,.5) * _CubeSize, float3(0, 0, -1))
        );
        float3 front = min(intersectA, intersectB);
        float3 back = max(intersectA, intersectB);
        float near = max(front.x, max(front.y, front.z));
        float far = min(back.x, min(back.y, back.z));
        float dist = near;//length(i.worldPos - _WorldCameraPos);
        float maxDist = far - near;//length(_CubeSize);
        float raymarchingStepSize = maxDist / _Samples;
        float transmittance = 1;
        float3 light = 0;
        //return float4(frac(near), frac(far), 0, 1);

        [loop]
        for(float i = 1; i <= _Samples; i++)
        {
            dist += raymarchingStepSize;
            float3 pos = _WorldCameraPos + ray * dist;
            float3 d = abs(pos - _CubePos) - (_CubeSize / 2);
            if(length(saturate(d)) > 0)
                break;
            float density = densityAt(pos);

            transmittance *= exp(-density * raymarchingStepSize * _Absorption);

            float occlusion = 0;
            [loop]
            for(float i = 1; i < 6; i++)
            {
                occlusion += densityAt(pos + (-_MainLightDirection *_ScatterDistanceMultiply * (i / 5)))* (_ScatterDistanceMultiply * .2);
            }
            float lightTransmittance = exp(-occlusion * _AbsorptionToLight) * (1 - exp(-(occlusion + 0.01) * 2 * _PowderEffectScale));
            light += _Color * _LightScale * lightTransmittance * density * transmittance * raymarchingStepSize;
            if(transmittance < 0.01)
                break;
        }
        light = light / saturate(1 - transmittance + 0.0001);
        float3 color = lerp(_DarkColor, _LightColor, light) * saturate(1 - transmittance);
        
        return float4(color, saturate(1 - transmittance));



        // return float4(ray, 1);
        // float3 color;
        // float density = raymarchingCloud(ray, color);
        // return float4(0, 0, 0,1 -  density);

        // return 0;
    }

    ENDHLSL

    SubShader{
        Pass {
            Cull Off
            ZWrite Off
            ZTest Off
            Blend One OneMinusSrcAlpha
            
            HLSLPROGRAM

            #pragma vertex cloudSkyVert
            #pragma fragment cloudOnSky

            #pragma enable_d3d11_debug_symbols

            ENDHLSL
        }
        Pass {
            Tags { "LightMode"="RaymarchingTest" }
            Cull Back
            ZWrite Off
            ZTest Off
            Blend One OneMinusSrcAlpha
            
            HLSLPROGRAM

            #pragma vertex cloudCubeVert
            #pragma fragment cloudCube

            #pragma enable_d3d11_debug_symbols

            ENDHLSL
        }
    }
}