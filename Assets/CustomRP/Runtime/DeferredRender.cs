using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class DeferredRender
{
    // ********************************  全局变量 *********************************
    RenderTexture gdepth;                                               // depth attachment
    RenderTexture[] gbuffers = new RenderTexture[4];                    // color attachments 
    RenderTargetIdentifier[] gbufferID = new RenderTargetIdentifier[4]; // tex ID 
    private Camera _camera;
    private CommandBuffer _cmd;
    private ScriptableRenderContext _context;
    
    public Cubemap specularIBL;
    public Cubemap diffuseIBL;
    public Texture BRDFLUT;
    
    // ***************************************************************************
    
    public void Setup(ScriptableRenderContext context, Camera camera)
    {
        _camera = camera;
        _context = context;
        
        // 初始化 CommandBuffer（仅一次）
        if (_cmd == null)
        {
            _cmd = new CommandBuffer { name = "GBuffer" };
        }
        CreateTexture();
        
        // 设置渲染目标并清除
        _cmd.Clear();
        _cmd.SetRenderTarget(gbufferID, gdepth);
        _cmd.ClearRenderTarget(true, true, Color.clear);
        
        // 设置 gbuffer 为全局纹理
        _cmd.SetGlobalTexture("_gdepth", gdepth);  // 直接设置帧缓冲的depth为全局变量
        for(int i=0; i<4; i++) 
            _cmd.SetGlobalTexture("_GT"+i, gbuffers[i]);
        
        context.ExecuteCommandBuffer(_cmd);
    }
    
    public void Render()
    {
        // 剔除
        _camera.TryGetCullingParameters(out var cullingParameters);
        var cullingResults = _context.Cull(ref cullingParameters);

        // 配置绘制设置
        ShaderTagId shaderTagId = new ShaderTagId("gbuffer");
        SortingSettings sortingSettings = new SortingSettings(_camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;

        Matrix4x4 viewMatrix = _camera.worldToCameraMatrix;
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false);
        Matrix4x4 vpMatrix = projMatrix * viewMatrix;
        Matrix4x4 vpMatrixInv = vpMatrix.inverse;
        _cmd.SetGlobalMatrix("_vpMatrix", vpMatrix);  
        _cmd.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);  // 传递视角投影矩阵的逆矩阵
        
        _cmd.SetGlobalTexture("_specularIBL", specularIBL);
        _cmd.SetGlobalTexture("_diffuseIBL", diffuseIBL);
        _cmd.SetGlobalTexture("_brdflut", BRDFLUT);
        
        // 绘制渲染器
        _context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        
        // 绘制天空盒和 Gizmos
        LightPass(_context, _camera);
        _context.DrawSkybox(_camera);
        if (Handles.ShouldRenderGizmos())
        {
            _context.DrawGizmos(_camera, GizmoSubset.PreImageEffects);
            _context.DrawGizmos(_camera, GizmoSubset.PostImageEffects);
        }
        
        // 设置相机矩阵
        
        // 提交渲染命令
        _context.Submit();
    }

    void CreateTexture()
    {
        int width = _camera.pixelWidth;
        int height = _camera.pixelHeight;

        // 检查并重新创建 RenderTextures（仅当尺寸变化时）
        if (gdepth == null || gdepth.width != width || gdepth.height != height)
        {
            // 释放旧 RenderTextures
            if (gdepth != null) gdepth.Release();
            foreach (var rt in gbuffers)
            {
                if (rt != null) rt.Release();
            }

            // 创建新 RenderTextures
            gdepth = new RenderTexture(width, height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
            gbuffers[0] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            gbuffers[1] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB2101010);
            gbuffers[2] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB64);
            gbuffers[3] = new RenderTexture(width, height, 0, RenderTextureFormat.ARGBFloat);

            // 更新 RenderTargetIdentifier
            for (int i = 0; i < 4; i++)
            {
                gbufferID[i] = gbuffers[i];
            }
        }
    }
    void LightPass(ScriptableRenderContext context, Camera camera)
    {
        // 使用 Blit
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "lightpass";

        Material mat = new Material(Shader.Find("Hidden/Custom RP/Post FX Stack/DeferredLightPass"));
        cmd.Blit(gbufferID[0], BuiltinRenderTextureType.CameraTarget, mat);
        context.ExecuteCommandBuffer(cmd);
    }

    public void CleanUp()
    {
        
    }
}