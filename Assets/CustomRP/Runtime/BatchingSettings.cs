using UnityEngine;

[System.Serializable]
public struct BatchingSettings
{
    [Tooltip("Enable or disable dynamic batching.")]
    public bool useDynamicBatching;

    [Tooltip("Enable or disable GPU instancing.")]
    public bool useGPUInstancing;

    [Tooltip("Enable or disable SRP batching.")]
    public bool useSRPBatching;

    // 提供一个默认的构造函数，设置默认值
    public static BatchingSettings Default => new BatchingSettings
    {
        useDynamicBatching = false,
        useGPUInstancing = false,
        useSRPBatching = true
    };
}