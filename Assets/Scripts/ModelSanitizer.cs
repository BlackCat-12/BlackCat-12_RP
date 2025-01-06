using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class ModelSanitizer : MonoBehaviour
{
    public float thresholdAngle = 1f; // 角度阈值，可以根据需要调整
    private FindSurfaces _findSurfaces ;

    private void Awake()
    {
        _findSurfaces = new FindSurfaces();
    }

    void Start()
    {
        // 获取当前对象的MeshFilter组件
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        Mesh mesh = meshFilter.sharedMesh;
        int maxSurfaceID = 1;

        // 获取顶点和索引数据
        Vector3[] vertices = mesh.vertices;
        Vector2[] uv = mesh.uv;
        int[] indices = mesh.triangles;
        Vector4[] tangents = mesh.tangents;
        Color[] colors;
        
        // 调用GetSurfaceIdColors函数
        colors = _findSurfaces.GetSurfaceIdColors(mesh);

        // 创建新的网格并应用优化后的索引
        Mesh newMesh = new Mesh();

        // 设置顶点数据
        newMesh.vertices = vertices;
        newMesh.tangents = tangents;
        newMesh.uv = uv;
        newMesh.colors = colors;
        newMesh.triangles = indices;

        if (vertices == null)
        {
            Debug.Log(" 未读取到顶点数据，可能未开启模型读写权限");
        }

        // 更新网格的法线和边界（通常是必要的）
        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();
        newMesh.name = "new" + mesh.name;

        // 将新的网格赋给MeshFilter
        meshFilter.mesh = newMesh;
    }
}