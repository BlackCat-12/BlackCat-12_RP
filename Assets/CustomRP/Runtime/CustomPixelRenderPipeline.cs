using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    //创建单个camera的渲染实例
    private CameraRenderer _renderer = new CameraRenderer();
    private bool _useDynamicBatching, _useGPUInstancing;
    private DeferredRenderingSettings _deferredRenderingSettings;

    private ShadowSetting _shadowSetting;
    private PostFXUI _postFXSettings;
    


    //构造函数
    public CustomRenderPipeline(BatchingSettings batchingSettings,
        DeferredRenderingSettings deferredRenderingSettings,
        ShadowSetting shadowSetting,
        PostFXUI postFXSettings)
    {
        //选择批处理配置
        GraphicsSettings.useScriptableRenderPipelineBatching = batchingSettings.useSRPBatching;
        
        _useDynamicBatching = batchingSettings.useDynamicBatching;
        _useGPUInstancing = batchingSettings.useGPUInstancing;

        _deferredRenderingSettings = deferredRenderingSettings;
        _shadowSetting = shadowSetting;
        _postFXSettings = postFXSettings;
        
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
            _renderer.Render(context, cameras[i], _useDynamicBatching, _useGPUInstancing, _shadowSetting, 
                _postFXSettings, _deferredRenderingSettings);
        }
    }
}