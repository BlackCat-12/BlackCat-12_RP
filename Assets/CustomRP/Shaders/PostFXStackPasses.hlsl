#ifndef CUSTOM_POST_FX_PASSES_INCLUDED
#define CUSTOM_POST_FX_PASSES_INCLUDED
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

SamplerState my_point_clamp_sampler;
SamplerState my_linear_clamp_sampler;
SamplerState sampler_linear_clamp;


// 全局纹理
TEXTURE2D(_DepthNormalTex);
TEXTURE2D(_SurfaceIdDepthTex);

// 中间交换纹理
TEXTURE2D(_PostFXSource);
TEXTURE2D(_PostFXSource2);
TEXTURE2D(_PostFXSource3);

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

float SampleSceneDepth(float2 screenUV)
{
    return 1 - SAMPLE_TEXTURE2D(_DepthNormalTex, my_point_clamp_sampler, screenUV).a;
}

float3 SampleSceneNormal(float2 screenUV)
{
    float3 normal = SAMPLE_TEXTURE2D(_DepthNormalTex, my_point_clamp_sampler, screenUV).xyz;
    return normal * 2 - 1;
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


#include "ColorGrading.hlsl"

float4 _ProjectionParams2;  
float4 _CameraViewTopLeftCorner;  
float4 _CameraViewXExtent;  
float4 _CameraViewYExtent;  

float _Intensity;
float _MaxDistance;
float _Thickness;


int _Stride;
int _StepCount = 30;
int _BinaryCount;
// #define MAXDISTANCE 10  
// #define STRIDE 3  
#define STEP_COUNT 200  
// // 能反射和不可能的反射之间的界限  
// #define THICKNESS 0.5  

// 能反射和不可能的反射之间的界限  

#define STEP_SIZE 0.1

void swap(inout float v0, inout float v1) {  
    float temp = v0;  
    v0 = v1;    
    v1 = temp;
}  

// half4 GetSource(half2 uv) {  
//     return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearRepeat, uv, _BlitMipLevel);  
// }  

// 还原观察空间下，进行向世界空间的旋转，片段相对于相机的位置，
half3 ReconstructViewPos(float2 uv, float linearEyeDepth) {  
    // Screen is y-inverted  
    uv.y = 1.0 - uv.y;  

    float zScale = linearEyeDepth * _ProjectionParams2.x; // divide by near plane  
    float3 viewPos = _CameraViewTopLeftCorner.xyz + _CameraViewXExtent.xyz * uv.x + _CameraViewYExtent.xyz * uv.y;  
    viewPos *= zScale;  
    return viewPos;  
}  

// 从视角坐标转裁剪屏幕ao坐标
float4 TransformViewToHScreen(float3 vpos, float2 screenSize) {  
    float4 cpos = mul(UNITY_MATRIX_P, vpos);  
    cpos.xy = float2(cpos.x, cpos.y * _ProjectionParams.x) * 0.5 + 0.5 * cpos.w;  
    cpos.xy *= screenSize;  
    return cpos;  
}

// 从视角空间坐标片元uv和深度  
void ReconstructUVAndDepth(float3 wpos, out float2 uv, out float depth) {  
    float4 cpos = mul(UNITY_MATRIX_VP, wpos);  
    uv = float2(cpos.x, cpos.y * _ProjectionParams.x) / cpos.w * 0.5 + 0.5;  
    depth = cpos.w;  
}

half4 SSRFinalPassFragment(Varyings input) : SV_Target {  
    return half4(GetSourceWithLinear(input.screenUV).rgb * _Intensity, 1.0);  
}

// jitter dither map
static half dither[16] = {
    0.0, 0.5, 0.125, 0.625,
    0.75, 0.25, 0.875, 0.375,
    0.187, 0.687, 0.0625, 0.562,
    0.937, 0.437, 0.812, 0.312
};

float4 _SourceSize;

bool ScreenSpaceRayMarching(inout float2 P, inout float3 Q, inout float K, float2 dp, float3 dq, float dk, float rayZ, bool permute, out float depthDistance, inout float2 hitUV) {
    // float end = endScreen.x * dir;
    float rayZMin = rayZ;
    float rayZMax = rayZ;
    float preZ = rayZ;

    // 进行屏幕空间射线步近
    UNITY_LOOP
    for (int i = 0; i < _StepCount; i++) {
        // 步近
        P += dp;
        Q += dq;
        K += dk;

        // 得到步近前后两点的深度
        rayZMin = preZ;
        rayZMax = (dq.z * 0.5 + Q.z) / (dk * 0.5 + K);
        preZ = rayZMax;
        if (rayZMin > rayZMax)
            swap(rayZMin, rayZMax);

        // 得到交点uv
        hitUV = permute > 0.5 ? P.yx : P;
        hitUV *= _SourceSize.zw;

        if (any(hitUV < 0.0) || any(hitUV > 1.0))
            return false;

        float surfaceDepth = -LinearEyeDepth(SampleSceneDepth(hitUV), _ZBufferParams);
        bool isBehind = (rayZMin + 0.1 <= surfaceDepth); // 加一个bias 防止stride过小，自反射

        depthDistance = abs(surfaceDepth - rayZMax);

        if (isBehind) {
            return true;
        }
    }
    return false;
}

bool BinarySearchRaymarching(float3 startView, float3 rDir, inout float2 hitUV) {
    float magnitude = _MaxDistance;

    float end = startView.z + rDir.z * magnitude;
    if (end > -_ProjectionParams.y)
        magnitude = (-_ProjectionParams.y - startView.z) / rDir.z;
    float3 endView = startView + rDir * magnitude;

    // 齐次屏幕空间坐标
    float4 startHScreen = TransformViewToHScreen(startView, _SourceSize.xy);
    float4 endHScreen = TransformViewToHScreen(endView, _SourceSize.xy);

    // inverse w
    float startK = 1.0 / startHScreen.w;
    float endK = 1.0 / endHScreen.w;

    //  结束屏幕空间坐标
    float2 startScreen = startHScreen.xy * startK;
    float2 endScreen = endHScreen.xy * endK;

    // 经过齐次除法的视角坐标
    float3 startQ = startView * startK;
    float3 endQ = endView * endK;

    float stride = _Stride;

    float depthDistance = 0.0;

    bool permute = false;

    // 根据斜率将dx=1 dy = delta
    float2 diff = endScreen - startScreen;
    if (abs(diff.x) < abs(diff.y)) {
        permute = true;

        diff = diff.yx;
        startScreen = startScreen.yx;
        endScreen = endScreen.yx;
    }

    // 计算屏幕坐标、齐次视坐标、inverse-w的线性增量
    float dir = sign(diff.x);
    float invdx = dir / diff.x;
    float2 dp = float2(dir, invdx * diff.y);
    float3 dq = (endQ - startQ) * invdx;
    float dk = (endK - startK) * invdx;

    dp *= stride;
    dq *= stride;
    dk *= stride;

    // 缓存当前深度和位置
    float rayZ = startView.z;

    float2 P = startScreen;
    float3 Q = startQ;
    float K = startK;

  
    UNITY_LOOP
     for (int i = 0; i < _BinaryCount; i++) {
         float2 ditherUV = fmod(P, 4);  
         float jitter = dither[ditherUV.x * 4 + ditherUV.y];  

         P += dp * jitter;  
         Q += dq * jitter;  
         K += dk * jitter;
         if (ScreenSpaceRayMarching(P, Q, K, dp, dq, dk, rayZ, permute, depthDistance, hitUV)) {
            if (depthDistance < _Thickness)
                return true;
            P -= dp;
            Q -= dq;
            K -= dk;
            rayZ = Q / K;

            dp *= 0.5;
            dq *= 0.5;
            dk *= 0.5;
        }
        else {
            return false;
        }
    }
    return false;
}
half4 SSRPassFragment3(Varyings input) : SV_Target {  
    float rawDepth = SampleSceneDepth(input.screenUV);  
    float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);   
    float3 vpos = ReconstructViewPos(input.screenUV, linearDepth);  
    float3 normal = SampleSceneNormal(input.screenUV);  // 观察空间法线方向
    float3 vDir = normalize(vpos);  
    float3 rDir = TransformWorldToViewDir(normalize(reflect(vDir, normal)));  //  观察空间反射方向

    float magnitude = _MaxDistance;  

    // 视空间坐标  
    vpos = _WorldSpaceCameraPos + vpos;  // 片段在世界空间的位置
    float3 startView = TransformWorldToView(vpos);  // 观察空间坐标
    float end = startView.z + rDir.z * magnitude;  
    if (end > -_ProjectionParams.y)  // 当结束点超出近平面时，限制。Unity观察空间为右手系，近远平面深度值为负
        magnitude = (-_ProjectionParams.y - startView.z) / rDir.z;  
    float3 endView = startView + rDir * magnitude;  // 使反射结束点始终在近平面以内，防止无效计算

    // 齐次屏幕空间坐标  
    float4 startHScreen = TransformViewToHScreen(startView, _SourceSize.xy);  // 将观察空间的起始点变换到屏幕空间
    float4 endHScreen = TransformViewToHScreen(endView, _SourceSize.xy);  

    // inverse w  
    float startK = 1.0 / startHScreen.w;  // K为w的倒数，保留即 1/Zview
    float endK = 1.0 / endHScreen.w;  

    //  结束屏幕空间坐标  
    float2 startScreen = startHScreen.xy * startK;  // 透视除法，xy转换为NDC
    float2 endScreen = endHScreen.xy * endK;  

    // 经过齐次除法的视角坐标  
    float3 startQ = startView * startK;  //  为观察空间赋予非线性特性
    float3 endQ = endView * endK;  

    // 根据斜率将dx=1 dy = delta  
    float2 diff = endScreen - startScreen;  
    bool permute = false;  
    if (abs(diff.x) < abs(diff.y)) {  // 进行DDA翻转
        permute = true;  

        diff = diff.yx;  
        startScreen = startScreen.yx;  
        endScreen = endScreen.yx;  
    }  
    // 计算屏幕坐标、齐次视坐标、inverse-w的线性增量  
    float dir = sign(diff.x);  
    float invdx = dir / diff.x;  
    float2 dp = float2(dir, invdx * diff.y);  
    float3 dq = (endQ - startQ) * invdx;  
    float dk = (endK - startK) * invdx;  

    dp *= _Stride;  
    dq *= _Stride;  
    dk *= _Stride;  

    // 缓存当前深度和位置  
    float rayZMin = startView.z;  
    float rayZMax = startView.z;  
    float preZ = startView.z;  

    float2 P = startScreen;  
    float3 Q = startQ;  
    float K = startK;  

    end = endScreen.x * dir;  

    // 进行屏幕空间射线步近  
    UNITY_LOOP  
    for (int i = 0; i < _StepCount && P.x * dir <= end; i++) {  
        // 步近  
        P += dp;  
        Q.z += dq.z;  
        K += dk;  
        // 得到步近前后两点的深度  
        rayZMin = preZ;  
        rayZMax = (dq.z * 0.5 + Q.z) / (dk * 0.5 + K);  
        preZ = rayZMax;        if (rayZMin > rayZMax)  
            swap(rayZMin, rayZMax);  
    
        // 得到交点uv  
        float2 hitUV = permute ? P.yx : P;  
        hitUV *= _SourceSize.zw;  
        if (any(hitUV < 0.0) || any(hitUV > 1.0))  
            return GetSourceWithLinear(input.screenUV);  
        float surfaceDepth = -LinearEyeDepth(SampleSceneDepth(hitUV), _ZBufferParams);  
        
        if (BinarySearchRaymarching(startView, rDir, hitUV))  
            return float4(1.0, 0.0, 0.0, 1.0);  
    }  
    return GetSourceWithLinear(input.screenUV);  
}

half4 SSRPassFragment2(Varyings input) : SV_Target {  
    float rawDepth = SampleSceneDepth(input.screenUV);  // 采样得当前片段深度值
    float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);  // 变换到观察空间线性深度值，方便进行比较
    float3 vpos = ReconstructViewPos(input.screenUV, linearDepth);  // 获得片段在观察空间的坐标
    float3 vnormal = SampleSceneNormal(input.screenUV);  // 法线在世界空间下的方向
    float3 vDir = normalize(vpos);  
    float3 rDir = normalize(reflect(vDir, vnormal));  // 求得反射方向

    float2 uv;  
    float depth;

    UNITY_LOOP
    for (int i = 0; i < STEP_COUNT; i++) {  
        float3 vpos2 = vpos + rDir * STEP_SIZE * i;  // 开始沿反射方向步进
        float2 uv2;  
        float stepDepth;  
        ReconstructUVAndDepth(vpos2, uv2, stepDepth);  // 计算步进点的深度值
        float stepRawDepth = SampleSceneDepth(uv2);  
        float stepSurfaceDepth = LinearEyeDepth(stepRawDepth, _ZBufferParams);   
        if (stepSurfaceDepth < stepDepth && stepDepth < stepSurfaceDepth + _Thickness)  // 若步进到达目标 ，条件二限制进入厚度，防止步进过深
            return GetSourceWithLinear(uv2);  // 返回目标位置颜色
    }    
    return half4(0, 0, 0, 1.0);  
}
#endif 