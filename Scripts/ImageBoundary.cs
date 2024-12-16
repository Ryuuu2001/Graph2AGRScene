using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Splines;
using UnityEngine.UI;

public class ImageBoundary : MonoBehaviour, IPointerClickHandler
{
    public GameObject nodePrefab;
    public GameObject edgePrefab;

    private Texture2D texture;
    private Image image;
    private GameObject action;
    private SplineContainer splineContainer;

    private void OnEnable()
    {
        image = GetComponent<Image>();

        texture = new Texture2D((int)image.rectTransform.rect.width, (int)image.rectTransform.rect.height);
        image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

        ClearTexture();

        for (int i = 0; i < Global.boundary2D.Count - 1; i++)
        {
            DrawLine((int)Global.boundary2D[i].x, (int)Global.boundary2D[i].y, (int)Global.boundary2D[i + 1].x, (int)Global.boundary2D[i + 1].y, Color.black);
        }
        DrawLine((int)Global.boundary2D[^1].x, (int)Global.boundary2D[^1].y, (int)Global.boundary2D[0].x, (int)Global.boundary2D[0].y, Color.black);

        texture.Apply();
    }

    private void Update()
    {
        if (Global.ispair)
        {
            GenerateLine(Global.nodePairs[0], Global.nodePairs[1]);
        }

        if (Global.isexist)
        {
            DeleteLine(Global.nodePairs[0], Global.nodePairs[1]);

            Global.isexist = false;
            Global.nodePairs.Clear();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            Vector2 localCursor;
            Vector2 localCursor_new;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localCursor))
            {
                localCursor_new.x = localCursor.x + rectTransform.rect.width / 2f;
                localCursor_new.y = localCursor.y + rectTransform.rect.height / 2f;

                GraphicRaycaster raycaster = GetComponentInParent<GraphicRaycaster>();
                PointerEventData pointerEventData = new PointerEventData(EventSystem.current);
                pointerEventData.position = eventData.position;
                List<RaycastResult> results = new List<RaycastResult>();
                raycaster.Raycast(pointerEventData, results);

                bool clickedOnChildImage = false;
                foreach (RaycastResult result in results)
                {
                    if (result.gameObject.GetComponent<Image>() != null && result.gameObject != gameObject)
                    {
                        clickedOnChildImage = true;
                        break;
                    }
                }

                if (!clickedOnChildImage && Function.IsPointInPolygon2D(Global.boundary2D, localCursor_new))
                {
                    GameObject instantiatedPrefab = Instantiate(nodePrefab, transform);

                    RectTransform prefabRectTransform = instantiatedPrefab.GetComponent<RectTransform>();
                    prefabRectTransform.anchoredPosition = localCursor_new;

                    Global.nodes.Add(localCursor_new);
                }
            }

            if (CheckEdgeClonesExist(image.transform))
            {
                action = GameObject.Find("Action");
                splineContainer = action.GetComponent<SplineContainer>();

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

                Function.GenerateTriangle(Global.nodes);

                for (int i = 0; i < Global.edges.Count / 2; i++)
                {
                    GenerateLine(Global.edges[2 * i].vertex_1, Global.edges[2 * i].vertex_2);
                }

                Function.GenerateVoronoi(Global.mesh);
            }
        }
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

    private void ClearTexture()
    {
        Color[] clearColors = new Color[texture.width * texture.height];
        for (int i = 0; i < clearColors.Length; i++)
        {
            clearColors[i] = new Color(255, 242, 249);
        }
        texture.SetPixels(clearColors);
    }

    private void DrawLine(int startX, int startY, int endX, int endY, Color color)
    {
        int deltaX = Mathf.Abs(endX - startX);
        int deltaY = Mathf.Abs(endY - startY);
        int stepX = startX < endX ? 1 : -1;
        int stepY = startY < endY ? 1 : -1;
        int error = deltaX - deltaY;

        while (true)
        {
            texture.SetPixel(startX, startY, color);
            if (startX == endX && startY == endY) break;
            int error2 = error * 2;
            if (error2 > -deltaY)
            {
                error -= deltaY;
                startX += stepX;
            }
            if (error2 < deltaX)
            {
                error += deltaX;
                startY += stepY;
            }
        }
    }

    private void GenerateLine(Vector2 startVertex, Vector2 endVertex)
    {
        GameObject line = Instantiate(edgePrefab, transform);
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

        Transform uiTransform = transform;

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
}
