using UnityEngine;

public struct RenderData
{
    [System.Serializable]
    public struct BatchingSettings
    {
        public bool useDynamicBatching;

        [Tooltip("Enable or disable GPU instancing.")]
        public bool useGPUInstancing;

        [Tooltip("Enable or disable SRP batching.")]
        public bool useSRPBatching;
    }

    [System.Serializable]
    public struct DeferredRenderingSettings
    {
        [Tooltip("Enable or disable deferred rendering.")]
        public bool useDeferredRender;

        [Tooltip("Specular IBL cubemap.")] public Cubemap specularIBL;

        [Tooltip("Diffuse IBL cubemap.")] public Cubemap diffuseIBL;

        [Tooltip("BRDF lookup texture.")] public Texture BRDFLUT;
    }
    
    [System.Serializable]
    public struct PostFXData
    {
        public bool usePostFx;
        public bool useHDR;
        public PostFXSettings PostFXSettings;
        public bool needDepthNormal;
    }
}
