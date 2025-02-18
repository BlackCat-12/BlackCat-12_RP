using UnityEngine;

[System.Serializable]
public struct DeferredRenderingSettings
{
    [Tooltip("Enable or disable deferred rendering.")]
    public bool useDeferredRender;

    [Tooltip("Specular IBL cubemap.")]
    public Cubemap specularIBL;

    [Tooltip("Diffuse IBL cubemap.")]
    public Cubemap diffuseIBL;

    [Tooltip("BRDF lookup texture.")]
    public Texture BRDFLUT;

    // 提供一个默认的构造函数，设置默认值
    public static DeferredRenderingSettings Default => new DeferredRenderingSettings
    {
        useDeferredRender = false,
        specularIBL = null,
        diffuseIBL = null,
        BRDFLUT = null
    };
}