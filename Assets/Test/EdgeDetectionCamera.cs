using UnityEngine;

[RequireComponent(typeof(Camera))]
public class EdgeDetectionCamera : MonoBehaviour
{
    private Camera _camera;

    void Start()
    {
        _camera = GetComponent<Camera>();
        // 启用深度和法线纹理
        _camera.depthTextureMode = DepthTextureMode.DepthNormals;
    }
}