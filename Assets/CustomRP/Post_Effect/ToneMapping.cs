using System;
using CustomRP.Runtime.Volume;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;
using BoolParameter = CustomRP.Runtime.Volume.BoolParameter;
using VolumeComponent = CustomRP.Runtime.Volume.VolumeComponent;

namespace CustomRP.Post_Effect
{
    [Serializable]
    [CustomRP.Runtime.Volume.VolumeComponentMenu("Post-processing/ToneMapping")]
    public class ToneMapping : VolumeComponent,IPostProcessComponent
    {
        public enum Mode { None = -1, ACES, Neutral, Reinhard }

        public EnumParameter<Mode> mode = new EnumParameter<Mode>(Mode.None); 
        
        
        public bool IsActive()
        {
            return enabled.value;
        }

        public void Prepare(bool useHDR)
        {
           
        }

        public void Render(CommandBuffer cmd, Camera camera, int fxSourceID, Material material)
        {
            RenderTextureFormat format = _useHDR? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
            PostFX_Pass pass = (int)mode.value < 0 ? PostFX_Pass.CopyWithLinear : PostFX_Pass.ToneMappingACES + (int)mode.value;
            cmd.GetTemporaryRT(fxSource2ID, camera.pixelWidth, camera.pixelHeight, 
                24, FilterMode.Point, format);
            
            Draw(fxSourceID, fxSource2ID, PostFX_Pass.CopyWithLinear, cmd, material);
            Draw(fxSource2ID, fxSourceID, pass, cmd, material);
            cmd.ReleaseTemporaryRT(fxSource2ID);
            
        }
    }
}