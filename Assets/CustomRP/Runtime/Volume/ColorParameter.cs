using UnityEngine;

namespace CustomRP.Runtime.Volume
{
    using System;
    using UnityEngine;

    [Serializable]
    public class ColorParameter : VolumeParameter<Color>
    {
        public ColorParameter(Color value, bool overrideState = false) : base(value, overrideState) { }

        public override void Interp(VolumeParameter from, VolumeParameter to, float t)
        {
            var cFrom = from as ColorParameter;
            var cTo = to as ColorParameter;
            value = Color.Lerp(cFrom.value, cTo.value, t);
        }
    }
}