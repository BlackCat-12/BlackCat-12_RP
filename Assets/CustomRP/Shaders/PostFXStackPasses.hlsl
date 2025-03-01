#ifndef CUSTOM_POST_FX_PASSES_INCLUDED
#define CUSTOM_POST_FX_PASSES_INCLUDED
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

SamplerState my_point_clamp_sampler;
SamplerState my_linear_clamp_sampler;

// 全局纹理
TEXTURE2D(_DepthNormalTex);
TEXTURE2D(_SurfaceIdDepthTex);

// 中间交换纹理
TEXTURE2D(_PostFXSource);
TEXTURE2D(_PostFXSource2);

TEXTURE2D(_OutlineTex);
TEXTURE2D(_FXPixelDownSample);
TEXTURE2D(_SurfaceIdDepthDownSampleTex);

float _Outline;
float4 _DepthNormalTex_TexelSize;
float4 _SurfaceIdDepthTex_TexelSize;
float4 _SurfaceIdDepthDownSampleTex_TexelSize;
float _DepthSensitivity;
float _NormalSensitivity;
float _DepthThreshold;
float _NormalThreshold;
float _DepthBias;
float _NormalBias;

float4 _ProjectionParams;
float4 _PostFXSource_TexelSize;
bool _BloomBicubicUpsampling;
float _BloomIntensity;
float4 _BloomThreshold;

//-----------------------------------------------

struct Attributes {
    float3 positionOS : POSITION;
    float3 normal : NORMAL;
};

struct Varyings {
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};

// Sobel kernels
const int kernelSize = 3;
static const float kernelX[9] = {
    -1, 0, 1,
    -2, 0, 2,
    -1, 0, 1
};

static const float kernelY[9] = {
    -1, -2, -1,
     0,  0,  0,
     1,  2,  1
};

static const float2 offset[4] = {
    float2(1, 0) ,float2(0, 1),float2(-1, 0), float2(0, -1)};

static const float2 offset_diagonal[8] = {
    float2(1, 0) ,float2(0, 1),float2(-1, 0), float2(0, -1),
    float2(1, 1), float2(-1, 1), float2(-1, -1), float2(1, -1)};

//----------------------------- 辅助函数 ------------------------------
float4 GetDepthNormalTexelSize()
{
    return _DepthNormalTex_TexelSize;
}

float4 GetSurfaceIdDepthTexelSize()
{
    return _SurfaceIdDepthTex_TexelSize;
}

float ApplySobel(float2 uv, float2 texelSize)
{
    float depthSumX = 0.0;
    float depthSumY = 0.0;

    for (int y = -1; y <= 1; y++)
    {
        for (int x = -1; x <= 1; x++)
        {
            float2 offset = float2(x * texelSize.x, y * texelSize.y);
            float sampleDepth = SAMPLE_TEXTURE2D_LOD(_DepthNormalTex, my_point_clamp_sampler, uv + offset, 0).w;

            int index = (y + 1) * 3 + (x + 1);

            depthSumX += sampleDepth * kernelX[index];
            depthSumY += sampleDepth * kernelY[index];
        }
    }
    float gradient = sqrt(depthSumX * depthSumX + depthSumY * depthSumY);
    return gradient;
}

float4 GetSourceTexelSize () {
    return _PostFXSource_TexelSize;
}


float4 GetSourceWithPoint(float2 screenUV)
{
    return SAMPLE_TEXTURE2D(_PostFXSource, my_point_clamp_sampler, screenUV);
}

float4 GetSourceWithLinear(float2 screenUV)
{
    return SAMPLE_TEXTURE2D(_PostFXSource, my_linear_clamp_sampler, screenUV);
}

float4 GetSource2(float2 screenUV) {
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource2, my_linear_clamp_sampler, screenUV, 0);
}

float4 GetSourceBicubic (float2 screenUV) {
    return SampleTexture2DBicubic(
        TEXTURE2D_ARGS(_PostFXSource, my_linear_clamp_sampler), screenUV,
        _PostFXSource_TexelSize.zwxy, 1.0, 0.0
    );
}

float3 ApplyBloomThreshold (float3 color) {
    float brightness = Max3(color.r, color.g, color.b);
    float soft = brightness + _BloomThreshold.y;
    soft = clamp(soft, 0.0, _BloomThreshold.z);
    soft = soft * soft * _BloomThreshold.w;
    float contribution = max(soft, brightness - _BloomThreshold.x);
    contribution /= max(brightness, 0.00001);
    return color * contribution;
}

//----------------------Pass--------------------------

Varyings DefaultPassVertex (uint vertexID : SV_VertexID) {
    Varyings output;
    output.positionCS = float4(
        vertexID <= 1 ? -1.0 : 3.0,
        vertexID == 1 ? 3.0 : -1.0,
        0.0, 1.0
    );
    output.screenUV = float2(
        vertexID <= 1 ? 0.0 : 2.0,
        vertexID == 1 ? 2.0 : 0.0
    );
    if (_ProjectionParams.x < 0.0) {
        output.screenUV.y = 1.0 - output.screenUV.y;
    }
    return output;
}

// ---------------------------- 描边相关 -------------------------------
float4 DrawEdgePassFragment(Varyings input) : SV_TARGET{
    
    float4 depthNormal = SAMPLE_TEXTURE2D_LOD(_DepthNormalTex, my_point_clamp_sampler, input.screenUV, 0);
    float3 normal = depthNormal.xyz;
    float depth = depthNormal.w;

    float2 _TexelSize = GetDepthNormalTexelSize();

    // 深度外描边
    float depthDiff = 0.0;
    for (int i; i <4; i++)
    {
        depthDiff += abs(depth - SAMPLE_TEXTURE2D_LOD(_DepthNormalTex, my_point_clamp_sampler, input.screenUV + offset[i] * _TexelSize, 0).w);
    }
    // 法线内描边
    float normalDiff = 0.0;
    for (int i = 0; i < 8; i++)
    {
        normalDiff += distance(normal, SAMPLE_TEXTURE2D_LOD(_DepthNormalTex, my_point_clamp_sampler, input.screenUV + offset_diagonal[i] * _TexelSize, 0).xyz);
    }
    
    // 根据敏感度调整梯度
    depthDiff = depthDiff * _DepthSensitivity;
    depthDiff = saturate(depthDiff);
    depthDiff = pow(depthDiff, _DepthBias);
    
    normalDiff = normalDiff * _NormalSensitivity;
    normalDiff = saturate(normalDiff);
    normalDiff = pow(normalDiff, _NormalBias);
    
    // 使用阈值检测边缘
    float outline = normalDiff + depthDiff;
    //float finalInnerEdge = innerEdge * (1.0 - outerEdge);
    
    float3 edgeColor = float3(outline,outline,outline);
    return float4(edgeColor, 1.0);
}

// 绘制描边图
float4 DrawEdgePassFragment02(Varyings input) : SV_TARGET
{
    float4 depthNormal = SAMPLE_TEXTURE2D_LOD(_DepthNormalTex, my_point_clamp_sampler, input.screenUV, 0);
    float2 _TexelSize = GetDepthNormalTexelSize();
    float depth = depthNormal.w;
    float3 normal = depthNormal.xyz;

    // 深度检测外轮廓
    bool isEdge = false;
    for (int i = 0; i < 4; i++)
    {
        float2 neighborUV = input.screenUV + offset[i] * _TexelSize;
        neighborUV = clamp(neighborUV, 0.0, 1.0);

        float depthNei = SAMPLE_TEXTURE2D_LOD(_DepthNormalTex, my_point_clamp_sampler, neighborUV, 0).w;
    
        // 一旦发现满足条件的邻居，标记为边缘并退出循环
        if (depthNei < depth)
        {
            isEdge = true;
            break;
        }
    }

    float outerEdge = isEdge ? 1.0 : 0.0;

    // 法线检测内轮廓

    float3 normalRight = SAMPLE_TEXTURE2D_LOD(_DepthNormalTex, my_point_clamp_sampler, input.screenUV + offset[0] * _TexelSize, 0).xyz;
    float3 normalUp = SAMPLE_TEXTURE2D_LOD(_DepthNormalTex, my_point_clamp_sampler, input.screenUV + offset[1] * _TexelSize, 0).xyz;

    float normalRightVal =1 - dot(normal, normalRight);
    float normalUpVal = 1 - dot(normal, normalUp);
    float normalEdge = saturate(normalRightVal + normalUpVal);
    
    normalEdge *= _NormalSensitivity;
    float innerEdge = step(_NormalThreshold, normalEdge);
    
    // 集合
    float finalInnerEdge = innerEdge * (1.0 - outerEdge);
    
    float3 edgeColor = float3(finalInnerEdge, 0.0, outerEdge);
    return float4(edgeColor, 1.0);
}

float4 DrawEdgeWithSurfaceIdPassFragment(Varyings input) : SV_TARGET
{
    float4 surfaceDepth = SAMPLE_TEXTURE2D_LOD(_SurfaceIdDepthDownSampleTex, my_point_clamp_sampler, input.screenUV, 0);
    float2 _TexelSize = _SurfaceIdDepthDownSampleTex_TexelSize;
    float3 surface = surfaceDepth.xyz;
    float depth = surfaceDepth.w;

    // 内外轮廓检验
    float isOuterEdge = 0.0;
    float currSurfaceVal = dot(surface, surface);
    float isInnerEdge = 0.0;
    for (int i = 0; i < 4; i++)
    {
        float2 neighborUV = input.screenUV + offset[i] * _TexelSize;
        neighborUV = clamp(neighborUV, 0.0, 1.0);

        float4 surfaceIdDepthNei = SAMPLE_TEXTURE2D_LOD(_SurfaceIdDepthDownSampleTex, my_point_clamp_sampler, neighborUV, 0);
        float3 surfaceIdNei = surfaceIdDepthNei.rgb;
        float depthNei = surfaceIdDepthNei.a;
    
        // 一旦发现满足条件的邻居，标记为边缘并退出循环
        if (depthNei < depth)
        {
            isOuterEdge = 1.0;
        }
        float neiSurfaceVal = dot(surfaceIdNei, surfaceIdNei);
        
        if (neiSurfaceVal < currSurfaceVal)  // 为完美覆盖外描边，大于邻居id值时，判断为边缘
        {
            isInnerEdge = 1.0;
            break;
        }
    }
    // 混合内外描边结果
    float finalInnerEdge = isInnerEdge * (1.0 - isOuterEdge);
    
    float3 edgeColor = float3(finalInnerEdge, 0.0, isOuterEdge);
    return float4(edgeColor, 1.0);
}

float4 DrawEdgePixelTex(Varyings input) : SV_TARGET
{
    // 采样描边纹理和下采样像素纹理
    float3 sourceTex = GetSourceWithPoint(input.screenUV);
    float3 outlineTex = SAMPLE_TEXTURE2D_LOD(_OutlineTex, my_point_clamp_sampler, input.screenUV, 0);
    
    // 定义调整因子
    float darkenFactor = 0.5; // 蓝色描边时的50%变暗
    float lightenFactor = 0.5; // 红色描边时的50%变亮
    
    // 定义颜色阈值
    float threshold = 0.9;
    
    // 计算蓝色和红色描边的激活状态
    float isBlueEdge = step(threshold, outlineTex.b) * (1.0 - step(threshold, outlineTex.r)) * (1.0 - step(threshold, outlineTex.g));
    float isRedEdge = step(threshold, outlineTex.r) * (1.0 - step(threshold, outlineTex.b)) * (1.0 - step(threshold, outlineTex.g));
    
    // 计算变暗和变亮的效果
    float3 darkenColor = sourceTex * (1.0 - darkenFactor) * isBlueEdge;
    float3 lightenColor = sourceTex * (1.0 + lightenFactor) * isRedEdge;
    
    // 组合最终颜色
    float3 finalColor = darkenColor + lightenColor + sourceTex * (1.0 - isBlueEdge - isRedEdge);
    
    // 限制最终颜色范围
    finalColor = saturate(finalColor);
    float outline = 1- (outlineTex.r + outlineTex.b);
    float3 finalCol = outline * sourceTex;
    float3 testCol =  outlineTex + finalCol;
    
    return float4(testCol, 1.0);
}

//阈值Pass
float4 BloomPrefilterPassFragment (Varyings input) : SV_TARGET {  //  常规下采样
    float3 color = ApplyBloomThreshold(GetSourceWithPoint(input.screenUV).rgb);
    return float4(color, 1.0);
}

float4 BloomPrefilterFirefliesPassFragment (Varyings input) : SV_TARGET {
    float3 color = 0.0;
    float weightSum = 0.0;
    
    float2 offsets[] = {
        float2(0.0, 0.0),
        float2(-1.0, -1.0), float2(-1.0, 1.0), float2(1.0, -1.0), float2(1.0, 1.0)//,
        //float2(-1.0, 0.0), float2(1.0, 0.0), float2(0.0, -1.0), float2(0.0, 1.0)
    };
    for (int i = 0; i < 5; i++) {
        float3 c =
            GetSourceWithLinear(input.screenUV + offsets[i] * GetSourceTexelSize().xy * 2.0).rgb;
        c = ApplyBloomThreshold(c);
        float w = 1.0 / (Luminance(c) + 1.0);
        color += c * w;
        weightSum += w;
    }
    color *= 1.0 / weightSum;  // 按权值进行扩散平均采样
    return float4(color, 1.0);
}

//水平滤波
float4 BloomHorizontalPassFragment (Varyings input) : SV_TARGET {
    float3 color = 0.0;
    float offsets[] = {
        -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
    };
    float weights[] = {
        0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
    };
    for (int i = 0; i < 5; i++) {
        float offset = offsets[i] * 2.0 * GetSourceTexelSize().x;
        color += GetSourceWithPoint(input.screenUV + float2(offset, 0.0)).rgb * weights[i];
    }
    return float4(color, 1.0);
}

//垂直滤波
float4 BloomVerticalPassFragment (Varyings input) : SV_TARGET {
    float3 color = 0.0;
    float offsets[] = {
        -3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
    };
    float weights[] = {
        0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
    };
    for (int i = 0; i < 5; i++) {
        float offset = offsets[i] * GetSourceTexelSize().y;
        color += GetSourceWithPoint(input.screenUV + float2(0.0, offset)).rgb * weights[i];
    }
    return float4(color, 1.0);
}

// 上采样混合函数
float4 BloomCombinePassFragment (Varyings input) : SV_TARGET {
    float3 lowRes;
    if (_BloomBicubicUpsampling) {
        lowRes = GetSourceBicubic(input.screenUV).rgb;
    }
    else {
        lowRes = GetSourceWithPoint(input.screenUV).rgb;
    }
    float3 highRes = GetSource2(input.screenUV).rgb;
    return float4(lowRes  * _BloomIntensity + highRes, 1.0);
}

float4 CopyPassWithPointFragment (Varyings input) : SV_TARGET {  // 采样Draw调用传来的from纹理
    return GetSourceWithPoint(input.screenUV);
}

float4 CopyPassWithLinearFragment (Varyings input) : SV_TARGET {
    return GetSourceWithLinear(input.screenUV);
}

float4 ToneMappingReinhardPassFragment (Varyings input) : SV_TARGET {
    float4 color = GetSourceWithLinear(input.screenUV);
    color.rgb /= color.rgb + 1.0;
    return color;
}

float4 ToneMappingNeutralPassFragment (Varyings input) : SV_TARGET {
    float4 color = GetSourceWithLinear(input.screenUV);
    color.rgb = min(color.rgb, 60.0);
    color.rgb = NeutralTonemap(color.rgb);
    return color;
}

float4 ToneMappingACESPassFragment (Varyings input) : SV_TARGET {
    float4 color = GetSourceWithLinear(input.screenUV);
    color.rgb = min(color.rgb, 60.0);
    color.rgb = AcesTonemap(unity_to_ACES(color.rgb));
    return color;
}

#endif 