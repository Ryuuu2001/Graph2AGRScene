using System.Collections;
using System.Collections.Generic;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Topology;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Splines;
using UnityEngine.UI;

public class Selection2D : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler
{
    public GameObject edgePrefab;

    private GameObject image;
    private Image image_self;

    private Color yellow = new Color(243.0f / 255.0f, 243.0f / 255.0f, 70.0f / 255.0f);
    private Color cyan = new Color(68.0f / 255.0f, 253.0f / 255.0f, 208.0f / 255.0f);

    private Color temp = new Color();

    private float minDis;
    private int minIndex;
    private int dragIndex;

    private float lastClickTime = 0f;
    private float doubleClickDelay = 0.3f;

    private GameObject action;
    private SplineContainer splineContainer;

    public void OnScroll(PointerEventData eventData)
    {
        
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            image_self = GetComponent<Image>();
            image_self.color = yellow;
            image_self.rectTransform.anchoredPosition += eventData.delta;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.time - lastClickTime < doubleClickDelay)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                foreach (Global.EdgeData edge in Global.edges)
                {
                    if (edge.vertex_1 == Global.nodes[dragIndex])
                    {
                        DeleteLine(edge.vertex_1, edge.vertex_2);
                    }
                }

                for (int i = Global.edges.Count - 1; i >= 0; i--)
                {
                    if (Global.edges[i].vertex_1 == Global.nodes[dragIndex])
                    {
                        Global.edges.RemoveAt(i);
                    }
                }

                for (int i = Global.edges.Count - 1; i >= 0; i--)
                {
                    if (Global.edges[i].vertex_2 == Global.nodes[dragIndex])
                    {
                        Global.edges.RemoveAt(i);
                    }
                }

                Destroy(gameObject);
                Global.nodes.RemoveAt(dragIndex);

                image = GameObject.Find("Boundary_Image_Garden");
                if (image == null)
                {
                    image = GameObject.Find("Boundary_Image_Farm");
                }
                action = GameObject.Find("Action");
                splineContainer = action.GetComponent<SplineContainer>();

                if (Global.currentLayoutMode == Global.LayoutMode.Garden)
                {
                    RemoveAllEdgeClones(image.transform);

                    Global.edges.Clear();
                    Global.triangles.Clear();
                    Global.circumCenter.Clear();
                    Global.edgePairs.Clear();
                    Global.isVoronoi = false;
                }

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

                if (Global.currentLayoutMode == Global.LayoutMode.Garden)
                {
                    Function.GenerateTriangle(Global.nodes);

                    for (int i = 0; i < Global.edges.Count / 2; i++)
                    {
                        GenerateLine(Global.edges[2 * i].vertex_1, Global.edges[2 * i].vertex_2);
                    }

                    Function.GenerateVoronoi(Global.mesh);
                }
            }
        }
        else
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                image_self = GetComponent<Image>();
                image_self.color = yellow;

                Global.imagePosition = image_self.rectTransform.anchoredPosition;
            }

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                lastClickTime = Time.time;

                Vector2 imagePosition_Left = image_self.rectTransform.anchoredPosition;

                Global.index = 0;
                foreach (Vector2 position in Global.outsidePoints)
                {
                    if (position != imagePosition_Left)
                    {
                        Global.index++;
                    }
                    else
                    {
                        break;
                    }
                }

                Global.Width_Input.GetComponent<InputField>().text = Global.PScopy[Global.index][0].ToString();
                Global.Length_Input.GetComponent<InputField>().text = Global.PScopy[Global.index][1].ToString();
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            minDis = 1.0f;
            minIndex = 0;

            image_self = GetComponent<Image>();
            temp = image_self.color;

            dragIndex = GetControllerIndex(image_self.rectTransform.anchoredPosition);

            Global.ismove = true;
            Global.movingNode = image_self.gameObject;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            image_self = GetComponent<Image>();
            image_self.color = temp;
            Global.nodes[dragIndex] = image_self.rectTransform.anchoredPosition;

            Global.ismove = false;
            Global.movingNode = null;

            image = GameObject.Find("Boundary_Image_Garden");
            if (image == null)
            {
                image = GameObject.Find("Boundary_Image_Farm");
            }

            action = GameObject.Find("Action");
            splineContainer = action.GetComponent<SplineContainer>();

            if (Global.currentLayoutMode == Global.LayoutMode.Garden)
            {
                RemoveAllEdgeClones(image.transform);

                Global.edges.Clear();
                Global.triangles.Clear();
                Global.circumCenter.Clear();
                Global.edgePairs.Clear();
                Global.isVoronoi = false;

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
            }

            if (Global.currentLayoutMode == Global.LayoutMode.Farmland)
            {
                Global.lower.Clear();
                Global.upper.Clear();
                Global.farmlandLine.Clear();
                Global.isGA = false;
            }

            Global.roadInstances.Clear();

            if (Global.currentLayoutMode == Global.LayoutMode.Garden)
            {
                Function.GenerateTriangle(Global.nodes);

                for (int i = 0; i < Global.edges.Count / 2; i++)
                {
                    GenerateLine(Global.edges[2 * i].vertex_1, Global.edges[2 * i].vertex_2);
                }

                Function.GenerateVoronoi(Global.mesh);
            }
            else if (Global.currentLayoutMode == Global.LayoutMode.Farmland)
            {
                if (CheckEdgeClonesExist(image.transform))
                {
                    Function.GenerateTriangle(Global.nodes);

                    for (int i = 0; i < Global.edges.Count / 2; i++)
                    {
                        GenerateLine(Global.edges[2 * i].vertex_1, Global.edges[2 * i].vertex_2);
                    }
                }
            }
        }
    }

    private int GetControllerIndex(Vector2 position)
    {
        for (int i = 0; i < Global.nodes.Count; i++)
        {
            float dis = (Global.nodes[i] - position).magnitude;

            if (dis < minDis)
            {
                minDis = dis;
                minIndex = i;
            }
        }

        return minIndex;
    }

    public void RemoveAllEdgeClones(Transform parent)
    {
        // 遍历子物体
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            // 如果是名为Edge(Clone)的GameObject，则删除
            if (child.name == "Edge(Clone)" || child.name == "Edge_GA(Clone)")
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

    private void GenerateLine(Vector2 startVertex, Vector2 endVertex)
    {
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

    private void DeleteLine(Vector2 startVertex, Vector2 endVertex)
    {
        float positionX = (startVertex.x + endVertex.x) / 2;
        float positionY = (startVertex.y + endVertex.y) / 2;

        Transform uiTransform = transform.parent;

        for (int i = uiTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = uiTransform.GetChild(i);
            if (child.name == "Edge(Clone)")
            {
                RectTransform rectTransform = child.GetComponent<RectTransform>();
                if (rectTransform != null && rectTransform.anchoredPosition == new Vector2(positionX, positionY))
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    public bool CheckEdgeClonesExist(Transform parent)
    {
        // 遍历子物体
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            // 如果是名为Edge(Clone)的GameObject，则删除
            if (child.name == "Edge(Clone)")
            {
                return true;
            }
            else
            {
                // 递归查找子物体
                RemoveAllEdgeClones(child);
            }
        }

        return false;
    }
}
