using UnityEngine;

[ExecuteInEditMode]
public class ShowVertexNormals : MonoBehaviour
{
    void OnDrawGizmos()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            Mesh mesh = mf.sharedMesh;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;

            Transform transform = this.transform;
            Gizmos.color = Color.red;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldVertex = transform.TransformPoint(vertices[i]);
                Vector3 worldNormal = transform.TransformDirection(normals[i]);

                Gizmos.DrawRay(worldVertex, worldNormal * 0.1f);
            }
        }
    }
}