// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using LineInfo = LineInspector.LineInspectorInfo;

public class LineInspectorPanel : MonoBehaviour
{
    [Header("Prefabs")]
    public LineInspectionMapLayer lineInspectionMapLayerPrefab;
    // Drawing tool
    public LineInspectorDrawTool lineInspectorDrawToolPrefab;
    // Elements of a line
    public ToggleButton endPtPrefab;
    public ToggleButton midPtPrefab;
    public ToggleButton inspectionDelPrefab;

    [Header("UI References")]
    public Toggle createLineInspectionToggle;
    public Toggle removeLineInspectionToggle;
    public TransectChartController transectController;

    [Header("Miscellaneous")]
    public float midPtOffset = 5.0f;

    // Constants
    private const int StartPtIndex = 0;
    private const int EndPtIndex = 1;
    private const int MidPtIndex = 2;
    private const float LineWidth = 0.01f;
    private const float HalfLineWidth = LineWidth * 0.5f;

    // Prefab Instances
    private LineInspectorDrawTool lineInspectorDrawTool;

    // Component references
    private DataLayers dataLayers;
    private InspectorTool inspectorTool;
    private InputHandler inputHandler;
	private MapViewArea mapViewArea;
    private SiteBrowser siteBrowser;
    private MapController map;
    private GridLayerController gridLayerController;
    private Patch patch;
    private InspectorOutput inspectorOutput;
	private Canvas canvas;

    private int maxInspectionCount = 0;

	// Line Inspector
	[HideInInspector]
    public LineInspector lineInspector = new LineInspector();
    [HideInInspector]
    public LineInfo[] lineInfos;
    [HideInInspector]
    public bool lineInspectorPanelFirstActive = true;


	//
	// Unity Methods
	//

	private void OnEnable()
    {
        // Reset toggles
        createLineInspectionToggle.isOn = false;
        removeLineInspectionToggle.isOn = false;
    }

    private void OnDisable()
    {
        // Reset
        createLineInspectionToggle.interactable = true;
		transectController.SetLineInfo(null);
    }

    private void Update()
    {
        bool isCursorWithinMapViewArea = inspectorTool.IsPosWithinMapViewArea(Input.mousePosition);
        if (!isCursorWithinMapViewArea)
        {
            inspectorTool.SetCursorTexture(inspectorTool.cursorDefault);
        }
    }

    //
    // Event Methods
    //

    private void OnCreateInspectionChanged(bool isOn)
    {
        InspectorTool.Action tmpAction = (isOn) ? InspectorTool.Action.CreateLineInspection : InspectorTool.Action.None;
        Texture2D otherTexture = (isOn) ? inspectorTool.cursorDraw : inspectorTool.cursorDefault;

        inspectorTool.SetCursorTexture(otherTexture);
        inspectorTool.SetAction(tmpAction);
    }

    private void OnRemoveInspectionChanged(bool isOn)
    {
        inspectorTool.SetAction((isOn) ? InspectorTool.Action.RemoveInspection : InspectorTool.Action.None);
    }

    private void OnLineInspectorToolLeftMouseDown()
    {
        Vector3 position = Input.mousePosition;
        var lineInfo = lineInfos[lineInspector.LineInspectionCount];
        var controlPtsTB = lineInfo.controlPtsTB;
        var controlPts = lineInfo.controlPts;

        // Check that next control point drawn is not at same position as first control point
        if (controlPtsTB.Count == 1 && lineInfo.controlPtsTB[StartPtIndex].transform.position == position)
            return;

        // 2 end control point already exists
        if (controlPtsTB.Count == 2)
            return;

        // Mouse click is outside of patch
        if (!inspectorTool.IsInsidePatch(position))
            return;

        if (!inputHandler.GetWorldPoint(position, out Vector3 worldPos))
            return;

        lineInspector.CreateLineInspection(lineInfo, lineInspector.CreatedLineInspectionCount + 1, inspectorTool.inspectionPrefab, inspectorTool.InspectContainer);
        lineInspector.CreateLine(lineInfo, lineInspector.CreatedLineInspectionCount + 1, inspectorTool.linePrefab);
        
        // Endpt of line inspection
        controlPtsTB.Add(Instantiate(endPtPrefab, position, Quaternion.identity));
        Coordinate coords = map.GetCoordinatesFromUnits(worldPos.x, worldPos.z);
        lineInfo.coords.Add(coords);
        int index = controlPtsTB.Count - 1;

        var currCP = controlPtsTB[index];
        currCP.transform.SetParent(lineInfo.lineInspection);
        currCP.name = ((index == 0) ? "StartPt" : "EndPt") + (lineInspector.CreatedLineInspectionCount + 1).ToString();

        var cp = currCP.GetComponent<ControlPt>();
        controlPts.Add(cp);
        cp.ControlPointIndex = controlPtsTB.Count - 1;
        cp.LineInfo = lineInfo;
        cp.SetCoord(coords);

        lineInspector.UpdateLineEndPtPosition(lineInfo, index, worldPos);
    }

    private void OnLineInspectorToolFinishDrawing(List<Coordinate> points)
    {
        AddLineInspection();
        foreach (var layer in gridLayerController.mapLayers)
        {
            transectController.AddGrid(layer.Grid, layer.Grid.patch.DataLayer.Color);
        }
		UpdateLinesElements(lineInspector.CurrLineInspection);
		ComputeAndUpdateLineProperties();

		createLineInspectionToggle.isOn = false;

        inspectorTool.SetCursorTexture(inspectorTool.cursorDefault);

		// Update inspection line info panel
		SetCurrInspection(lineInspector.CurrLineInspection);
		inspectorOutput.ShowSummaryHeaderAndDropdown(true);
		inspectorOutput.SetDropDownInteractive(lineInspector.LineInspectionCount > 1);
        inspectorOutput.ShowHeader(lineInspector.LineInspectionCount >= 1);
        inspectorOutput.ShowPropertiesAndSummaryLabel(lineInspector.LineInspectionCount >= 1);
    }

    private void OnLineToolCancel()
    {
        createLineInspectionToggle.isOn = false;
    }

    public void OnRemoveLineInspection(LineInfo lineInfo)
    {
        --lineInspector.LineInspectionCount;

        if (lineInspector.LineInspectionCount == 1)
            inspectorOutput.SetDropDownInteractive(false);

        if (lineInspector.LineInspectionCount == 0)
        {
            inspectorTool.SetAction(InspectorTool.Action.None);

            lineInspector.CurrLineInspection = -1;

            removeLineInspectionToggle.interactable = false;

			inspectorOutput.ResetAndClearOutput();

            // Update transect chart and output
            UpdateTransectLineInfoAndAllGridDatas(null);
            inspectorOutput.UpdateLineInspectorOutput(null, dataLayers);
        }
        else
        {
            // Do necessary swapping of elements in lineInfos array upon deletion of inspection line
            var inspectionLine = lineInfo.inspectionDelete.transform.parent;
            int index = inspectionLine.GetSiblingIndex();
            int count = lineInspector.LineInspectionCount;
            if (index < count)
            {
                for (int i = index; i < count; ++i)
                {
                    var temp = lineInfos[i];
                    lineInfos[i] = lineInfos[i + 1];
                    var tmpinspectionLine = lineInfos[i + 1].inspectionDelete.transform.parent;

                    var tempIndex = tmpinspectionLine.GetSiblingIndex();
                    inspectionLine.SetSiblingIndex(tempIndex);
                    foreach (var controlPt in lineInfos[i].controlPts)
                        controlPt.InspectionIndex = i;

                    lineInfos[i + 1] = temp;
                }
            }

            // Update currLineInspection value  
            if (lineInspector.CurrLineInspection == index)
                lineInspector.CurrLineInspection = Mathf.Clamp(index - 1, 0, 2);
            else
                Mathf.Clamp(--lineInspector.CurrLineInspection, 0, 2);

        }
        lineInspector.RemoveLineInspectorInfoProperties(lineInfo);
		SetCurrInspection(lineInspector.CurrLineInspection);

        createLineInspectionToggle.interactable = true;
    }

    private void OnOtherGridFilterChange(GridData grid)
    {
        UpdateOutput();
    }

    private void OnOtherGridChange(GridData grid)
    {
        UpdateOutput();
    }

    private void OnMapUpdate()
    {
        UpdateControlPtsAndInspectionDel();

		if (lineInspector.CurrLineInspection >= 0)
        {
            var currLine = lineInfos[lineInspector.CurrLineInspection];
            currLine.mapLayer.Refresh(currLine.coords);

            UpdateTransectLineInfoAndAllGridDatas(currLine);
        }
    }

	private Coroutine waitForLayout;

	private void OnMapViewAreaChange()
	{
		if (waitForLayout != null)
		{
			StopCoroutine(waitForLayout);
		}
		waitForLayout = StartCoroutine(WaitForLayoutToFinish());
	}

	private IEnumerator WaitForLayoutToFinish()
	{
		yield return WaitFor.Frames(WaitFor.InitialFrames);
		if (lineInfos != null)
		{
			foreach (var lineInfo in lineInfos)
			{
				if (lineInfo.line == null)
					continue;

				lineInfo.mapViewAreaChanged = true;

				for (int i = 0; i < 2; ++i)
				{
					var controlPt = lineInfo.controlPts[i];

					// Update position of control pts of line taking into account of screen size
					Vector3 worldPos = lineInfo.line.GetPosition(i);
					Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
					controlPt.transform.localPosition = !lineInfo.scaleFactor.Equals(1.0f) ?
														 screenPos * lineInfo.scaleFactor :
														 screenPos / canvas.scaleFactor;
				}

				// Compute new inspection delete and update
				Vector3 startPt = lineInfo.controlPtsTB[StartPtIndex].transform.localPosition;
				Vector3 endPt = lineInfo.controlPtsTB[EndPtIndex].transform.localPosition;
				Vector3 midPt = (startPt + endPt) * 0.5f;
				lineInfo.inspectionDelete.UpdatePosition(midPt);

				// Determine whether inspection delete should be shown
				bool showInspectionDel = lineInfo.inspectionDelete.IsRectWithinMapViewArea(midPt) && removeLineInspectionToggle.isOn;
				lineInfo.inspectionDelete.ShowInspectionDelete(showInspectionDel);
			}
		}
		waitForLayout = null;
	}

	private void OnBeforeActiveSiteChange(Site nextSite, Site previousSite)
    {
		if (inspectorOutput != null)
			inspectorOutput.ResetAndClearOutput();
		ClearAllInspectionsAndTransectCharts();
    }

	//
	// Public Methods
	//

	public void Init(ToolLayerController toolLayers, Canvas canvas, int maxInspectionCount)
    {
        // Initializations
        inspectorTool = ComponentManager.Instance.Get<InspectorTool>();
		this.canvas = canvas;
        this.maxInspectionCount = maxInspectionCount;

        lineInspector.Init(toolLayers, endPtPrefab, midPtPrefab, inspectionDelPrefab, canvas);
        InitLineInspectorInfo();
    }

    public void InitComponentsAndListeners()
    {
        // Get Components from inspectorTool
        map = inspectorTool.Map;
        gridLayerController = map.GetLayerController<GridLayerController>();
        patch = inspectorTool.Patch;
        inspectorOutput = inspectorTool.InspectOutput;
        dataLayers = ComponentManager.Instance.Get<DataLayers>();
        inputHandler = ComponentManager.Instance.Get<InputHandler>();
        mapViewArea = ComponentManager.Instance.Get<MapViewArea>();
		siteBrowser = ComponentManager.Instance.Get<SiteBrowser>();

		// Add listeners
		map.OnMapUpdate += OnMapUpdate;
		mapViewArea.OnMapViewAreaChange += OnMapViewAreaChange;
        siteBrowser.OnBeforeActiveSiteChange += OnBeforeActiveSiteChange;
		createLineInspectionToggle.onValueChanged.AddListener(OnCreateInspectionChanged);
        removeLineInspectionToggle.onValueChanged.AddListener(OnRemoveInspectionChanged);
    }

    public void StartCreateLineInspector()
    {
        inputHandler.OnLeftMouseDown += OnLineInspectorToolLeftMouseDown;

        if (lineInspectorDrawTool == null)
        {
            lineInspectorDrawTool = Instantiate(lineInspectorDrawToolPrefab);
            lineInspectorDrawTool.name = lineInspectorDrawToolPrefab.name;
            lineInspectorDrawTool.ForceDrawingMethod(LineInspectorDrawTool.Method.Clicking);
            lineInspectorDrawTool.OnFinishDrawing += OnLineInspectorToolFinishDrawing;
            lineInspectorDrawTool.OnCancel += OnLineToolCancel;
        }

        lineInspectorDrawTool.Init(patch, map);
        lineInspectorDrawTool.Activate();
    }

    public void FinishCreateLineInspector()
    {
        if (lineInspectorDrawTool != null)
        {
            lineInspectorDrawTool.Deactivate();
            lineInspectorDrawTool.OnFinishDrawing -= OnLineInspectorToolFinishDrawing;
            lineInspectorDrawTool.OnCancel -= OnLineToolCancel;

            Destroy(lineInspectorDrawTool.gameObject);
            lineInspectorDrawTool = null;
        }

        inputHandler.OnLeftMouseDown -= OnLineInspectorToolLeftMouseDown;
    }

    public void ChangeKnobsAndLine(LineInfo lineInfo, bool solidThin)
    {
        var startPt = (lineInfo.controlPts[StartPtIndex] as EndPt);
        var endPt = (lineInfo.controlPts[EndPtIndex] as EndPt);
        var line = lineInfo.line;

        if (solidThin)
        {
            // Change endpt knobs to solid and thinner line width
            startPt.KnobSolid();
            endPt.KnobSolid();
            line.widthMultiplier = HalfLineWidth;
        }
        else
        {
            // Change endpt knobs to small and normal line width
            startPt.KnobSmall();
            endPt.KnobSmall();
            line.widthMultiplier = LineWidth;
        }
    }

    public void AllowRemoveLineInspections(bool allow)
    {
        for (int i = 0; i < maxInspectionCount; ++i)
        {
            if (lineInfos[i].line == null)
                continue;

            lineInfos[i].inspectionDelete.ShowInspectionDelete(allow);
            if (allow)
            {
                ChangeKnobsAndLine(lineInfos[i], true);
            }
            else
            {
                // Revert curr line
                if (i == lineInspector.CurrLineInspection)
                {
                    ChangeKnobsAndLine(lineInfos[i], false);
                }
            }
        }
    }

    public void ComputeAndUpdateLineProperties()
    {
        inspectorOutput.ComputeAndUpdateTotalLength(lineInfos, lineInspector.CurrLineInspection);
		inspectorOutput.ComputeAndUpdateMetrics();
    }

    public void UpdateControlPtsAndInspectionDel()
    {
        if (lineInfos == null)
            return;

        foreach (var lineInfo in lineInfos)
        {
            if (lineInfo.line == null)
                continue;

			for (int i = 0; i < 2; ++i)
			{
				var controlPt = lineInfo.controlPts[i];
				controlPt.UpdatePositionAndLinePtsFromCoord();

				// Update position of control pts of line taking into account of screen size
				Vector3 controlPtLocalPos = controlPt.transform.localPosition;
				Vector3 pos = lineInfo.mapViewAreaChanged ?
							  (
								!lineInfo.scaleFactor.Equals(1.0f) ? controlPtLocalPos / lineInfo.scaleFactor :
																	 controlPtLocalPos * canvas.scaleFactor
							  )
							  : controlPtLocalPos;

				bool showControlPt = mapViewArea.Contains(pos);
                controlPt.gameObject.SetActive(showControlPt);
            }


            // Compute new inspection delete pos and update
            Vector3 startPt = lineInfo.controlPtsTB[StartPtIndex].transform.localPosition;
            Vector3 endPt = lineInfo.controlPtsTB[EndPtIndex].transform.localPosition;
            Vector3 midPt = (startPt + endPt) * 0.5f;
            lineInfo.inspectionDelete.UpdatePosition(midPt);

            // Determine whether inspection delete should be shown
            bool showInspectionDel = lineInfo.inspectionDelete.IsRectWithinMapViewArea(midPt) && removeLineInspectionToggle.isOn;
            lineInfo.inspectionDelete.ShowInspectionDelete(showInspectionDel);
        }
    }

    public void UpdateTransectLineInfoAndAllGridDatas(LineInfo lineInfo)
    {
        // Update data values
        transectController.SetLineInfo(lineInfo);
        foreach (var layer in gridLayerController.mapLayers)
        {
            transectController.UpdateGridData(layer.Grid);
        }
    }

    public void UpdateTransectChartAndOutput(LineInfo lineInfo, bool currLine)
    {
        if (lineInfo == null || lineInfo.mapLayer == null)
            return;

        if (currLine)
        {
			// Add visible grids to the budget layer
			foreach(var layer in gridLayerController.mapLayers)
			{
                AddGridData(layer.Grid);
			}
            lineInspector.LineInspectorGrids.Clear();

            lineInfo.mapLayer.Show(true);
            // Update data values
            lineInfo.mapLayer.Refresh(lineInfo.coords);
            UpdateTransectLineInfoAndAllGridDatas(lineInfo);
        }
        else
        {
            // Hide at the beginning
            lineInfo.mapLayer.Show(false);

            // Move visible grids from budget layer to list
            if (lineInfo.mapLayer.grids.Count > 0)
            {
                lineInspector.LineInspectorGrids.AddRange(lineInfo.mapLayer.grids);
                lineInfo.mapLayer.Clear();
            }
        }
        UpdateOutput();
    }

    public void AddDefaultLineInspection()
    {
        // Update counts
        ++lineInspector.LineInspectionCount;
        lineInspector.CurrLineInspection = lineInspector.LineInspectionCount - 1;
        ++lineInspector.CreatedLineInspectionCount;

        // Creation and update of inspection line and its elements
        lineInspector.CreateLineInspection(lineInfos[0], lineInspector.CreatedLineInspectionCount, inspectorTool.inspectionPrefab, inspectorTool.InspectContainer);

        lineInspector.ComputeStartAndEndPosOfDefaultLine(out Vector3 startPos, out Vector3 endPos);

        lineInspector.CreateLine(lineInfos[0], lineInspector.CreatedLineInspectionCount, inspectorTool.linePrefab);
        lineInspector.UpdateLineEndPtPosition(lineInfos[0], StartPtIndex, startPos);
        lineInspector.UpdateLineEndPtPosition(lineInfos[0], EndPtIndex, endPos);

        lineInspector.CreateAndAddControlPt(lineInfos[0], 0, startPos);
        lineInspector.CreateAndAddControlPt(lineInfos[0], 0, endPos);

        lineInspector.CreateLineInspectionMidPoint(lineInfos[0], midPtOffset);
        lineInspector.CreateInspectorDeleteButton(lineInfos[0]);

        lineInspector.CreateLineMapLayer(lineInfos[0], lineInspectionMapLayerPrefab);

        removeLineInspectionToggle.interactable = true;
        lineInspectorPanelFirstActive = false;
    }

    public void LineShowGridAndUpdateOutput(GridMapLayer mapLayer, bool show)
    {
        if (lineInspector.CurrLineInspection >= 0)
        {
            var otherGrid = mapLayer.Grid;
            var currLineInfo = lineInfos[lineInspector.CurrLineInspection];

            if (show)
            {
                // Add to line inspection map layer
                if (currLineInfo.mapLayer.IsVisible())
                {
                    AddGridData(otherGrid);
                    currLineInfo.mapLayer.Refresh(currLineInfo.coords);
                }
                else
                {
                    lineInspector.LineInspectorGrids.Add(otherGrid);
                }
            }
            else
            {
                // Remove from layer
                if (currLineInfo.mapLayer.IsVisible())
                {
                    RemoveGridData(otherGrid);
                    currLineInfo.mapLayer.Refresh(currLineInfo.coords);
                }
                else
                {
                    lineInspector.LineInspectorGrids.Remove(otherGrid);
                }
            }

			string dataLayerName = mapLayer.Grid.patch.DataLayer.Name;
			inspectorOutput.ShowInspectorOutputItemLabel(dataLayerName, show);
			UpdateOutput();
        }
    }

    public void SetCurrInspection(int index)
    {
        lineInspector.CurrLineInspection = index;
        UpdateLinesElements(lineInspector.CurrLineInspection);
        inspectorOutput.SetLineInspection(lineInspector.CurrLineInspection);
    }

    public void UpdateLineEndPtAndMidPtFromWorldPos(LineInfo lineInfo, int controlPtIndex, Vector3 worldPos)
    {
        lineInspector.UpdateLineEndPtFromWorldPos(lineInfo, controlPtIndex, worldPos);
        lineInspector.UpdateLineMidPt(lineInfo, midPtOffset);
    }

    //
    // Private Methods
    //

    private void InitLineInspectorInfo()
    {
        // Initialize line inspections
        lineInfos = new LineInfo[maxInspectionCount];
        for (int i = 0; i < maxInspectionCount; ++i)
        {
			var lineInfo = new LineInfo
			{
				inspectionDelete = null,
				lineInspection = null,
				controlPtsTB = new List<ToggleButton>(),
				controlPts = new List<ControlPt>(),
				coords = new List<Coordinate>(),
				line = null,
				inspectionLine = null,
				mapLayer = null,
				mapViewAreaChanged = false,
				scaleFactor = 1.0f
            };
            lineInfos[i] = lineInfo;
        }
    }

    private void AddGridData(GridData otherGrid)
    {
        if (otherGrid.patch != null && otherGrid.patch is GraphPatch)
            return;

        if (lineInfos[lineInspector.CurrLineInspection] == null)
            return;

        lineInfos[lineInspector.CurrLineInspection].mapLayer.Add(otherGrid);

        otherGrid.OnGridChange += OnOtherGridChange;
        otherGrid.OnValuesChange += OnOtherGridChange;
        otherGrid.OnFilterChange += OnOtherGridFilterChange;
    }

    private void RemoveGridData(GridData otherGrid)
    {
        if (otherGrid.patch != null && otherGrid.patch is GraphPatch)
            return;

        lineInfos[lineInspector.CurrLineInspection].mapLayer.Remove(otherGrid);

        otherGrid.OnGridChange -= OnOtherGridChange;
        otherGrid.OnValuesChange -= OnOtherGridChange;
        otherGrid.OnFilterChange -= OnOtherGridFilterChange;
    }

    private void UpdateOutput()
	{
		if (lineInspector.CurrLineInspection == -1)
			return;

		inspectorOutput.UpdateLineInspectorOutput(lineInfos[lineInspector.CurrLineInspection], dataLayers);
    }

    private void UpdateLinesElements(int currIndex)
    {
        for (int i = 0; i < maxInspectionCount; ++i)
        {
            if (lineInfos[i].line == null)
                continue;

            bool currLine = (i == currIndex);
            UpdateTransectChartAndOutput(lineInfos[i], currLine);
            if (!currLine)
            {
                ChangeKnobsAndLine(lineInfos[i], true);
            }
            else
            {
                ChangeKnobsAndLine(lineInfos[i], false);
            }
        }
    }

    private void AddLineInspection()
    {
		if (lineInspector.LineInspectionCount == maxInspectionCount || lineInfos[lineInspector.LineInspectionCount].controlPtsTB.Count < 2)
			return;

        // Update counts
        ++lineInspector.LineInspectionCount;
        lineInspector.CurrLineInspection = lineInspector.LineInspectionCount - 1;
        ++lineInspector.CreatedLineInspectionCount;

        var lineInfo = lineInfos[lineInspector.CurrLineInspection];

        lineInspector.CreateLineInspectionMidPoint(lineInfo, midPtOffset);
        lineInspector.CreateInspectorDeleteButton(lineInfo);

        // Update inspection index of all of current inspection line's control pts
        foreach (var controlPt in lineInfo.controlPts)
            controlPt.InspectionIndex = lineInspector.CurrLineInspection;

        lineInspector.CreateLineMapLayer(lineInfo, lineInspectionMapLayerPrefab);

        removeLineInspectionToggle.interactable = true;

        // Disable ability to create anymore inspection lines
        if (lineInspector.LineInspectionCount == maxInspectionCount)
        {
            createLineInspectionToggle.interactable = false;
            inspectorTool.SetAction(InspectorTool.Action.None);
        }

		lineInfo.mapViewAreaChanged = false;
    }

    private void ClearAllInspectionsAndTransectCharts()
    {
        foreach (var lineInfo in lineInfos)
        {
            if (lineInfo.line == null)
                continue;

            lineInspector.RemoveLineInspectorInfoProperties(lineInfo);
            --lineInspector.LineInspectionCount;
        }

        inspectorTool.SetAction(InspectorTool.Action.None);
        lineInspector.CurrLineInspection = -1;
        lineInspector.CreatedLineInspectionCount = 0;
        removeLineInspectionToggle.interactable = false;
        createLineInspectionToggle.interactable = true;

        // Update transect chart and output
        transectController.SetLineInfo(null);
        inspectorOutput.UpdateLineInspectorOutput(null, dataLayers);
    }
}