using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Selection : MonoBehaviour
{
    private bool isDragging = false;

    private Vector3 offset;

    private float minDis = 1.0f;
    private int minIndex;
    private int dragIndex;

    private float rotationSpeed = 50.0f;
    private float currentRotation = 180.0f;

    private void OnMouseDown()
    {
        dragIndex = GetControllerIndex(transform.position);

        isDragging = true;

        offset = transform.position - GetMouseWorldPos();
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }

    private void Update()
    {
        if (isDragging)
        {
            Vector3 targetPos = GetMouseWorldPos() + offset;
            transform.position = new Vector3(targetPos.x, Terrain.activeTerrain.SampleHeight(targetPos), targetPos.z);
            Global.mainBuildingData[dragIndex].position = transform.position;
        }

        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity) && hit.collider.gameObject == gameObject)
            {
                Global.mainBuildingData.RemoveAt(dragIndex);
                Destroy(gameObject);
            }
        }

        if (Global.rotatablePrefab == gameObject)
        {
            if (Input.GetKey(KeyCode.Z))
            {
                currentRotation += rotationSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Euler(0, currentRotation, 0);
                Global.mainBuildingData[dragIndex].angle = currentRotation;
            }

            if (Input.GetKey(KeyCode.X))
            {
                currentRotation -= rotationSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Euler(0, currentRotation, 0);
                Global.mainBuildingData[dragIndex].angle = currentRotation;
            }
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            return hit.point;
        }
        return Vector3.zero;
    }

    private int GetControllerIndex(Vector3 position)
    {
        for (int i = 0; i < Global.mainBuildingData.Count; i++)
        {
            float dis = (Global.mainBuildingData[i].position - position).magnitude;

            if (dis < minDis)
            {
                minDis = dis;
                minIndex = i;
            }
        }

        return minIndex;
    }
}
