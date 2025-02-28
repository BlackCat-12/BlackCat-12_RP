using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    //创建单个camera的渲染实例
    private CameraRenderer _renderer = new CameraRenderer();
    private RenderData.BatchingSettings _batchingSettings;

    private ShadowSetting shadowSetting;
    private RenderData.PostFXData postFXSettings;
    private RenderData.DeferredRenderingSettings _deferredRenderingSettings;
    


    //构造函数
    public CustomRenderPipeline(RenderData.BatchingSettings batchingSettings, ShadowSetting shadowSetting, 
        RenderData.PostFXData postFXSettings, RenderData.DeferredRenderingSettings deferredRenderingSettings)
    {
        //选择批处理配置
        _batchingSettings = batchingSettings;
        GraphicsSettings.useScriptableRenderPipelineBatching = batchingSettings.useSRPBatching;
        
        this.shadowSetting = shadowSetting;
        this.postFXSettings = postFXSettings;
        _deferredRenderingSettings = deferredRenderingSettings;
        
        //线性空间光照强度
        GraphicsSettings.lightsUseLinearIntensity = true;
    }
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        throw new System.NotImplementedException();
    }

    //每一帧依次调用每一个Camera的Render函数
    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        //依次渲染每一个camera
        for (int i = 0; i < cameras.Count; i++)
        {
            _renderer.Render(context, cameras[i], _batchingSettings, shadowSetting, postFXSettings, _deferredRenderingSettings);
        }
    }
}