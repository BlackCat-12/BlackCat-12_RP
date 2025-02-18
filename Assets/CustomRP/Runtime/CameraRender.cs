using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Camera = UnityEngine.Camera;
using CustomRP.Runtime.Volume;
public partial class CameraRenderer 
{
    // 本身变量
    private ScriptableRenderContext _context;
    private Camera _camera;
    private const string bufferName = "Render Camera";
    private CullingResults _cullingResults;
    private CommandBuffer _buffer = new CommandBuffer { name = bufferName };
    
    private bool _usePostFX = false, _useForwardRD = true, _useDeferredRD = false;
    
    // 实例化脚本
    private PostFXStack _postFXStack = new PostFXStack();
    private Lighting _lighting = new Lighting();
    private RenderPostFX_PrePass _renderPostFXPrePass = new RenderPostFX_PrePass();
    private VolumeManager _volumeManager = VolumeManager.instance;
    private DeferredRender _deferredRender = new DeferredRender();
    
    // Shader相关属性
    private static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");
    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
        litShaderTagId = new ShaderTagId("CustomLit"),
        ToonTag = new ShaderTagId("ToonTag");
        
    public void Render(ScriptableRenderContext context, Camera camera,  bool useDynamicBatching, bool useGPUInstancing, 
         ShadowSetting shadowSetting, PostFXUI postFXSettings, DeferredRenderingSettings deferredRenderingSettings)
    {
        _context = context;
        _camera = camera;
        
        PreSetup(shadowSetting, deferredRenderingSettings, postFXSettings);
        // 初始化配置阶段
        MainSetup(shadowSetting, postFXSettings.PostFXSettings);
        
        // 渲染阶段
        // 后处理准备阶段
        if (_useDeferredRD)
        {
            RenderDeferred();
        }else if (_usePostFX)
        {
            RenderPostFX(useDynamicBatching, useGPUInstancing);
        }
        else
        {
            RenderForward(useDynamicBatching, useGPUInstancing);
        }
        
        // 清除释放资源阶段
        Cleanup();
        Submit();
    }

    private void PreSetup(ShadowSetting shadowSetting, DeferredRenderingSettings deferredRenderingSettings, 
        PostFXUI postFxui)
    {
        PrepareBuffer();
        // 绘制UI等
        PrepareForSceneWindow();
        
        // 平截头体剔除，获取剔除结果
        if (!Cull(shadowSetting.maxDistance))
        {
            return;
        }

        _usePostFX = _volumeManager.CheckEffectActive() && postFxui.usePostFx;
        _useDeferredRD = deferredRenderingSettings.useDeferredRender;
    }
    
    private void MainSetup(ShadowSetting shadowSetting, PostFXSettings postFXSettings)
    {
        _context.SetupCameraProperties(_camera);  // 将摄像机属性应用到渲染上下文
        
        _buffer.BeginSample(bufferName);
        _lighting.Setup(_context, _cullingResults, shadowSetting);
        _buffer.EndSample(bufferName);
        ExecuteBuffer();
        
        if (_useDeferredRD)
        {
            _deferredRender.Setup(_context, _camera);
        }else if (_usePostFX)
        {
            _renderPostFXPrePass.Setup(_context ,_camera);
            _postFXStack.Setup(_context, _camera, postFXSettings);
        }
        else
        {
            SetupForwardRD();
        }
    }
    
    // 渲染初始化，清除渲染缓存，开始采样 
    private void SetupForwardRD()
    {
        // 后处理准备
        _buffer.BeginSample(SampleName);
        _context.SetupCameraProperties(_camera);
        // 获取Camera的清除标志
        CameraClearFlags flags = _camera.clearFlags;
        
 
        ExecuteBuffer();
    }

    void RenderPostFX(bool useDynamicBatching, bool useGPUInstancing)
    {
      
        _renderPostFXPrePass.Render(ref _cullingResults, _volumeManager.GetPostFXPrePasses());
        SwitchRenderTarget(PostFXStack.frameBufferId);
        
        // 进行常规绘制渲染；若开启后处理，则结果将渲染到中间纹理上
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmosBeforePostFX();

        // 若开启了后处理，则将摄像机渲染结果传入，进行渲染
    
        _postFXStack.Render(PostFXStack.frameBufferId);
        
        DrawGizmosAfterPostFX();
    }

    void RenderDeferred()
    {
        _deferredRender.Render();
    }
    void RenderForward(bool useDynamicBatching, bool useGPUInstancing)
    {
        _buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
        ExecuteBuffer();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
    }
    
    
    // 将缓存中的指令提交
    void Submit () {
        _buffer.EndSample(SampleName);
        ExecuteBuffer();
        _context.Submit();
    }
    
    // 绘制可见的几何体
    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        // 初始化渲染参数设置
        var sortingSetting = new SortingSettings(_camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        
        // 初始化绘制设置
        var drawingSetting = new DrawingSettings(unlitShaderTagId ,sortingSetting)
        {
            enableInstancing = useGPUInstancing,
            enableDynamicBatching = useDynamicBatching
        };
        
        drawingSetting.SetShaderPassName(1, litShaderTagId);
        drawingSetting.SetShaderPassName(2, ToonTag);
        var filteringSetting = new FilteringSettings(RenderQueueRange.opaque);
        
        // 绘制不透明几何体
        _context.DrawRenderers(_cullingResults, ref drawingSetting, ref filteringSetting);
        
        // 绘制天空盒
        _context.DrawSkybox(_camera);
        
        // 绘制透明物体
        sortingSetting.criteria = SortingCriteria.CommonTransparent;
        drawingSetting.sortingSettings = sortingSetting;
        filteringSetting.renderQueueRange = RenderQueueRange.transparent;
        _context.DrawRenderers(_cullingResults, ref drawingSetting, ref filteringSetting);
    }
    
    // 缓存的执行与清除
    void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }
    
    // 切换渲染目标
    void SwitchRenderTarget(int colorTarget)
    {
        _buffer.SetRenderTarget(
            colorTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
            colorTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
        );
        _buffer.ClearRenderTarget(true, true, Color.clear);
        ExecuteBuffer();
    }

    bool Cull(float maxShadowDistance)
    {
        if (_camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            // 传入Shadow的剔除距离
            p.shadowDistance = Mathf.Min(maxShadowDistance, _camera.farClipPlane);
            // 在上下文调用剔除函数
            _cullingResults = _context.Cull(ref p);
            return true;
        }
        return false;
    }
    
    // 释放中间缓存
    void Cleanup()
    {
        _lighting.Cleanup();
        if (_usePostFX)
        {
            _postFXStack.Cleanup();
            _renderPostFXPrePass.Cleanup();
        }

        if (_useDeferredRD)
        {
            _deferredRender.CleanUp();
        }
    }
}