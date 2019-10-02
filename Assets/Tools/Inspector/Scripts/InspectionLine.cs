// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;
using UnityEngine.EventSystems;

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
    private const int MidPtIndex = 2;

    private bool needsUpdate = false;

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
        if (lineInfo.controlPts.Count < 3 || lineInspectorPanel.removeLineInspectionToggle.isOn ||
            lineInspectorPanel.createLineInspectionToggle.isOn || lineInspector.CurrLineInspection == -1)
            return;

        // Inspection line is currently selected inspection line
        if (lineInfo == lineInspectorPanel.lineInfos[lineInspector.CurrLineInspection])
        {
            if (lineInspector.IsCursorNearLine(lineInfo, Input.mousePosition, midPtOffset))
                RequestToMoveLine();
            else
                inspectorTool.SetCursorTexture(inspectorTool.cursorDefault);
        }
        else
        {
            if (lineInspector.IsCursorNearLine(lineInfo, Input.mousePosition, midPtOffset) &&
                inspectorTool.IsPosWithinMapViewArea(Input.mousePosition))
            {
                lineInspectorPanel.ChangeKnobsAndLine(lineInfo, false);
                if (inputHandler.IsLeftMouseDown)
                    SelectLine();
            }
            else
                lineInspectorPanel.ChangeKnobsAndLine(lineInfo, true);
        }
    }

    private void LateUpdate()
    {
        if(needsUpdate)
        {
            needsUpdate = false;

            MoveLine();
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
            needsUpdate = true;
        }
    }

    private void MoveLine()
    {
        inspectorTool.SetCursorTexture(inspectorTool.cursorMove);
        lineInspector.UpdateMidPtPos(lineInfo);
    }

    private void SelectLine()
    {
        lineInspectorPanel.SetCurrInspection(lineInfo.controlPts[StartPtIndex].InspectionIndex);
		lineInspectorPanel.ComputeAndUpdateLineProperties();
    }
}