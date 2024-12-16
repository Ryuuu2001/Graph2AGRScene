using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Splines.Examples;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Splines;
using UnityEngine.UI;

public class Action : MonoBehaviour
{
    public Terrain terrain;
    public GameObject action;

    public Dropdown dropDown;
    public Dropdown layoutDropDown;
    public Dropdown areaFunctionDropdown;
    public Slider brushSizeSlider;
    public Slider brushStrengthSlider;
    public Slider plantSizeSlider;
    public Slider plantDensitySlider;

    public GameObject fence;
    public GameObject gate;
    public GameObject house1;
    public GameObject house2;
    public GameObject well;

    public GameObject tree1;
    public GameObject tree2;
    public GameObject tree3;
    public GameObject tree4;

    public Image gardenImage;
    public Image farmlandImage;

    public Texture2D brushTexture;

    public Button[] buildingButtons;
    public Button[] plantButtons;

    public SplineContainer splineContainer;

    private TerrainData terrainData;

    private Transform image_Transform;

    private Texture2D newTex;
    private Color[] craterData;

    private Vector3 fenceColliderSize;
    private Vector3 fenceColliderCenter;
    private Vector3 gateColliderSize;
    private Vector3 gateColliderCenter;

    private Vector3 lastPosition;

    Vector2 areaPoint = Vector2.zero;

    private int xRes;
    private int yRes;

    private bool isDrawing = false;

    public GameObject Menu;

    private void Start()
    {
        terrainData = terrain.terrainData;

        splineContainer.RemoveSplineAt(0);
        action.GetComponent<Spline_Road>().enabled = false;

        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.positionCount = 0;

        BoxCollider fenceBoxCollider = fence.GetComponent<BoxCollider>();
        fenceColliderSize = fenceBoxCollider.size;
        fenceColliderCenter = fenceBoxCollider.center;

        BoxCollider gateBoxCollider = gate.GetComponent<BoxCollider>();
        gateColliderSize = gateBoxCollider.size;
        gateColliderCenter = gateBoxCollider.center;

        Global.currentMode = Global.Mode.Road_Editor;
        Global.currentLayoutMode = Global.LayoutMode.Garden;
        Global.currentBuilding = Global.Building.None;
        Global.currentPlant = Global.Plant.None;

        dropDown.value = 0;
        layoutDropDown.value = 1;

        brushSizeSlider.value = 5;
        brushStrengthSlider.value = 5;
        plantSizeSlider.value = 5;
        plantDensitySlider.value = 5;

        xRes = terrainData.heightmapResolution;
        yRes = terrainData.heightmapResolution;

        brushScaling();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            Menu.SetActive(false);
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            Menu.SetActive(true);
        }

        if (Global.viewModel && !IsLayerPrefabOnTerrain(LayerMask.GetMask("Fence")))
        {
            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                if (splineContainer.Spline != null)
                {
                    return;
                }

                LineRenderer lineRenderer = GetComponent<LineRenderer>();
                lineRenderer.enabled = true;

                isDrawing = true;

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                {
                    lastPosition = hit.point;

                    Global.boundary.Add(hit.point);
                    Global.fenceKnots.Add(new BezierKnot(new float3(hit.point.x, hit.point.y, hit.point.z)));
                    AddPointToPath(new Vector3(hit.point.x, hit.point.y + 0.1f, hit.point.z));
                }
            }

            if (Input.GetMouseButtonUp(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                if (splineContainer.Spline != null)
                {
                    return;
                }

                LineRenderer lineRenderer = GetComponent<LineRenderer>();
                lineRenderer.enabled = false;

                isDrawing = false;

                var spline = splineContainer.AddSpline();
                spline.Closed = true;

                foreach (BezierKnot knot in Global.fenceKnots)
                {
                    spline.Add(knot, TangentMode.AutoSmooth);
                }

                ModifyTerrainTextures();

                Function.Transform2D(terrain, gardenImage, Global.boundary);
            }

            if (isDrawing)
            {
                if (splineContainer.Spline != null)
                {
                    return;
                }

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                {
                    // 获取鼠标竖偏移量
                    float hor = Input.GetAxis("Mouse X");
                    float ver = Input.GetAxis("Mouse Y");

                    if (hor != 0 || ver != 0)
                    {
                        Global.boundary.Add(hit.point);
                        AddPointToPath(new Vector3(hit.point.x, hit.point.y + 0.1f, hit.point.z));
                    }

                    float dis = Vector3.Distance(lastPosition, hit.point);

                    if (dis > 5)
                    {
                        Global.fenceKnots.Add(new BezierKnot(new float3(hit.point.x, hit.point.y, hit.point.z)));
                        lastPosition = hit.point;
                    }
                }
            }
        }

        if (!Global.viewModel && Global.fenceInstances.Count > 0)
        {
            action.GetComponent<Spline_Fence>().enabled = false;
        }

        if (Global.reDrawModel)
        {
            Clear();
            ReSetTexture();
            Global.reDrawModel = !Global.reDrawModel;

            splineContainer.RemoveSplineAt(0);
            Global.fenceInstances.Clear();
            Global.fenceKnots.Clear();

            action.GetComponent<Spline_Fence>().enabled = true;

            gardenImage.GetComponent<ImageBoundary>().enabled = false;
            gardenImage.sprite = null;

            farmlandImage.GetComponent<ImageBoundary>().enabled = false;
            farmlandImage.sprite = null;
        }

        if (Global.currentMode == Global.Mode.Road_Editor)
        {
            Global.roadContextObject.SetActive(true);
            Global.buildingContextObject.SetActive(false);
            Global.plantContextObject.SetActive(false);
            Global.terrainContextObject.SetActive(false);

            if (Global.gardenContextObject.activeSelf)
            {
                image_Transform = Global.gardenContextObject.transform.Find("Boundary_Image_Garden");
            }

            if (Global.farmlandContextObject.activeSelf)
            {
                image_Transform = Global.farmlandContextObject.transform.Find("Boundary_Image_Farm");
            }

            if (Global.imagePosition != new Vector2(0.0f, 0.0f))
            {
                if (Global.currentAreaFunction == Global.AreaFunction.Planting)
                {
                    FindImageByPosition(image_Transform, Global.imagePosition, new Color(237.0f / 255.0f, 28.0f / 255.0f, 36.0f / 255.0f));
                }
                else if (Global.currentAreaFunction == Global.AreaFunction.Nursery)
                {
                    FindImageByPosition(image_Transform, Global.imagePosition, new Color(255.0f / 255.0f, 242.0f / 255.0f, 0.0f / 255.0f));
                }
                else if (Global.currentAreaFunction == Global.AreaFunction.Warehouse)
                {
                    FindImageByPosition(image_Transform, Global.imagePosition, new Color(34.0f / 255.0f, 177.0f / 255.0f, 76.0f / 255.0f));
                }
                else if (Global.currentAreaFunction == Global.AreaFunction.Office)
                {
                    FindImageByPosition(image_Transform, Global.imagePosition, new Color(0.0f / 255.0f, 162.0f / 255.0f, 232.0f / 255.0f));
                }
                else if (Global.currentAreaFunction == Global.AreaFunction.Research)
                {
                    FindImageByPosition(image_Transform, Global.imagePosition, new Color(163.0f / 255.0f, 73.0f / 255.0f, 164.0f / 255.0f));
                }
                else if (Global.currentAreaFunction == Global.AreaFunction.Greenhouse)
                {
                    FindImageByPosition(image_Transform, Global.imagePosition, new Color(127.0f / 255.0f, 127.0f / 255.0f, 127.0f / 255.0f));
                }
            }

            if (Global.currentLayoutMode == Global.LayoutMode.Garden)
            {
                Global.gardenContextObject.SetActive(true);
                Global.farmlandContextObject.SetActive(false);

                if (IsLayerPrefabOnTerrain(LayerMask.GetMask("Fence")))
                {
                    if (Global.coroutineStart != 0 && Global.coroutineStart == Global.coroutineEnd)
                    {
                        action.GetComponent<Spline_Road>().enabled = true;
                    }

                    if (Global.boundary.Count == Global.boundary2D.Count && Global.boundary2D.Count != 0)
                    {
                        gardenImage.GetComponent<ImageBoundary>().enabled = true;
                    }

                    if (Global.isVoronoi)
                    {
                        foreach (Global.EdgeData edge in Global.edgePairs)
                        {
                            List<BezierKnot> roadKnots_local = new List<BezierKnot>();

                            Vector3 start = Function.Transform3D_Single(gardenImage, terrain, edge.vertex_1);
                            Vector3 end = Function.Transform3D_Single(gardenImage, terrain, edge.vertex_2);
                            Vector3 direction = new Vector3((end - start).x, 0.0f, (end - start).z).normalized;
                            float totalDistance = Vector3.Distance(start, end);
                            float currentDistance = 0;

                            if (Function.IsPointInPolygon(Global.boundary, end))
                            {
                                while (currentDistance < totalDistance)
                                {
                                    Vector3 point = start + direction * currentDistance;
                                    roadKnots_local.Add(new BezierKnot(new float3(point.x, point.y, point.z)));

                                    currentDistance += 2;
                                }

                                if (currentDistance >= totalDistance)
                                {
                                    Vector3 point = end;
                                    roadKnots_local.Add(new BezierKnot(new float3(point.x, point.y, point.z)));
                                }

                                var spline = splineContainer.AddSpline();
                                spline.Closed = false;

                                foreach (BezierKnot knot in roadKnots_local)
                                {
                                    spline.Add(knot, TangentMode.AutoSmooth);
                                }
                            }
                            else
                            {
                                while (currentDistance < totalDistance)
                                {
                                    Vector3 point = start + direction * currentDistance;

                                    if (Function.IsPointInPolygon(Global.boundary, point))
                                    {
                                        roadKnots_local.Add(new BezierKnot(new float3(point.x, point.y, point.z)));
                                        currentDistance += 2;
                                    }
                                    else
                                    {
                                        Vector3 endPoint = Function.NearestPoint(Global.boundary, point);
                                        roadKnots_local.Add(new BezierKnot(new float3(endPoint.x, endPoint.y, endPoint.z)));
                                        currentDistance = totalDistance;
                                    }
                                }

                                var spline = splineContainer.AddSpline();
                                spline.Closed = false;

                                foreach (BezierKnot knot in roadKnots_local)
                                {
                                    spline.Add(knot, TangentMode.AutoSmooth);
                                }
                            }
                        }

                        action.GetComponent<Spline_Road>().enabled = true;
                        Global.isVoronoi = false;
                    }
                }
            }

            if (Global.currentLayoutMode == Global.LayoutMode.Farmland)
            {
                Global.gardenContextObject.SetActive(false);
                Global.farmlandContextObject.SetActive(true);

                if (IsLayerPrefabOnTerrain(LayerMask.GetMask("Fence")))
                {
                    if (Global.coroutineStart != 0 && Global.coroutineStart == Global.coroutineEnd)
                    {
                        action.GetComponent<Spline_Road>().enabled = true;
                    }

                    if (Global.boundary.Count == Global.boundary2D.Count && Global.boundary2D.Count != 0)
                    {
                        farmlandImage.GetComponent<ImageBoundary>().enabled = true;
                    }

                    if (Global.isGA)
                    {
                        foreach (Global.EdgeData edge in Global.farmlandLine)
                        {
                            List<BezierKnot> roadKnots_local = new List<BezierKnot>();

                            Vector3 start = Function.Transform3D_Single(farmlandImage, terrain, edge.vertex_1);
                            Vector3 end = Function.Transform3D_Single(farmlandImage, terrain, edge.vertex_2);
                            Vector3 direction = new Vector3((end - start).x, 0.0f, (end - start).z).normalized;
                            float totalDistance = Vector3.Distance(start, end);
                            float currentDistance = 0;

                            if (Function.IsPointInPolygon(Global.boundary, end))
                            {
                                while (currentDistance < totalDistance)
                                {
                                    Vector3 point = start + direction * currentDistance;
                                    roadKnots_local.Add(new BezierKnot(new float3(point.x, point.y, point.z)));

                                    currentDistance += 2;
                                }

                                if (currentDistance >= totalDistance)
                                {
                                    Vector3 point = end;
                                    roadKnots_local.Add(new BezierKnot(new float3(point.x, point.y, point.z)));
                                }

                                var spline = splineContainer.AddSpline();
                                spline.Closed = false;

                                foreach (BezierKnot knot in roadKnots_local)
                                {
                                    spline.Add(knot, TangentMode.AutoSmooth);
                                }
                            }
                            else
                            {
                                while (currentDistance < totalDistance)
                                {
                                    Vector3 point = start + direction * currentDistance;

                                    if (Function.IsPointInPolygon(Global.boundary, point))
                                    {
                                        roadKnots_local.Add(new BezierKnot(new float3(point.x, point.y, point.z)));
                                        currentDistance += 2;
                                    }
                                    else
                                    {
                                        Vector3 endPoint = Function.NearestPoint(Global.boundary, point);
                                        roadKnots_local.Add(new BezierKnot(new float3(endPoint.x, endPoint.y, endPoint.z)));
                                        currentDistance = totalDistance;
                                    }
                                }

                                var spline = splineContainer.AddSpline();
                                spline.Closed = false;

                                foreach (BezierKnot knot in roadKnots_local)
                                {
                                    spline.Add(knot, TangentMode.AutoSmooth);
                                }
                            }
                        }

                        action.GetComponent<Spline_Road>().enabled = true;
                        Global.isGA = false;
                    }
                }
            }
        }

        if (Global.currentMode == Global.Mode.Building_Editor)
        {
            Global.roadContextObject.SetActive(false);
            Global.buildingContextObject.SetActive(true);
            Global.plantContextObject.SetActive(false);
            Global.terrainContextObject.SetActive(false);

            for (int i = 0; i < buildingButtons.Length; i++)
            {
                int index = i;
                buildingButtons[i].onClick.AddListener(() => SetBuildingButtonColors(index));
            }

            if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Building")))
                {
                    Global.rotatablePrefab = hit.collider.gameObject;
                }
            }

            if (Global.currentBuilding == Global.Building.Gate)
            {
                if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Fence")))
                    {
                        GameObject gameObjectHit = hit.collider.gameObject;

                        if (gameObjectHit.CompareTag("Wall"))
                        {
                            Vector3 gatePosition = gameObjectHit.transform.position;
                            Vector3 gateRotation = gameObjectHit.transform.eulerAngles;

                            Destroy(gameObjectHit);
                            Instantiate(gate, new Vector3(gatePosition.x,
                                (float)(gateColliderSize.y / 2 - gateColliderCenter.y + 5),
                                gatePosition.z), Quaternion.Euler(gateRotation));

                            Global.mainBuildingData.Add(new Global.BuildingData(new Vector3(gatePosition.x, 5, gatePosition.z), gateRotation.y));
                        }

                        if (gameObjectHit.CompareTag("Gate"))
                        {
                            Vector3 fencePosition = gameObjectHit.transform.position;
                            Vector3 fenceRotation = gameObjectHit.transform.eulerAngles;

                            Destroy(gameObjectHit);
                            Instantiate(fence, new Vector3(fencePosition.x,
                                (float)(fenceColliderSize.y / 2 - fenceColliderCenter.y + 5),
                                fencePosition.z), Quaternion.Euler(fenceRotation));

                            Global.mainBuildingData.Remove(new Global.BuildingData(new Vector3(fencePosition.x, 5, fencePosition.z), fenceRotation.y));
                        }
                    }
                }
            }

            if (Global.currentBuilding == Global.Building.House1)
            {
                if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                    {
                        Collider[] colliders = Physics.OverlapSphere(hit.point, 5, LayerMask.GetMask("Building"));

                        if (colliders.Length == 0)
                        {
                            if (Function.IsPointInPolygon(Global.boundary, hit.point))
                            {
                                Global.mainBuildingData.Add(new Global.BuildingData(hit.point, 180.0f));
                                Instantiate(house1, hit.point, Quaternion.Euler(0, 180.0f, 0));
                            }
                        }
                    }
                }
            }

            if (Global.currentBuilding == Global.Building.House2)
            {
                if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                    {
                        Collider[] colliders = Physics.OverlapSphere(hit.point, 5, LayerMask.GetMask("Building"));

                        if (colliders.Length == 0)
                        {
                            if (Function.IsPointInPolygon(Global.boundary, hit.point))
                            {
                                Global.mainBuildingData.Add(new Global.BuildingData(hit.point, 180.0f));
                                Instantiate(house2, hit.point, Quaternion.Euler(0, 180.0f, 0));
                            }
                        }
                    }
                }
            }

            if (Global.currentBuilding == Global.Building.Well)
            {
                if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                    {
                        Collider[] colliders = Physics.OverlapSphere(hit.point, 5, LayerMask.GetMask("Building"));

                        if (colliders.Length == 0)
                        {
                            if (Function.IsPointInPolygon(Global.boundary, hit.point))
                            {
                                Instantiate(well, hit.point, Quaternion.identity);
                            }
                        }
                    }
                }
            }
        }

        if (Global.currentMode == Global.Mode.Plant_Painter)
        {
            Global.roadContextObject.SetActive(false);
            Global.buildingContextObject.SetActive(false);
            Global.plantContextObject.SetActive(true);
            Global.terrainContextObject.SetActive(false);

            for (int i = 0; i < plantButtons.Length; i++)
            {
                int index = i;
                plantButtons[i].onClick.AddListener(() => SetPlantButtonColors(index));
            }

            if (Input.GetKey(KeyCode.LeftShift))
            {
                if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                    {
                        Vector2 Point = Function.Transform2D_Single(terrain, gardenImage, hit.point);
                        areaPoint = Function.NearestPoint2D(Global.nodes, Point);
                    }
                }

                if (Input.GetMouseButton(0))
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                    {
                        Collider[] colliders = Physics.OverlapSphere(hit.point, Global.plantSize, LayerMask.GetMask("Plant"));

                        if (colliders.Length > 0)
                        {
                            foreach (Collider collider in colliders)
                            {
                                Vector2 point = Function.Transform2D_Single(terrain, gardenImage, collider.gameObject.transform.position);
                                Vector2 currentAreaPoint = Function.NearestPoint2D(Global.nodes, point);

                                if (currentAreaPoint == areaPoint)
                                {
                                    Destroy(collider.gameObject);
                                    Global.plantPosition.Remove(collider.gameObject.transform.position);
                                }
                            }
                        }
                    }
                }

                if (Input.GetMouseButtonUp(0))
                {
                    areaPoint = Vector2.zero;
                }
            }
            else
            {
                if (Global.currentPlant == Global.Plant.Tree1)
                {
                    if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                    {
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                        {
                            Vector2 Point = Function.Transform2D_Single(terrain, gardenImage, hit.point);
                            areaPoint = Function.NearestPoint2D(Global.nodes, Point);
                        }
                    }

                    if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
                    {
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                        {
                            float hor = Input.GetAxis("Mouse X");
                            float ver = Input.GetAxis("Mouse Y");

                            if (hor != 0 || ver != 0)
                            {
                                for (int i = 0; i < (int)Global.plantDensity; i++)
                                {
                                    Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * Global.plantSize;
                                    Vector3 spawnPosition = new Vector3(hit.point.x + randomOffset.x, hit.point.y, hit.point.z + randomOffset.y);

                                    if (Function.IsPointInPolygon(Global.boundary, spawnPosition) && !IsPointNearAnyKnot(spawnPosition, 3.0f))
                                    {
                                        if (Global.plantPosition.Count == 0)
                                        {
                                            Vector2 point = Function.Transform2D_Single(terrain, gardenImage, spawnPosition);
                                            Vector2 currentAreaPoint = Function.NearestPoint2D(Global.nodes, point);

                                            if (currentAreaPoint == areaPoint)
                                            {
                                                Global.plantPosition.Add(spawnPosition);
                                                Instantiate(tree1, spawnPosition, Quaternion.identity);
                                            }
                                        }
                                        else
                                        {
                                            if (Function.CompareDistance(Global.plantPosition, spawnPosition, -0.5 * Global.plantDensity + 7))
                                            {
                                                Vector2 point = Function.Transform2D_Single(terrain, gardenImage, spawnPosition);
                                                Vector2 currentAreaPoint = Function.NearestPoint2D(Global.nodes, point);

                                                if (currentAreaPoint == areaPoint)
                                                {
                                                    Global.plantPosition.Add(spawnPosition);
                                                    Instantiate(tree1, spawnPosition, Quaternion.identity);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        areaPoint = Vector2.zero;
                    }
                }

                if (Global.currentPlant == Global.Plant.Tree2)
                {
                    if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                    {
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                        {
                            Vector2 Point = Function.Transform2D_Single(terrain, gardenImage, hit.point);
                            areaPoint = Function.NearestPoint2D(Global.nodes, Point);
                        }
                    }

                    if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
                    {
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                        {
                            float hor = Input.GetAxis("Mouse X");
                            float ver = Input.GetAxis("Mouse Y");

                            if (hor != 0 || ver != 0)
                            {
                                for (int i = 0; i < (int)Global.plantDensity; i++)
                                {
                                    Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * Global.plantSize;
                                    Vector3 spawnPosition = new Vector3(hit.point.x + randomOffset.x, hit.point.y, hit.point.z + randomOffset.y);

                                    if (Function.IsPointInPolygon(Global.boundary, spawnPosition) && !IsPointNearAnyKnot(spawnPosition, 3.0f))
                                    {
                                        if (Global.plantPosition.Count == 0)
                                        {
                                            Vector2 point = Function.Transform2D_Single(terrain, gardenImage, spawnPosition);
                                            Vector2 currentAreaPoint = Function.NearestPoint2D(Global.nodes, point);

                                            if (currentAreaPoint == areaPoint)
                                            {
                                                Global.plantPosition.Add(spawnPosition);
                                                Instantiate(tree2, spawnPosition, Quaternion.identity);
                                            }
                                        }
                                        else
                                        {
                                            if (Function.CompareDistance(Global.plantPosition, spawnPosition, -0.5 * Global.plantDensity + 7))
                                            {
                                                Vector2 point = Function.Transform2D_Single(terrain, gardenImage, spawnPosition);
                                                Vector2 currentAreaPoint = Function.NearestPoint2D(Global.nodes, point);

                                                if (currentAreaPoint == areaPoint)
                                                {
                                                    Global.plantPosition.Add(spawnPosition);
                                                    Instantiate(tree2, spawnPosition, Quaternion.identity);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        areaPoint = Vector2.zero;
                    }
                }

                if (Global.currentPlant == Global.Plant.Tree3)
                {
                    if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                    {
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                        {
                            Vector2 Point = Function.Transform2D_Single(terrain, gardenImage, hit.point);
                            areaPoint = Function.NearestPoint2D(Global.nodes, Point);
                        }
                    }

                    if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
                    {
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                        {
                            float hor = Input.GetAxis("Mouse X");
                            float ver = Input.GetAxis("Mouse Y");

                            if (hor != 0 || ver != 0)
                            {
                                for (int i = 0; i < (int)Global.plantDensity; i++)
                                {
                                    Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * Global.plantSize;
                                    Vector3 spawnPosition = new Vector3(hit.point.x + randomOffset.x, hit.point.y, hit.point.z + randomOffset.y);

                                    if (Function.IsPointInPolygon(Global.boundary, spawnPosition) && !IsPointNearAnyKnot(spawnPosition, 3.0f))
                                    {
                                        if (Global.plantPosition.Count == 0)
                                        {
                                            Vector2 point = Function.Transform2D_Single(terrain, gardenImage, spawnPosition);
                                            Vector2 currentAreaPoint = Function.NearestPoint2D(Global.nodes, point);

                                            if (currentAreaPoint == areaPoint)
                                            {
                                                Global.plantPosition.Add(spawnPosition);
                                                Instantiate(tree3, spawnPosition, Quaternion.identity);
                                            }
                                        }
                                        else
                                        {
                                            if (Function.CompareDistance(Global.plantPosition, spawnPosition, -0.5 * Global.plantDensity + 7))
                                            {
                                                Vector2 point = Function.Transform2D_Single(terrain, gardenImage, spawnPosition);
                                                Vector2 currentAreaPoint = Function.NearestPoint2D(Global.nodes, point);

                                                if (currentAreaPoint == areaPoint)
                                                {
                                                    Global.plantPosition.Add(spawnPosition);
                                                    Instantiate(tree3, spawnPosition, Quaternion.identity);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        areaPoint = Vector2.zero;
                    }
                }

                if (Global.currentPlant == Global.Plant.Tree4)
                {
                    if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
                    {
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                        {
                            Vector2 Point = Function.Transform2D_Single(terrain, gardenImage, hit.point);
                            areaPoint = Function.NearestPoint2D(Global.nodes, Point);
                        }
                    }

                    if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
                    {
                        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                        {
                            float hor = Input.GetAxis("Mouse X");
                            float ver = Input.GetAxis("Mouse Y");

                            if (hor != 0 || ver != 0)
                            {
                                for (int i = 0; i < (int)Global.plantDensity; i++)
                                {
                                    Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * Global.plantSize;
                                    Vector3 spawnPosition = new Vector3(hit.point.x + randomOffset.x, hit.point.y, hit.point.z + randomOffset.y);

                                    if (Function.IsPointInPolygon(Global.boundary, spawnPosition) && !IsPointNearAnyKnot(spawnPosition, 3.0f))
                                    {
                                        if (Global.plantPosition.Count == 0)
                                        {
                                            Vector2 point = Function.Transform2D_Single(terrain, gardenImage, spawnPosition);
                                            Vector2 currentAreaPoint = Function.NearestPoint2D(Global.nodes, point);

                                            if (currentAreaPoint == areaPoint)
                                            {
                                                Global.plantPosition.Add(spawnPosition);
                                                Instantiate(tree4, spawnPosition, Quaternion.identity);
                                            }
                                        }
                                        else
                                        {
                                            if (Function.CompareDistance(Global.plantPosition, spawnPosition, -0.5 * Global.plantDensity + 7))
                                            {
                                                Vector2 point = Function.Transform2D_Single(terrain, gardenImage, spawnPosition);
                                                Vector2 currentAreaPoint = Function.NearestPoint2D(Global.nodes, point);

                                                if (currentAreaPoint == areaPoint)
                                                {
                                                    Global.plantPosition.Add(spawnPosition);
                                                    Instantiate(tree4, spawnPosition, Quaternion.identity);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (Input.GetMouseButtonUp(0))
                    {
                        areaPoint = Vector2.zero;
                    }
                }
            }
        }

        if (Global.currentMode == Global.Mode.Terrain_Editor)
        {
            Global.roadContextObject.SetActive(false);
            Global.buildingContextObject.SetActive(false);
            Global.plantContextObject.SetActive(false);
            Global.terrainContextObject.SetActive(true);

            brushScaling();

            if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                {
                    if (Function.IsPointInPolygon(Global.boundary, hit.point))
                    {
                        int x = (int)Mathf.Lerp(0, xRes, Mathf.InverseLerp(0, terrainData.size.x, hit.point.x));
                        int z = (int)Mathf.Lerp(0, yRes, Mathf.InverseLerp(0, terrainData.size.z, hit.point.z));

                        x = Mathf.Clamp(x, newTex.width / 2, xRes - newTex.width / 2);
                        z = Mathf.Clamp(z, newTex.height / 2, yRes - newTex.height / 2);

                        int startX = x - newTex.width / 2;
                        int startY = z - newTex.height / 2;

                        float[,] areaT = terrainData.GetHeights(startX, startY, newTex.width, newTex.height);

                        for (int i = 0; i < newTex.height; i++)
                        {
                            for (int j = 0; j < newTex.width; j++)
                            {
                                if (Input.GetKey(KeyCode.LeftShift))
                                {
                                    if (i < 1 || i > newTex.height - 2 || j < 1 || j > newTex.width - 2)
                                        continue;

                                    float heightSum = 0;
                                    for (int ySub = -1; ySub <= 1; ySub++)
                                    {
                                        for (int xSub = -1; xSub <= 1; xSub++)
                                        {
                                            heightSum += areaT[i + ySub, j + xSub];
                                        }
                                    }

                                    areaT[i, j] = Mathf.Lerp(areaT[i, j], (heightSum / 9), craterData[i * newTex.width + j].a * Global.brushStrength * 5000);
                                }
                                else
                                {
                                    if (i < 2 || i > newTex.height - 3 || j < 2 || j > newTex.width - 3)
                                        continue;

                                    areaT[i, j] = areaT[i, j] - craterData[i * newTex.width + j].a * Global.brushStrength / 100.0f;

                                    float heightSum = 0;
                                    for (int ySub = -2; ySub <= 2; ySub++)
                                    {
                                        for (int xSub = -2; xSub <= 2; xSub++)
                                        {
                                            heightSum += areaT[i + ySub, j + xSub];
                                        }
                                    }

                                    areaT[i, j] = Mathf.Lerp(areaT[i, j], (heightSum / 25), craterData[i * newTex.width + j].a * Global.brushStrength * 20);
                                }
                            }
                        }
                        terrainData.SetHeights(x - newTex.width / 2, z - newTex.height / 2, areaT);
                    }
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    private void OnDestroy()
    {
        ReSetHeight();
        ReSetTexture();
    }

    // 向LineRenderer中添加顶点
    private void AddPointToPath(Vector3 point)
    {
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount++;
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, point);
    }

    // 挂载在ReDraw按钮上，清除LineRender，Global.boundary和Building层级的预制体
    private void Clear()
    {
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;

        Global.boundary.Clear();
        Global.boundary2D.Clear();

        GameObject[] GameObjects = FindObjectsOfType<GameObject>();
        foreach (GameObject go in GameObjects)
        {
            if (go.layer == LayerMask.NameToLayer("Fence"))
            {
                Destroy(go);
            }
        }
    }

    // Building按钮相关颜色与选择
    private void SetBuildingButtonColors(int clickedIndex)
    {
        for (int i = 0; i < buildingButtons.Length; i++)
        {
            if (i == clickedIndex)
            {
                buildingButtons[i].image.color = Color.green;
            }
            else
            {
                buildingButtons[i].image.color = Color.white;
            }
        }

        switch (clickedIndex)
        {
            case 0:
                Global.currentBuilding = Global.Building.Gate;
                break;
            case 1:
                Global.currentBuilding = Global.Building.House1;
                break;
            case 2:
                Global.currentBuilding = Global.Building.House2;
                break;
            case 3:
                Global.currentBuilding = Global.Building.Well;
                break;
        }
    }

    // Plant按钮相关颜色与选择
    private void SetPlantButtonColors(int clickedIndex)
    {
        for (int i = 0; i < plantButtons.Length; i++)
        {
            if (i == clickedIndex)
            {
                plantButtons[i].image.color = Color.green;
            }
            else
            {
                plantButtons[i].image.color = Color.white;
            }
        }

        switch (clickedIndex)
        {
            case 0:
                Global.currentPlant = Global.Plant.Tree1;
                break;
            case 1:
                Global.currentPlant = Global.Plant.Tree2;
                break;
            case 2:
                Global.currentPlant = Global.Plant.Tree3;
                break;
            case 3:
                Global.currentPlant = Global.Plant.Tree4;
                break;
        }
    }

    // 判断Terrain上是否存在某一层级的物体
    private bool IsLayerPrefabOnTerrain(LayerMask layerMask)
    {
        bool isExistent = false;

        Vector3 terrainCenter = new Vector3(Terrain.activeTerrain.transform.position.x + Terrain.activeTerrain.terrainData.size.x / 2,
            Terrain.activeTerrain.transform.position.y, Terrain.activeTerrain.transform.position.z + Terrain.activeTerrain.terrainData.size.z / 2);

        float radius = (Terrain.activeTerrain.terrainData.size.x >= Terrain.activeTerrain.terrainData.size.z) ?
            Terrain.activeTerrain.terrainData.size.x / 2 : Terrain.activeTerrain.terrainData.size.z / 2;

        Collider[] colliders = Physics.OverlapSphere(terrainCenter, radius, layerMask);
        if(colliders.Length > 0)
        {
            isExistent = !isExistent;
        }

        return isExistent;
    }

    // 重置Terrain高度
    private void ReSetHeight()
    {
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

    // 重置Terrain纹理
    private void ReSetTexture()
    {
        float[,,] alphaMap = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        for (int x = 0; x < terrainData.alphamapWidth; x++)
        {
            for (int z = 0; z < terrainData.alphamapHeight; z++)
            {
                alphaMap[x, z, 0] = 1.0f;
                alphaMap[x, z, 1] = 0.0f;
                alphaMap[x, z, 2] = 0.0f;
            }
        }

        terrainData.SetAlphamaps(0, 0, alphaMap);
    }

    // 调整笔刷属性
    private void brushScaling()
    {
        // 实例化纹理
        newTex = Instantiate(brushTexture) as Texture2D;

        if (brushTexture.width * (int)Global.brushSize / 100 != 0 && brushTexture.height * (int)Global.brushSize / 100 != 0)
        {
            // TextureScale.cs脚本中的函数
            TextureScale.Point(newTex,
                brushTexture.width * (int)Global.brushSize / 100,
                brushTexture.height * (int)Global.brushSize / 100);
        }

        // 应用纹理
        newTex.Apply();

        // 获取纹理每个像素信息
        craterData = newTex.GetPixels();
    }

    // 调整边界内外纹理
    private void ModifyTerrainTextures()
    {
        float[,,] alphaMap = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        for (int x = 0; x < terrainData.alphamapWidth; x++)
        {
            for (int z = 0; z < terrainData.alphamapHeight; z++)
            {
                Vector3 position = new Vector3(z / (float)terrainData.alphamapHeight * terrainData.size.z, 0,
                    x / (float)terrainData.alphamapWidth * terrainData.size.x);

                if (Function.IsPointInPolygon(Global.boundary, position))
                {
                    alphaMap[x, z, 0] = 0.0f;
                    alphaMap[x, z, 1] = 1.0f;
                    alphaMap[x, z, 2] = 0.0f;
                }
                else
                {
                    alphaMap[x, z, 0] = 0.0f;
                    alphaMap[x, z, 1] = 0.0f;
                    alphaMap[x, z, 2] = 1.0f;
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, alphaMap);
    }

    // 判断给定点与最近的Knot距离是否小于指定距离
    bool IsPointNearAnyKnot(Vector3 point, float distanceThreshold)
    {
        foreach (var spline in splineContainer.Splines)
        {
            foreach (var knot in spline.Knots)
            {
                float distance = Vector3.Distance(point, (Vector3)knot.Position);

                if (distance < distanceThreshold)
                {
                    return true;
                }
            }
        }

        return false;
    }

    void FindImageByPosition(Transform parent, Vector2 position, Color color)
    {
        foreach (Transform child in parent)
        {
            if (child.name == "Node(Clone)")
            {
                Image image = child.GetComponent<Image>();

                if (image.rectTransform.anchoredPosition == position)
                {
                    image.color = color;
                }
            }
        }
    }
}
