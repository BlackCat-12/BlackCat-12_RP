using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    RenderData.BatchingSettings _batchingSettings;
    
    [SerializeField] private ShadowSetting shadowSetting ;
    [SerializeField] private RenderData.PostFXData _postFXData;
    [SerializeField] private RenderData.DeferredRenderingSettings _deferredRenderingSettings;
    
    protected override RenderPipeline CreatePipeline()
    {
        //在创建RP实例时，传入是否批处理设置，在修改了设置后，会创建新的实例
        return new CustomRenderPipeline(_batchingSettings, shadowSetting, _postFXData, _deferredRenderingSettings);
    }
}