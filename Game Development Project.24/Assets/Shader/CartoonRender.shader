Shader "Cartoon/CartoonRender"
{
	Properties
	{
		[Header(Texture)]
		_MainTex ("Main Texture", 2D) = "white" {}
		_NormalMap ("Normal Map", 2D) = "bump" {}

		[Header(Balance)]
		_Tint ("Tint", Color) = (1,1,1,1)
		_Exposure ("Exposure", Range(0.5, 2.0)) = 1.0
		_Contrast ("Contrast", Range(0.1, 2.0)) = 1.0

	}

	SubShader
	{
		Tags
		{
			"RenderType" = "Opaque"
			"RenderPipeline" = "UniversalRenderPipeline"
		}

		HLSLINCLUDE
		    #pragma multi_compile _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma multi_compile_fragment _LIGHT_LAYERS
            #pragma multi_compile_fragment _LIGHT_COOKIES
            #pragma multi_compile_fragment _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _SHADOWS_SOFT

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			CBUFFER_START (UnityPerMaterial)

				sampler2D _MainTex;
				sampler2D _NormalMap;
				float _Exposure;
				float _Contrast;
				float4 _Tint;

			CBUFFER_END
		ENDHLSL

		Pass
		{
			Name "UniversalForward"

			Tags
			{
				"LightMode" = "UniversalForward"
			}

			Cull Off
			ZWrite On
			ZTest LEqual
			Blend Off

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			struct Attributes
			{
				float4 positionOS : POSITION;
				float2 uv : TEXCOORD0;
				float3 normalOS : NORMAL;
				float4 color : COLOR;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 normalWS : NORMAL;
				float4 color : COLOR;
			};

			Varyings vert(Attributes v)
			{
				Varyings o;
				VertexPositionInputs VertexInput = GetVertexPositionInputs(v.positionOS.xyz);
				VertexNormalInputs NormalInput = GetVertexNormalInputs(v.normalOS);
				o.normalWS = NormalInput.normalWS;
				o.positionCS = VertexInput.positionCS;
				o.uv = v.uv;
				o.color = v.color;
				return o;
			};

			half4 frag(Varyings i) : SV_Target
			{
				Light light = GetMainLight();
				half4 vertexColor = i.color;
				half3 L = normalize(light.direction);
				half3 N = normalize(i.normalWS);
				half NoL = dot(N,L);
				half4 basecolor = tex2D(_MainTex, i.uv);
				half lambert = NoL;
				half halflambert = lambert * 0.5 + 0.5;
				halflambert *= pow (halflambert, 2.0);
				half lambertStep = smoothstep(0.01,0.4, halflambert);
				lambertStep = (lambertStep - 0.5) * _Contrast + 0.5;
				lambertStep = saturate(lambertStep);
				half3 finalcolor = basecolor.rgb * _Tint.rgb * lambertStep * _Exposure * vertexColor.rgb;
				return half4(finalcolor,1.0);
			}

			ENDHLSL
		}

		Pass
		{
			Name "ShadowCaster"
			Tags
			{
				"LightMode" = "ShadowCaster"
			}

			ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Off

			HLSLPROGRAM
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex ShadowVS
            #pragma fragment ShadowFS

            float3 _LightDirection;
            float3 _LightPosition;

            struct attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct varryings
            {
                float4 positionCS : SV_POSITION;
            };

            float4 GetShadowPositionHClip(attributes v)
            {
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            varryings ShadowVS (attributes v)
            {
                varryings o;
                o.positionCS = GetShadowPositionHClip(v);
                return o;
            }

            float4 ShadowFS(varryings i) : SV_Target
            {
                return 0;
            }
            ENDHLSL
		}
	}
}