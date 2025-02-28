using System.Collections;
using System.Collections.Generic;
using CustomRP.Runtime.Volume;
using UnityEngine;
using UnityEngine.Rendering;

public class RenderPostFX_PrePass 
{
    private Camera _camera;
    private ScriptableRenderContext _context;
    private CullingResults _cullingResults;
    private CommandBuffer _cmd;
    private List<PostFX_PrePass> _fxPrePasses;
    
    const string bufferName = "PreparePostFX";

    private delegate void PostFXPrePassDelegate();
    private delegate void FXPreCleanup();
    
    private Dictionary<PostFX_PrePass, PostFXPrePassDelegate> postFXPrePassFunctions = 
        new Dictionary<PostFX_PrePass, PostFXPrePassDelegate>();
    private Dictionary<PostFX_PrePass, FXPreCleanup> FXPreCleanups = 
        new Dictionary<PostFX_PrePass, FXPreCleanup>();
    
    // Shader相关
    private static ShaderTagId normalDepthTagId = new ShaderTagId("DepthNormals"),  // Pass标签
        surfaceIdDepthTagId = new ShaderTagId("SurfaceIdDepth");

    private static int depthNormalId = Shader.PropertyToID("_DepthNormalTex"), // 全局纹理属性
        surfaceIdDepthId = Shader.PropertyToID("_SurfaceIdDepthTex");

    public RenderPostFX_PrePass()
    {
        postFXPrePassFunctions[PostFX_PrePass.NormalDepthTex] = DrawNormalDepthTex;
        postFXPrePassFunctions[PostFX_PrePass.SurfaceIdDepthTex] = DrawSurfaceIdDepthTex;

        FXPreCleanups[PostFX_PrePass.NormalDepthTex] = CleanNormalDepthTex;
        FXPreCleanups[PostFX_PrePass.SurfaceIdDepthTex] = CleanSurfaceIdDepthTex;
    }
    
    public void Setup(ScriptableRenderContext context, Camera camera)
    {
        _camera = camera;
        _context = context;
    }

    public void Render(ref CullingResults cullResults, List<PostFX_PrePass> fxPrePasses)
    {
        _fxPrePasses = fxPrePasses;
        _cmd = CommandBufferPool.Get(bufferName);  // 池化指令，减少消耗
        _cmd.BeginSample(bufferName);

        _cullingResults = cullResults;

        foreach (var pass in fxPrePasses)   
        {
            postFXPrePassFunctions[pass].Invoke();
            //Debug.Log(pass.ToString());
        }
        
        _cmd.EndSample(bufferName);
        _context.ExecuteCommandBuffer(_cmd);  // 执行指令，并清除命令缓存区
        _cmd.Clear();
        CommandBufferPool.Release(_cmd);
    }

    public void DrawSurfaceIdDepthTex()
    {
        _cmd.GetTemporaryRT(surfaceIdDepthId, _camera.pixelWidth, _camera.pixelHeight,  
            24, FilterMode.Point, RenderTextureFormat.ARGBHalf);

        _cmd.SetRenderTarget(
            surfaceIdDepthId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
            surfaceIdDepthId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
        );
        
        _cmd.ClearRenderTarget(true, true, Color.clear);
        
        // 执行命令缓冲区，确保在渲染前正确设置RenderTarget
        _context.ExecuteCommandBuffer(_cmd);
        _cmd.Clear();
        
        var drawSettings = new DrawingSettings(surfaceIdDepthTagId, new SortingSettings(_camera));
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        
        // 执行渲染
        _context.DrawRenderers(_cullingResults, ref drawSettings, ref filteringSettings);
    }

    void DrawNormalDepthTex()
    {
        _cmd.GetTemporaryRT(depthNormalId, _camera.pixelWidth, _camera.pixelHeight,  
            24, FilterMode.Point, RenderTextureFormat.ARGBHalf);

        _cmd.SetRenderTarget(
            depthNormalId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
            depthNormalId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
        );
        
        _cmd.ClearRenderTarget(true, true, Color.clear);
        
        // 执行命令缓冲区，确保在渲染前正确设置RenderTarget
        _context.ExecuteCommandBuffer(_cmd);
        _cmd.Clear();
        
        var drawSettings = new DrawingSettings(normalDepthTagId, new SortingSettings(_camera));
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
        
        // 执行渲染
        _context.DrawRenderers(_cullingResults, ref drawSettings, ref filteringSettings);
        
        _cmd.SetGlobalTexture(depthNormalId, depthNormalId);
    }

    public void CleanSurfaceIdDepthTex()
    {
        _cmd.ReleaseTemporaryRT(surfaceIdDepthId);
    }
    
    public void CleanNormalDepthTex()
    {
        _cmd.ReleaseTemporaryRT(depthNormalId);
    }
    
    public void Cleanup()
    {
        if (_fxPrePasses == null)
        {
            return;
        }
        foreach (var pass in _fxPrePasses)   
        {
            FXPreCleanups[pass]?.Invoke();
        }
    }
}
