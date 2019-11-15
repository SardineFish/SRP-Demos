Shader "SarRP/ForwardDefault" {
    Properties {
        _MainTex("Main Texture", 2D) = "white" {}
        _Color("Color", Color) = (1, 1, 1, 1)
		_Normal ("Normal Textire", 2D) = "bump"{}
        _BumpScale ("Bump Scale", Float) = 1.0
    }
    SubShader {
        Tags {
            "RenderType" = "Opaque"
            "RenderPipeline" = "SardineRenderPipeline"
            "IgnoreProjector" = "true"
        }

        Pass {
            Name "ForwardLit"
            Tags {"LightMode" = "ForwardOpaque"}

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
			#include "UnityCG.cginc"
            #include "./Lib.hlsl"

			#define PI 3.14159265358979323846264338327950288419716939937510

            float4 _Color;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _Normal;
			float4 _Normal_ST;
            float _BumpScale;

			struct a2v{
				float4 vertex: POSITION;
				float3 normal: NORMAL;
				float4 texcoord: TEXCOORD0;
				float4 tangent: TANGENT;
			};
			struct v2f {
				float4 pos:SV_POSITION;
				float2 uv: TEXCOORD0;
				float3 worldPos: TEXCOORD1;
				float3 t2w0: TEXCOORD2;
				float3 t2w1: TEXCOORD3;
				float3 t2w2: TEXCOORD4;

			};

			v2f vert(a2v v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				float3 worldNormal = UnityObjectToWorldNormal(v.normal);
				float3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
				float3 worldBinormal = cross(worldNormal, worldTangent) * v.tangent.w;
				o.t2w0 = float3(worldTangent.x, worldBinormal.x, worldNormal.x);
				o.t2w1 = float3(worldTangent.y, worldBinormal.y, worldNormal.y);
				o.t2w2 = float3(worldTangent.z, worldBinormal.z, worldNormal.z);
				return o;
			}

            inline float3 diffuseLambert(float3 albedo){
                return albedo / PI;
            }

			float4 frag(v2f i):SV_TARGET{
				
				float4 albedo = tex2D(_MainTex, i.uv) * _Color;
				float3 ambient = unity_AmbientSky.rgb * albedo.rgb;
				float4 packNormal = tex2D(_Normal, i.uv); 
                // float shadow = SHADOW_ATTENUATION(i);
                // UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);

				float3 normal = UnpackNormal(packNormal);
                normal.xy *= _BumpScale;
                normal.z = sqrt(1 - saturate(dot(normal.xy, normal.xy)));
                
				normal = normalize(float3(dot(normal, i.t2w0.xyz), dot(normal, i.t2w1.xyz), dot(normal, i.t2w2.xyz)));
				float3 lightDir = -_MainLightDirection;
				float3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));

				float3 halfDir = normalize(lightDir + viewDir);
				float nv = saturate(dot(normal, viewDir));
				float nl = saturate(dot(normal, lightDir));
				float nh = saturate(dot(normal, halfDir));
				float lv = saturate(dot(lightDir, viewDir));
				float hl = saturate(dot(halfDir, lightDir));

                float3 light = _MainLightColor;// * atten;

				float3 diffuseTerm = PI * diffuseLambert(albedo.rgb) * light * nl + ambient;
				float3 color = diffuseTerm;
				return float4(color, albedo.a);
			}

            ENDHLSL

        }
    }
}