namespace CustomRP.Runtime.Volume
{
    using System;
    
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class VolumeComponentMenuAttribute : Attribute
    {
        public readonly string menu;

        public VolumeComponentMenuAttribute(string menu)
        {
            this.menu = menu;
        }
    }
}