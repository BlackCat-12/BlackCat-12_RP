using UnityEngine;

namespace CustomRP.Runtime.Volume
{
    using System;
    using UnityEngine;

    [Serializable]
    public class FloatParameter : VolumeParameter<float>
    {
        public FloatParameter(float value, bool overrideState = false) : base(value, overrideState) { }

        public override void Interp(VolumeParameter from, VolumeParameter to, float t)
        {
            var fFrom = from as FloatParameter;
            var fTo = to as FloatParameter;
            value = Mathf.Lerp(fFrom.value, fTo.value, t);
        }
    }
}