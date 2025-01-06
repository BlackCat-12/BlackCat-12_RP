using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline : RenderPipeline
{
    //创建单个camera的渲染实例
    private CameraRenderer _renderer = new CameraRenderer();
    private bool useDynamicBatching, useGPUInstancing;

    private ShadowSetting shadowSetting;
    private PostFXSettings postFXSettings;
    


    //构造函数
    public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatching, ShadowSetting shadowSetting, PostFXSettings postFXSettings)
    {
        //选择批处理配置
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatching;
        
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;

        this.shadowSetting = shadowSetting;
        this.postFXSettings = postFXSettings;
        
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
            _renderer.Render(context, cameras[i], useDynamicBatching, useGPUInstancing, shadowSetting, postFXSettings);
        }
    }
}