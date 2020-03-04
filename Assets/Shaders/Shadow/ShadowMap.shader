Shader "SarRP/Shadow/ShadowMap" {
    Properties {

    }

    HLSLINCLUDE 

    #include "../Lib.hlsl"
    
    #pragma enable_d3d11_debug_symbols

    float simpleDepth(v2f i): SV_TARGET
    {
        return 0;//i.pos.z;
    }

    float4x4 _LightViewProjection;
    float4 _ShadowParameters;

    v2f_light psmVert(appdata_base i)
    {
        v2f_light o;
        float4 pWorld = mul(unity_ObjectToWorld, float4(i.vertex));
        float4 pView = mul(UNITY_MATRIX_V, pWorld);
        float4 pClip = mul(UNITY_MATRIX_P, pView);
	    //pClip = UnityObjectToClipPos(i.vertex);
        float4 pNDC = pClip / pClip.w;
        pNDC.y *= _ProjectionParams.x;
        pNDC.w = 1;
        //o.pos = UnityWorldToClipPos(pNDC);
        o.pos = mul(_LightViewProjection, pNDC);
        //o.pos.z = 1 - o.pos.z;
        //o.pos = mul(unity_ObjectToWorld, float4(i.vertex.xyz, 1));
        //o.pos = mul(_LightViewProjection, o.pos);
        o.uv = 0;
        return o;
    }

    float psmFrag(v2f_light i, out float depth : SV_DEPTH) : SV_TARGET
    {
        if(_ShadowParameters.x > 0)
            depth = 1 - i.pos.z;
        else
            depth = i.pos.z;
        return 0;
    }

    ENDHLSL

    SubShader {
        // #0 Simple shadow map
        Pass {
            Tags { "LightMode"="ShadowCaster" }
            ZTest Less
            ZWrite On
            Cull Back

            HLSLPROGRAM

            #pragma vertex default_vert
            #pragma fragment simpleDepth

            ENDHLSL
        }

        // #1 PSM shadow map
        Pass {
            Tags { "LightMode"="ShadowCaster" }
            ZTest Less
            ZWrite On
            Cull Off

            HLSLPROGRAM
            


            #pragma vertex psmVert
            #pragma fragment psmFrag

            ENDHLSL
        }
    }
}