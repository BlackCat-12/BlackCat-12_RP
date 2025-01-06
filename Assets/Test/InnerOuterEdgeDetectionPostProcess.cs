using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class InnerOuterEdgeDetectionPostProcess : MonoBehaviour
{
    public Material edgeDetectMaterial;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if(edgeDetectMaterial != null)
        {
            Graphics.Blit(src, dest, edgeDetectMaterial);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}