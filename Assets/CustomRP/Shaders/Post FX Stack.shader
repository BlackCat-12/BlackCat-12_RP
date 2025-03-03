Shader "Hidden/Custom RP/Post FX Stack" {
	
	SubShader {
		Cull Off
		ZTest Always
		ZWrite Off
		
		HLSLINCLUDE
		#include "../ShaderLibrary/Common.hlsl"
		#include "PostFXStackPasses.hlsl"
		ENDHLSL
		
		
		Pass {
			Name "CopyWithPoint"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment CopyPassWithPointFragment
			ENDHLSL
		}
		
			Pass {
			Name "CopyWithLinear"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment CopyPassWithLinearFragment
			ENDHLSL
		}
		
		Pass {
			Name "Bloom Vertical"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomVerticalPassFragment
			ENDHLSL
		}

		Pass {
			Name "Bloom Horizontal"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomHorizontalPassFragment
			ENDHLSL
		}
		
		Pass {
			Name "Bloom PrefilterPass"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomPrefilterPassFragment
			ENDHLSL
		}

		Pass {
			Name "Bloom BloomPrefilterFirefliesPass"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomPrefilterPassFragment
			ENDHLSL
		}
		
		
		
			Pass {
			Name "Bloom CombinePass"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomCombinePassFragment
			ENDHLSL
		}
		
		Pass
		{
			Name "Edge DectectWithNormalDepth"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment DrawEdgePassFragment
			ENDHLSL
		}
		
		Pass
		{
			Name "Edge DectectWithSurfaceIdDepth"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment DrawEdgeWithSurfaceIdPassFragment
			ENDHLSL
		}
		Pass
		{
			Name "Draw EdgePixelTex"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment DrawEdgePixelTex
			ENDHLSL
		}
		
		Pass {
			Name "Color Grading None"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ColorGradingNonePassFragment
			ENDHLSL
		}

		Pass {
			Name "Color Grading ACES"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ColorGradingACESPassFragment
			ENDHLSL
		}

		Pass {
			Name "Color Grading Neutral"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ColorGradingNeutralPassFragment
			ENDHLSL
		}
		
		Pass {
			Name "Color Grading Reinhard"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ColorGradingReinhardPassFragment
			ENDHLSL
		}

		Pass {
			Name "Final"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment FinalPassFragment
			ENDHLSL
		}

		Pass
		{
		    Name "SSR Rarmarching Pass"
		    
		    HLSLPROGRAM
		    #pragma vertex DefaultPassVertex
		    #pragma fragment SSRPassFragment2
		    ENDHLSL
		}

		Pass {
		    Name "SSR Addtive Pass"

		    ZTest NotEqual
		    ZWrite Off
		    Cull Off
		    Blend One One, One Zero

		    HLSLPROGRAM
		    #pragma vertex DefaultPassVertex
		    #pragma fragment SSRFinalPassFragment
		    ENDHLSL
		}

		Pass {
		    Name "SSR Balance Pass"

		    ZTest NotEqual
		    ZWrite Off
		    Cull Off
		    Blend SrcColor OneMinusSrcColor, One Zero

		    HLSLPROGRAM
		    #pragma vertex DefaultPassVertex
		    #pragma fragment SSRFinalPassFragment
		    ENDHLSL
		}
	}
}
