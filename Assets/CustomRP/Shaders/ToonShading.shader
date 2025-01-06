// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unity Shaders Book/Chapter 14/Toon Shading" {
	Properties {
		_Color ("Color Tint", Color) = (1, 1, 1, 1)
		_MainTex ("Main Tex", 2D) = "white" {}
		_Ramp ("Ramp Texture", 2D) = "white" {}
		_Specular ("Specular", Color) = (1, 1, 1, 1)
		_SpecularScale ("Specular Scale", Range(0, 0.1)) = 0.01
	}
    SubShader {
		Tags {  "LightMode" = "ToonTag"}
		
		Pass {
			Tags {  "LightMode" = "ToonTag"}
			
			CGPROGRAM
		
			#pragma vertex vert
			#pragma fragment frag
			
			#pragma multi_compile_fwdbase
		
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"
			#include "UnityShaderVariables.cginc"
			
			fixed4 _Color;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _Ramp;
			fixed4 _Specular;
			fixed _SpecularScale;
		
			struct a2v {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				float4 texcoord : TEXCOORD0;
				float4 tangent : TANGENT;
			}; 
		
			struct v2f {
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
				float3 worldNormal : TEXCOORD1;
				float3 worldPos : TEXCOORD2;
				SHADOW_COORDS(3)
			};
			
			v2f vert (a2v v) {
				v2f o;
				
				o.pos = UnityObjectToClipPos( v.vertex);
				o.uv = TRANSFORM_TEX (v.texcoord, _MainTex);
				o.worldNormal  = UnityObjectToWorldNormal(v.normal);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				
				TRANSFER_SHADOW(o);
				
				return o;
			}
			
			float4 frag(v2f i) : SV_Target { 
				fixed3 worldNormal = normalize(i.worldNormal);
				fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(i.worldPos));
				fixed3 worldViewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
				fixed3 worldHalfDir = normalize(worldLightDir + worldViewDir);
				
				fixed4 c = tex2D (_MainTex, i.uv);
				fixed3 albedo = c.rgb * _Color.rgb;
				
				fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz * albedo;
				
				UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
				
				fixed diff =  dot(worldNormal, worldLightDir);
				diff = (diff * 0.5 + 0.5) * atten;
				
				fixed3 diffuse = _LightColor0.rgb * albedo * tex2D(_Ramp, float2(diff, diff)).rgb;
				
				fixed spec = dot(worldNormal, worldHalfDir);
				//计算邻近像素的近似导数值，作为平滑插值的范围
				fixed w = fwidth(spec) * 2.0;
				fixed3 specular = _Specular.rgb * lerp(0, 1, smoothstep(-w, w, spec + _SpecularScale - 1)) * step(0.0001, _SpecularScale);
				
				return fixed4(ambient + diffuse + specular, 1.0);
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
	}
	
}