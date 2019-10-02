// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LineInspector {

	public class LineInspectorInfo
	{
        public InspectionDelete inspectionDelete;
		public RectTransform lineInspection;
		public List<ToggleButton> controlPtsTB;
		public List<ControlPt> controlPts;
		public List<Coordinate> coords;
		public LineRenderer line;
        public InspectionLine inspectionLine;
		public LineInspectionMapLayer mapLayer;
		public bool mapViewAreaChanged;
		public float scaleFactor;
	}
	
	// Constants
	private const int StartPtIndex = 0;
	private const int EndPtIndex = 1;
	private const int MidPtIndex = 2;

	// Component references
	private InputHandler inputHandler;
	private MapController map;
	private ToolLayerController toolLayers;
	private Canvas canvas;
	private LineInspectorPanel lineInspectorPanel;

	// Prefab references
	private ToggleButton endPtPrefab;
	private ToggleButton midPtPrefab;
	private ToggleButton inspectionDelPrefab;

    // Inspections count
    private int lineInspectionCount = 0;
	public int LineInspectionCount
	{
		get { return this.lineInspectionCount; }
		set { this.lineInspectionCount = value; }
	}

	private int currLineInspection = -1;
	public int CurrLineInspection
	{
		get { return this.currLineInspection; }
		set { this.currLineInspection = value; }
	}

	private int createdLineInspectionCount = 0;
	public int CreatedLineInspectionCount
	{
		get { return this.createdLineInspectionCount; }
		set { this.createdLineInspectionCount = value; }
	}

	private List<GridData> lineInspectorGrids = new List<GridData>();
	public List<GridData> LineInspectorGrids
	{
		get { return this.lineInspectorGrids; }
		set { this.lineInspectorGrids = value; }
	}

	//
	// Public Methods
	//

	public void Init(ToolLayerController toolLayers, ToggleButton endPtPrefab, ToggleButton midPtPrefab, ToggleButton inspectionDelPrefab, Canvas canvas)
	{
		inputHandler = ComponentManager.Instance.Get<InputHandler>();
		map = ComponentManager.Instance.Get<MapController>();
		lineInspectorPanel = ComponentManager.Instance.Get<InspectorTool>().lineInspectorPanel;

		this.toolLayers = toolLayers;
		this.canvas = canvas;

		this.endPtPrefab = endPtPrefab;
		this.midPtPrefab = midPtPrefab;
		this.inspectionDelPrefab = inspectionDelPrefab;
    }

	public bool IsCursorNearLine(LineInspectorInfo lineInfo, Vector3 cursorPos, float range)
	{
		//Vector3 startPt = lineInfo.controlPtsTB[StartPtIndex].transform.localPosition;
		//Vector3 endPt = lineInfo.controlPtsTB[EndPtIndex].transform.localPosition;

		Vector3 startPt = Camera.main.WorldToScreenPoint(lineInfo.line.GetPosition(StartPtIndex));
		Vector3 endPt = Camera.main.WorldToScreenPoint(lineInfo.line.GetPosition(EndPtIndex));

		float lineLength = (endPt - startPt).magnitude;

		Vector3 lineDir = (endPt - startPt).normalized;
		Vector3 startPtToCursor = cursorPos - startPt;
		float cursorProjLine = Vector3.Dot(startPtToCursor, lineDir);
		float offsetDist = (startPtToCursor - lineDir * cursorProjLine).magnitude;

		bool isCursorNearLine = (offsetDist <= range * canvas.scaleFactor && cursorProjLine > (0.1 * lineLength) && cursorProjLine <= (0.9 * lineLength));
		return isCursorNearLine;
	}

	public void RemoveLineInspectorInfoProperties(LineInspectorInfo lineInfo)
	{
		// Remove map layer
		if (lineInfo.mapLayer != null)
		{
			toolLayers.Remove(lineInfo.mapLayer);
			if (lineInfo.mapLayer.grids.Count > 0)
			{
				lineInspectorGrids.AddRange(lineInfo.mapLayer.grids);
				lineInfo.mapLayer.Clear();
			}
			GameObject.Destroy(lineInfo.mapLayer.gameObject);
			lineInfo.mapLayer = null;
		}

        // Remove line inspection delete
        if (lineInfo.inspectionDelete)
        {
            GameObject.Destroy(lineInfo.inspectionDelete.gameObject);
            lineInfo.inspectionDelete = null;
        }

        // Remove control points
        foreach (var controlPtTB in lineInfo.controlPtsTB)
		{
			GameObject.Destroy(controlPtTB.gameObject);
		}
		lineInfo.controlPtsTB.Clear();
		lineInfo.controlPts.Clear();
		lineInfo.coords.Clear();

		// Remove line renderer
		if (lineInfo.line)
		{
			GameObject.Destroy(lineInfo.line.gameObject);
			lineInfo.line = null;
		}

		// Remove line inspection
		if (lineInfo.lineInspection != null)
		{
			GameObject.Destroy(lineInfo.lineInspection.gameObject);
			lineInfo.lineInspection = null;
		}

		lineInfo.mapViewAreaChanged = false;
	}

	public void DeleteAllLineInspection(LineInspectorInfo[] lineInfos)
	{
		foreach (var lineInfo in lineInfos)
		{
			RemoveLineInspectorInfoProperties(lineInfo);
		}
		lineInspectionCount = 0;
		currLineInspection = -1;
	}

	public void ShowLinesAndControlPts(bool show, LineInspectorInfo[] lineInfos, Toggle adjustmentControlsToggle)
	{
        if (lineInfos == null)
            return;

        foreach (var lineInfo in lineInfos)
		{
			if (lineInfo.line != null)
				lineInfo.line.gameObject.SetActive(show);

			foreach (var controlPtTB in lineInfo.controlPtsTB)
				controlPtTB.gameObject.SetActive(show && adjustmentControlsToggle.isOn);
		}
	}

	public void UpdateLineEndPtPosition(LineInspectorInfo lineInfo, int index, Vector3 pos)
	{
		lineInfo.line.positionCount++;
		lineInfo.line.SetPosition(index, pos);
	}

	public void UpdateLineEndPtFromWorldPos(LineInspectorInfo lineInfo, int controlPtIndex, Vector3 worldPos)
	{
		// Update control point currently being dragged then update position of endpoint of line
		lineInfo.line.SetPosition(controlPtIndex, worldPos);

		Coordinate endPtCoord = map.GetCoordinatesFromUnits(worldPos.x, worldPos.z);
		lineInfo.coords[controlPtIndex] = endPtCoord;
		lineInfo.controlPts[controlPtIndex].SetCoord(endPtCoord);
	}

	public void UpdateLineMidPt(LineInspectorInfo lineInfo, float midPtOffset)
	{
		// Update midPt position (screen pos)
		Vector3 startPt = lineInfo.controlPtsTB[StartPtIndex].transform.localPosition;
		Vector3 endPt = lineInfo.controlPtsTB[EndPtIndex].transform.localPosition;
		Vector3 midPt = (startPt + endPt) * 0.5f;

		Quaternion rot = Quaternion.FromToRotation(Vector2.right, (endPt - startPt).normalized);
		Vector3 offset = rot * (Vector3.down * midPtOffset);

		Vector3 newMidPtPos = midPt + offset;
		lineInfo.controlPtsTB[MidPtIndex].transform.localPosition = newMidPtPos;

		inputHandler.GetWorldPoint(newMidPtPos, out Vector3 newMidPtWorldPos);

		Coordinate midPtCoord = map.GetCoordinatesFromUnits(newMidPtWorldPos.x, newMidPtWorldPos.z);
		lineInfo.coords[MidPtIndex] = midPtCoord;
		lineInfo.controlPts[MidPtIndex].SetCoord(midPtCoord);
	}

    public void UpdateInspectionDelete(LineInspectorInfo lineInfo)
    {
        // Compute midpt
        Vector3 startPt = lineInfo.controlPtsTB[StartPtIndex].transform.localPosition;
        Vector3 endPt = lineInfo.controlPtsTB[EndPtIndex].transform.localPosition;
        Vector3 midPt = (startPt + endPt) * 0.5f;
        lineInfo.inspectionDelete.UpdatePosition(midPt);
    }

    public void UpdateMidPtPos(LineInspectorInfo lineInfo)
    {
		Vector3 screenPos = Input.mousePosition;
		// Update position of control pts of line taking into account of screen size
		lineInfo.controlPtsTB[MidPtIndex].transform.localPosition = lineInfo.mapViewAreaChanged ?
																	(
																		!lineInfo.scaleFactor.Equals(1.0f) ? screenPos * lineInfo.scaleFactor :
																											 screenPos / canvas.scaleFactor
																	)
																	: screenPos;
	}

    public void CreateLineInspection(LineInspectorInfo lineInfo, int count, RectTransform lineInspectionPrefab, RectTransform inspectorContainer)
	{
		if (lineInfo.lineInspection == null)
		{
			lineInfo.lineInspection = GameObject.Instantiate(lineInspectionPrefab);
			lineInfo.lineInspection.SetParent(inspectorContainer);
			lineInfo.lineInspection.name = "InspectionLine" + count.ToString();
			lineInfo.scaleFactor = canvas.scaleFactor;
		}
	}

	public void ComputeStartAndEndPosOfDefaultLine(out Vector3 start, out Vector3 end)
	{
		var mapViewArea = ComponentManager.Instance.Get<MapViewArea>();
		var canvasT = canvas.transform;

		float halfWidth = mapViewArea.Rect.width * canvas.scaleFactor * 0.5f;
        float offset = mapViewArea.Rect.width * canvas.scaleFactor * 0.1f;

		inputHandler.GetWorldPoint(new Vector3(canvasT.localPosition.x - halfWidth + offset, 0), out Vector3 tempStart);
        start = new Vector3(tempStart.x, -0.001f, tempStart.y);
		inputHandler.GetWorldPoint(new Vector3(canvasT.localPosition.x + halfWidth - offset, 0), out Vector3 tempEnd);
        end = new Vector3(tempEnd.x, -0.001f, tempEnd.y);
	}

	public void CreateLine(LineInspectorInfo lineInfo, int count, LineRenderer linePrefab)
	{
		// Init line prefab
		if (lineInfo.line == null)
		{
			lineInfo.line = GameObject.Instantiate(linePrefab);
			lineInfo.line.transform.SetParent(lineInfo.lineInspection);
			lineInfo.line.name = "Line" + count.ToString();
            lineInfo.inspectionLine = lineInfo.line.GetComponent<InspectionLine>();
            lineInfo.inspectionLine.SetLineInfo(lineInfo);
        }
	}

	public void CreateAndAddControlPt(LineInspectorInfo lineInfo, int inspectionIndex, Vector3 pos)
	{
		var controlPtsTB = lineInfo.controlPtsTB;
		var controlPts = lineInfo.controlPts;

		Vector3 screenPos = Camera.main.WorldToScreenPoint(pos);
		controlPtsTB.Add(GameObject.Instantiate(endPtPrefab, screenPos, Quaternion.identity));
		int cpIndex = controlPtsTB.Count - 1;
		controlPtsTB[cpIndex].transform.SetParent(lineInfo.lineInspection);
		controlPtsTB[cpIndex].name = ((cpIndex == 0) ? "StartPt" : "EndPt") + createdLineInspectionCount.ToString();

		Coordinate coord = map.GetCoordinatesFromUnits(pos.x, pos.z);
		lineInfo.coords.Add(coord);
		var cp = controlPtsTB[cpIndex].GetComponent<ControlPt>();
		controlPts.Add(cp);
		cp.InspectionIndex = inspectionIndex;
		cp.ControlPointIndex = cpIndex;
		cp.LineInfo = lineInfo;
		cp.SetCoord(coord);
	}

	public void CreateLineInspectionMidPoint(LineInspectorInfo lineInfo, float midPtOffset)
	{
		Vector3 startPt = lineInfo.controlPtsTB[StartPtIndex].transform.localPosition;
		Vector3 endPt = lineInfo.controlPtsTB[EndPtIndex].transform.localPosition;
		Vector3 midPt = (startPt + endPt) * 0.5f;

		Quaternion rot = Quaternion.FromToRotation(Vector2.right, (endPt - startPt).normalized);
		Vector3 offset = rot * (Vector3.down * midPtOffset);

		lineInfo.controlPtsTB.Add(GameObject.Instantiate(midPtPrefab, midPt, Quaternion.identity));
		lineInfo.controlPtsTB[MidPtIndex].transform.SetParent(lineInfo.lineInspection);
		lineInfo.controlPtsTB[MidPtIndex].name = "MidPt" + createdLineInspectionCount.ToString();

		var cp2 = lineInfo.controlPtsTB[MidPtIndex].GetComponent<ControlPt>();
		lineInfo.controlPts.Add(cp2);
		cp2.ControlPointIndex = lineInfo.controlPtsTB.Count - 1;
		cp2.LineInfo = lineInfo;

		Vector3 newMidPtPos = midPt + offset;
		lineInfo.controlPtsTB[MidPtIndex].transform.localPosition = newMidPtPos;
		Coordinate coordMid = map.GetCoordinatesFromUnits(newMidPtPos.x, newMidPtPos.y);
		lineInfo.coords.Add(coordMid);
		cp2.SetCoord(coordMid);

		foreach (var controlPtTB in lineInfo.controlPtsTB)
		{
			controlPtTB.GetComponent<Image>().raycastTarget = true;
		}
	}

    public void CreateInspectorDeleteButton(LineInspectorInfo lineInfo)
    {
        Vector3 startPt = lineInfo.controlPtsTB[StartPtIndex].transform.localPosition;
        Vector3 endPt = lineInfo.controlPtsTB[EndPtIndex].transform.localPosition;
        Vector3 midPt = (startPt + endPt) * 0.5f;

        ToggleButton insspectionDelTB = GameObject.Instantiate(inspectionDelPrefab, midPt, Quaternion.identity);
        lineInfo.inspectionDelete = insspectionDelTB.GetComponent<InspectionDelete>();

        lineInfo.inspectionDelete.transform.SetParent(lineInfo.lineInspection);
        lineInfo.inspectionDelete.transform.name = "InspectionDel" + createdLineInspectionCount.ToString();

        lineInfo.inspectionDelete.SetLineInfo(lineInfo);
    }

	public void CreateLineMapLayer(LineInspectorInfo lineInfo, LineInspectionMapLayer lineInspectionMapLayerPrefab)
	{
		lineInfo.mapLayer = CreateMapLayer(lineInspectionMapLayerPrefab, "LineInspectionLayer" + createdLineInspectionCount.ToString());
		lineInfo.mapLayer.Init(lineInfo.coords);
		lineInfo.mapLayer.ShowTransect(false);
	}

	//
	// Private Methods
	//

	private T CreateMapLayer<T>(T prefab, string layerName) where T : GridMapLayer
	{
		T mapLayer = GameObject.Instantiate(prefab);
		toolLayers.Add(mapLayer, new GridData(), layerName, Color.white);
		return mapLayer;
	}
}
