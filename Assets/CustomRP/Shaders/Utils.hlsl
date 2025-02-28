#ifndef CUSTOM_UTILS_INCLUDED
#define CUSTOM_UTILS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"

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
        o.pos = TransformObjectToHClip(v.positionOS);
        o.positionVS = TransformObject2View(v.positionOS);
        o.normal = TransformObjectToWorldNormal(v.normal);
        return o;
    }
                
    float4 NormalDepthFragment(v2f i) : SV_Target
    {
        // 将观察空间深度映射到 [0, 1]
        float depth = 1.0;
        float3 normal = i.normal;
        return float4(normal, depth);
    }

#endif