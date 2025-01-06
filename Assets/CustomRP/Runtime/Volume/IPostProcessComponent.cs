using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRP.Runtime.Volume
{
    public interface IPostProcessComponent
    {
        bool IsActive();

        void Prepare();
        void Render(CommandBuffer cmd, Camera camera, int fxSourceID, Material material);
        
        
    }
}