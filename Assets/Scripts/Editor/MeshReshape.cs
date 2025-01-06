using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;

public class MeshReshape : ScriptableWizard
{

    public Mesh testMesh;
    [Range(0,90)]
    public float thresholdAngle = 5.0f;
    
    [MenuItem("Tool/MeshReshape")]
    public static void MenuEntryCall()
    {
        DisplayWizard<MeshReshape>("MeshReshape");
    }

    private void OnWizardCreate()
    {
        if (testMesh == null)
        {
            EditorUtility.DisplayDialog("错误", "请分配一个 Mesh。", "确定");
        }
        
        string originalPath = AssetDatabase.GetAssetPath(testMesh);
        if (string.IsNullOrEmpty(originalPath))
        {
            EditorUtility.DisplayDialog("错误", "无法获取原始 Mesh 的路径。", "确定");
            return;
        }

        Vector3[] vertices = testMesh.vertices;
        int[] indices = testMesh.triangles;

        int[] newIndices = MeshUtils.WeldVertices(vertices, indices, thresholdAngle);

        Mesh newMesh = new Mesh();
        newMesh.name = "Reshape_" + testMesh.name;
        newMesh.vertices = vertices;
        newMesh.triangles = newIndices;
        newMesh.normals = testMesh.normals;
        newMesh.tangents = testMesh.tangents;
        newMesh.uv = testMesh.uv;
        
        
        string directory = System.IO.Path.GetDirectoryName(originalPath);
        string newMeshPath = System.IO.Path.Combine(directory, newMesh.name + ".asset"); // 保存在同目录下
        
        AssetDatabase.CreateAsset(newMesh, newMeshPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("成功", $"新的 Mesh 已保存到 {newMeshPath}", "确定");
    }
}
