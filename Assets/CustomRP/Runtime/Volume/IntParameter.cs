using System;
using UnityEngine;

namespace CustomRP.Runtime.Volume
{

    [Serializable]
    public class IntParameter : VolumeParameter<int>
    {
        public IntParameter(int value, bool overrideState = false) : base(value, overrideState) { }

        public override void Interp(VolumeParameter from, VolumeParameter to, float t)
        {
        }
    }
}