Shader "Cloud/Raymarching" {
    Properties {
        _NoiseTex("Noise Texture", 3D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
        _Scale("Scale", Float) = 1
        _Thickness("Thickness", Float) = 1
        _Near("Near", Float) = 0
        _Far("Far", Float) = 20
        _Step("Step", Float) = 0.5
        _Samples("Sample Count", Int) = 64
        _CloudThreshold("Cloud Threshold", Range(0, 1)) = .5
        _DensityScale("Density Scale", Range(0, 1)) = .5
        _CloudBottom("Min Cloud Altitude", Float) = 20
        _CloudTop("Max Cloud Altitude", Float) = 30
        _FBMOctave("FBM Octave", Int) = 4
        _BeerLawScale("Beer's Law Scale", Float) = 1
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

    Texture3D _NoiseTex;
    SamplerState noise_linear_repeat_sampler;
    float4 _Color;
    float _Scale;
    float _Thickness;
    float _CloudThreshold;
    float _DensityScale;
    float3 _WorldCameraPos;
    float4 _CameraClipPlane;
    float4x4 _ViewProjectionInverseMatrix;
    float _Step;
    float _Near;
    float _Far;
    float _CloudBottom;
    float _CloudTop;
    float _Samples;
    int _FBMOctave;
    float _BeerLawScale;

    v2f vert(appdata_full i)
    {
        v2f o;
        o.pos = UnityObjectToClipPos(i.vertex);
        o.uv = i.texcoord;
        float4 p = float4(i.vertex.x, i.vertex.y, -1, 1);
        p = p * _CameraClipPlane.x;
        o.worldPos = mul(_ViewProjectionInverseMatrix, p);
        o.ray = o.worldPos - _WorldCameraPos;
        o.ray = normalize(o.ray) * (length(o.ray) / _CameraClipPlane.x);
        return o;
    }

    inline float sampleNoise(float3 uv)
    {
        return _NoiseTex.Sample(noise_linear_repeat_sampler, uv).r;
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

    #define MAX_ITERATION 128

    inline float raymarchingCloud(float3 ray, out float3 light)
    {
        light = 0;
        float alpha = 0;

        float near = raymarchingMinDistance(ray);
        float far = raymarchingMaxDistance(ray);
        if(far <= 0)
            return 0;
        near = near < 0 ? 0 : near;
        float dist = near;

        [loop]
        for(float i = 1; i <= _Samples; i++)
        {
            dist = lerp(near, far, i / _Samples);

            float3 pos = _WorldCameraPos + dist * ray;
            float3 uv = float3(pos.x / _Scale, pos.y / _Thickness, pos.z / _Scale);

            float density = cloudDensity(fbm(uv));

            float d = dist - near;
            light += exp(-d * _BeerLawScale * density) * (1.0f / _Samples) * _Color.rgb * (1 - density);
            alpha += density;

            /*if(alpha >= 1)
                break;*/

        }
        return saturate(alpha);
    }

    float4 cloudTest(v2f i) : SV_TARGET
    {
        float3 ray = normalize(i.ray);
        float3 color;
        float density = raymarchingCloud(ray, color);
        return float4(color, _Color.a * density);

        return 0;
    }



    ENDHLSL

    SubShader{
        Pass {
            Cull Off
            ZWrite Off
            ZTest Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment cloudTest

            #pragma enable_d3d11_debug_symbols

            ENDHLSL
        }
    }
}