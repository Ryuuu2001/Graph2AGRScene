using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Voronoi;
using TriangleNet.Topology;
using TriangleNet.Voronoi.Legacy;
using System.Drawing;
using Unity.Splines.Examples;

public class Function : MonoBehaviour
{
    // 检测点是否在3维多边形范围内
    public static bool IsPointInPolygon(List<Vector3> polygonPoints, Vector3 point)
    {
        bool isInside = false;

        int j = polygonPoints.Count - 1;
        for (int i = 0; i < polygonPoints.Count; j = i++)
        {
            if (((polygonPoints[i].z > point.z) != (polygonPoints[j].z > point.z)) &&
                (point.x < (polygonPoints[j].x - polygonPoints[i].x) * (point.z - polygonPoints[i].z) / (polygonPoints[j].z - polygonPoints[i].z) + polygonPoints[i].x))
            {
                isInside = !isInside;
            }
        }

        return isInside;
    }

    // 检测点是否在2维多边形范围内
    public static bool IsPointInPolygon2D(List<Vector2> polygonPoints, Vector2 point)
    {
        bool isInside = false;

        int j = polygonPoints.Count - 1;
        for (int i = 0; i < polygonPoints.Count; j = i++)
        {
            if (((polygonPoints[i].y > point.y) != (polygonPoints[j].y > point.y)) &&
                (point.x < (polygonPoints[j].x - polygonPoints[i].x) * (point.y - polygonPoints[i].y) / (polygonPoints[j].y - polygonPoints[i].y) + polygonPoints[i].x))
            {
                isInside = !isInside;
            }
        }

        return isInside;
    }

    // 比较给定坐标与列表中所有坐标距离大于某一值
    public static bool CompareDistance(List<Vector3> list, Vector3 position, double dis)
    {
        // 遍历列表中的每一个位置
        foreach (Vector3 point in list)
        {
            // 计算给定位置与当前位置之间的距离
            float distance = Vector3.Distance(position, point);

            // 如果有一个位置与给定位置的距离小于等于给定距离，则返回false
            if (distance <= dis)
            {
                return false;
            }
        }

        // 如果所有位置与给定位置的距离都大于给定距离，则返回true
        return true;
    }

    // 获取给定坐标与列表中所有坐标距离小于某一值的个数
    public static int ArrangePointCount(List<Vector3> list, Vector3 position, double dis)
    {
        int count = 0;

        // 遍历list中的每个Vector3
        foreach (Vector3 point in list)
        {
            // 计算position与当前点之间的距离
            float distance = Vector3.Distance(point, position);

            // 如果距离小于dis，计数器加1
            if (distance < dis)
            {
                count++;
            }
        }

        return count;
    }

    // 在探头范围内，获取给定坐标与列表中所有坐标距离小于某一值的个数
    public static int AngleArrangePointCount(List<Vector3> list, Vector3 position, Vector3 direction, double dis)
    {
        int count = 0;
        direction.Normalize();

        // 遍历list中的每个Vector3
        foreach (Vector3 point in list)
        {
            // 计算position与当前点之间的距离
            float distance = Vector3.Distance(point, position);

            // 如果距离小于dis，计数器加1
            if (distance < dis)
            {
                Vector3 toPoint = (point - position).normalized;

                float dotProduct = Vector3.Dot(toPoint, direction);

                float cos80 = Mathf.Cos(30 * Mathf.Deg2Rad);

                if (dotProduct >= cos80)
                {
                    count++;
                }
            }
        }

        return count;
    }

    // 获取给定坐标与列表中所有坐标距离最近的坐标
    public static Vector3 NearestPoint(List<Vector3> list, Vector3 position)
    {
        Vector3 nearestPoint = list[0];
        float minDistance = Vector3.Distance(position, nearestPoint);

        // 遍历list中的每个Vector3
        foreach (Vector3 point in list)
        {
            float distance = Vector3.Distance(position, point);

            // 如果找到更近的点，则更新最近点和最小距离
            if (distance < minDistance)
            {
                nearestPoint = point;
                minDistance = distance;
            }
        }
        return nearestPoint;
    }

    // 获取给定坐标与列表中所有坐标距离最近的坐标(2D)
    public static Vector2 NearestPoint2D(List<Vector2> list, Vector2 position)
    {
        Vector2 nearestPoint = list[0];
        float minDistance = Vector2.Distance(position, nearestPoint);

        // 遍历list中的每个Vector2
        foreach (Vector2 point in list)
        {
            float distance = Vector2.Distance(position, point);

            // 如果找到更近的点，则更新最近点和最小距离
            if (distance < minDistance)
            {
                nearestPoint = point;
                minDistance = distance;
            }
        }
        return nearestPoint;
    }

    // 将Terrain上的3D坐标转换成UI.Image上的2D坐标(一组)
    public static void Transform2D(Terrain terrain, Image image, List<Vector3> boundary)
    {
        TerrainData terrainData = terrain.terrainData;

        float terrainWidth = terrainData.size.x;
        float terrainHeight = terrainData.size.z;

        float imageWidth = image.rectTransform.rect.width;
        float imageHeight = image.rectTransform.rect.height;

        foreach (Vector3 point in boundary)
        {
            float localX = point.x - terrain.transform.position.x;
            float localZ = point.z - terrain.transform.position.z;

            float RatioX = localX / terrainWidth;
            float RatioZ = localZ / terrainHeight;

            float imageX = imageWidth * RatioX;
            float imageZ = imageHeight * RatioZ;

            Global.boundary2D.Add(new Vector2 (imageX, imageZ));
        }
    }

    // 将Terrain上的3D坐标转换成UI.Image上的2D坐标(一个)
    public static Vector2 Transform2D_Single(Terrain terrain, Image image, Vector3 node)
    {
        TerrainData terrainData = terrain.terrainData;

        float terrainWidth = terrainData.size.x;
        float terrainHeight = terrainData.size.z;

        float imageWidth = image.rectTransform.rect.width;
        float imageHeight = image.rectTransform.rect.height;

        float localX = node.x - terrain.transform.position.x;
        float localZ = node.z - terrain.transform.position.z;

        float RatioX = localX / terrainWidth;
        float RatioZ = localZ / terrainHeight;

        float imageX = imageWidth * RatioX;
        float imageZ = imageHeight * RatioZ;

        return new Vector2(imageX, imageZ);
    }

    // 将UI.Image上的2D坐标转换成Terrain上的3D坐标(一组)
    public static void Transform3D(Image image, Terrain terrain, List<Vector2> nodes)
    {
        TerrainData terrainData = terrain.terrainData;

        float terrainWidth = terrainData.size.x;
        float terrainHeight = terrainData.size.z;

        float imageWidth = image.rectTransform.rect.width;
        float imageHeight = image.rectTransform.rect.height;

        foreach (Vector2 point in nodes)
        {
            float RatioX = point.x / imageWidth;
            float RatioY = point.y / imageHeight;

            float terrainX = terrainWidth * RatioX;
            float terrainZ = terrainHeight * RatioY;
            float terrainY = terrain.SampleHeight(new Vector3 (terrainX, 0, terrainZ));
        }
    }

    // 将UI.Image上的2D坐标转换成Terrain上的3D坐标(一个)
    public static Vector3 Transform3D_Single(Image image, Terrain terrain, Vector2 node)
    {
        TerrainData terrainData = terrain.terrainData;

        float terrainWidth = terrainData.size.x;
        float terrainHeight = terrainData.size.z;

        float imageWidth = image.rectTransform.rect.width;
        float imageHeight = image.rectTransform.rect.height;

        float RatioX = node.x / imageWidth;
        float RatioY = node.y / imageHeight;

        float terrainX = terrainWidth * RatioX;
        float terrainZ = terrainHeight * RatioY;
        float terrainY = terrain.SampleHeight(new Vector3(terrainX, 0, terrainZ));

        return new Vector3(terrainX, terrainY, terrainZ);
    }

    public static void GenerateTriangle(List<Vector2> nodes)
    {
        // 创建一个新的几何对象
        Polygon geometry = new Polygon();

        // 添加节点到几何对象
        foreach (Vector2 node in nodes)
        {
            geometry.Add(new Vertex(node.x, node.y));
        }

        // 生成三角剖分
        Global.mesh = new GenericMesher().Triangulate(geometry);

        // 获取剖分结果
        foreach (Edge edge in Global.mesh.Edges)
        {
            Vector2 Node_1 = Global.nodes[edge.P0];
            Vector2 Node_2 = Global.nodes[edge.P1];

            Global.edges.Add(new Global.EdgeData(Node_1, Node_2));
            Global.edges.Add(new Global.EdgeData(Node_2, Node_1));
        }
    }

    public static void GenerateVoronoi(IMesh mesh)
    {
        GameObject image = GameObject.Find("Boundary_Image_Garden");
        if (image == null)
        {
            image = GameObject.Find("Boundary_Image_Farm");
        }
        Dictionary<int, List<int>> adjacency = new Dictionary<int, List<int>>();
        Dictionary<int, List<Global.EdgeData>> externalEdges = new Dictionary<int, List<Global.EdgeData>>();
        int[,] array = new int[mesh.Triangles.Count, mesh.Triangles.Count];
        int currentTriangleIndex;
        int same;

        float maxDis = 0;
        Vector2 startPoint = Vector2.zero;
        Vector2 endPoint = Vector2.zero;

        // 获取三角形及其外接圆圆心
        foreach (Triangle triangle in mesh.Triangles)
        {
            // 存储三角形
            Global.triangles.Add(triangle);

            // 存储三角形外接圆圆心
            Vector2 circumcenter = CalculateCircumcenter(triangle);
            Global.circumCenter.Add(circumcenter);

            if (!IsPointInPolygon2D(Global.boundary2D, circumcenter))
            {
                for (int i = 0; i < 3; i++)
                {
                    for(int j = i + 1; j < 3; j++)
                    {
                        Vector2 pointA = new Vector2((float)triangle.GetVertex(i).X, (float)triangle.GetVertex(i).Y);
                        Vector2 pointB = new Vector2((float)triangle.GetVertex(j).X, (float)triangle.GetVertex(j).Y);

                        float dis = Vector2.Distance(pointA, pointB);

                        if (dis > maxDis)
                        {
                            maxDis = dis;
                            startPoint = pointA;
                            endPoint = pointB;
                        }
                    }
                }

                Global.edges.Remove(new Global.EdgeData(startPoint, endPoint));
                Global.edges.Remove(new Global.EdgeData(endPoint, startPoint));

                float positionX = (startPoint.x + endPoint.x) / 2;
                float positionY = (startPoint.y + endPoint.y) / 2;

                Vector2 vector = new Vector2(positionX, positionY);

                foreach (Transform child in image.transform)
                {
                    RectTransform rectTransform = child.GetComponent<RectTransform>();
                    if (child.name == "Edge(Clone)" && rectTransform.anchoredPosition == vector)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            maxDis = 0;
            startPoint = Vector2.zero;
            endPoint = Vector2.zero;
        }

        // 获取三角形的邻接关系存储在字典中
        foreach (Triangle triangle in mesh.Triangles)
        {
            // 获取当前三角形的索引
            currentTriangleIndex = Global.triangles.IndexOf(triangle);

            // 初始化当前三角形的邻接三角形列表
            if (!adjacency.ContainsKey(currentTriangleIndex))
            {
                adjacency[currentTriangleIndex] = new List<int>();
            }

            foreach (Triangle otherTriangle in mesh.Triangles)
            {
                same = 0;

                if (Global.triangles.IndexOf(otherTriangle) != currentTriangleIndex)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (triangle.GetVertex(i).X == otherTriangle.GetVertex(j).X && 
                                triangle.GetVertex(i).Y == otherTriangle.GetVertex(j).Y)
                            {
                                same++;
                                break;
                            }
                        }
                    }

                    if (same == 2 && !adjacency[currentTriangleIndex].Contains(Global.triangles.IndexOf(otherTriangle)))
                    {
                        adjacency[currentTriangleIndex].Add(Global.triangles.IndexOf(otherTriangle));
                    }
                }
            }
        }

        foreach (KeyValuePair<int, List<int>> entry in adjacency)
        {
            int key = entry.Key;
            List<int> neighbors = entry.Value;

            foreach (int value in entry.Value)
            {
                array[key, value] = 1;
            }
        }

        // 调整内部连线的起点终点，保证起点一定在范围内
        for (int i = 0; i < mesh.Triangles.Count; i++)
        {
            for (int j = i + 1; j < mesh.Triangles.Count; j++)
            {
                if (array[i,j] == 1)
                {
                    if (!IsPointInPolygon2D(Global.boundary2D, Global.circumCenter[i]))
                    {
                        Global.edgePairs.Add(new Global.EdgeData(Global.circumCenter[j], Global.circumCenter[i]));
                    }
                    else if (!IsPointInPolygon2D(Global.boundary2D, Global.circumCenter[j]))
                    {
                        Global.edgePairs.Add(new Global.EdgeData(Global.circumCenter[i], Global.circumCenter[j]));
                    }
                    else if (!IsPointInPolygon2D(Global.boundary2D, Global.circumCenter[i]) && !IsPointInPolygon2D(Global.boundary2D, Global.circumCenter[j]))
                    {
                        ;
                    }
                    else if (IsPointInPolygon2D(Global.boundary2D, Global.circumCenter[i]) && IsPointInPolygon2D(Global.boundary2D, Global.circumCenter[j]))
                    {
                        Global.edgePairs.Add(new Global.EdgeData(Global.circumCenter[i], Global.circumCenter[j]));
                    }
                }
            }
        }

        // 找到所有的外部边和对应三角形存储在字典中
        foreach (Triangle triangle in mesh.Triangles)
        {
            // 获取当前三角形的索引
            currentTriangleIndex = Global.triangles.IndexOf(triangle);

            // 初始化当前三角形的外部边列表
            if (!externalEdges.ContainsKey(currentTriangleIndex))
            {
                externalEdges[currentTriangleIndex] = new List<Global.EdgeData>();
            }

            if (adjacency[currentTriangleIndex].Count < 3)
            {
                Vector2 point0 = new Vector2((float)triangle.GetVertex(0).X, (float)triangle.GetVertex(0).Y);
                Vector2 point1 = new Vector2((float)triangle.GetVertex(1).X, (float)triangle.GetVertex(1).Y);
                Vector2 point2 = new Vector2((float)triangle.GetVertex(2).X, (float)triangle.GetVertex(2).Y);

                Global.EdgeData[] edges = new Global.EdgeData[]
                {
                    new Global.EdgeData(point0, point1),
                    new Global.EdgeData(point1, point2),
                    new Global.EdgeData(point2, point0)
                };

                foreach (Global.EdgeData edge in edges)
                {
                    bool isExternal = true;

                    foreach (int neighborIndex in adjacency[currentTriangleIndex])
                    {
                        Vector2 pointA = new Vector2((float)Global.triangles[neighborIndex].GetVertex(0).X, (float)Global.triangles[neighborIndex].GetVertex(0).Y);
                        Vector2 pointB = new Vector2((float)Global.triangles[neighborIndex].GetVertex(1).X, (float)Global.triangles[neighborIndex].GetVertex(1).Y);
                        Vector2 pointC = new Vector2((float)Global.triangles[neighborIndex].GetVertex(2).X, (float)Global.triangles[neighborIndex].GetVertex(2).Y);

                        Global.EdgeData[] neighborEdges = new Global.EdgeData[]
                        {
                            new Global.EdgeData(pointA, pointB),
                            new Global.EdgeData(pointB, pointA),
                            new Global.EdgeData(pointB, pointC),
                            new Global.EdgeData(pointC, pointB),
                            new Global.EdgeData(pointC, pointA),
                            new Global.EdgeData(pointA, pointC)
                        };

                        foreach (Global.EdgeData neighborEdge in neighborEdges)
                        {
                            if (edge.Equals(neighborEdge))
                            {
                                isExternal = false;
                                break;
                            }
                        }
                    }

                    if (isExternal)
                    {
                        externalEdges[currentTriangleIndex].Add(edge);
                    }
                }
            }

        }

        foreach (KeyValuePair<int, List<Global.EdgeData>> entry in externalEdges)
        {
            int key = entry.Key;
            List<Global.EdgeData> neighbors = entry.Value;

            foreach (Global.EdgeData value in entry.Value)
            {
                // 计算外部边中点坐标
                Vector2 midPoint = (value.vertex_1 + value.vertex_2) / 2;

                List<Vector2> points = new List<Vector2>();
                for (int i = 0; i < 3; i++)
                {
                    Vector2 point = new Vector2((float)Global.triangles[key].GetVertex(i).X, (float)Global.triangles[key].GetVertex(i).Y);
                    points.Add(point);
                }

                // 计算外部边所在三角形是否为锐角三角形
                if (IsPointInPolygon2D(points, Global.circumCenter[key]))
                {
                    Vector2 direction = (midPoint - Global.circumCenter[key]).normalized;
                    Vector2 start = Global.circumCenter[key];
                    Vector2 currentPoint = start;

                    while(IsPointInPolygon2D(Global.boundary2D, currentPoint))
                    {
                        currentPoint += direction;
                    }

                    Vector2 end = NearestPoint2D(Global.boundary2D, currentPoint);

                    Global.edgePairs.Add(new Global.EdgeData(start, end));
                }
                else
                {
                    float distance0 = Vector2.Distance(points[0], points[1]);
                    float distance1 = Vector2.Distance(points[1], points[2]);
                    float distance2 = Vector2.Distance(points[2], points[0]);

                    float max = Mathf.Max(distance0, distance1);
                    max = Mathf.Max(max, distance2);

                    float distance = Vector2.Distance(new Vector2(value.vertex_1.x, value.vertex_1.y), new Vector2(value.vertex_2.x, value.vertex_2.y));
                    Vector2 direction;

                    if (distance == max)
                    {
                        direction = (Global.circumCenter[key] - midPoint).normalized;
                    }
                    else
                    {
                        direction = (midPoint - Global.circumCenter[key]).normalized;
                    }

                    Vector2 start = Global.circumCenter[key];
                    Vector2 currentPoint = start;

                    while (IsPointInPolygon2D(Global.boundary2D, currentPoint))
                    {
                        currentPoint += direction;
                    }

                    Vector2 end = NearestPoint2D(Global.boundary2D, currentPoint);

                    Global.edgePairs.Add(new Global.EdgeData(end, start));
                }

            }
        }

        Global.isVoronoi = true;
    }

    public static Vector2 CalculateCircumcenter(Triangle triangle)
    {
        var v0 = triangle.GetVertex(0);
        var v1 = triangle.GetVertex(1);
        var v2 = triangle.GetVertex(2);

        // 三角形顶点坐标
        float x0 = (float)v0.X, y0 = (float)v0.Y;
        float x1 = (float)v1.X, y1 = (float)v1.Y;
        float x2 = (float)v2.X, y2 = (float)v2.Y;

        // 顶点坐标的平方和
        float distSqA = x0 * x0 + y0 * y0;
        float distSqB = x1 * x1 + y1 * y1;
        float distSqC = x2 * x2 + y2 * y2;

        // 辅助变量
        float numeratorX = distSqA * (y2 - y1) + distSqB * (y0 - y2) + distSqC * (y1 - y0);
        float numeratorY = -(distSqA * (x2 - x1) + distSqB * (x0 - x2) + distSqC * (x1 - x0));
        float denominator = 2 * (x0 * (y2 - y1) + x1 * (y0 - y2) + x2 * (y1 - y0));

        // 计算圆心坐标
        float centerX = numeratorX / denominator;
        float centerY = numeratorY / denominator;

        return new Vector2(centerX, centerY);
    }
}
