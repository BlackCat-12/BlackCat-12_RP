Shader "Custom RP/Lit" {
	
	Properties {
		_BaseMap("Texture", 2D) = "white" {}
		_BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
		[Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows ("Receive Shadows", Float) = 1
		[KeywordEnum(On, Clip, Dither, Off)] _Shadows ("Shadows", Float) = 0

		_Metallic ("Metallic", Range(0, 1)) = 0
		_Smoothness ("Smoothness", Range(0, 1)) = 0.5

		[Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha ("Premultiply Alpha", Float) = 0

		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
	}
	
	SubShader {
		Pass {
			Tags {
				"LightMode" = "CustomLit"
			}

			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _CLIPPING
			#pragma shader_feature _RECEIVE_SHADOWS
			#pragma shader_feature _PREMULTIPLY_ALPHA
			#pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
			#pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
			#pragma multi_compile_instancing
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment
			#include "LitPass.hlsl"
			ENDHLSL
		}
		
		Pass
        {
        	Name "SurfaceIdDepth"
	        Tags { "RenderType" = "Opaque" "LightMode" = "SurfaceIdDepth" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 color : TEXCOORD0;
				float3 positionVS : TEXCOORD1;
            };
            
            v2f vert(appdata_full v)
            {
                v2f o;
            	o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.positionVS = UnityObjectToViewPos(float3(0.0, 0.0, 0.0));
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float depth = -(i.positionVS.z * _ProjectionParams.w);
            	float3 surfaceId = i.color;
                return float4(surfaceId, depth); // 将深度信息以灰度方式显示
            }
            ENDCG
        }
	
		 Pass
        {
        	Name "DrawDepthNormal"
	        Tags { "RenderType" = "Opaque" "LightMode" = "DepthNormals" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
				float3 positionVS : TEXCOORD1;
            };

            struct Attributes {
			    float3 positionOS : POSITION;
			    float3 normal : NORMAL;
			};

            v2f vert(Attributes v)
            {
                v2f o;
            	o.pos = UnityObjectToClipPos(v.positionOS);
                o.positionVS = UnityObjectToViewPos(v.positionOS);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float depth = -(i.positionVS.z * _ProjectionParams.w);
            	float3 normal = normalize(mul((float3x3)unity_MatrixITMV, i.normal));
                return float4(normal, depth);
            }
            ENDCG
        }
        
		Pass {
			Tags {
				"LightMode" = "ShadowCaster"
			}

			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "ShadowCasterPass.hlsl"
			ENDHLSL
		}
	}

	CustomEditor "CustomShaderGUI"
}