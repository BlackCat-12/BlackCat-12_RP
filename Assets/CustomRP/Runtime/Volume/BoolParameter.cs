namespace CustomRP.Runtime.Volume
{
    using System;
    using UnityEngine;

    [Serializable]
    public class BoolParameter : VolumeParameter<bool>
    {
        public BoolParameter(bool value, bool overrideState = false) : base(value, overrideState) { }
    }
}