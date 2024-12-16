using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TriangleNet.Meshing;
using TriangleNet.Topology;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UI;

public class Global : MonoBehaviour
{
    public enum Mode
    {
        Road_Editor,
        Building_Editor,
        Plant_Painter,
        Terrain_Editor
    }
    public static Mode currentMode;

    public enum AreaFunction
    {
        Planting,
        Nursery,
        Warehouse,
        Office,
        Research,
        Greenhouse
    }
    public static AreaFunction currentAreaFunction;

    public enum LayoutMode
    {
        Garden,
        Farmland
    }
    public static LayoutMode currentLayoutMode;

    public enum Building
    {
        None,
        Gate,
        House1,
        House2,
        Well
    }
    public static Building currentBuilding;

    public enum Plant
    {
        None,
        Tree1,
        Tree2,
        Tree3,
        Tree4
    }
    public static Plant currentPlant;

    public class BuildingData
    {
        public Vector3 position;
        public float angle;

        public BuildingData(Vector3 pos, float an)
        {
            position = pos;
            angle = an;
        }

        public override bool Equals(object obj)
        {
            if (obj is BuildingData)
            {
                BuildingData other = (BuildingData)obj;
                return position.Equals(other.position) && angle.Equals(other.angle);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return position.GetHashCode() ^ angle.GetHashCode();
        }
    }

    public class EdgeData
    {
        public Vector2 vertex_1;
        public Vector2 vertex_2;

        public EdgeData(Vector2 v1, Vector2 v2)
        {
            vertex_1 = v1;
            vertex_2 = v2;
        }

        public override bool Equals(object obj)
        {
            if (obj is EdgeData)
            {
                EdgeData other = (EdgeData)obj;
                return vertex_1.Equals(other.vertex_1) && vertex_2.Equals(other.vertex_2);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return vertex_1.GetHashCode() ^ vertex_2.GetHashCode();
        }
    }

    public static GameObject roadContextObject = GameObject.Find("RoadContext");
    public static GameObject buildingContextObject = GameObject.Find("BuildingContext");
    public static GameObject plantContextObject = GameObject.Find("PlantContext");
    public static GameObject terrainContextObject = GameObject.Find("TerrainContext");

    public static GameObject Width_Input = GameObject.Find("Width_InputField");
    public static GameObject Length_Input = GameObject.Find("Length_InputField");

    public static GameObject gardenContextObject = GameObject.Find("GardenContext");
    public static GameObject farmlandContextObject = GameObject.Find("FarmlandContext");

    public static GameObject rotatablePrefab;

    public static int coroutineStart = 0;
    public static int coroutineEnd = 0;

    public static float brushSize;
    public static float brushStrength;

    public static float plantSize;
    public static float plantDensity;

    public static bool viewModel = false;
    public static bool reDrawModel = false;

    public static bool ispair = false;
    public static bool isexist = false;
    public static bool ismove = false;

    public static bool isVoronoi = false;
    public static bool isGA = false;

    public static List<Vector3> boundary = new List<Vector3>();     // terrain上的边界坐标
    public static List<Vector2> boundary2D = new List<Vector2>();   // image上的边界坐标

    public static List<Vector2> nodes = new List<Vector2>();        // image上的交互点坐标

    public static List<Vector2> nodePairs = new List<Vector2>();    // 记录右键交互点
    public static List<EdgeData> edges = new List<EdgeData>();      // 记录连接交互点的边

    public static GameObject movingNode;                            // 记录正在移动的交互点

    public static IMesh mesh;                                       // 记录三角剖分的结果
    public static List<Triangle> triangles = new List<Triangle>();  // 记录mesh中的三角形（无序），但是可以用GetVertex(Index).ID访问三角形的顶点；顶点也是无序，但编码与List<Vector2> nodes相同
    public static List<Vector2> circumCenter = new List<Vector2>(); // 记录对应三角形的外接圆圆心坐标
    public static List<EdgeData> edgePairs = new List<EdgeData>();  // 记录Voronoi图中边的两个端点

    public static List<Vector2> outsidePoints = new List<Vector2>();
    public static List<Vector2> insidePoints = new List<Vector2>();
    public static List<int> pointsMode = new List<int>();
    
    public static int width;
    public static int height;
    public static List<double> lower = new List<double>();
    public static List<double> upper = new List<double>();
    public static double[][] PScopy;
    public static List<EdgeData> farmlandLine = new List<EdgeData>();

    public static int index;
    public static Vector2 imagePosition;

    public static List<Vector3> roadPoints = new List<Vector3>();
    public static List<BuildingData> mainBuildingData = new List<BuildingData>();

    public static List<BezierKnot> fenceKnots = new List<BezierKnot>();
    public static List<GameObject> fenceInstances = new List<GameObject>();
    public static List<GameObject> roadInstances = new List<GameObject>();

    public static List<Vector2> treeBoundary2D = new List<Vector2>();
    public static List<Vector3> treeBoundary = new List<Vector3>();
    public static List<Vector3> plantPosition = new List<Vector3>();

    public static List<Vector2> generatedPositions = new List<Vector2>();
    public static List<Image> image_x = new List<Image>();
    public static List<Image> image_y = new List<Image>();
}
