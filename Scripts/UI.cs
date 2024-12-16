using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using TriangleNet;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Topology;
using Unity.Burst.CompilerServices;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public Camera Camera;
    public Terrain terrain;

    public GameObject sphere;
    public GameObject edgePrefab;
    public GameObject GAPrefab;
    public GameObject nodePrefab;

    public Button reLocate_Button;
    public Button reDraw_Button;
    public Dropdown mode_DropDown;
    public Dropdown areaFunction_DropDown;    
    public Dropdown layout_DropDown;
    public Slider brushSize_Slider;
    public Slider brushStrength_Slider;
    public Slider plantSize_Slider;
    public Slider plantDensity_Slider;

    public GameObject action;

    private int currentState;
    private GameObject image;
    private SplineContainer splineContainer;

    private void Start()
    {
        if (!Global.viewModel)
        {
            reLocate_Button.interactable = true;
            reDraw_Button.interactable = false;
            Camera.transform.position = new Vector3(50, 50, -15);
            Camera.transform.eulerAngles = new Vector3(45, 0, 0);
        }

        currentState = 0;
    }

    public void ClickView()
    {
        Global.viewModel = !Global.viewModel;

        if (Global.viewModel)
        {
            reLocate_Button.interactable = false;
            reDraw_Button.interactable = true;
            Camera.transform.position = new Vector3(50, 100, 50);
            Camera.transform.eulerAngles = new Vector3(90, 0, 0);
        }

        if (!Global.viewModel)
        {
            reLocate_Button.interactable = true;
            reDraw_Button.interactable = false;
            Camera.transform.position = new Vector3(50, 50, -15);
            Camera.transform.eulerAngles = new Vector3(45, 0, 0);
        }
    }

    public void ClickReDraw()
    {
        Global.reDrawModel = !Global.reDrawModel;
    }

    public void ClickReLocate()
    {
        Camera.transform.position = new Vector3(50, 50, -15);
        Camera.transform.eulerAngles = new Vector3(50, 0, 0);
    }

    public void ChangeModeValue()
    {
        switch (mode_DropDown.value)
        {
            case 0:
                Global.currentMode = Global.Mode.Road_Editor;
                break;
            case 1:
                Global.currentMode = Global.Mode.Building_Editor;
                break;
            case 2:
                Global.currentMode = Global.Mode.Plant_Painter;
                break;
            case 3:
                Global.currentMode = Global.Mode.Terrain_Editor;
                break;
        }
    }

    public void ChangeAreaFunctionValue()
    {
        switch (areaFunction_DropDown.value)
        {
            case 0:
                Global.currentAreaFunction = Global.AreaFunction.Planting;
                break;
            case 1:
                Global.currentAreaFunction = Global.AreaFunction.Nursery;
                break;
            case 2:
                Global.currentAreaFunction = Global.AreaFunction.Warehouse;
                break;
            case 3:
                Global.currentAreaFunction = Global.AreaFunction.Office;
                break;
            case 4:
                Global.currentAreaFunction = Global.AreaFunction.Research;
                break;
            case 5:
                Global.currentAreaFunction = Global.AreaFunction.Greenhouse;
                break;
        }
    }

    public void ChangeLayoutModeValue()
    {
        GameObject[] GameObjects;

        switch (layout_DropDown.value)
        {
            case 0:
                Global.currentLayoutMode = Global.LayoutMode.Garden;

                Global.nodes.Clear();
                Global.edges.Clear();
                Global.triangles.Clear();
                Global.circumCenter.Clear();
                Global.edgePairs.Clear();

                RemoveAllEdgeClones(Global.roadContextObject.transform);
                RemoveAllNodeClones(Global.roadContextObject.transform);

                if (splineContainer != null)
                {
                    while (splineContainer.Splines.Count > 1)
                    {
                        splineContainer.RemoveSplineAt(1);
                    }
                }

                GameObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject go in GameObjects)
                {
                    if (go.layer == LayerMask.NameToLayer("Road") ||
                        go.layer == LayerMask.NameToLayer("Plant") ||
                        go.layer == LayerMask.NameToLayer("Buidling"))
                    {
                        Destroy(go);
                    }
                }

                Global.coroutineStart = 0;
                Global.coroutineEnd = 0;
                Global.roadPoints.Clear();
                Global.roadInstances.Clear();

                break;
            case 1:
                Global.currentLayoutMode = Global.LayoutMode.Farmland;

                Global.nodes.Clear();
                Global.edges.Clear();
                Global.triangles.Clear();
                Global.circumCenter.Clear();
                Global.edgePairs.Clear();

                RemoveAllEdgeClones(Global.roadContextObject.transform);
                RemoveAllNodeClones(Global.roadContextObject.transform);

                if (splineContainer != null)
                {
                    while (splineContainer.Splines.Count > 1)
                    {
                        splineContainer.RemoveSplineAt(1);
                    }
                }

                GameObjects = FindObjectsOfType<GameObject>();
                foreach (GameObject go in GameObjects)
                {
                    if (go.layer == LayerMask.NameToLayer("Road") || 
                        go.layer == LayerMask.NameToLayer("Plant") || 
                        go.layer == LayerMask.NameToLayer("Buidling"))
                    {
                        Destroy(go);
                    }
                }

                Global.coroutineStart = 0;
                Global.coroutineEnd = 0;
                Global.roadPoints.Clear();
                Global.roadInstances.Clear();

                break;
        }
    }

    public void ClickReSet()
    {
        TerrainData terrainData = terrain.terrainData;

        int width = terrainData.heightmapResolution;
        int height = terrainData.heightmapResolution;
        float[,] heights = new float[width, height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                heights[i, j] = 0.05f;
            }
        }

        terrainData.SetHeights(0, 0, heights);
    }

    public void ClickWidthUp()
    {
        Global.PScopy[Global.index][0]++;
        Global.Width_Input.GetComponent<InputField>().text = Global.PScopy[Global.index][0].ToString();
    }

    public void ClickWidthDown()
    {
        Global.Width_Input.GetComponent<InputField>().text = Global.PScopy[Global.index][0].ToString();
    }

    public void ClickLengthUp()
    {
        Global.Length_Input.GetComponent<InputField>().text = Global.PScopy[Global.index][1].ToString();
    }

    public void ClickLengthDown()
    {
        Global.Length_Input.GetComponent<InputField>().text = Global.PScopy[Global.index][1].ToString();
    }

    public void ChangeBrushSizeValue()
    {
        Global.brushSize = brushSize_Slider.value;
    }

    public void ChangeBrushStrengthValue()
    {
        Global.brushStrength = brushStrength_Slider.value;
    }

    public void ChangePlantSizeValue()
    {
        Global.plantSize = plantSize_Slider.value;
    }

    public void ChangePlantDensityValue()
    {
        Global.plantDensity = plantDensity_Slider.value;
    }

    public void ClickGeneration()
    {
        GameObject[] gates = GameObject.FindGameObjectsWithTag("Gate");

        if (gates.Length == 0)
        {
            return;
        }
        else
        {
            foreach (GameObject gate in gates)
            {
                Global.coroutineStart++;
                StartCoroutine(RandomWalkCoroutine_Main(gate));
            }
        }
    }

    public void ClickClear()
    {
        action.GetComponent<Spline_Road>().enabled = false;
        splineContainer = action.GetComponent<SplineContainer>();

        while (splineContainer.Splines.Count > 1)
        {
            splineContainer.RemoveSplineAt(1);
        }

        GameObject[] GameObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject go in GameObjects)
        {
            if (go.layer == LayerMask.NameToLayer("Road"))
            {
                Destroy(go);
            }
        }

        Global.coroutineStart = 0;
        Global.coroutineEnd = 0;
        Global.roadPoints.Clear();
        Global.roadInstances.Clear();
    }

    public void ClickGeneration2D_Garden()
    {
        splineContainer = action.GetComponent<SplineContainer>();
        image = GameObject.Find("Boundary_Image_Garden");

        RemoveAllEdgeClones(image.transform);
        Global.edges.Clear();
        Global.triangles.Clear();
        Global.circumCenter.Clear();
        Global.edgePairs.Clear();
        Global.isVoronoi = false;

        Global.lower.Clear();
        Global.upper.Clear();
        Global.farmlandLine.Clear();
        Global.isGA = false;

        while (splineContainer.Splines.Count > 1)
        {
            splineContainer.RemoveSplineAt(1);
        }

        GameObject[] GameObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject go in GameObjects)
        {
            if (go.layer == LayerMask.NameToLayer("Road"))
            {
                Destroy(go);
            }
        }

        Global.roadInstances.Clear();

        Function.GenerateTriangle(Global.nodes);

        for (int i = 0; i < Global.edges.Count / 2; i++)
        {
            GenerateLine(Global.edges[2 * i].vertex_1, Global.edges[2 * i].vertex_2);
        }

        Function.GenerateVoronoi(Global.mesh);
    }

    public void ClickGeneration2D_Farm()
    {
        splineContainer = action.GetComponent<SplineContainer>();
        image = GameObject.Find("Boundary_Image_Farm");

        string filePath = @"C:\Users\Summcry\Desktop\IUI\Python\Exp\xiaorong.txt";

        try
        {
            // 使用 StreamWriter 以写模式打开文件，这将清空文件内容
            using (StreamWriter writer = new StreamWriter(filePath, false)) // false 表示不续写，直接覆盖
            {
                // 不写入任何内容，因此文件将被清空
            }

            Console.WriteLine("File content cleared successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        string filePath2 = @"C:\Users\Summcry\Desktop\IUI\Python\Exp\50.txt";

        try
        {
            // 使用 StreamWriter 以写模式打开文件，这将清空文件内容
            using (StreamWriter writer = new StreamWriter(filePath2, false)) // false 表示不续写，直接覆盖
            {
                // 不写入任何内容，因此文件将被清空
            }

            Console.WriteLine("File content cleared successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        RemoveAllEdgeClones(image.transform);
        Global.edges.Clear();
        Global.triangles.Clear();
        Global.circumCenter.Clear();
        Global.edgePairs.Clear();
        Global.isVoronoi = false;

        Global.lower.Clear();
        Global.upper.Clear();
        Global.farmlandLine.Clear();
        Global.isGA = false;

        while (splineContainer.Splines.Count > 1)
        {
            splineContainer.RemoveSplineAt(1);
        }

        GameObject[] GameObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject go in GameObjects)
        {
            if (go.layer == LayerMask.NameToLayer("Road"))
            {
                Destroy(go);
            }
        }

        Global.roadInstances.Clear();

        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (var point in Global.boundary2D)
        {
            if (point.x < minX)
            {
                minX = (int)point.x;
            }
            if (point.x > maxX)
            {
                maxX = (int)point.x;
            }
            if (point.y < minY)
            {
                minY = (int)point.y;
            }
            if (point.y > maxY)
            {
                maxY = (int)point.y;
            }
        }

        Global.width = maxX - minX;
        Global.height = maxY - minY;

        Global.outsidePoints = ConvexHull.GetConvexHull(Global.nodes);
        Global.insidePoints = Global.nodes.Except(Global.outsidePoints).ToList();

        Vector2 leftBottom = new Vector2(minX, minY);
        Vector2 leftTop = new Vector2(minX, maxY);
        Vector2 rightTop = new Vector2(maxX, maxY);
        Vector2 rightBottom = new Vector2(maxX, minY);

        int[] Index = { -1, -1, -1, -1 };
        float distance = float.MaxValue;
        float dis = 0.0f;

        for (int i = 0; i < Index.Length; i++)
        {
            for (int j = 0; j < Global.outsidePoints.Count; j++)
            {
                if (i == 0)
                {
                    dis = Vector2.Distance(leftBottom, Global.outsidePoints[j]);
                }
                else if (i == 1)
                {
                    dis = Vector2.Distance(leftTop, Global.outsidePoints[j]);
                }
                else if (i == 2)
                {
                    dis = Vector2.Distance(rightTop, Global.outsidePoints[j]);
                }
                else if (i == 3)
                {
                    dis = Vector2.Distance(rightBottom, Global.outsidePoints[j]);
                }

                if (dis < distance)
                {
                    distance = dis;
                    Index[i] = j;
                }
            }

            distance = float.MaxValue;
        }

        Global.pointsMode = new List<int>();
        for (int i = 0; i < Global.outsidePoints.Count; i++)
        {
            Global.pointsMode.Add(-1);
        }

        for (int i = 0; i < Global.pointsMode.Count; i++)
        {
            if (i >= Index[0] && i < Index[1])
            {
                Global.pointsMode[i] = 1;
            }
            else if (i >= Index[1] && i < Index[2])
            {
                Global.pointsMode[i] = 2;
            }
            else if (i >= Index[2] && i < Index[3])
            {
                Global.pointsMode[i] = 3;
            }
            else if (i >= Index[3] || i < Index[0])
            {
                Global.pointsMode[i] = 4;
            }
        }

        while (Global.pointsMode[0] != 1)
        {
            int firstElement_Mode = Global.pointsMode[0];
            Global.pointsMode.RemoveAt(0);
            Global.pointsMode.Add(firstElement_Mode);

            Vector2 firstElement_Position = Global.outsidePoints[0];
            Global.outsidePoints.RemoveAt(0);
            Global.outsidePoints.Add(firstElement_Position);
        }

        Polygon geometry = new Polygon();

        foreach (Vector2 node in Global.nodes)
        {
            geometry.Add(new Vertex(node.x, node.y));
        }

        Global.mesh = new GenericMesher().Triangulate(geometry);

        Dictionary<Vector2, List<Vector2>> adjacentPoints = new Dictionary<Vector2, List<Vector2>>();
        Dictionary<Vector2, Vector2> certainPoints = new Dictionary<Vector2, Vector2>();

        for (int i = 0; i < Global.outsidePoints.Count; i++)
        {
            adjacentPoints[Global.outsidePoints[i]] = new List<Vector2>();

            foreach (Triangle triangle in Global.mesh.Triangles)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (triangle.GetVertex(j).X == Global.outsidePoints[i].x &&
                        triangle.GetVertex(j).Y == Global.outsidePoints[i].y)
                    {
                        Vector2 point1 = new Vector2((float)triangle.GetVertex((j + 1) % 3).X, (float)triangle.GetVertex((j + 1) % 3).Y);
                        Vector2 point2 = new Vector2((float)triangle.GetVertex((j + 2) % 3).X, (float)triangle.GetVertex((j + 2) % 3).Y);

                        if (!adjacentPoints[Global.outsidePoints[i]].Contains(point1))
                        {
                            adjacentPoints[Global.outsidePoints[i]].Add(point1);
                        }

                        if (!adjacentPoints[Global.outsidePoints[i]].Contains(point2))
                        {
                            adjacentPoints[Global.outsidePoints[i]].Add(point2);
                        }

                        if (!Global.edges.Contains(new Global.EdgeData(point1, Global.outsidePoints[i])) && !Global.edges.Contains(new Global.EdgeData(Global.outsidePoints[i], point1)))
                        {
                            Global.edges.Add(new Global.EdgeData(Global.outsidePoints[i], point1));
                        }

                        if (!Global.edges.Contains(new Global.EdgeData(point2, Global.outsidePoints[i])) && !Global.edges.Contains(new Global.EdgeData(Global.outsidePoints[i], point2)))
                        {
                            Global.edges.Add(new Global.EdgeData(Global.outsidePoints[i], point2));
                        }
                    }
                }
            }
        }

        if (Global.insidePoints.Count == 2)
        {
            Global.edges.Add(new Global.EdgeData(Global.insidePoints[0], Global.insidePoints[1]));
        }

        for (int i = 0; i < Global.edges.Count; i++)
        {
            GenerateLine(Global.edges[i].vertex_1, Global.edges[i].vertex_2);
        }

        int index_outside = 0;
        float rate = 100.0f;

        foreach (var key in adjacentPoints)
        {
            certainPoints[Global.outsidePoints[index_outside]] = new Vector2();

            for (int i = 0; i < key.Value.Count; i++)
            {
                for (int j = i + 1; j < key.Value.Count; j++)
                {
                    if (Global.pointsMode[index_outside] == 1)
                    {
                        Vector2 point1 = key.Value[i];
                        Vector2 point2 = key.Value[j];

                        if ((point1.x <= point2.x && point1.y >= point2.y) ||
                            (point2.x <= point1.x && point2.y >= point1.y))
                        {
                            int maxPointX = (int)point1.x >= (int)point2.x ? (int)point1.x : (int)point2.x;
                            int maxPointY = (int)point1.y >= (int)point2.y ? (int)point1.y : (int)point2.y;

                            Vector2 newPoint = new Vector2(maxPointX, maxPointY);
                            float newRate = Math.Abs(Global.outsidePoints[index_outside].x - maxPointX) / Math.Abs(Global.outsidePoints[index_outside].y - maxPointY);

                            if (certainPoints[Global.outsidePoints[index_outside]] == new Vector2(0, 0))
                            {
                                certainPoints[Global.outsidePoints[index_outside]] = newPoint;
                                rate = newRate;
                            }
                            else
                            {
                                if (Math.Abs(newRate - 1) <= Math.Abs(rate - 1))
                                {
                                    certainPoints[Global.outsidePoints[index_outside]] = newPoint;
                                    rate = newRate;
                                }
                            }
                        }
                    }
                    else if (Global.pointsMode[index_outside] == 2)
                    {
                        Vector2 point1 = key.Value[i];
                        Vector2 point2 = key.Value[j];

                        if ((point1.x <= point2.x && point1.y <= point2.y) ||
                            (point2.x <= point1.x && point2.y <= point1.y))
                        {
                            int maxPointX = (int)point1.x >= (int)point2.x ? (int)point1.x : (int)point2.x;
                            int minPointY = (int)point1.y <= (int)point2.y ? (int)point1.y : (int)point2.y;

                            Vector2 newPoint = new Vector2(maxPointX, minPointY);
                            float newRate = Math.Abs(Global.outsidePoints[index_outside].x - maxPointX) / Math.Abs(Global.outsidePoints[index_outside].y - minPointY);

                            if (certainPoints[Global.outsidePoints[index_outside]] == new Vector2(0, 0))
                            {
                                certainPoints[Global.outsidePoints[index_outside]] = newPoint;
                                rate = newRate;
                            }
                            else
                            {
                                if (Math.Abs(newRate - 1) <= Math.Abs(rate - 1))
                                {
                                    certainPoints[Global.outsidePoints[index_outside]] = newPoint;
                                    rate = newRate;
                                }
                            }
                        }
                    }
                    else if (Global.pointsMode[index_outside] == 3)
                    {
                        Vector2 point1 = key.Value[i];
                        Vector2 point2 = key.Value[j];

                        if ((point1.x <= point2.x && point1.y >= point2.y) ||
                            (point2.x <= point1.x && point2.y >= point1.y))
                        {
                            int minPointX = (int)point1.x <= (int)point2.x ? (int)point1.x : (int)point2.x;
                            int minPointY = (int)point1.y <= (int)point2.y ? (int)point1.y : (int)point2.y;

                            Vector2 newPoint = new Vector2(minPointX, minPointY);
                            float newRate = Math.Abs(Global.outsidePoints[index_outside].x - minPointX) / Math.Abs(Global.outsidePoints[index_outside].y - minPointY);

                            if (certainPoints[Global.outsidePoints[index_outside]] == new Vector2(0, 0))
                            {
                                certainPoints[Global.outsidePoints[index_outside]] = newPoint;
                                rate = newRate;
                            }
                            else
                            {
                                if (Math.Abs(newRate - 1) <= Math.Abs(rate - 1))
                                {
                                    certainPoints[Global.outsidePoints[index_outside]] = newPoint;
                                    rate = newRate;
                                }
                            }
                        }
                    }
                    else if (Global.pointsMode[index_outside] == 4)
                    {
                        Vector2 point1 = key.Value[i];
                        Vector2 point2 = key.Value[j];

                        if ((point1.x <= point2.x && point1.y <= point2.y) ||
                            (point2.x <= point1.x && point2.y <= point1.y))
                        {
                            int minPointX = (int)point1.x <= (int)point2.x ? (int)point1.x : (int)point2.x;
                            int maxPointY = (int)point1.y >= (int)point2.y ? (int)point1.y : (int)point2.y;

                            Vector2 newPoint = new Vector2(minPointX, maxPointY);
                            float newRate = Math.Abs(Global.outsidePoints[index_outside].x - minPointX) / Math.Abs(Global.outsidePoints[index_outside].y - maxPointY);

                            if (certainPoints[Global.outsidePoints[index_outside]] == new Vector2(0, 0))
                            {
                                certainPoints[Global.outsidePoints[index_outside]] = newPoint;
                                rate = newRate;
                            }
                            else
                            {
                                if (Math.Abs(newRate - 1) <= Math.Abs(rate - 1))
                                {
                                    certainPoints[Global.outsidePoints[index_outside]] = newPoint;
                                    rate = newRate;
                                }
                            }
                        }
                    }
                }
            }

            index_outside++;
            rate = 100.0f;
        }

        foreach (var key in certainPoints)
        {
            Vector2 point1 = key.Key;
            Vector2 point2 = key.Value;

            int areaMinX = (int)point1.x <= (int)point2.x ? (int)point1.x : (int)point2.x;
            int areaMaxX = (int)point1.x >= (int)point2.x ? (int)point1.x : (int)point2.x;
            int areaMinY = (int)point1.y <= (int)point2.y ? (int)point1.y : (int)point2.y;
            int areaMaxY = (int)point1.y >= (int)point2.y ? (int)point1.y : (int)point2.y;

            Global.lower.Add(areaMinX);
            Global.upper.Add(areaMaxX);

            Global.lower.Add(areaMinY);
            Global.upper.Add(areaMaxY);
        }

        Dictionary<Vector2, List<Vector2>> adjacentPoints_inside = new Dictionary<Vector2, List<Vector2>>();

        for (int i = 0; i < Global.insidePoints.Count; i++)
        {
            adjacentPoints_inside[Global.insidePoints[i]] = new List<Vector2>();

            foreach (Triangle triangle in Global.mesh.Triangles)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (triangle.GetVertex(j).X == Global.insidePoints[i].x &&
                        triangle.GetVertex(j).Y == Global.insidePoints[i].y)
                    {
                        Vector2 point1 = new Vector2((float)triangle.GetVertex((j + 1) % 3).X, (float)triangle.GetVertex((j + 1) % 3).Y);
                        Vector2 point2 = new Vector2((float)triangle.GetVertex((j + 2) % 3).X, (float)triangle.GetVertex((j + 2) % 3).Y);

                        if (!adjacentPoints_inside[Global.insidePoints[i]].Contains(point1))
                        {
                            adjacentPoints_inside[Global.insidePoints[i]].Add(point1);
                        }

                        if (!adjacentPoints_inside[Global.insidePoints[i]].Contains(point2))
                        {
                            adjacentPoints_inside[Global.insidePoints[i]].Add(point2);
                        }

                    }
                }
            }
        }

        int maxX_inside = int.MinValue;
        int minX_inside = int.MaxValue;
        int maxY_inside = int.MinValue;
        int minY_inside = int.MaxValue;

        foreach (var key in adjacentPoints_inside)
        {
            foreach (var value in key.Value)
            {
                if (value.x >= maxX_inside)
                {
                    maxX_inside = (int)value.x;
                }

                if (value.x <= minX_inside)
                {
                    minX_inside = (int)value.x;
                }

                if (value.y >= maxY_inside)
                {
                    maxY_inside = (int)value.y;
                }

                if (value.y <= minY_inside)
                {
                    minY_inside = (int)value.y;
                }
            }

            Global.lower.Add(minX_inside);
            Global.upper.Add((int)key.Key.x);

            Global.lower.Add((int)key.Key.y);
            Global.upper.Add(maxY_inside);

            Global.lower.Add((int)key.Key.x);
            Global.upper.Add(maxX_inside);

            Global.lower.Add(minY_inside);
            Global.upper.Add((int)key.Key.y);

            maxX_inside = int.MinValue;
            minX_inside = int.MaxValue;
            maxY_inside = int.MinValue;
            minY_inside = int.MaxValue;
        }

        for (int i = 0; i < Global.lower.Count; i++)
        {
            if (i % 2 == 0)
            {
                Global.lower[i] -= minX;
                Global.upper[i] -= minX;
            }
            else
            {
                Global.lower[i] -= minY;
                Global.upper[i] -= minY;
            }
        }

        GA.GeneticAlgorithm(Global.width, Global.height, Global.lower.ToArray(), Global.upper.ToArray());

        for (int i = 0; i < Global.PScopy.Length; i++)
        {
            Global.PScopy[i][0] += minX;
            Global.PScopy[i][1] += minY;
        }

        // 初始映射点
        GenerateMappingNode();

        // 初始道路
        AdaptLine();

        Global.isGA = true;
    }

    private void GenerateMappingNode()
    {
        for (int i = 0; i < Global.PScopy.Length; i++)
        {
            Vector2 position = new Vector2((float)Global.PScopy[i][0], (float)Global.PScopy[i][1]);
            if (!Global.generatedPositions.Contains(position))
            {
                GameObject node = Instantiate(nodePrefab, image.transform);
                RectTransform rectTransform = node.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = new Vector2((float)Global.PScopy[i][0], (float)Global.PScopy[i][1]);
                }

                Global.generatedPositions.Add(position);
            }
        }


    }

    public void ClickClear2D()
    {
        Global.nodes.Clear();
        Global.edges.Clear();
        Global.triangles.Clear();
        Global.circumCenter.Clear();
        Global.edgePairs.Clear();

        RemoveAllEdgeClones(Global.roadContextObject.transform);
        RemoveAllNodeClones(Global.roadContextObject.transform);

        if (splineContainer != null)
        {
            while (splineContainer.Splines.Count > 1)
            {
                splineContainer.RemoveSplineAt(1);
            }
        }

        GameObject[] GameObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject go in GameObjects)
        {
            if (go.layer == LayerMask.NameToLayer("Road") ||
                go.layer == LayerMask.NameToLayer("Plant") ||
                go.layer == LayerMask.NameToLayer("Building"))
            {
                Destroy(go);
            }
        }

        Global.coroutineStart = 0;
        Global.coroutineEnd = 0;
        Global.roadPoints.Clear();
        Global.roadInstances.Clear();

        Global.lower.Clear();
        Global.upper.Clear();
        Global.outsidePoints.Clear();
        Global.insidePoints.Clear();
        Global.pointsMode.Clear();
    }

    // 根据起点和终点在Node之间画线
    private void GenerateLine(Vector2 startVertex, Vector2 endVertex)
    {
        image = GameObject.Find("Boundary_Image_Garden");
        if (image == null)
        {
            image = GameObject.Find("Boundary_Image_Farm");
        }

        GameObject line = Instantiate(edgePrefab, image.transform);
        line.transform.SetSiblingIndex(0);

        Vector2 direction = endVertex - startVertex;
        float distance = direction.magnitude;
        direction.Normalize();

        RectTransform rectTransform = line.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(distance, 2);
        rectTransform.anchoredPosition = startVertex + direction * distance / 2;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // 为GA的结果画线
    public void GenerateLine_GA(Vector2 startVertex, Vector2 endVertex)
    {
        image = GameObject.Find("Boundary_Image_Garden");
        if (image == null)
        {
            image = GameObject.Find("Boundary_Image_Farm");
        }

        GameObject line = Instantiate(GAPrefab, image.transform);
        line.transform.SetSiblingIndex(0);

        Vector2 direction = endVertex - startVertex;
        float distance = direction.magnitude;
        direction.Normalize();

        RectTransform rectTransform = line.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(distance, 2);
        rectTransform.anchoredPosition = startVertex + direction * distance / 2;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rectTransform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // 递归查找目录，删除所有名为Edge(Clone)的GameObject
    public void RemoveAllEdgeClones(Transform parent)
    {
        // 遍历子物体
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            // 如果是名为Edge(Clone)的GameObject，则删除
            if (child.name == "Edge(Clone)" || child.name == "Edge_GA(Clone)" || child.name == "Node_GA(Clone)")
            {
                Destroy(child.gameObject);
            }
            else
            {
                // 递归查找子物体
                RemoveAllEdgeClones(child);
            }
        }
    }

    // 递归查找目录，删除所有名为Node(Clone)的GameObject
    public void RemoveAllNodeClones(Transform parent)
    {
        // 遍历子物体
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            // 如果是名为Edge(Clone)的GameObject，则删除
            if (child.name == "Node(Clone)")
            {
                Destroy(child.gameObject);
            }
            else
            {
                // 递归查找子物体
                RemoveAllNodeClones(child);
            }
        }
    }

    public void AdaptLine()
    {
        for (int i = 0; i < Global.outsidePoints.Count; i++)
        {
            if (Global.pointsMode[i] == 1)
            {
                Vector2 start = new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]);
                while (Function.IsPointInPolygon2D(Global.boundary2D, start))
                {
                    start.x -= 1;
                }
                Vector2 xEdge = new Vector2(start.x + 1, start.y);

                start = new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]);
                while (Function.IsPointInPolygon2D(Global.boundary2D, start))
                {
                    start.y -= 1;
                }
                Vector2 yEdge = new Vector2(start.x, start.y + 1);

                if (i == 0)
                {
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                yEdge));
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                xEdge));
                }
                else
                {
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i - 1][1])));
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                xEdge));
                }
            }
            else if (Global.pointsMode[i] == 2)
            {
                Vector2 start = new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]);
                while (Function.IsPointInPolygon2D(Global.boundary2D, start))
                {
                    start.x -= 1;
                }
                Vector2 xEdge = new Vector2(start.x + 1, start.y);

                start = new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]);
                while (Function.IsPointInPolygon2D(Global.boundary2D, start))
                {
                    start.y += 1;
                }
                Vector2 yEdge = new Vector2(start.x, start.y - 1);

                if (Global.pointsMode[i - 1] == 1)
                {
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                yEdge));
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                xEdge));
                }
                else
                {
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                yEdge));
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                new Vector2((int)Global.PScopy[i - 1][0], (int)Global.PScopy[i][1])));
                }
            }
            else if (Global.pointsMode[i] == 3)
            {
                Vector2 start = new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]);
                while (Function.IsPointInPolygon2D(Global.boundary2D, start))
                {
                    start.x += 1;
                }
                Vector2 xEdge = new Vector2(start.x - 1, start.y);

                start = new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]);
                while (Function.IsPointInPolygon2D(Global.boundary2D, start))
                {
                    start.y += 1;
                }
                Vector2 yEdge = new Vector2(start.x, start.y - 1);

                if (Global.pointsMode[i - 1] == 2)
                {
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                yEdge));
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                xEdge));
                }
                else
                {
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i - 1][1])));
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                xEdge));
                }
            }
            else if (Global.pointsMode[i] == 4)
            {
                Vector2 start = new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]);
                while (Function.IsPointInPolygon2D(Global.boundary2D, start))
                {
                    start.x += 1;
                }
                Vector2 xEdge = new Vector2(start.x - 1, start.y);

                start = new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]);
                while (Function.IsPointInPolygon2D(Global.boundary2D, start))
                {
                    start.y -= 1;
                }
                Vector2 yEdge = new Vector2(start.x, start.y + 1);

                if (Global.pointsMode[i - 1] == 3)
                {
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                yEdge));
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                xEdge));
                }
                else
                {
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                yEdge));
                    Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[i][0], (int)Global.PScopy[i][1]),
                                                                new Vector2((int)Global.PScopy[i - 1][0], (int)Global.PScopy[i][1])));
                }
            }
        }

        for (int i = 0; i < Global.insidePoints.Count; i++)
        {
            Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[Global.outsidePoints.Count + 2 * i][0],
                                                                    (int)Global.PScopy[Global.outsidePoints.Count + 2 * i][1]),
                                                        new Vector2((int)Global.PScopy[Global.outsidePoints.Count + 2 * i + 1][0],
                                                                    (int)Global.PScopy[Global.outsidePoints.Count + 2 * i][1])));
            Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[Global.outsidePoints.Count + 2 * i][0],
                                                                    (int)Global.PScopy[Global.outsidePoints.Count + 2 * i][1]),
                                                        new Vector2((int)Global.PScopy[Global.outsidePoints.Count + 2 * i][0],
                                                                    (int)Global.PScopy[Global.outsidePoints.Count + 2 * i + 1][1])));

            Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[Global.outsidePoints.Count + 2 * i + 1][0],
                                                                    (int)Global.PScopy[Global.outsidePoints.Count + 2 * i + 1][1]),
                                                        new Vector2((int)Global.PScopy[Global.outsidePoints.Count + 2 * i][0],
                                                                    (int)Global.PScopy[Global.outsidePoints.Count + 2 * i + 1][1])));
            Global.farmlandLine.Add(new Global.EdgeData(new Vector2((int)Global.PScopy[Global.outsidePoints.Count + 2 * i + 1][0],
                                                                    (int)Global.PScopy[Global.outsidePoints.Count + 2 * i + 1][1]),
                                                        new Vector2((int)Global.PScopy[Global.outsidePoints.Count + 2 * i + 1][0],
                                                                    (int)Global.PScopy[Global.outsidePoints.Count + 2 * i][1])));
        }

        for (int i = 0; i < Global.farmlandLine.Count; i++)
        {
            GenerateLine_GA(Global.farmlandLine[i].vertex_1, Global.farmlandLine[i].vertex_2);
        }
    }

    public IEnumerator RandomWalkCoroutine_Main(GameObject gate)
    {
        splineContainer = action.GetComponent<SplineContainer>();

        float angle = (gate.transform.eulerAngles.y % 360 - 90) % 360;
        float radian = angle * Mathf.Deg2Rad;
        float height = terrain.SampleHeight(new Vector3(gate.transform.position.x, 0, gate.transform.position.z));
        Vector3 point = new Vector3(gate.transform.position.x, height, gate.transform.position.z);
        Vector3 direction = new Vector3(Mathf.Sin(radian), 0.0f, Mathf.Cos(radian));

        List<BezierKnot> roadKnots_local = new List<BezierKnot>();
        roadKnots_local.Add(new BezierKnot(new float3(gate.transform.position.x, height, gate.transform.position.z)));
        Global.roadPoints.Add(new Vector3(gate.transform.position.x, height, gate.transform.position.z));

        point += direction * 1;

        while (Function.IsPointInPolygon(Global.boundary, point))
        {
            int count = Function.AngleArrangePointCount(Global.roadPoints, point, direction, 1.01f);
            //int count = Function.ArrangePointCount(Global.roadPoints, point, 1.01f);
            if (count <= 1)
            {
                roadKnots_local.Add(new BezierKnot(new float3(point.x, point.y, point.z)));
                Global.roadPoints.Add(point);

                switch (currentState)
                {
                    case 0:
                        direction = State0Function(direction);
                        break;
                    case 1:
                        direction = State1Function(direction);
                        break;
                    case 2:
                        direction = State2Function(direction);
                        break;
                    case 3:
                        direction = State3Function(point, direction);
                        break;
                }
                currentState = MarkovChain.TransitionState(currentState);

                point += direction * 1;
            }
            else
            {
                Vector3 intersection = Function.NearestPoint(Global.roadPoints, point);
                roadKnots_local.Add(new BezierKnot(new float3(intersection.x, intersection.y, intersection.z)));
                break;
            }

            yield return null;
        }

        if (!Function.IsPointInPolygon(Global.boundary, point))
        {
            Vector3 intersection = Function.NearestPoint(Global.boundary, point);
            roadKnots_local.Add(new BezierKnot(new float3(intersection.x, intersection.y, intersection.z)));
        }

        var spline = splineContainer.AddSpline();
        spline.Closed = false;

        foreach (BezierKnot knot in roadKnots_local)
        {
            spline.Add(knot, TangentMode.AutoSmooth);
        }

        Global.coroutineEnd++;
    }

    public IEnumerator RandomWalkCoroutine_Secondary(Vector3 point, Vector3 direction)
    {
        currentState = 0;

        splineContainer = action.GetComponent<SplineContainer>();

        List<BezierKnot> roadKnots_local = new List<BezierKnot>();
        roadKnots_local.Add(new BezierKnot(new float3(point.x, point.y, point.z)));

        point += direction * 1;

        while (Function.IsPointInPolygon(Global.boundary, point))
        {
            int count = Function.AngleArrangePointCount(Global.roadPoints, point, direction, 1.01f);
            //int count = Function.ArrangePointCount(Global.roadPoints, point, 1.01f);
            if (count <= 1)
            {
                roadKnots_local.Add(new BezierKnot(new float3(point.x, point.y, point.z)));
                Global.roadPoints.Add(point);

                switch (currentState)
                {
                    case 0:
                        direction = State0Function(direction);
                        break;
                    case 1:
                        direction = State1Function(direction);
                        break;
                    case 2:
                        direction = State2Function(direction);
                        break;
                    case 3:
                        direction = State3Function(point, direction);
                        break;
                }
                currentState = MarkovChain.TransitionState(currentState);

                point += direction * 1;
            }
            else
            {
                Vector3 intersection = Function.NearestPoint(Global.roadPoints, point);
                roadKnots_local.Add(new BezierKnot(new float3(intersection.x, intersection.y, intersection.z)));
                break;
            }

            yield return null;
        }

        if (!Function.IsPointInPolygon(Global.boundary, point))
        {
            Vector3 intersection = Function.NearestPoint(Global.boundary, point);
            roadKnots_local.Add(new BezierKnot(new float3(intersection.x, intersection.y, intersection.z)));
        }

        var spline = splineContainer.AddSpline();
        spline.Closed = false;

        foreach (BezierKnot knot in roadKnots_local)
        {
            spline.Add(knot, TangentMode.AutoSmooth);
        }

        Global.coroutineEnd++;
    }

    // State1:方向不变
    public Vector3 State0Function(Vector3 direction)
    {
        return direction;
    }

    // State2:方向在0-20间旋转任意角度
    public Vector3 State1Function(Vector3 direction)
    {
        System.Random rand = new System.Random();
        double randomAngle = 20 * rand.NextDouble();

        Quaternion rotation = Quaternion.Euler(0, (float)randomAngle, 0);

        direction = rotation * direction;
        direction.y = 0;

        return direction;
    }

    // State3:方向在-20-0间旋转任意角度
    public Vector3 State2Function(Vector3 direction)
    {
        System.Random rand = new System.Random();
        double randomAngle = -20 * rand.NextDouble();

        Quaternion rotation = Quaternion.Euler(0, (float)randomAngle, 0);

        direction = rotation * direction;
        direction.y = 0;

        return direction;
    }

    // State4:将原direction旋转一个角度，并生成一个新的direction，沿新的方向创建副路（新的协程）
    public Vector3 State3Function(Vector3 point, Vector3 direction)
    {
        Vector3 newDirection;

        System.Random rand = new System.Random();
        double randomAngle_1 = -30 * rand.NextDouble() - 30;
        double randomAngle_2 = randomAngle_1 + 30 * rand.NextDouble() + 60;

        Quaternion rotation_1 = Quaternion.Euler(0, (float)randomAngle_1, 0);
        Quaternion rotation_2 = Quaternion.Euler(0, (float)randomAngle_2, 0);

        direction = rotation_1 * direction;
        direction.y = 0;

        newDirection = rotation_2 * direction;
        newDirection.y = 0;

        Global.coroutineStart++;
        StartCoroutine(RandomWalkCoroutine_Secondary(point, newDirection));

        return direction;
    }
}
