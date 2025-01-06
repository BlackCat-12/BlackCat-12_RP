Shader "Custom RP/Unlit" {
	
	Properties
	{
		_MainTex("Albedo", 2D) = "white" {}
		_BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
		[Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
		[Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
	}
	
	SubShader {
		Pass
		{
			Blend [_SrcBlend] [_DstBlend]
			ZWrite [_ZWrite]
			HLSLPROGRAM
			#pragma shader_feature _CLIPPING
			#pragma multi_compile_instancing
			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment
			#include "UnlitPass.hlsl"
			ENDHLSL
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
                o.positionVS = UnityObjectToViewPos(float3(0.0, 0.0, 0.0));
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float depth = -(i.positionVS.z * _ProjectionParams.w);

            	float3 normal = normalize(mul((float3x3)unity_MatrixITMV, i.normal));
                return float4(normal, depth); // 将深度信息以灰度方式显示
            }
            ENDCG
        }
	}
	CustomEditor "CustomShaderGUI"
}