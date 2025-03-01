using System;

namespace CustomRP.Runtime.Volume
{
    [Serializable]
    public class StructParameter<T> : VolumeParameter<T> where T : struct
    {
        public StructParameter(T value, bool overrideState = false) 
            : base(value, overrideState) { }

        public override void Interp(VolumeParameter from, VolumeParameter to, float t)
        {
            // 实现结构体的插值逻辑
        }
    }
}