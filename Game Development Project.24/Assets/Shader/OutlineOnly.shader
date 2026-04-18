Shader "Cartoon/OutlineOnly"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.85, 0.1, 1)
        _OutlineWidth ("Outline Width", Range(0.0001, 0.2)) = 0.03
        _EdgeSoftness ("Edge Softness", Range(0.01, 0.5)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry+10"
        }

        Pass
        {
            Name "OutlineOnly"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _OutlineColor;
            float _OutlineWidth;
            float _EdgeSoftness;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                positionWS += normalWS * _OutlineWidth;
                o.positionCS = TransformWorldToHClip(positionWS);
                o.normalWS = normalWS;
                o.positionWS = positionWS;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 normalWS = normalize(i.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(i.positionWS));
                float ndotv = dot(normalWS, viewDir);
                float edge = smoothstep(0.0, _EdgeSoftness, 1.0 - abs(ndotv));
                return half4(_OutlineColor.rgb * edge, _OutlineColor.a * edge);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
