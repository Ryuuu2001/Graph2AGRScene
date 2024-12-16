using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Splines;
using UnityEngine.UI;

public class UpdateMappingNode : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    private Image image_self; // ��ǰ�϶���Image
    private List<Image> allImages; // �洢������ΪNode_GA(Clone)��Image���

    private GameObject image;
    public GameObject GAPrefab;

    private SplineContainer splineContainer;
    private GameObject action;

    private Image farmlandImage;
    private Terrain terrain;

    private void Start()
    {
        image = GameObject.Find("Boundary_Image_Farm");
        action = GameObject.Find("Action");
        splineContainer = action.GetComponent<SplineContainer>();
        terrain = GameObject.Find("Terrain").GetComponent<Terrain>();
        farmlandImage = GameObject.Find("Boundary_Image_Farm").GetComponent<Image>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // ��ȡ���������е�Image���
            allImages = new List<Image>(FindObjectsOfType<Image>());

            // ���˳���UI���Node_GA(Clone)����
            allImages.RemoveAll(img => img.gameObject.layer != LayerMask.NameToLayer("UI") || img.gameObject.name != "Node_GA(Clone)");

            image_self = GetComponent<Image>();

            // ��ȡ��ǰimage_self������
            Vector2 selfPosition = image_self.rectTransform.anchoredPosition;

            // �������е�Image��������image_self��ͬx��y�����Image
            foreach (Image img in allImages)
            {
                if (img != image_self) // ȷ�����ƶ�����
                {
                    Vector2 imgPosition = img.rectTransform.anchoredPosition;

                    // �ж��Ƿ�����ͬ��x����
                    if (Mathf.Approximately(selfPosition.x, imgPosition.x))
                    {
                        Global.image_x.Add(img);
                    }

                    // �ж��Ƿ�����ͬ��y����
                    if (Mathf.Approximately(selfPosition.y, imgPosition.y))
                    {
                        Global.image_y.Add(img);
                    }
                }
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            Vector2 oldImgPosition = image_self.rectTransform.anchoredPosition;
            image_self.rectTransform.anchoredPosition += eventData.delta;
            Vector2 newImgPosition = image_self.rectTransform.anchoredPosition;

            Global.Width_Input.GetComponent<InputField>().text = newImgPosition.x.ToString();
            Global.Length_Input.GetComponent<InputField>().text = newImgPosition.y.ToString();

            for (int i = 0; i < Global.PScopy.Length; i++)
            {
                if (Global.PScopy[i][0] == oldImgPosition.x && Global.PScopy[i][1] == oldImgPosition.y)
                {
                    Global.PScopy[i][0] = newImgPosition.x;
                    Global.PScopy[i][1] = newImgPosition.y;
                }
            }

            foreach (Image img in Global.image_x)
            {
                Vector2 oldAnchoredPos = img.rectTransform.anchoredPosition;
                Vector2 anchoredPos = img.rectTransform.anchoredPosition;
                anchoredPos.x = image_self.rectTransform.anchoredPosition.x;
                img.rectTransform.anchoredPosition = anchoredPos;
                Vector2 newAnchoredPos = img.rectTransform.anchoredPosition;

                for (int i = 0; i < Global.PScopy.Length; i++)
                {
                    if (Global.PScopy[i][0] == oldAnchoredPos.x && Global.PScopy[i][1] == oldAnchoredPos.y)
                    {
                        Global.PScopy[i][0] = newAnchoredPos.x;
                        Global.PScopy[i][1] = newAnchoredPos.y;
                    }
                }
            }

            foreach (Image img in Global.image_y)
            {
                Vector2 oldAnchoredPos = img.rectTransform.anchoredPosition;
                Vector2 anchoredPos = img.rectTransform.anchoredPosition;
                anchoredPos.y = image_self.rectTransform.anchoredPosition.y;
                img.rectTransform.anchoredPosition = anchoredPos;
                Vector2 newAnchoredPos = img.rectTransform.anchoredPosition;

                for (int i = 0; i < Global.PScopy.Length; i++)
                {
                    if (Global.PScopy[i][0] == oldAnchoredPos.x && Global.PScopy[i][1] == oldAnchoredPos.y)
                    {
                        Global.PScopy[i][0] = newAnchoredPos.x;
                        Global.PScopy[i][1] = newAnchoredPos.y;
                    }
                }
            }

            RemoveAllEdgeClones(image.transform);
            Global.farmlandLine.Clear();

            AdaptLine();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Global.image_x.Clear();
        Global.image_y.Clear();

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
    }

    public void RemoveAllEdgeClones(Transform parent)
    {
        // ����������
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            // �������ΪEdge(Clone)��GameObject����ɾ��
            if (child.name == "Edge_GA(Clone)")
            {
                Destroy(child.gameObject);
            }
            else
            {
                // �ݹ����������
                RemoveAllEdgeClones(child);
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

    public void GenerateLine_GA(Vector2 startVertex, Vector2 endVertex)
    {
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
}
