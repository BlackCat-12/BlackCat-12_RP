using System.Collections.Generic;
using UnityEngine;
public static class MeshUtils
{
    public static int[] WeldVertices(Vector3[] vertices, int[] indices, float thresholdAngle = 5.0f)
    {
        List<int> newIndices = new List<int>();
        Dictionary<int, List<int>> mergedMap = new Dictionary<int, List<int>>();
        Dictionary<int, int> vertexAliases = new Dictionary<int, int>();

        // Helper functions
        void Merge(int i1, int i2)
        {
            if (!mergedMap.ContainsKey(i1))
                mergedMap[i1] = new List<int>();
            mergedMap[i1].Add(i2);
        }

        void AliasDeletedVertex(int deletedVertex, int remainingVertex)
        {
            if (deletedVertex == remainingVertex) return;
            vertexAliases[deletedVertex] = remainingVertex;
        }

        // 计算并存储要合并的两条边
        List<int[][]> edgesToMerge = ComputeEdgesToMerge(vertices, indices, thresholdAngle);

        // Convert the array of edges to merge to a map
        Dictionary<int[], int[][]> edgesToMergeMap = new Dictionary<int[], int[][]>(new EdgeComparer());
        foreach (var edgesList in edgesToMerge)
        {
            foreach (var edge in edgesList)
            {
                edgesToMergeMap[edge] = edgesList;
            }
        }

        // Go through all triangles
        for (int i = 0; i < indices.Length; i += 3)
        {
            // Look at all 3 edges
            int i1 = indices[i + 0];
            int i2 = indices[i + 1];
            int i3 = indices[i + 2];
            var edges = new List<int[]>
            {
                new int[] { i1, i2 },
                new int[] { i1, i3 },
                new int[] { i2, i3 }
            };

            foreach (var edge in edges)
            {
                int index0 = edge[0];
                int index1 = edge[1];
                int[] reverseEdge = new int[] { index1, index0 };
                bool isReverse = false;

                int[][] edgeToMerge = null;
                if (edgesToMergeMap.ContainsKey(edge))
                {
                    edgeToMerge = edgesToMergeMap[edge];
                }
                if (edgesToMergeMap.ContainsKey(reverseEdge))
                {
                    edgeToMerge = edgesToMergeMap[reverseEdge];
                    isReverse = true;
                }

                if (edgeToMerge != null)
                {
                    // Once you found an edge to merge,
                    // you need to find its sibling edge, then merge the vertices in the right orientation
                    int[] possibleEdge1 = edgeToMerge[0];
                    int[] possibleEdge2 = edgeToMerge[1];
                    int[] otherEdge = possibleEdge1;
                    int[] originalEdge = possibleEdge2;
                    // Just pick the one that is NOT the current edgeToMerge
                    if ((possibleEdge1[0] == index0 && possibleEdge1[1] == index1) ||
                        (possibleEdge1[0] == index1 && possibleEdge1[1] == index0))
                    {
                        otherEdge = possibleEdge2;
                        originalEdge = possibleEdge1;
                    }

                    int index2 = otherEdge[0];
                    int index3 = otherEdge[1];
                    index0 = originalEdge[0];
                    index1 = originalEdge[1];

                    if (index0 == index2 && index1 == index3)
                    {
                        // Not sure why this happens, but sometimes
                        // you get these degenerate self edges
                        continue;
                    }

                    // Merge index0 and index1, with index2 & 3
                    // Figure out which orientation to merge in
                    // if you have:
                    //  1 ---- 2
                    //  3 ----- 4
                    // You want to merge 1,3, and 2,4
                    // NOT the other way around
                    Vector3 v0 = GetVertexFromIndexBuffer(index0, vertices);
                    Vector3 v2 = GetVertexFromIndexBuffer(index2, vertices);
                    if (Vector3.Distance(v0, v2) > 0.1f)
                    {
                        int tmp = index3;
                        index3 = index2;
                        index2 = tmp;
                    }

                    // Replace deleted indices
                    if (vertexAliases.ContainsKey(index0)) index0 = vertexAliases[index0];
                    if (vertexAliases.ContainsKey(index1)) index1 = vertexAliases[index1];
                    if (vertexAliases.ContainsKey(index2)) index2 = vertexAliases[index2];
                    if (vertexAliases.ContainsKey(index3)) index3 = vertexAliases[index3];

                    Merge(index0, index2);
                    Merge(index1, index3);
                    // 0 was merged with 2, so we consider 2 the deleted vertex
                    AliasDeletedVertex(index2, index0);
                    AliasDeletedVertex(index3, index1);

                    // Remove the edges we've merged from the map
                    int[] mergedEdge = new int[] { index2, index3 };
                    edgesToMergeMap.Remove(edgeToMerge[0]);
                    edgesToMergeMap.Remove(mergedEdge);
                }
            }
        }

        var finalMergeMap = FillOutMergeMap(mergedMap);

        // Go through the original index buffer, replace indices with the merged indices
        List<int> newIndexBuffer = new List<int>();
        for (int i = 0; i < indices.Length; i++)
        {
            int index = indices[i];
            int newIndex = index;
            if (finalMergeMap.ContainsKey(index))
            {
                newIndex = finalMergeMap[index];
            }
            newIndexBuffer.Add(newIndex);
        }

        return newIndexBuffer.ToArray();
    }

    static Vector3 GetVertexFromIndexBuffer(int index, Vector3[] vertices)
    {
        return vertices[index];
    }

    static Dictionary<int, int> FillOutMergeMap(Dictionary<int, List<int>> mergeMap)
    {
        /*
            如果你的映射如下：

            0: [1, 2, 3]

            这将为 1、2、3 创建条目，使它们都被替换为 0

            所以结果如下：

            0: [1, 2, 3],
            1: 0,
            2: 0,
            3: 0,
        */
        Dictionary<int, int> newMergeMap = new Dictionary<int, int>();
        foreach (var key in mergeMap.Keys)
        {
            var indices = mergeMap[key];
            foreach (var ind in indices)
            {
                newMergeMap[ind] = key;
            }
        }
        return newMergeMap;
    }

    static List<int[][]> ComputeEdgesToMerge(Vector3[] vertices, int[] indices, float thresholdAngle = 1f)
    {
        float DEG2RAD = Mathf.PI / 180f;
        float thresholdDot = Mathf.Cos(DEG2RAD * thresholdAngle);

        int indexCount = indices.Length;

        int[] indexArr = new int[3];
        string[] hashes = new string[3];

        Dictionary<string, EdgeInfo> edgeData = new Dictionary<string, EdgeInfo>();
        List<int[][]> edgesToMerge = new List<int[][]>();

        for (int i = 0; i < indexCount; i += 3)
        {
            indexArr[0] = indices[i];
            indexArr[1] = indices[i + 1];
            indexArr[2] = indices[i + 2];

            Vector3 a = GetVertexFromIndexBuffer(indexArr[0], vertices);
            Vector3 b = GetVertexFromIndexBuffer(indexArr[1], vertices);
            Vector3 c = GetVertexFromIndexBuffer(indexArr[2], vertices);

            Vector3 normal = GetNormal(a, b, c);

            Vector3[] triangle = { a, b, c };

            // 为边创建哈希值
            hashes[0] = HashVertex(a);
            hashes[1] = HashVertex(b);
            hashes[2] = HashVertex(c);

            // 跳过退化三角形
            if (hashes[0] == hashes[1] || hashes[1] == hashes[2] || hashes[2] == hashes[0])
            {
                continue;
            }

            // 遍历每条边
            for (int j = 0; j < 3; j++)
            {
                int jNext = (j + 1) % 3;
                string vecHash0 = hashes[j];
                string vecHash1 = hashes[jNext];
                Vector3 v0 = triangle[j];
                Vector3 v1 = triangle[jNext];

                string hash = vecHash0 + "_" + vecHash1;
                string reverseHash = vecHash1 + "_" + vecHash0;

                // 以边顶点绘制的相反顺序检索相邻边
                if (edgeData.ContainsKey(reverseHash) && edgeData[reverseHash] != null)
                {
                    // 如果找到兄弟边，检查是否满足角度阈值，然后添加到待合并的边列表
                    if (Vector3.Dot(normal, edgeData[reverseHash].normal) > thresholdDot)
                    {
                        // 合并这些边
                        int[] edge1 = { edgeData[reverseHash].index0, edgeData[reverseHash].index1 };
                        int[] edge2 = { indexArr[j], indexArr[jNext] };

                        edgesToMerge.Add(new int[][] { edge1, edge2 });
                    }
                    edgeData[reverseHash] = null;
                }
                else if (!edgeData.ContainsKey(hash))
                {
                    // 如果边不存在，则添加到 edgeData
                    edgeData[hash] = new EdgeInfo()
                    {
                        index0 = indexArr[j],
                        index1 = indexArr[jNext],
                        normal = normal
                    };
                }
            }
        }

        return edgesToMerge;
    }

    static Vector3 GetNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 resultNormal = Vector3.Cross(c - b, a - b);
        if (resultNormal.sqrMagnitude > 0)
        {
            return resultNormal.normalized;
        }
        return Vector3.zero;
    }

    static int precisionPoints = 4;
    static float precision = Mathf.Pow(10f, precisionPoints);

    static string HashVertex(Vector3 v)
    {
        return $"{Mathf.Round(v.x * precision)},{Mathf.Round(v.y * precision)},{Mathf.Round(v.z * precision)}";
    }

    class EdgeInfo
    {
        public int index0;
        public int index1;
        public Vector3 normal;
    }

    class EdgeComparer : IEqualityComparer<int[]>
    {
        public bool Equals(int[] x, int[] y)
        {
            if (x.Length != y.Length)
                return false;
            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i])
                    return false;
            }
            return true;
        }

        public int GetHashCode(int[] obj)
        {
            unchecked // 允许溢出
            {
                int hash = 17;
                foreach (int i in obj)
                {
                    hash = hash * 31 + i;
                }
                return hash;
            }
        }
    }
}
