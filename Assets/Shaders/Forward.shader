Shader "SimpleRP/ForwardDefault" {
    Properties {
        _MainTex("Main Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
    }
    SubShader {
        Tags {
            "RenderType" = "Opaque"
            "Queue"="Geometry"
            "RenderPipeline" = "SimpleRenderPipeline"
            "IgnoreProjector" = "true"
        }

        Pass {
            Name "ForwardLit"
            Tags {"LightMode" = "SimpleForward"}

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
			#include "UnityCG.cginc"
            #include "./Lib.hlsl"

			#define PI 3.14159265358979323846264338327950288419716939937510

            float4 _Color;
			sampler2D _MainTex;
			float4 _MainTex_ST;

			struct a2v{
				float4 vertex: POSITION;
				float3 normal: NORMAL;
				float4 texcoord: TEXCOORD0;
			};
			struct v2f {
				float4 pos:SV_POSITION;
				float2 uv: TEXCOORD0;
				float3 normal : NORMAL;
				float3 worldPos : TEXCOORD1;

			};

			v2f vert(a2v v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.normal = v.normal;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}

            inline float3 diffuseLambert(float3 albedo){
                return albedo / PI;
            }

			float4 frag(v2f i):SV_TARGET{
				
				float4 albedo = tex2D(_MainTex, i.uv) * _Color;
				float3 ambient = _AmbientColor.rgb * albedo.rgb;
                float3 light = _MainLightColor;

				float3 normal = i.normal;
				float3 lightDir = -_MainLightDirection;
				float3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));

				float nl = saturate(dot(normal, lightDir));


				float3 diffuse = PI * diffuseLambert(albedo.rgb) * light * nl;

				float3 color = diffuse + ambient;

				return float4(color, albedo.a);
			}

            ENDHLSL

        }
    }
}