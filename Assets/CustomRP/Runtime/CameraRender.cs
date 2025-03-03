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
    private bool useHDR;
    private bool _usePostFX;
    
    // 实例化脚本
    PostFXStack postFXStack = new PostFXStack();
    private Lighting _lighting = new Lighting();
    private RenderPostFX_PrePass _renderPostFXPrePass = new RenderPostFX_PrePass();
    private VolumeManager _volumeManager = VolumeManager.instance;
    private DeferredRender _deferredRender = new DeferredRender();
    private DrawNormalDepth _drawNormalDepth;
    
    // Shader相关属性
    private static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer"),
        depthNormalTex = Shader.PropertyToID("_DepthNormalTex");

    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
        litShaderTagId = new ShaderTagId("CustomLit"),
        ToonTag = new ShaderTagId("ToonTag");
        
    public void Render(ScriptableRenderContext context, Camera camera,  RenderData.BatchingSettings batchingSettings, 
         ShadowSetting shadowSetting, RenderData.PostFXData postFXSettings, RenderData.DeferredRenderingSettings deferredRenderingSettings)
    {
        this._context = context;
        this._camera = camera;
        _usePostFX = _volumeManager.CheckEffectActive() && postFXSettings.usePostFx;  //只有当至少一个effect启动，且场景开启后处理选择
        
        PrepareBuffer();
        // 绘制UI等
        PrepareForSceneWindow();
        
      
        // 平截头体剔除，获取剔除结果
        if (!Cull(shadowSetting.maxDistance))
        {
            return;
        }
        useHDR = postFXSettings.useHDR && camera.allowHDR;
        
        // 初始化配置阶段
        // 运行时初始化设置
        _buffer.BeginSample(bufferName);
        ExecuteBuffer();
        _lighting.Setup(_context, _cullingResults, shadowSetting);
        _buffer.EndSample(bufferName);
            
        _renderPostFXPrePass.Setup(context ,camera);
        postFXStack.Setup(context, camera,  postFXSettings.PostFXSettings, useHDR);
        Setup();

        if (postFXSettings.needDepthNormal)
        {
            _drawNormalDepth ??= new DrawNormalDepth();
            _drawNormalDepth.DrawNormalDepthTex(_context, _camera, ref _cullingResults);
          
            _buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            ExecuteBuffer();
        }
        
        
        if (_usePostFX)
        {
            _renderPostFXPrePass.Render(ref _cullingResults, _volumeManager.GetPostFXPrePasses());
            SwitchRenderTarget(frameBufferId);
            
            // 进行常规绘制渲染；若开启后处理，则结果将渲染到中间纹理上
            DrawVisibleGeometry(batchingSettings.useDynamicBatching, batchingSettings.useGPUInstancing);
            DrawUnsupportedShaders();
            DrawGizmosBeforePostFX();

            // 若开启了后处理，则将摄像机渲染结果传入，进行渲染
        
            postFXStack.Render(frameBufferId);
            
            DrawGizmosAfterPostFX();
        }
        else if (deferredRenderingSettings.useDeferredRender)
        {
            _deferredRender.Setup(_context, _camera);
            _deferredRender.Render();
            
        }
        else
        {
            _buffer.BeginSample(bufferName);
            DrawVisibleGeometry(batchingSettings.useDynamicBatching, batchingSettings.useGPUInstancing);
            DrawUnsupportedShaders();
            DrawGizmosBeforePostFX();
            _buffer.EndSample(bufferName);
        }
        // 清除释放资源阶段
            Cleanup(postFXSettings);
            Submit();
    }
    // 渲染初始化，清除渲染缓存，开始采样 
    private void Setup()
    {
        // 后处理准备
        _buffer.BeginSample(SampleName);
        _context.SetupCameraProperties(_camera);
        // 获取Camera的清除标志
        CameraClearFlags flags = _camera.clearFlags;
        
        // 若开启后处理，则将中间纹理设置为Camera的渲染目标
        if (_usePostFX)
        {
            // 清除中间缓存区的原内容
            if (flags > CameraClearFlags.Color)
            {
                flags = CameraClearFlags.Color;
            }
            // 为nameId创建存储摄像机输出的RT
            _buffer.GetTemporaryRT(frameBufferId, _camera.pixelWidth, _camera.pixelHeight, 
                32, FilterMode.Point, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);  // 查看是否使用HDR
        }
        // 清除摄像机缓存区的缓存
        _buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags <= CameraClearFlags.Color,
            flags == CameraClearFlags.Color ?
                _camera.backgroundColor.linear : Color.clear
        );
        ExecuteBuffer();
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
    void SwitchRenderTarget(RenderTargetIdentifier colorTarget)
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
    void Cleanup(RenderData.PostFXData postFXData)
    {
        _lighting.Cleanup();
        if (postFXData.needDepthNormal)
        {
            _drawNormalDepth.cleanup();
        }
        if (_usePostFX)
        {
            postFXStack.Cleanup();
            _renderPostFXPrePass.Cleanup();
            _buffer.ReleaseTemporaryRT(frameBufferId);
        }
    }
}