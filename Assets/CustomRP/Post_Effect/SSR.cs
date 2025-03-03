using System;
using CustomRP.Runtime.Volume;
using UnityEngine;
using UnityEngine.Rendering;
using BoolParameter = CustomRP.Runtime.Volume.BoolParameter;
using FloatParameter = CustomRP.Runtime.Volume.FloatParameter;
using IntParameter = CustomRP.Runtime.Volume.IntParameter;
using VolumeComponent = CustomRP.Runtime.Volume.VolumeComponent;

[Serializable]
[CustomRP.Runtime.Volume.VolumeComponentMenu("Post-processing/SSR")]
// SSR必须位于第一位
public class SSR :  VolumeComponent,IPostProcessComponent
{ 
    public enum BlendMode{
        Addtive,
        Balance
    }
    
    public FloatParameter Intensity = new FloatParameter(0.8f);
    public FloatParameter MaxDistance = new FloatParameter(10.0f);
    public IntParameter Stride = new IntParameter(30);
    public IntParameter StepCount = new IntParameter(12);
    public FloatParameter Thickness = new FloatParameter(0.5f);
    public IntParameter BinaryCount = new IntParameter(6);
    public  BoolParameter jitterDither = new BoolParameter(true);
    public  EnumParameter<BlendMode> blendMode = new EnumParameter<BlendMode>(BlendMode.Addtive);
    public  FloatParameter BlurRadius = new FloatParameter(1.0f); 
    
    internal enum ShaderPass{
        Raymarching,
        Blur,
        Addtive,
        Balance,
    }
    
    private ProfilingSampler mProfilingSampler = new ProfilingSampler("SSR");
    private RenderTextureDescriptor mSSRDescriptor;
    

    private static readonly int mProjectionParams2ID = Shader.PropertyToID("_ProjectionParams2"),
        mCameraViewTopLeftCornerID = Shader.PropertyToID("_CameraViewTopLeftCorner"),
        mCameraViewXExtentID = Shader.PropertyToID("_CameraViewXExtent"),
        mCameraViewYExtentID = Shader.PropertyToID("_CameraViewYExtent"),
        mSourceSizeID = Shader.PropertyToID("_SourceSize"),
        mSSRParams0ID = Shader.PropertyToID("_SSRParams0"),
        mSSRParams1ID = Shader.PropertyToID("_SSRParams1"),
        IntensityID =  Shader.PropertyToID("_Intensity"),
        MaxDistanceID = Shader.PropertyToID("_MaxDistance"),
        StrideID = Shader.PropertyToID("_Stride"),
        StepCountID = Shader.PropertyToID("_StepCount"),
        ThicknessID = Shader.PropertyToID("_Thickness"),
        BinaryCountID = Shader.PropertyToID("_BinaryCount");

    private const string mJitterKeyword = "_JITTER_ON";
    
    private const string mSSRTexture0Name = "_SSRTexture0",
        mSSRTexture1Name = "_SSRTexture1";

    public bool IsActive()
    {
        return enabled.value;
    }

    public SSR()
    {
        _postFXPrePass = PostFX_PrePass.NormalDepthTex;
    }
    public void Prepare(bool useHDR)
    {
        _useHDR = useHDR;
    }

    public void Render(CommandBuffer cmd, Camera camera, int fxSourceID, Material material)
    {
        OnCameraSetup(cmd, camera, material);
        
        RenderTextureFormat format = _useHDR? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        
        cmd.GetTemporaryRT(fxSource2ID, camera.pixelWidth, camera.pixelHeight, 0 , FilterMode.Bilinear,format);
        cmd.GetTemporaryRT(fxSource3ID, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, format);
        
        using (new ProfilingScope(cmd, mProfilingSampler)) {
            // SSR
            Draw(fxSourceID, fxSource2ID, PostFX_Pass.CopyWithPoint, cmd, material);
            // Horizontal Blur
            // cmd.SetGlobalVector(mBlurRadiusID, new Vector4(BlurRadius.value, 0.0f, 0.0f, 0.0f));
            // Draw(fxSource2ID, fxSource3ID, PostFX_Pass.BloomHorizontal,cmd, material);
            //
            // // Vertical Blur
            // // cmd.SetGlobalVector(mBlurRadiusID, new Vector4(0.0f, BlurRadius.value, 0.0f, 0.0f));
            // Draw(fxSource3ID, fxSource2ID, PostFX_Pass.BloomVertical, cmd, material);
            // // Additive Pass
            Draw(fxSource2ID, fxSourceID, blendMode.value == BlendMode.Addtive ? 
                PostFX_Pass.SSRAddtive: PostFX_Pass.SSRBalance, cmd, material);
        }
        
        cmd.ReleaseTemporaryRT(fxSource2ID);
        cmd.ReleaseTemporaryRT(fxSource3ID);
    }
    
    // 发送屏幕空间重构世界空间的数据
    public void OnCameraSetup(CommandBuffer cmd, Camera camera, Material material) {

        // 发送参数
        Matrix4x4 view = camera.worldToCameraMatrix;
        // 使用GPU投影矩阵适配当前图形API
        Matrix4x4 proj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
        Matrix4x4 vp = proj * view;
    
        // 将camera view space 的平移置为0，用来计算world space下相对于相机的vector
        Matrix4x4 cview = view;
        cview.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
        Matrix4x4 cviewProj = proj * cview;
    
        // 计算viewProj逆矩阵，即从裁剪空间变换到世界空间
        Matrix4x4 cviewProjInv = cviewProj.inverse;
        
        // 计算世界空间下，近平面四个角的坐标
        var near = camera.nearClipPlane;
        // Vector4 topLeftCorner = cviewProjInv * new Vector4(-near, near, -near, near);
        // Vector4 topRightCorner = cviewProjInv * new Vector4(near, near, -near, near);
        // Vector4 bottomLeftCorner = cviewProjInv * new Vector4(-near, -near, -near, near);
        Vector4 topLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1.0f, 1.0f, -1.0f, 1.0f));
        Vector4 topRightCorner = cviewProjInv.MultiplyPoint(new Vector4(1.0f, 1.0f, -1.0f, 1.0f));
        Vector4 bottomLeftCorner = cviewProjInv.MultiplyPoint(new Vector4(-1.0f, -1.0f, -1.0f, 1.0f));
    
        // 计算相机近平面上方向向量
        Vector4 cameraXExtent = topRightCorner - topLeftCorner;
        Vector4 cameraYExtent = bottomLeftCorner - topLeftCorner;
        
        // 发送ReconstructViewPos参数
        cmd.SetGlobalVector(mCameraViewTopLeftCornerID, topLeftCorner);
        cmd.SetGlobalVector(mCameraViewXExtentID, cameraXExtent);
        cmd.SetGlobalVector(mCameraViewYExtentID, cameraYExtent);
        // 优先在CPU端做除法
 
        var position = camera.transform.position;
        cmd.SetGlobalVector(mProjectionParams2ID, new Vector4(1.0f / near, position.x, position.y, position.z));
    
        cmd.SetGlobalVector(mSourceSizeID, new Vector4(mSSRDescriptor.width, mSSRDescriptor.height, 1.0f / mSSRDescriptor.width, 1.0f / mSSRDescriptor.height));
    
        // 发送SSR参数
        cmd.SetGlobalVector(mSSRParams0ID, new Vector4(MaxDistance.value, Stride.value, StepCount.value, Thickness.value));
        cmd.SetGlobalVector(mSSRParams1ID, new Vector4(BinaryCount.value, Intensity.value, 0.0f, 0.0f));
        cmd.SetGlobalFloat(IntensityID, Intensity.value);
        cmd.SetGlobalFloat(MaxDistanceID, MaxDistance.value);
        cmd.SetGlobalInt(StrideID, Stride.value);
        cmd.SetGlobalInt(StepCountID, StepCount.value);
        cmd.SetGlobalFloat(ThicknessID, Thickness.value);
        cmd.SetGlobalInt(BinaryCountID, BinaryCount.value);
        // 设置全局keyword
        if (jitterDither.value) {
            material.EnableKeyword(mJitterKeyword);
        }
        else {
            material.DisableKeyword(mJitterKeyword);
        }
    }
}
