using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GodPerspective : MonoBehaviour
{
    // 摄像机移动速度
    private float moveSpeed = 20.0f;
    private float rotateSpeed = 5.0f;

    private float zoomSpeed = 1.0f;
    float mouseScrollDelta = 0.0f;
    private float minFOV = 20.0f;
    private float maxFOV = 80.0f;

    private float rotationSpeed = 1.0f; // 旋转速度
    private float radius = 60f; // 半径：sqrt(4900) = 70
    private float yPosition = 30f; // 固定y坐标
    private Vector3 circleCenter = new Vector3(50, 0, 50); // 圆心 (50, 50, 50)

    private float angle = 0f; // 当前角度，用于计算圆周上的位置
    private bool isRotating = false; // 是否在旋转

    private void Update()
    {
        if (Global.viewModel) return;

        // 按下P键开始或停止旋转
        if (Input.GetKeyDown(KeyCode.P))
        {
            isRotating = !isRotating;

            // 当按下P键并开始旋转时，设置摄像机到初始位置
            if (isRotating)
            {
                // 设置摄像机在圆周上的初始位置，y=150
                transform.position = new Vector3(circleCenter.x + radius, yPosition, circleCenter.z);
                angle = 0f; // 初始化角度为0
            }
        }

        // 如果处于旋转状态
        if (isRotating)
        {
            // 顺时针旋转：更新角度，角度递减
            angle -= rotationSpeed * Time.deltaTime;

            // 计算新的x和z位置，使用三角函数计算相对于圆心的位置
            float x = Mathf.Cos(angle) * radius + circleCenter.x;
            float z = Mathf.Sin(angle) * radius + circleCenter.z;

            // 更新摄像机的位置，保持y为150
            transform.position = new Vector3(x, yPosition, z);
            transform.LookAt(circleCenter);
        }

        // 调整摄像机视角
        if (Input.GetMouseButtonDown(1))
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (Input.GetMouseButtonUp(1))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // 鼠标右键为旋转总开关，控制旋转视角
        if (!Cursor.visible)
        {
            // 获取鼠标横向偏移量
            float hor = Input.GetAxis("Mouse X");
            if (hor != 0)
                transform.RotateAround(transform.position, Vector3.up, hor * rotateSpeed);

            // 获取鼠标竖向偏移量
            float ver = Input.GetAxis("Mouse Y");
            if (ver != 0)
                transform.RotateAround(transform.position, transform.right, -ver * rotateSpeed);
        }

        // 控制摄像机水平旋转
        if (Input.GetKey(KeyCode.Q))
        {
            transform.eulerAngles += new Vector3(0.0f, -0.1f, 0.0f);
        }

        if (Input.GetKey(KeyCode.E))
        {
            transform.eulerAngles += new Vector3(0.0f, 0.1f, 0.0f);
        }

        if (Input.mouseScrollDelta.y != 0.0f)
        {
            mouseScrollDelta = Input.mouseScrollDelta.y;
            float newFOV = Camera.main.fieldOfView - mouseScrollDelta * zoomSpeed;

            newFOV = Mathf.Clamp(newFOV, minFOV, maxFOV);
            Camera.main.fieldOfView = newFOV;
        }

        // 物体的3D坐标由X、Y、Z三轴记录
        // 创建读取并存储三个位置变化的变量
        // Input输入，GetAxis获取轴向，并在双引号内指明
        float xAxis = Input.GetAxis("Horizontal");
        float zAxis = Input.GetAxis("Vertical");

        // 若X、Z没有变化，则不执行后续操作
        if (xAxis == 0 && zAxis == 0) return;

        // Time.deltaTime为Unity提供一个时间修正
        // 偏移量恰好等于moveSpeed
        float xOffset = xAxis * moveSpeed * Time.deltaTime;
        float zOffset = zAxis * moveSpeed * Time.deltaTime;

        // 控制场景里实例的位置，依靠Transform里的Position
        // 不改变Y轴的位置，给予0的变化
        transform.Translate(xOffset, 0, zOffset);
    }
}
