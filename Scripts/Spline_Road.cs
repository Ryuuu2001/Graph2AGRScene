using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Splines;
using UnityEngine;

public class Spline_Road : SplineComponent
{
    public GameObject road;

    private SplineContainer splineContainer;
    private Spline spline;

    private Vector3 roadColliderSize;

    private int index = 0;
    private int indexOffset = 0;

    private float Spacing = 1.0f;
    private float splineLength = 0.0f;
    private float currentDist = 0.0f;
    private float k_Epsilon = 0.001f;

    private List<float> TimesCache = new();

    void Start()
    {
        BoxCollider roadBoxCollider = road.GetComponent<BoxCollider>();
        roadColliderSize = roadBoxCollider.size;

        splineContainer = GetComponent<SplineContainer>();

        Spacing = roadColliderSize.z - 0.1f;
    }

    void Update()
    {
        index = 0;
        indexOffset = 0;
        splineLength = 0.0f;

        // 遍历除边界外的所有Spline，连接Knot并生成道路预制体
        for (int j = 1; j < splineContainer.Splines.Count; j++)
        {
            splineLength = splineContainer.CalculateLength(j);
            spline = splineContainer.Splines[j];

            NativeSpline nativeSpline = new NativeSpline(spline, splineContainer.transform.localToWorldMatrix, Allocator.TempJob);

            currentDist = 0f;

            TimesCache.Clear();

            while (currentDist <= (splineLength + k_Epsilon))
            {
                if (!SpawnPrefab(index))
                    break;

                TimesCache.Add(currentDist / splineLength);

                currentDist += Spacing;

                index++;
            }

            for (int i = indexOffset; i < index; i++)
            {
                GameObject instance = Global.roadInstances[i];
                float splineT = TimesCache[i - indexOffset];

                nativeSpline.Evaluate(splineT, out var position, out var direction, out var splineUp);
                instance.transform.position = position;

                float3 up = math.normalizesafe(splineUp);
                float3 forward = math.normalizesafe(direction);

                float3 remappedForward = math.normalizesafe(GetAxis(AlignAxis.ZAxis));
                float3 remappedUp = math.normalizesafe(GetAxis(AlignAxis.YAxis));
                Quaternion axisRemapRotation = Quaternion.Inverse(quaternion.LookRotationSafe(remappedForward, remappedUp));

                instance.transform.rotation = quaternion.LookRotationSafe(forward, up) * axisRemapRotation;
            }

            indexOffset = index;
        }
    }

    bool SpawnPrefab(int index)
    {
        if (index >= Global.roadInstances.Count)
        {
            Global.roadInstances.Add(Instantiate(road));
        }

        Global.roadInstances[index].transform.localPosition = road.transform.localPosition;
        Global.roadInstances[index].transform.localRotation = road.transform.localRotation;
        Global.roadInstances[index].transform.localScale = road.transform.localScale;

        return true;
    }
}
