#ifndef CUSTOM_RAYMARCHING_PASS_INCLUDE
#define CUSTOM_RAYMARCHING_PASS_INCLUDE

#include "../ShaderLibrary/Common.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"
#include "../ShaderLibrary/Lighting.hlsl"


SamplerState my_linear_clamp_sampler;

TEXTURE3D(_VolumeTex);

float4 _BaseMap_ST;
float4 _BaseColor;
float4 _ShadowColor;
float _NumSteps;
float _StepSize;
float _DensityScale;
float4 _Offset;
float _NumLightSteps;
float _LightStepSize;
float _MyLightAbsorb;
float _DarknessThreshold;
float _Transmittance;


struct Attributes {
	float3 positionOS : POSITION;
	
};

struct Varyings {
	float4 positionCS : SV_POSITION;
	float3 positionOS : TEXCOORD0;
	
};

float3 raymarch_Density(float3 rayOrigin, float3 rayDirection, float3 lightDir)
{
	float density = 0;
	float transmission = 0;
	float lightAccumulation = 0;
	float finalLight = 0;


	for(int i =0; i< _NumSteps; i++){
		rayOrigin += (rayDirection*_StepSize);

		//The blue dot position
		float3 samplePos = rayOrigin+_Offset;
		float sampledDensity = SAMPLE_TEXTURE3D(_VolumeTex, my_linear_clamp_sampler, samplePos).r;
		density += sampledDensity*_DensityScale;

		//light loop
		float3 lightRayOrigin = samplePos;
		
		for(int j = 0; j < _NumLightSteps; j++){
			//The red dot position
			lightRayOrigin += -lightDir*_LightStepSize;
			float lightDensity = SAMPLE_TEXTURE3D(_VolumeTex, my_linear_clamp_sampler, lightRayOrigin).r;
			//The accumulated density from samplePos to the light - the higher this value the less light reaches samplePos
			lightAccumulation += lightDensity;
		}

		//The amount of light received along the ray from param rayOrigin in the direction rayDirection
		float lightTransmission = exp(-lightAccumulation);
		//shadow tends to the darkness threshold as lightAccumulation rises
		float shadow = _DarknessThreshold + lightTransmission * (1.0 -_DarknessThreshold);
		//The final light value is accumulated based on the current density, transmittance value and the calculated shadow value 
		finalLight += density*_Transmittance*shadow;
		//Initially a param its value is updated at each step by lightAbsorb, this sets the light lost by scattering
		_Transmittance *= exp(-density*_MyLightAbsorb);
	}
	transmission = exp(-density);

	float3 result = float3(finalLight, transmission, _Transmittance);
	return result;
}

Varyings raymarching_SDF_Vertex (Attributes input) {
	Varyings output;
	
	output.positionCS = TransformObjectToHClip(input.positionOS);
	output.positionOS = input.positionOS;
	
	return output;
}
 

	
float4 raymarching_SDF_Fragment (Varyings input) : SV_Target {

	float3 cameraPosOS = TransformWorldToObject(_WorldSpaceCameraPos);
	float3 lightDir = GetMainLightDirection();
	
	// 中间变量
	float3 rayDir = normalize(input.positionOS - cameraPosOS);
	float3 lightDirOS = normalize(TransformObjectToWorld(lightDir));
	float3 result = raymarch_Density(input.positionOS, rayDir, lightDirOS);
	float alpha = 1 - result.g;
	float3 finalCol = lerp(_BaseColor, _ShadowColor, result.r);
	return float4(finalCol, alpha);
}

#endif