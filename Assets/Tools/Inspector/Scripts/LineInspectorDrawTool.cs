// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(LineRenderer))]
public class LineInspectorDrawTool : DrawingTool
{
    private static readonly float DoubleClickTime = 0.2f;

    public event UnityAction<List<Coordinate>> OnFinishDrawing;

    public enum Method
    {
        None,
        Clicking
    }

    // Component references
    private LineRenderer lineRenderer;

    // Misc
    private Method forcedMethod = Method.None;
    private Method method = Method.None;
    private float lastClickTime = 0;
    private Patch patch;
    protected MapController map;

    // Control points
    private int endPtCount = 0;
    private const int maxEndPtCount = 2;

    private readonly List<Coordinate> points = new List<Coordinate>();

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
        if (method == Method.Clicking)
        {
            UpdatePoint(points.Count - 1);
        }
    }

    //
    // Inheritance Methods
    //

    protected override void OnLeftMouseUp()
    {
        if (patch == null)
        {
            Debug.LogError("No available site to set the start point");
            return;
        }

        switch (method)
        {
            case Method.None:
                if (forcedMethod == Method.None || forcedMethod == Method.Clicking)
                    StartDraw(Method.Clicking);
                break;

            case Method.Clicking:
                if (!(Time.time - lastClickTime <= DoubleClickTime))
                    ++endPtCount;

                if (endPtCount == maxEndPtCount)
                {
                    FinishDraw();
                }
                break;
        }
    }

    protected override void OnLeftMouseDrag()
    {
    }

    protected override void OnLeftMouseDragStart()
    {
    }

    protected override void OnLeftMouseDragEnd()
    {
    }

    //
    // Public Methods
    //

    public void Init(Patch patch, MapController map)
    {
        this.patch = patch;
        this.map = map;

        lineRenderer.positionCount = 0;
    }

    public void ForceDrawingMethod(Method m)
    {
        forcedMethod = m;
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

    private void StartDraw(Method method)
    {
        isDrawing = true;
        DisallowOtherInput();

        lineRenderer.positionCount = 0;
        points.Clear();

        this.method = method;

        AddPoint();
        AddPoint();

        ++endPtCount;
        lastClickTime = Time.time;
    }

    private void AddPoint()
    {
        AddPoint(Input.mousePosition);
    }

    private void AddPoint(Vector3 position)
    {
        Vector3 point;
        inputHandler.GetWorldPoint(position, out point);
        Coordinate coorPoint = map.GetCoordinatesFromUnits(point.x, point.z);

        if (!IsInsidePatch(coorPoint.Longitude, coorPoint.Latitude))
        {
            lineRenderer.positionCount = 0;
            points.Clear();
            method = Method.None;
            Debug.LogError("No available site to set the point");
        }
        else
        {
            lineRenderer.positionCount++;
            lineRenderer.SetPosition(points.Count, point);
            points.Add(coorPoint);
        }
    }

    private void UpdatePoint(int index)
    {
        Vector3 point;
        inputHandler.GetWorldPoint(Input.mousePosition, out point);

        lineRenderer.SetPosition(index, point);
        Coordinate coorPoint = map.GetCoordinatesFromUnits(point.x, point.z);

        points[index] = coorPoint;
    }

    private void FinishDraw()
    {
        // Reset
        ResetDrawing();

        if (OnFinishDrawing != null)
            OnFinishDrawing(points);

        AllowOtherInput();

        endPtCount = 0;
    }

    private void ResetDrawing()
    {
        isDrawing = false;
        method = Method.None;
        lineRenderer.positionCount = 0;
    }

    private bool IsInsidePatch(double longitude, double latitude)
    {
        if (!(patch is GridedPatch))
            return false;

        GridData g = (patch as GridedPatch).grid;
        return longitude >= g.west && longitude <= g.east && latitude >= g.south && latitude <= g.north;
    }
}
