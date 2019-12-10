Shader "Cloud/CoverageDebug" {
    Properties {
        _CoverageTex ("Coverage Texture", 2D) = "white" {}
        _Scale ("Scale", Float) = 1
        _Low("Low", Range(0, 1)) = .4
        _High("High", Range(0, 1)) = .6
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


            sampler2D _CoverageTex;
            float _Scale;
            float _Low;
            float _High;

            inline float softThreshold(float value, float threshold, float smooth)
            {
                float knee = threshold * smooth;
                float soft = value - threshold + knee;
                soft = clamp(soft, 0, 2 * knee);
                soft = soft * soft / (4 * knee + 0.00001);
                float output = max(soft, value - threshold);
                output /= max(value, 0.00001);
                return output;
            }

            float4 frag(v2f_screen i) : SV_TARGET
            {
                float2 coord = i.uv.xy * _Scale;
                float noise = tex2D(_CoverageTex, coord);// _NoiseTex.Sample(noise_linear_repeat_sampler, pos * _Scale).r;// tex3D(_NoiseTex, pos * _Scale).r;
                noise = noise;
                noise = smoothstep(_Low, _High, noise);
                return noise;
            }

            ENDCG
        }
    }
}