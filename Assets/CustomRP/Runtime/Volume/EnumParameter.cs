using System;
using UnityEngine;

namespace CustomRP.Runtime.Volume
{
    [Serializable]
    public class EnumParameter<T> : VolumeParameter<T> where T : struct, Enum
    {
        public EnumParameter(T value, bool overrideState = false) 
            : base(value, overrideState) { }

        public override void Interp(VolumeParameter from, VolumeParameter to, float t)
        {
            var fromVal = (EnumParameter<T>)from;
            var toVal = (EnumParameter<T>)to;
            
            // 基础插值策略：超过0.5时使用目标值
            value = t >= 0.5f ? toVal.value : fromVal.value;
        }
    }
}