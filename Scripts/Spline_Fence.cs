using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Splines;
using UnityEngine;

public class Spline_Fence : SplineComponent
{
    public GameObject fence;

    private SplineContainer splineContainer;
    private Spline spline;

    private Vector3 fenceColliderSize;

    private int index = 0;
    private int indexOffset = 0;

    private float Spacing = 1.0f;
    private float splineLength = 0.0f;
    private float currentDist = 0.0f;
    private float k_Epsilon = 0.001f;

    private List<float> TimesCache = new();

    void Start()
    {
        BoxCollider fenceBoxCollider = fence.GetComponent<BoxCollider>();
        fenceColliderSize = fenceBoxCollider.size;

        splineContainer = GetComponent<SplineContainer>();

        Spacing = fenceColliderSize.z - 0.1f;
    }

    void Update()
    {
        index = 0;
        indexOffset = 0;
        splineLength = 0.0f;

        if (splineContainer.Spline != null)
        {
            splineLength = splineContainer.CalculateLength(0);
            spline = splineContainer.Splines[0];

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
                GameObject instance = Global.fenceInstances[i];
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
        if (index >= Global.fenceInstances.Count)
        {
            Global.fenceInstances.Add(Instantiate(fence));
        }

        Global.fenceInstances[index].transform.localPosition = fence.transform.localPosition;
        Global.fenceInstances[index].transform.localRotation = fence.transform.localRotation;
        Global.fenceInstances[index].transform.localScale = fence.transform.localScale;

        return true;
    }
}
