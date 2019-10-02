// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LassoDrawingInfo : DrawingInfo
{
    public List<Vector3> points = new List<Vector3>();
    public Vector3 min;
    public Vector3 max;
}

[RequireComponent(typeof(LineRenderer))]
public class LassoTool : DrawingTool
{
    private static readonly float DoubleClickTime = 0.2f;
    private static readonly float PointsMaxDistance = 5f;
    private static readonly float LinesMaxAngle = 3f;
    private static readonly float LinesMaxAngleCos = Mathf.Cos(LinesMaxAngle * Mathf.Deg2Rad);
    private static readonly float SimplifyMaxDistance = 0.015f;

    public enum LassoMethod
    {
        None,
        Clicking,
        Dragging,
    }


    public override event UnityAction<DrawingInfo> OnDraw;

    // Component references
    private LineRenderer lineRenderer;

    // Misc
    private LassoMethod method = LassoMethod.None;
    private float lastClickTime = 0;

    private readonly LassoDrawingInfo info = new LassoDrawingInfo();

    private Vector3 firstPoint;
    private Vector3 secondLastPoint;
    private Vector3 thirdLastPoint;


    //
    // Unity Methods
    //

    protected override void Awake()
    {
        base.Awake();
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (method == LassoMethod.Clicking)
        {
            UpdatePoint(info.points.Count - 1);
        }
    }

    //
    // Inheritance Methods
    //

    protected override void DisableInput()
    {
        base.DisableInput();

        AllowOtherInput();
    }

    protected override void OnLeftMouseUp()
    {
        // Check if it was a left-click without drag
        if (canDraw && !inputHandler.IsDraggingLeft && !inputHandler.IsDraggingRight)
        {
            switch (method)
            {
                case LassoMethod.None:
                    StartLasso(LassoMethod.Clicking);
                    break;

                case LassoMethod.Clicking:
                    // Check if it's a double-click or if sigle-click happened close to the first point
                    if (info.points.Count > 2 && (Time.time - lastClickTime <= DoubleClickTime || IsNearFirstPoint()))
                    {
                        FinishLasso();
                    }
                    else
                    {
                        UpdatePoint(info.points.Count - 1);
                        AddPoint();
                        CheckLoop();
                    }
                    lastClickTime = Time.time;
                    break;
            }
        }
    }

    protected override void OnLeftMouseDragStart()
    {
        if (canDraw && !inputHandler.IsDraggingRight && method == LassoMethod.None)
        {
            StartLasso(LassoMethod.Dragging);
        }
    }

    protected override void OnLeftMouseDrag()
    {
        if (method == LassoMethod.Dragging)
        {
            // Update last point with mouse position
            UpdatePoint(info.points.Count - 1);

            var v1 = Input.mousePosition - secondLastPoint;
            if (v1.magnitude >= PointsMaxDistance)
            {
                if (info.points.Count < 3)
                {
                    AddPoint();
                }
                else
                {
                    Vector3 v2 = secondLastPoint - thirdLastPoint;
                    float cos = Vector3.Dot(v1, v2) / (v1.magnitude * v2.magnitude);
                    if (cos <= LinesMaxAngleCos)
                    {
                        AddPoint();
                    }
                    else
                    {
                        // Update second last point with mouse position
                        UpdatePoint(info.points.Count - 2);
                        secondLastPoint = Input.mousePosition;
                    }
                    CheckLoop();
                }
            }
        }
    }

    protected override void OnLeftMouseDragEnd()
    {
        if (method == LassoMethod.Dragging)
        {
            FinishLasso();
        }
    }

    protected override void OnRightMouseUp()
    {
        if (method != LassoMethod.None)
        {
            CleanUpLasso();
        }
        else
        {
            base.OnRightMouseUp();
        }
    }


    //
    // Private Methods
    //

    private void AllowOtherInput()
    {
        inputHandler.OnRightMouseDown -= NoOp;
        inputHandler.OnRightMouseDragStart -= NoOp;
        inputHandler.OnRightMouseDrag -= NoOp;
        inputHandler.OnMouseWheel -= NoOp;
    }

    private void DisallowOtherInput()
    {
        inputHandler.OnRightMouseDown += NoOp;
        inputHandler.OnRightMouseDragStart += NoOp;
        inputHandler.OnRightMouseDrag += NoOp;
        inputHandler.OnMouseWheel += NoOp;
    }

    private void NoOp() { }
    private void NoOp(float value) { }

    private bool IsNearFirstPoint()
    {
        return
            Math.Abs(Input.mousePosition.x - firstPoint.x) <= inputHandler.PixelDragThreshold &&
            Math.Abs(Input.mousePosition.y - firstPoint.y) <= inputHandler.PixelDragThreshold;
    }

    private void StartLasso(LassoMethod newMethod)
    {
        DisallowOtherInput();

        isDrawing = true;
        method = newMethod;

        firstPoint = thirdLastPoint = secondLastPoint = Input.mousePosition;
        lineRenderer.positionCount = 0;
        info.points.Clear();

        AddPoint();
        AddPoint();
    }

    private void AddPoint()
    {
        AddPoint(Input.mousePosition);
    }

    private void AddPoint(Vector3 position)
    {
        Vector3 point;
        inputHandler.GetWorldPoint(position, out point);

        lineRenderer.positionCount++;
        lineRenderer.SetPosition(info.points.Count, point);
        info.points.Add(point);

        thirdLastPoint = secondLastPoint;
        secondLastPoint = position;
    }

    private void UpdatePoint(int index)
    {
        Vector3 point;
        inputHandler.GetWorldPoint(Input.mousePosition, out point);

        lineRenderer.SetPosition(index, point);
        info.points[index] = point;
    }

    private void FinishLasso()
    {
        lineRenderer.positionCount--;
        info.points.RemoveAt(info.points.Count - 1);

        AddPoint(firstPoint);
        AddPoint(firstPoint);

        CheckLoop();

        info.points.RemoveAt(info.points.Count - 1);

        SimplifyLasso();

        if (OnDraw != null)
            OnDraw(info);

        CleanUpLasso();
    }

    private void CleanUpLasso()
    {
        method = LassoMethod.None;
        lineRenderer.positionCount = 0;
        isDrawing = false;

        AllowOtherInput();
    }

    private void CheckLoop()
    {
        var points = info.points;
        int lastIndex = points.Count - 2;
        int secondLastIndex = lastIndex - 1;
        Vector3 last = points[lastIndex];
        Vector3 secondLast = points[secondLastIndex];
        Vector3 intersection = Vector3.zero;
        for (int i = secondLastIndex - 1; i > 0; i--)
        {
            if (LineIntersection(points[i - 1], points[i], secondLast, last, ref intersection))
            {
                Vector3 i1 = (points[i - 1] + secondLast) * 0.5f;
                Vector3 i2 = (points[i] + last) * 0.5f;
                i1 = Vector3.Lerp(i1, intersection, 0.8f);
                i2 = Vector3.Lerp(i2, intersection, 0.8f);

                // Duplicate the last 2 points
                points.Add(last);
                points.Add(points[lastIndex + 1]);
                int count = lineRenderer.positionCount + 2;
                lineRenderer.positionCount = count;
                lineRenderer.SetPosition(count - 1, lineRenderer.GetPosition(count - 3));
                lineRenderer.SetPosition(count - 2, lineRenderer.GetPosition(count - 4));

                // Reverse loop points
                for (int j = i, k = secondLastIndex; j < k; j++, k--)
                {
                    Vector3 temp = points[j];
                    points[j] = points[k];
                    points[k] = temp;
                    lineRenderer.SetPosition(j, lineRenderer.GetPosition(k));
                    lineRenderer.SetPosition(k, temp);
                }

                // Shift loop points
                for (int j = lastIndex; j > i; j--)
                {
                    points[j] = points[j - 1];
                    lineRenderer.SetPosition(j, lineRenderer.GetPosition(j - 1));
                }

                // Set intersection points
                points[i] = i1;
                points[lastIndex + 1] = i2;
                lineRenderer.SetPosition(i, i1);
                lineRenderer.SetPosition(lastIndex + 1, i2);

                // Reset initial variables
                lastIndex = points.Count - 2;
                secondLastIndex = lastIndex - 1;
                last = points[lastIndex];
                secondLast = points[secondLastIndex];
            }
        }
    }

    private void SimplifyLasso()
    {
        int i1 = 0;
        Vector3 p1 = info.points[i1];

        info.min = info.max = p1;

        int last = info.points.Count;
        for (int i2 = 2; i2 < last; i2++)
        {
            Vector3 p2 = info.points[i2];

            for (int i = i1 + 1; i < i2; i++)
            {
                Vector3 p = info.points[i];
                float distance = DistancePointToLine(p, p1, p2);
                if (distance > SimplifyMaxDistance)
                {
                    int removeCount = i2 - i1 - 2;
                    if (removeCount > 0)
                    {
                        info.points.RemoveRange(i1 + 1, removeCount);
                        last = info.points.Count;
                        i2 -= removeCount;
                    }

                    i1 = i2 - 1;
                    p1 = info.points[i1];

                    info.min = Vector3.Min(info.min, p1);
                    info.max = Vector3.Max(info.max, p1);
                    break;
                }
            }
        }
    }

    private static float DistancePointToLine(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 AB = b - a;
        float distance = Vector3.Dot(p - a, AB) / AB.sqrMagnitude;

        if (distance <= 0)
            return Vector3.Distance(p, a);
        else if (distance >= 1)
            return Vector3.Distance(p, b);
        else
            return Vector3.Distance(p, a + AB * distance); 
    }

    private static bool LineIntersection(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, ref Vector3 intersection)
    {
        Vector3 vec1 = p2 - p1;
        Vector3 vec2 = p4 - p3;

        float a = Vector3.Dot(vec1, vec1);
        float b = Vector3.Dot(vec1, vec2);
        float e = Vector3.Dot(vec2, vec2);

        float d = a * e - b * b;
        if (Math.Abs(d) <= 0.000000001f)
            return false;

        Vector3 r = p1 - p3;
        float c = Vector3.Dot(vec1, r);
        float f = Vector3.Dot(vec2, r);

        float s = (b * f - c * e) / d;
        float t = (a * f - c * b) / d;

        intersection = p1 + vec1 * s;

        return s > 0f && s < 1f && t > 0f && t < 1f;
    }

}
