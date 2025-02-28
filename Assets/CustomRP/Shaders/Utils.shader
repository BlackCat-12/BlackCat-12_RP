Shader "Hidden/Custom RP/Utils" {
	
	SubShader 
	{
		HLSLINCLUDE
		#include "../ShaderLibrary/Common.hlsl"
		#include "Utils.hlsl"
		ENDHLSL
        
		Pass
		{
			Name "DrawNormalDepth"
			Tags { "RenderType" = "Opaque" "LightMode" = "DepthNormals" }
			
			HLSLPROGRAM
				#pragma vertex vert
				#pragma fragment NormalDepthFragment
			ENDHLSL
		}
	}
}