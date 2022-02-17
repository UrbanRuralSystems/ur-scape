// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System;
using UnityEngine;

using LineInfo = LineInspector.LineInspectorInfo;

public class InspectionLine : MonoBehaviour
{
	[Header("Miscellaneous")]
    public float midPtOffset = 15.0f;

    // Component References
    private InputHandler inputHandler;
    private InspectorTool inspectorTool;
    private LineInfo lineInfo;
    private LineInspectorPanel lineInspectorPanel;
    private LineInspector lineInspector;

    // Constants
    private const int StartPtIndex = 0;
    private const int EndPtIndex = 1;

    private enum DragType
    {
        None,
        Move
    }
    private DragType dragType = DragType.None;
    private Vector3 dragOrigin;

    //
    // Unity Methods
    //

    private void Start()
    {
        inputHandler = ComponentManager.Instance.Get<InputHandler>();
        inspectorTool = ComponentManager.Instance.Get<InspectorTool>();
        lineInspectorPanel = inspectorTool.lineInspectorPanel;
        lineInspector = lineInspectorPanel.lineInspector;
    }

    private void Update()
    {
        // Incomplete line or in delete or draw mode
        if (lineInfo.controlPts.Count < 2 || lineInspectorPanel.removeLineInspectionToggle.isOn ||
            lineInspectorPanel.createLineInspectionToggle.isOn || lineInspector.CurrLineInspection == -1)
            return;

        var mousePosition = Input.mousePosition;
        bool isCursorWithinMapViewArea = inspectorTool.IsPosWithinMapViewArea(mousePosition);
        ShowControlPoints(isCursorWithinMapViewArea);

        // Inspection line is currently selected inspection line
        if (lineInfo == lineInspectorPanel.lineInfos[lineInspector.CurrLineInspection])
        {
            if (lineInspector.IsCursorNearLine(lineInfo, mousePosition, midPtOffset))
            {
                RequestToMoveLine();
                UpdateTransectHighlight();
            }
            else
            {
                inspectorTool.SetCursorTexture(inspectorTool.cursorDefault);
                lineInspectorPanel.transectController.ShowHighlight(false);
            }
        }
        else
        {
            if (lineInspector.IsCursorNearLine(lineInfo, mousePosition, midPtOffset) && isCursorWithinMapViewArea)
            {
                lineInspectorPanel.ChangeKnobsAndLine(lineInfo, false);
                if (inputHandler.IsLeftMouseDown && !inputHandler.IsDraggingLeft)
                    SelectLine();
            }
            else
                lineInspectorPanel.ChangeKnobsAndLine(lineInfo, true);
        }
    }

    //
    // Event Methods
    //

    private void OnLeftMouseDragStart()
    {
        if (dragType == DragType.None)
        {
            dragType = DragType.Move;
            StartMove();
        }
    }

    private void OnLeftMouseDrag()
    {
        if (dragType == DragType.Move)
        {
            float deltaX = inputHandler.MouseDelta.x;
            float deltaY = inputHandler.MouseDelta.y;
            if (Math.Abs(deltaX) > 0 || Math.Abs(deltaY) > 0)
            {
                MoveLine();
            }
        }
    }

    private void OnLeftMouseDragEnd()
    {
        if (dragType == DragType.Move)
        {
            inputHandler.OnLeftMouseDragStart -= OnLeftMouseDragStart;
            inputHandler.OnLeftMouseDrag -= OnLeftMouseDrag;
            inputHandler.OnLeftMouseDragEnd -= OnLeftMouseDragEnd;

            dragType = DragType.None;
        }
    }

    //
    // Public Methods
    //

    public void SetLineInfo(LineInfo lineInfo)
    {
        this.lineInfo = lineInfo;
    }

    //
    // Private Methods
    //

    private void RequestToMoveLine()
    {
        // Moving a line is requested in the following cases:
        // - Cursor is near the currently selected line
#if UNITY_EDITOR
        if (Application.isPlaying)
#endif
        {
            inspectorTool.SetCursorTexture(inspectorTool.cursorMove);

            inputHandler.OnLeftMouseDragStart += OnLeftMouseDragStart;
            inputHandler.OnLeftMouseDrag += OnLeftMouseDrag;
            inputHandler.OnLeftMouseDragEnd += OnLeftMouseDragEnd;
        }
    }

    private void StartMove()
    {
        dragOrigin = Input.mousePosition;
    }

    private void MoveLine()
    {
        Vector3 point = Input.mousePosition;
        var offset = point - dragOrigin;

        var startPt = lineInfo.controlPts[StartPtIndex] as EndPt;
        var endPt = lineInfo.controlPts[EndPtIndex] as EndPt;

        var targetStartPtPos = startPt.transform.position + new Vector3(offset.x, offset.y, 0.0f);
        var targetEndPtPos = endPt.transform.position + new Vector3(offset.x, offset.y, 0.0f);

        if (inspectorTool.IsPosWithinMapViewArea(targetStartPtPos) && inspectorTool.IsPosWithinMapViewArea(targetEndPtPos))
        {
            startPt.UpdatePosition(targetStartPtPos);
            endPt.UpdatePosition(targetEndPtPos);

            lineInspectorPanel.SetCurrInspection(lineInfo.controlPts[StartPtIndex].InspectionIndex);
        }

        dragOrigin = point;
    }

    private void SelectLine()
    {
        lineInspectorPanel.SetCurrInspection(lineInfo.controlPts[StartPtIndex].InspectionIndex);
		lineInspectorPanel.ComputeAndUpdateLineProperties();
    }

    private void UpdateTransectHighlight()
    {
        inputHandler.GetWorldPoint(Input.mousePosition, out Vector3 point);
        float percent = Vector3.Distance(point, lineInfo.line.GetPosition(0)) / Vector3.Distance(lineInfo.line.GetPosition(1), lineInfo.line.GetPosition(0));
        lineInspectorPanel.transectController.UpdateHighlightPos(percent);
        lineInspectorPanel.transectController.ShowHighlight(true);
    }

    private void ShowControlPoints(bool show)
    {
        foreach (var controlPt in lineInfo.controlPts)
        {
            controlPt.gameObject.SetActive(show);
        }
    }
}