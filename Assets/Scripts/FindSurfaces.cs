using System.Collections.Generic;
using UnityEngine;

public class FindSurfaces
{
    public  int surfaceID = 1;
    public  Color[] GetSurfaceIdColors(Mesh mesh)
    {
        int numVertices = mesh.vertexCount;
        Dictionary<int, int> vertexIdToSurfaceId = GenerateSurfaceIds(mesh);

        // 查找最大表面ID
        int maxSurfaceIdInt = 0;
        foreach (var kvp in vertexIdToSurfaceId)
        {
            if (kvp.Value > maxSurfaceIdInt)
                maxSurfaceIdInt = kvp.Value;
        }
        float maxSurfaceId = (float)(maxSurfaceIdInt + 1); // 防止除以零
        
        Color[] colors = new Color[numVertices];
        for (int i = 0; i < numVertices; i++)
        {
            int vertexId = i;
            int surfaceIdValue = 0;
            if (vertexIdToSurfaceId.ContainsKey(vertexId))
                surfaceIdValue = vertexIdToSurfaceId[vertexId];
            else
                surfaceIdValue = maxSurfaceIdInt; // 为未包含的顶点分配唯一ID

            // 归一化后计算表面颜色值
            float normalizedSurfaceId = surfaceIdValue / maxSurfaceId;
            
            int surfaceId = Mathf.RoundToInt(normalizedSurfaceId * 100.0f);
            
            float hue = (surfaceId * 137.5f) % 360f;
            // 动态调整饱和度和亮度
            float saturation = 0.5f + 0.5f * normalizedSurfaceId; // 饱和度随ID增加而增加
            float value = 0.8f + 0.2f * normalizedSurfaceId; // 亮度稍微增加
            
            Color color = Color.HSVToRGB(hue / 360f, saturation, value);
            colors[i] = color;
            
        }
        return colors;
    }

    /*
     * 返回一个 vertexIdToSurfaceId 映射
     * 给定一个顶点，返回其表面ID
     */
    private  Dictionary<int, int> GenerateSurfaceIds(Mesh mesh)
    {
        int numVertices = mesh.vertexCount;
        int[] indexBuffer = mesh.triangles;
        int numIndices = indexBuffer.Length;

        // 顶点邻居邻接表
        Dictionary<int, List<int>> vertexMap = new Dictionary<int, List<int>>();

        // 同一三角形内，各顶点相邻
        for (int i = 0; i < numIndices; i += 3)
        {
            int i1 = indexBuffer[i + 0];
            int i2 = indexBuffer[i + 1];
            int i3 = indexBuffer[i + 2];

            AddEdge(i1, i2, vertexMap);
            AddEdge(i1, i3, vertexMap);
            AddEdge(i2, i3, vertexMap);
        }

        // 添加边到邻接表
        void AddEdge(int a, int b, Dictionary<int, List<int>> map)
        {
            if (!map.ContainsKey(a)) map[a] = new List<int>();
            if (!map.ContainsKey(b)) map[b] = new List<int>();

            if (!map[a].Contains(b)) map[a].Add(b);
            if (!map[b].Contains(a)) map[b].Add(a);
        }

        // 查找连通区域（表面）
        List<int> frontierNodes = new List<int>();
        for (int i = 0; i < numVertices; i++)
        {
            frontierNodes.Add(i);
        }

        HashSet<int> exploredNodes = new HashSet<int>();
        Dictionary<int, int> vertexIdToSurfaceId = new Dictionary<int, int>();

        while (frontierNodes.Count > 0)
        {
            // 依次取出顶点并从顶点集删除
            int node = frontierNodes[frontierNodes.Count - 1];
            frontierNodes.RemoveAt(frontierNodes.Count - 1);

            if (exploredNodes.Contains(node)) continue;

            // 非递归获取所有邻居
            List<int> surfaceVertices = GetNeighborsNonRecursive(node, vertexMap);
            // 将它们标记为已探索
            foreach (int v in surfaceVertices)
            {
                exploredNodes.Add(v);
                vertexIdToSurfaceId[v] = surfaceID;
            }

            surfaceID += 1;
        }

        return vertexIdToSurfaceId;
    }

    // 非递归获取邻居节点
    private  List<int> GetNeighborsNonRecursive(int node, Dictionary<int, List<int>> vertexMap)
    {
        Stack<int> frontier = new Stack<int>();
        HashSet<int> explored = new HashSet<int>();
        List<int> result = new List<int>();

        frontier.Push(node);

        // 获取邻居，以及邻居的邻居节点
        while (frontier.Count > 0)
        {
            int currentNode = frontier.Pop();
            if (explored.Contains(currentNode)) continue;
            explored.Add(currentNode);
            result.Add(currentNode);

            if (vertexMap.ContainsKey(currentNode))
            {
                foreach (int neighbor in vertexMap[currentNode])
                {
                    if (!explored.Contains(neighbor))
                    {
                        frontier.Push(neighbor);
                    }
                }
            }
        }
        return result;
    }
}
