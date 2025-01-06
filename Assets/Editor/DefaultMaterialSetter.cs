using UnityEngine;
using UnityEditor;

public class DefaultMaterialSetter : AssetPostprocessor
{
    // 指定你想要作为默认材质的路径
    private static string defaultMaterialPath = "Assets/Materials/Lit.mat";

    void OnPostprocessModel(GameObject g)
    {
        // 获取材质
        Material defaultMaterial = AssetDatabase.LoadAssetAtPath<Material>(defaultMaterialPath);
        if (defaultMaterial == null)
        {
            Debug.LogError("默认材质未找到，请检查路径是否正确！");
            return;
        }

        // 获取所有子物体的MeshRenderer组件并赋予默认材质
        MeshRenderer[] renderers = g.GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            renderer.sharedMaterial = defaultMaterial;
        }
    }
}