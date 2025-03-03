using UnityEngine;
using UnityEngine.Rendering;

public class DrawNormalDepth
{
    private CommandBuffer _buffer;
    private string passName = "DrawDepthNormal";

    private ScriptableRenderContext _context;
    // Shader相关
    private static ShaderTagId normalDepthTagId = new ShaderTagId("DepthNormals");  // Pass标签
    private static int depthNormalId = Shader.PropertyToID("_DepthNormalTex"); // 全局纹理属性
    
    public DrawNormalDepth()
    {
        _buffer = new CommandBuffer()
        {
            name = passName
        };
    }
    
    public void DrawNormalDepthTex(ScriptableRenderContext context, Camera camera, ref CullingResults cullingResults)
    {
        _buffer.BeginSample(passName);
        _context = context;
        // 获取临时渲染纹理，包含24位深度缓冲区
        _buffer.GetTemporaryRT(depthNormalId, camera.pixelWidth, camera.pixelHeight, 24, FilterMode.Point, RenderTextureFormat.ARGB32);

        // 设置RenderTarget，仅绑定颜色缓冲区，深度自动使用该RT的深度部分
        _buffer.SetRenderTarget(
            depthNormalId,
            RenderBufferLoadAction.DontCare,
            RenderBufferStoreAction.Store
        );
    
        _buffer.ClearRenderTarget(true, true, Color.clear);
    
        // 执行命令缓冲区以确保RenderTarget正确设置
        context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    
        var drawSettings = new DrawingSettings(normalDepthTagId, new SortingSettings(camera));
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
    
        // 绘制渲染器到目标纹理
        context.DrawRenderers(cullingResults, ref drawSettings, ref filteringSettings);
    
        // 将临时纹理设置为全局纹理
        //_buffer.SetGlobalTexture(depthNormalId, depthNormalId);
        _buffer.EndSample(passName);
        context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }

    public void cleanup()
    {
        _buffer.ReleaseTemporaryRT(depthNormalId);
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }
    ~DrawNormalDepth()
    {
        _buffer.Release();
    }
}
