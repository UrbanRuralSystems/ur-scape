// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(LineRenderer))]
public class LineTool : DrawingTool
{
    private static readonly float DoubleClickTime = 0.2f;
    private static readonly float PointsMaxDistance = 5f;
    private static readonly float LinesMaxAngle = 3f;
    private static readonly float LinesMaxAngleCos = Mathf.Cos(LinesMaxAngle * Mathf.Deg2Rad);

	public event UnityAction<List<Coordinate>> OnFinishDrawing;

    public enum Method
    {
        None,
        Clicking,
        Dragging,
    }

    // Component references
    private LineRenderer lineRenderer;

	// Misc
	private Method forcedMethod = Method.None;
    private Method method = Method.None;
    private float lastClickTime = 0;
    private Patch patch;
    protected MapController map;

    private readonly List<Coordinate> points = new List<Coordinate>();

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

		if (!inputHandler.IsDraggingLeft && !inputHandler.IsDraggingRight)
		{
			switch (method)
			{
				case Method.None:
					if (forcedMethod == Method.None || forcedMethod == Method.Clicking)
						StartDraw(Method.Clicking);
					break;

				case Method.Clicking:
					// Check if it's a double-click 
					if (points.Count > 2 && (Time.time - lastClickTime <= DoubleClickTime))
					{
						FinishDraw();
					}
					else
					{
						UpdatePoint(points.Count - 1);
						AddPoint();
					}
					lastClickTime = Time.time;
					break;
			}
		}
    }

    protected override void OnLeftMouseDrag()
    {
        if (method == Method.Dragging)
        {
            // Update last point with mouse position
            UpdatePoint(points.Count - 1);

            var v1 = Input.mousePosition - secondLastPoint;
            if (v1.magnitude >= PointsMaxDistance)
            {
                if (points.Count < 3)
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
                        UpdatePoint(points.Count - 2);
                        secondLastPoint = Input.mousePosition;
                    }
                }
            }
        }
    }

    protected override void OnLeftMouseDragStart()
    {
        if (!inputHandler.IsDraggingRight && method == Method.None)
        {
            StartDraw(Method.Dragging);
        }
    }

    protected override void OnLeftMouseDragEnd()
    {
        if (method == Method.Dragging)
        {
            FinishDraw();
        }
    }

    protected override void OnRightMouseUp()
    {
        if (method != Method.None)
        {
			// Resets the current drawing
			ResetDrawing();
			points.Clear();
		}
		else
        {
            base.OnRightMouseUp();
        }
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

        thirdLastPoint = secondLastPoint = Input.mousePosition;

        lineRenderer.positionCount = 0;
        points.Clear();

        this.method = method;

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

            thirdLastPoint = secondLastPoint;
            secondLastPoint = position;
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

		// Update roads layer
		points.RemoveAt(points.Count - 1);

		if (OnFinishDrawing != null)
			OnFinishDrawing(points);

		AllowOtherInput();
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
