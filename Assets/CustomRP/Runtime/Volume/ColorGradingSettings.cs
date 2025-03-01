using System;
using UnityEngine;

namespace CustomRP.Runtime.Volume
{
    [Serializable]
    public struct ColorAdjustmentsSettings 
    {
        public float postExposure; // 后曝光
        [Range(-100f, 100f)] public float contrast; // 对比度
        [ColorUsage(false, true)] public Color colorFilter; // 颜色滤镜
        [Range(-180f, 180f)] public float hueShift; // 色调偏移
        [Range(-100f, 100f)] public float saturation; // 饱和度
        
    }
    
    [Serializable]
    public struct WhiteBalanceSettings {

        [Range(-100f, 100f)]
        public float temperature, tint;
    }
    
    [Serializable]
    public struct SplitToningSettings {

        [ColorUsage(false)]
        public Color shadows, highlights;

        [Range(-100f, 100f)]
        public float balance;
    }
    
    [Serializable]
    public struct ChannelMixerSettings {

        public Vector3 red, green, blue;
        
    }
    
    [Serializable]
    public struct ShadowsMidtonesHighlightsSettings {

        [ColorUsage(false, true)]
        public Color shadows, midtones, highlights;

        [Range(0f, 2f)]
        public float shadowsStart, shadowsEnd, highlightsStart, highLightsEnd;
        
    }
}