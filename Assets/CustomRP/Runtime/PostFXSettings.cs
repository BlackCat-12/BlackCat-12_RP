using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Windows.WebCam;

[CreateAssetMenu (menuName = "Rendering/Custom Post FX Settings")]
public class PostFXSettings : ScriptableObject
{
    [SerializeField] private Shader _shader = default;
    [System.NonSerialized] private Material _material;

    public bool needDepthNormalTex = false;
    //Bloom
    [System.Serializable]
    public struct BloomSettings
    {
        [Range(0f, 16f)] public int maxIteration;
        [Min(1f)] public int downScaleLimit;
        public bool bicubicUpsampling;
        [Min(0f)]
        public float threshold;
        [Range(0f, 1f)]
        public float thresholdKnee;
        [Min(0f)]
        public float intensity;
    }
    
    [Serializable]
    public struct PixelSetting
    {
        [Range(1f, 5f)] public int maxIteration;
        public float outline;
        [Range(0f, 30f)] public float depthSensitivity;
        [Range(0f, 10f)] public float normalSensitivity;
        [Range(0f, 5f)] public float depthThreshold;
        [Range(0f, 10f)] public float normalThreshold;
        [Range(0f, 8f)] public float normalBias;
        [Range(0f, 8f)] public float depthBias;
        
    }

    [SerializeField]
    private BloomSettings bloom = default;

    [SerializeField] 
    private PixelSetting pixel = new PixelSetting()
    {
        maxIteration = 1,
    };
    //只读属性
    public BloomSettings Bloom => bloom;
    public PixelSetting Pixel => pixel;
    
    public Material Material
    {
        get
        {
            //创建不可见材质
            if (_material == null && _shader != null)
            {
                _material = new Material(_shader);
                _material.hideFlags = HideFlags.HideAndDontSave;
            }
            return _material;
        }
    }
}