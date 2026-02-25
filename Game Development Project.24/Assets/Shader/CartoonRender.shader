Shader "Cartoon/CartoonRender"
{
    Properties
    {
        [Header(Texture)]
        _MainTex ("Main Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        [Toggle(_USE_AO)] _USE_AO ("Use AO", Float) = 1
        _AOTexture ("AO Texture", 2D) = "white" {}

        [Header(Balance)]
        _Tint ("Tint", Color) = (1,1,1,1)
        _Exposure ("Exposure", Range(0.5, 2.0)) = 1.0
        _Contrast ("Contrast", Range(0.1, 2.0)) = 1.0

        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.05)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalRenderPipeline"
        }

        Pass
        {
            Name "Outline"

            Tags
            {
                "LightMode" = "SRPDefaultUnlit" 
            }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _OutlineColor;
            float _OutlineWidth;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);

                // Extrude along normal direction in world space
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

                // Use smoothstep to create a soft outline effect based on the angle between the normal and view direction
                float outlineMask = smoothstep(0.0, 0.2, 1 - abs(ndotv));

                return _OutlineColor * outlineMask;
            }

            ENDHLSL
        }

        Pass
        {
            Name "UniversalForward"
            Tags
            {
                "LightMode"="UniversalForward"
            }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _USE_AO

            #pragma multi_compile _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            TEXTURE2D(_AOTexture);
            SAMPLER(sampler_AOTexture);

            float4 _MainTex_ST;
            float4 _NormalMap_ST;
            float4 _AOTexture_ST;

            float4 _Tint;
            float _Exposure;
            float _Contrast;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 tangentWS  : TEXCOORD1;
                float3 bitangentWS: TEXCOORD2;
                float2 uv         : TEXCOORD3;
            };

            Varyings vert (Attributes v)
            {
                Varyings o;

                VertexPositionInputs posInput = GetVertexPositionInputs(v.positionOS.xyz);

                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                float3 tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
                float tangentSign = v.tangentOS.w * GetOddNegativeScale();
                float3 bitangentWS = cross(normalWS, tangentWS) * tangentSign;

                o.positionCS = posInput.positionCS;
                o.normalWS = normalWS;
                o.tangentWS = tangentWS;
                o.bitangentWS = bitangentWS;
                o.uv = v.uv;

                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                Light light = GetMainLight();

                half3 L = normalize(light.direction);

                // Normal Map
                float2 normalUV = TRANSFORM_TEX(i.uv, _NormalMap);
                half4 normalTex = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalUV);
                half3 normalTS = UnpackNormal(normalTex);

                half3x3 TBN = half3x3(
                    normalize(i.tangentWS),
                    normalize(i.bitangentWS),
                    normalize(i.normalWS)
                );

                half3 N = normalize(mul(normalTS, TBN));

                // Base texture
                float2 mainUV = TRANSFORM_TEX(i.uv, _MainTex);
                half4 baseCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUV);

                // Lighting
                half NoL = dot(N, L);
                half halfLambert = NoL * 0.5 + 0.5;
                halfLambert = pow(halfLambert, 2.0);

                half lightStep1 = step(0.5, halfLambert);
                half lightStep2 = step(0.75, halfLambert);
                half lightStep = lightStep1 * 0.7 + lightStep2 * 0.3;
                lightStep = (lightStep - 0.5) * _Contrast + 0.5;
                lightStep = saturate(lightStep);

                // AO
                float2 aoUV = TRANSFORM_TEX(i.uv, _AOTexture);
                half ao = SAMPLE_TEXTURE2D(_AOTexture, sampler_AOTexture, aoUV).r;

                #ifdef _USE_AO
                    lightStep *= ao;
                #endif

                half3 finalColor = baseCol.rgb * _Tint.rgb * lightStep * _Exposure;

                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode"="ShadowCaster"
            }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowPassVertex(Attributes v)
            {
                Varyings o;

                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);

                float4 positionCS = TransformWorldToHClip(positionWS);

                o.positionCS = positionCS;
                return o;
            }

            float4 ShadowPassFragment(Varyings i) : SV_Target
            {
                return 0;
            }

            ENDHLSL
        }
    }
}