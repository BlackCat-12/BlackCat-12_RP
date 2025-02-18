using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{

    [SerializeField] private BatchingSettings batchingSettings;
    [SerializeField] private DeferredRenderingSettings deferredRenderingSettings;
    [SerializeField] private ShadowSetting shadowSetting;
    [SerializeField] private PostFXUI postFXSettings;

    protected override RenderPipeline CreatePipeline()
    {
        // 在创建 RP 实例时，传入分组后的结构体
        return new CustomRenderPipeline(batchingSettings, deferredRenderingSettings, shadowSetting, postFXSettings);
    }
}
