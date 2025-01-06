Shader "Custom RP/raymarching" {
	
	Properties
	{
		_VolumeTex("volumeTex", 3D) = "white"{}
		_BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_ShadowColor("ShadowColor", Color) = (0.0, 0.0, 0.0, 1.0)
		_NumSteps("numSteps", Float) = 16
		_StepSize ("stepSize", Float) = 0.1
		_DensityScale("densityScale", Float) = 0.5
		_Offset("offset", Vector) = (0.5, 0.5, 0.5, 0.5) 
		_NumLightSteps("numLightSteps",Float) = 0
		_LightStepSize("lightStepSize", Float) = 0
		_MyLightAbsorb("lightStepSize", Float) = 0
		_DarknessThreshold("darknessThreshold", Float) = 0
		_Transmittance("transmittance", Float) = 0
		
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
	}
	
	SubShader {
		Pass
		{	
			Blend [_SrcBlend] [_DstBlend]
			HLSLPROGRAM
			#pragma multi_compile_instancing
			#pragma vertex raymarching_SDF_Vertex
			#pragma fragment raymarching_SDF_Fragment
			#include "raymarching_SDF.hlsl"
			
			ENDHLSL
		}
	}
	CustomEditor "CustomShaderGUI"
}