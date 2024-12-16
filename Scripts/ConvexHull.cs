using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConvexHull : MonoBehaviour
{
    public static List<Vector2> GetConvexHull(List<Vector2> points)
    {
        // Step 1: Find the point with the lowest y-coordinate, break ties by x-coordinate
        Vector2 start = points.OrderBy(p => p.y).ThenBy(p => p.x).First();

        // Step 2: Sort points by polar angle with start point
        var sortedPoints = points.OrderBy(p => Math.Atan2(p.y - start.y, p.x - start.x)).ToList();

        // Step 3: Use a stack to process the points and form the convex hull
        Stack<Vector2> hull = new Stack<Vector2>();
        hull.Push(start);
        hull.Push(sortedPoints[1]);

        for (int i = 2; i < sortedPoints.Count; i++)
        {
            Vector2 top = hull.Pop();
            while (IsCounterClockwise(hull.Peek(), top, sortedPoints[i]) <= 0)
            {
                top = hull.Pop();
            }
            hull.Push(top);
            hull.Push(sortedPoints[i]);
        }

        return hull.ToList();
    }

    public static float IsCounterClockwise(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p2.x - p1.x) * (p3.y - p1.y) - (p2.y - p1.y) * (p3.x - p1.x);
    }
}
