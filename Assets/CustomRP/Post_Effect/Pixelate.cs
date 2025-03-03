using System;
using UnityEngine;
using CustomRP.Runtime.Volume;
using UnityEngine.Rendering;
using BoolParameter = CustomRP.Runtime.Volume.BoolParameter;
using FloatParameter = CustomRP.Runtime.Volume.FloatParameter;
using IntParameter = CustomRP.Runtime.Volume.IntParameter;
using VolumeComponent = CustomRP.Runtime.Volume.VolumeComponent;

[Serializable]
[CustomRP.Runtime.Volume.VolumeComponentMenu("Post-processing/Pixelate")]
public class Pixelate : VolumeComponent,IPostProcessComponent
{
    public IntParameter maxIteration = new IntParameter(2);
    public FloatParameter outlineDarkenFactor = new FloatParameter(0.5f);
    public FloatParameter outlineLightenFactor = new FloatParameter(0.5F);
    public BoolParameter checkEdge = new BoolParameter(false);

    private static int surfaceIdDepthId ;
    private static int fxPixelDownSampleID,  // Pixel相关
        surfaceIdDepthDownSampleID ,
        outlineID ,
        edgePixelTexID,
        outlineDarkenFactorID = Shader.PropertyToID("_OutlineDarkenFactor"),
        outlineLightenFactorID = Shader.PropertyToID("_OutlineLightenFactor"),
        checkEdgeID = Shader.PropertyToID("_CheckEdge");
   
    
    // public override void GetParameters()
    // {
    //     base.GetParameters();
    // }

    public bool IsActive()
    {
        return enabled.value;
    }

    public Pixelate()
    {
        _postFXPrePass = PostFX_PrePass.SurfaceIdDepthTex;
    }
    public void Prepare(bool useHDR)
    {
        fxPixelDownSampleID = Shader.PropertyToID("_FXPixelDownSample");  // Pixel相关
        surfaceIdDepthDownSampleID = Shader.PropertyToID("_SurfaceIdDepthDownSampleTex");
        outlineID = Shader.PropertyToID("_OutlineTex");
        edgePixelTexID = Shader.PropertyToID("_EdgePixelTexId");
        
        surfaceIdDepthId = Shader.PropertyToID("_SurfaceIdDepthTex");

        _useHDR = useHDR;
    }

    // TODO: 修改draw调用
    // void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, PostFX_Pass pass, CommandBuffer buffer, Material material)
    // {
    //     buffer.SetGlobalTexture(fxSourceID, from);  // 设置全局渲染源纹理
    //     buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
    //     // 进行绘制
    //     buffer.DrawProcedural(Matrix4x4.identity, material, (int)pass, MeshTopology.Triangles, 3);
    // }

    public void Render(CommandBuffer buffer, Camera _camera, int fxSourceID, Material material)
    {
        // 获取Setting设置
        // TODO: 池化处理
        buffer.BeginSample("Pixelate");
        // 创建纹理
        RenderTextureFormat format = _useHDR? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        int downWidth = (int)(_camera.pixelWidth / maxIteration.value);
        int downHeight = (int)(_camera.pixelHeight / maxIteration.value);
        buffer.SetGlobalFloat(outlineDarkenFactorID, outlineDarkenFactor.value); 
        buffer.SetGlobalFloat(outlineLightenFactorID, outlineLightenFactor.value);

        float check = checkEdge.value == true ? 1 : 0;
        buffer.SetGlobalFloat(checkEdgeID, check);
        
        buffer.GetTemporaryRT(fxPixelDownSampleID, downWidth, downHeight,  // 过滤模式指当读取采样本贴图写入到其他贴图时的过滤
            32, FilterMode.Point, format);
        buffer.GetTemporaryRT(outlineID, downWidth, downHeight, 
            32, FilterMode.Point, format);
        buffer.GetTemporaryRT(edgePixelTexID, _camera.pixelWidth, _camera.pixelHeight, 
            24, FilterMode.Point, format);
        buffer.GetTemporaryRT(surfaceIdDepthDownSampleID, downWidth, downHeight, 
            32, FilterMode.Point, format);
        
        // 下采样像素化
        Draw(fxSourceID,fxPixelDownSampleID , PostFX_Pass.CopyWithPoint, buffer, material);
        Draw(surfaceIdDepthId, surfaceIdDepthDownSampleID, PostFX_Pass.CopyWithPoint, buffer, material);  // ?表面Id
        buffer.SetGlobalTexture(fxPixelDownSampleID, fxPixelDownSampleID);
        buffer.SetGlobalTexture(surfaceIdDepthDownSampleID, surfaceIdDepthDownSampleID);
        
        // 绘制边缘图
        Draw(outlineID, outlineID, PostFX_Pass.EdgeDectectWithSurfaceIdDepth, buffer, material);
        buffer.SetGlobalTexture(outlineID, outlineID);
        
        // 混合描边和原颜色
        Draw(fxPixelDownSampleID, fxSourceID, PostFX_Pass.DrawEdgePixel, buffer, material);
        
        //buffer.SetGlobalTexture(fxSourceID, fxSourceID);
        
        buffer.ReleaseTemporaryRT(fxPixelDownSampleID);
        buffer.ReleaseTemporaryRT(outlineID);
        buffer.ReleaseTemporaryRT(edgePixelTexID);
        buffer.ReleaseTemporaryRT(surfaceIdDepthDownSampleID);
        
        buffer.EndSample("Pixelate");
    }
}