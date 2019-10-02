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

public class BrushDrawingInfo : DrawingInfo
{
    public List<Vector3> points = new List<Vector3>();
}

public class BrushTool : DrawingTool
{
    public override event UnityAction<DrawingInfo> OnDraw;

    private BrushDrawingInfo info = new BrushDrawingInfo();


    //
    // Unity Methods
    //

    protected override void Awake()
    {
        base.Awake();

        info.points.Add(Vector3.zero);
    }


    //
    // Events
    //

    protected override void OnLeftMouseDragStart()
    {
        if (canDraw && !inputHandler.IsDraggingRight)
        {
            isDrawing = true;
        }
    }

    protected override void OnLeftMouseDrag()
    {
        if (isDrawing && canDraw)
        {
            Draw();
        }
    }

    protected override void OnLeftMouseDragEnd()
    {
        if (isDrawing)
        {
            isDrawing = false;

            if (canDraw)
            {
                Draw();
            }
        }
    }

    protected override void OnLeftMouseUp()
    {
        if (!inputHandler.IsDraggingLeft)
        {
            if (canDraw)
            {
                Draw();
            }
        }
    }


    //
    // Private Methods
    //

    private void Draw()
    {
        Vector3 worldPos;
        inputHandler.GetWorldPoint(Input.mousePosition, out worldPos);
        info.points[0] = worldPos;

        var delta = inputHandler.MouseDelta;
        int segments = Mathf.FloorToInt(delta.magnitude * 0.1f);
        if (segments > 1)
        {
            var offset = delta / segments;
            Vector3 pos = inputHandler.PreviousMousePos;
            for (int i = 1; i < segments; i++)
            {
                pos += offset;
                inputHandler.GetWorldPoint(pos, out worldPos);
                info.points.Add(worldPos);
            }
        }

        if (OnDraw != null)
            OnDraw(info);

        if (info.points.Count > 1)
        {
            info.points.RemoveRange(1, info.points.Count - 1);
        }
    }
}

