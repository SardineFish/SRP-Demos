Shader "Cloud/Debug" {
    Properties {
        _NoiseTex ("Noise Texture", 3D) = "white" {}
        _ZPos ("Z Pos", Range(0, 1)) = 0
        _Scale ("Scale", Float) = 1
        _ValueScale("Value Scale", Float) = .5
        _ValueOffset("Value Offset", Float) = .5
    }
    SubShader {
        Tags {"PreviewType"="Plane"}
        // #0
        Pass {
            Tags {"LightMode" = "ForwardLit"}
            ZWrite Off
            ZTest Off
            Cull Off

            CGPROGRAM
            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag

            struct v2f_screen
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f_screen vert(appdata_full i)
            {
                v2f_screen o;
                o.pos = UnityObjectToClipPos(i.vertex);
                o.uv = i.texcoord;
                return o;
            }


            Texture3D _NoiseTex;
            SamplerState noise_linear_repeat_sampler;
            float _ZPos;
            float _Scale;
            float _ValueScale;
            float _ValueOffset;

            float4 frag(v2f_screen i) : SV_TARGET
            {
                float3 pos = float3(i.uv.xy, _ZPos);
                float noise = _NoiseTex.Sample(noise_linear_repeat_sampler, pos * _Scale).r;// tex3D(_NoiseTex, pos * _Scale).r;
                noise = noise * _ValueScale + _ValueOffset;
                return noise;
            }

            ENDCG
        }
    }
}