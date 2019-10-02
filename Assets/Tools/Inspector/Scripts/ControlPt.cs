// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using LineInfo = LineInspector.LineInspectorInfo;

public abstract class ControlPt : MonoBehaviour
{
	protected int inspectionIndex = 0;
    public int InspectionIndex
    {
        get { return this.inspectionIndex; }
        set { this.inspectionIndex = value; }
    }

	protected int controlPtIndex = 0;
    public int ControlPointIndex
    {
        get { return this.controlPtIndex; }
        set { this.controlPtIndex = value; }
    }

	protected LineInfo lineInfo = null;
    public LineInfo LineInfo
    {
        get { return this.lineInfo; }
        set { this.lineInfo = value; }
    }

    protected Coordinate coords;
    public Coordinate Coords
    {
        get { return this.coords; }
        set { this.coords = value; }
    }

    // Component References
    protected InspectorTool inspectorTool;
    protected LineInspectorPanel lineInspectorPanel;
    protected LineInspector lineInspector;
    protected InputHandler inputHandler;
	protected Canvas canvas;

    protected virtual void Start()
    {
        inspectorTool = ComponentManager.Instance.Get<InspectorTool>();
        lineInspectorPanel = inspectorTool.lineInspectorPanel;
        lineInspector = lineInspectorPanel.lineInspector;
        inputHandler = ComponentManager.Instance.Get<InputHandler>();
		canvas = GameObject.FindGameObjectWithTag("Canvas").GetComponent<Canvas>();
	}

	protected abstract void UpdatePosOnDrag();

	protected void UpdateCurrLineInspectionIndex()
	{
		lineInspectorPanel.SetCurrInspection(inspectionIndex);
	}

	//
	// Public Methods
	//

	public virtual void UpdatePositionAndLinePtsFromCoord()
	{
	}
    
    public void SetCoord(Coordinate coords)
    {
        this.coords = coords;
    }

    public void Show(bool show)
    {
        gameObject.SetActive(show);
    }
}
