namespace CustomRP.Runtime.Volume
{
    using System;
    using UnityEngine;


    [Serializable]
    public abstract class VolumeParameter
    {
        [SerializeField]
        public bool overrideState = false;
        public abstract void Interp(VolumeParameter from, VolumeParameter to, float t);
    }

    [Serializable]
    public class VolumeParameter<T> : VolumeParameter
    {
        [SerializeField]
        protected T m_Value;

        public T value
        {
            get => m_Value;
            set => m_Value = value;
        }

        public VolumeParameter(T value, bool overrideState = false)
        {
            m_Value = value;
            this.overrideState = overrideState;
        }

        public override void Interp(VolumeParameter from, VolumeParameter to, float t)
        {
            // 实现参数插值逻辑
        }
    }


}