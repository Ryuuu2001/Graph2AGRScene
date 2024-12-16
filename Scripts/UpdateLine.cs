using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class UpdateLine : MonoBehaviour
{
    private Vector2 startVertex;
    private Vector2 endVertex;

    private GameObject startNode;
    private GameObject endNode;

    RectTransform moveTransform;

    private void Start()
    {
        if (Global.ispair)
        {
            startVertex = Global.nodePairs[0];
            endVertex = Global.nodePairs[1];
        }
        else
        {
            RectTransform rectTransform = GetComponent<RectTransform>();

            foreach (Global.EdgeData edge in Global.edges)
            {
                Vector2 middlePoint = (edge.vertex_1 + edge.vertex_2) / 2;
                if(rectTransform.anchoredPosition == middlePoint)
                {
                    startVertex = edge.vertex_1;
                    endVertex = edge.vertex_2;
                }
            }
        }

        Transform parent = transform.parent;
        foreach (Transform sibling in parent)
        {
            if (sibling.name == "Node(Clone)")
            {
                RectTransform rectTransform = sibling.GetComponent<RectTransform>();
                if (rectTransform != null && rectTransform.anchoredPosition == startVertex)
                {
                    startNode = sibling.gameObject;
                }
                if (rectTransform != null && rectTransform.anchoredPosition == endVertex)
                {
                    endNode = sibling.gameObject;
                }
            }
        }

        Global.ispair = false;
        Global.nodePairs.Clear();
    }

    private void Update()
    {
        if (Global.ismove)
        {
            if(startNode == Global.movingNode)
            {
                moveTransform = Global.movingNode.GetComponent<RectTransform>();

                Vector2 direction = endVertex - moveTransform.anchoredPosition;
                float distance = direction.magnitude;
                direction.Normalize();

                RectTransform rectTransform = GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(distance, 2);
                rectTransform.anchoredPosition = moveTransform.anchoredPosition + direction * distance / 2;

                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                rectTransform.rotation = Quaternion.Euler(0, 0, angle);

                foreach (Global.EdgeData edge in Global.edges)
                {
                    if (edge.vertex_1.Equals(startVertex))
                    {
                        edge.vertex_1 = moveTransform.anchoredPosition;
                    }

                    if (edge.vertex_2.Equals(startVertex))
                    {
                        edge.vertex_2 = moveTransform.anchoredPosition;
                    }
                }

                startVertex = moveTransform.anchoredPosition;
            }

            if (endNode == Global.movingNode)
            {
                moveTransform = Global.movingNode.GetComponent<RectTransform>();

                Vector2 direction = moveTransform.anchoredPosition - startVertex;
                float distance = direction.magnitude;
                direction.Normalize();

                RectTransform rectTransform = GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(distance, 2);
                rectTransform.anchoredPosition = startVertex + direction * distance / 2;

                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                rectTransform.rotation = Quaternion.Euler(0, 0, angle);

                foreach (Global.EdgeData edge in Global.edges)
                {
                    if (edge.vertex_1.Equals(endVertex))
                    {
                        edge.vertex_1 = moveTransform.anchoredPosition;
                    }

                    if (edge.vertex_2.Equals(endVertex))
                    {
                        edge.vertex_2 = moveTransform.anchoredPosition;
                    }
                }

                endVertex = moveTransform.anchoredPosition;
            }
        }
    }
}
