// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using LineInfo = LineInspector.LineInspectorInfo;
using AreaInfo = AreaInspector.AreaInspectorInfo;

public class InspectorTool : Tool
{
	[Header("Prefabs")]
    public InspectorToggle inspectorTogglePrefab;
    public RectTransform inspectorContainerPrefab;
    // Drawing tools
	public LassoTool lassoToolPrefab;
    // Prefab for LineInspectorInfo's lineInspection
    public RectTransform inspectionPrefab;
    // Line Renderer for line
    public LineRenderer linePrefab;
    // Output
    public InspectorOutput outputPrefab;

    [Header("UI References")]
	public Transform areaInspectorPanel;
	public Toggle lineModeToggle;
	public Toggle areaModeToggle;
 //   public Toggle adjustmentControlsToggle;
    public Transform areaInspectionsList;
	public Toggle createAreaInspectionToggle;
	public Toggle removeAreaInspectionToggle;
    public LineInspectorPanel lineInspectorPanel;

    [Header("Miscellaneous")]
    public int maxInspectionCount = 3;
    public float midPtOffset = 5.0f;

	[Header("Cursor Textures")]
	public Texture2D cursorDefault;
	public Texture2D cursorDraw;
	public Texture2D cursorMove;

    public enum Action
    {
        None,
        CreateLineInspection,
        CreateAreaInspection,
        RemoveInspection,
    }

	public enum InspectorType
	{
		Line,
		Area
	}

	// Constants
	private const int StartPtIndex = 0;
	private const int EndPtIndex = 1;
	private const int MidPtIndex = 2;

	private const float LineWidth = 0.01f;
	private const float HalfLineWidth = LineWidth * 0.5f;

	private const string newAreaInspectionPrefix = "Inspection Area";

	// Prefab Instances
	private LassoTool lassoTool;
	private RectTransform inspectorContainer;
    public RectTransform InspectContainer
    {
        get { return this.inspectorContainer; }
    }
	private InspectorOutput inspectorOutput;
    public InspectorOutput InspectOutput
    {
        get { return this.inspectorOutput; }
    }

    // Component references
    private DataLayers dataLayers;
	private GridLayerController gridLayerController;
	private InputHandler inputHandler;
	private OutputPanel outputPanel;
	private MapViewArea mapViewArea;
	private Canvas canvas;
	private InfoPanel infoPanel;

	private Action action = Action.None;
	private InspectorType inspectorType = InspectorType.Line;
	public InspectorType InspectType
	{
		get { return this.inspectorType; }
		set { this.inspectorType = value; }
	}
	private Patch patch;
    public Patch Patch
    {
        get { return this.patch; }
    }
    public MapController Map
    {
        get { return this.map; }
    }

	// Line Inspector
	private LineInspector lineInspector;
    private LineInfo[] lineInfos;

	// Area Inspector
	private AreaInspector areaInspector = new AreaInspector();
	private AreaInfo[] areaInfos;

	//
	// Event Methods
	//

	protected override void OnComponentRegistrationFinished()
    {
        base.OnComponentRegistrationFinished();

        // Get Components
        dataLayers = ComponentManager.Instance.Get<DataLayers>();
        gridLayerController = map.GetLayerController<GridLayerController>();
        inputHandler = ComponentManager.Instance.Get<InputHandler>();
        outputPanel = ComponentManager.Instance.Get<OutputPanel>();
        mapViewArea = ComponentManager.Instance.Get<MapViewArea>();
		canvas = GameObject.FindWithTag("Canvas").GetComponent<Canvas>();
		infoPanel = FindObjectOfType<InfoPanel>();


		lineInspectorPanel.Init(toolLayers, canvas, maxInspectionCount);
        lineInspector = lineInspectorPanel.lineInspector;
        lineInfos = lineInspectorPanel.lineInfos;

		areaInspector.Init(toolLayers);
		InitAreaInspectorInfo();
	}

	protected override void OnToggleTool(bool isOn)
    {
        if (isOn)
        {
            TurnOn();
        }
        else
        {
            TurnOff();
        }
    }

	protected override void OnActiveTool(bool isActive)
    {
		if (isActive)
		{
			if (inspectorOutput != null)
				outputPanel.SetPanel(inspectorOutput.transform);

			if (inspectorType == InspectorType.Line && lineInspector.CurrLineInspection >= 0)
			{
				var currLine = lineInfos[lineInspector.CurrLineInspection];
				lineInspectorPanel.UpdateTransectChartAndOutput(currLine, true);
			}
		}
		else
		{
			outputPanel.SetPanel(null);
		}
    }

    private void OnCreateInspectionChanged(bool isOn)
    {
        Action tmpAction = Action.None;

        if(isOn)
        {
            tmpAction = Action.CreateAreaInspection;

			SetCursorTexture(cursorDraw);
			SetAction(tmpAction);
        }
        else
        {
			SetCursorTexture(cursorDefault);
			SetAction(tmpAction);
        }
    }

    private void OnRemoveInspectionChanged(bool isOn)
    {
		SetAction((isOn) ? Action.RemoveInspection : Action.None);
    }

    private void OnLinePanelToggleChange(bool isOn)
    {
		outputPanel.SetPanel(inspectorOutput.transform);

		if (isOn)
		{
			inspectorType = InspectorType.Line;
			inspectorOutput.ActivateEntryInfo(inspectorType);

            ActivatePanels(isOn);
            lineInspectorPanel.UpdateControlPtsAndInspectionDel();

			if (lineInspectorPanel.lineInspectorPanelFirstActive)
            {
                lineInspectorPanel.AddDefaultLineInspection();
                lineInspectorPanel.ComputeAndUpdateLineProperties();
            }

			foreach (var layer in gridLayerController.mapLayers)
			{
				OnShowGrid(layer, true);
			}

			// Update transect chart
			lineInspectorPanel.UpdateTransectLineInfoAndAllGridDatas(lineInfos[lineInspector.CurrLineInspection]);
		}
		areaInspector.ShowAreaLines(false, areaInfos);
	}

	private void OnAreaPanelToggleChange(bool isOn)
    {
		outputPanel.SetPanel(inspectorOutput.transform);

		if (isOn)
		{
			inspectorType = InspectorType.Area;
			inspectorOutput.ActivateEntryInfo(inspectorType);

			ActivatePanels(isOn);
			areaInspector.ShowAreaLines(isOn, areaInfos);

			for (int i = 0; i < maxInspectionCount; ++i)
			{
				areaInspector.UpdateAreaInspectorToggle(areaInfos[i], i == areaInspector.CurrAreaInspection);
			}
		}
	}

    private void OnAdjustmentControlsToggleChanged(bool isOn)
    {
        foreach(var line in lineInfos)
        {
            foreach (var controlPtTB in line.controlPtsTB)
            {
                controlPtTB.gameObject.SetActive(isOn);
            }
        }
    }

	private void OnRemoveAreaInspection(AreaInfo areaInfo)
	{
		// Remove Inspector Toggle event listeners
		areaInfo.uiElement.RemoveOnPointerExitEvent();
		areaInfo.uiElement.RemoveOnPointerEnterEvent();

		areaInspector.RemoveAreaInspectorInfoProperties(areaInfo);
		areaInfo.uiElement.ResetToggle();
		--areaInspector.AreaInspectionCount;

		if (areaInspector.AreaInspectionCount == 0)
		{
			SetAction(Action.None);
			areaInspector.CurrAreaInspection = -1;
			removeAreaInspectionToggle.interactable = false;

			// Update Inspector Toggles
			for (int i = 0; i < maxInspectionCount; ++i)
			{
				if (areaInfos[i] == null)
					continue;

				areaInspector.UpdateAreaInspectorToggle(areaInfos[i], false);
				areaInfos[i].uiElement.transform.SetSiblingIndex(i);
			}

			// Update output
			inspectorOutput.UpdateAreaInspectorOutput(null, dataLayers);
		}
		else
		{
			int index = areaInfo.uiElement.transform.GetSiblingIndex();
			if (index < areaInspector.AreaInspectionCount)
			{
				// Push active inspection UI elements to the front of array
				for (int i = index; i < areaInspector.AreaInspectionCount; ++i)
				{
					var temp = areaInfos[i];
					areaInfos[i] = areaInfos[i + 1];

					var tempIndex = areaInfos[i + 1].uiElement.transform.GetSiblingIndex();
					areaInfo.uiElement.transform.SetSiblingIndex(tempIndex);

					areaInfos[i + 1] = temp;
				}
			}

			// Update currAreaInspection value
			if (areaInspector.CurrAreaInspection == index)
				areaInspector.CurrAreaInspection = Mathf.Clamp(index - 1, 0, 2);
			else
				Mathf.Clamp(--areaInspector.CurrAreaInspection, 0, 2);

			UpdateAreasElements();
		}

		createAreaInspectionToggle.interactable = true;
	}

	private void OnAreaInspectionToggleChanged(AreaInfo areaInfo, bool isOn)
	{
		for (int i = 0; i < maxInspectionCount; ++i)
		{
			bool currArea = areaInfos[i] == areaInfo;
			areaInspector.UpdateAreaInspectorToggle(areaInfos[i], currArea);

			if (currArea)
				SetCurrInspection(i);
		}
	}

    private void OnDrawArea(DrawingInfo info)
    {
        var lassoInfo = info as LassoDrawingInfo;
        var areaInfo = areaInfos[areaInspector.AreaInspectionCount];
        int count = areaInspector.CreatedAreaInspectionCount + 1;
        areaInspector.CreateAreaInspection(areaInfo, count, inspectionPrefab, inspectorContainer);
        areaInspector.CreateArea(areaInfo, count, linePrefab, lassoInfo.points);

        AddAreaInspection();
        createAreaInspectionToggle.isOn = false;
        SetCursorTexture(cursorDefault);
    }

    private void OnShowGrid(GridMapLayer mapLayer, bool show)
    {
		// Hide message if there is one upon show grid
        if(messenger != null && messenger.messageText.text != "")
        {
			gridLayerController.OnShowGrid -= OnShowGrid;

			HideMessage();
            messenger.messageText.text = "";
            FinishTurnOn();
        }

		if(inspectorType == InspectorType.Line)
			lineInspectorPanel.LineShowGridAndUpdateOutput(mapLayer, show);
    }
	private void InitAreaInspectorInfo()
	{
		// Instantiate and initialize InspectorToggles
		areaInfos = new AreaInfo[maxInspectionCount];
		for (int i = 0; i < maxInspectionCount; ++i)
		{
			var areaInfo = new AreaInfo
			{
				uiElement = Instantiate(inspectorTogglePrefab, areaInspectionsList, false),
				areaInspection = null,
				coords = new List<Coordinate>(),
				line = null
			};

			var areaInfoUIElem = areaInfo.uiElement;
			areaInfoUIElem.toggle.onValueChanged.AddListener((isOn) => OnAreaInspectionToggleChanged(areaInfo, isOn));
			areaInfoUIElem.button.onClick.AddListener(() => OnRemoveAreaInspection(areaInfo));
			areaInfoUIElem.ResetToggle();

			areaInfos[i] = areaInfo;
		}
	}

	private void TurnOn()
    {
        // Check if there's at least one active data layer
        if (dataLayers.activeLayerPanels.Count == 0)
        {
            ShowMessage(Translator.Get(ToolMessenger.NoDataLayersMessage), MessageType.Warning);
            gridLayerController.OnShowGrid += OnShowGrid;
            return;
        }
        else
        {
            FinishTurnOn();
        }
    }

    private void TurnOff()
    {
		// Remove listeners
		// Area Inspectors
		foreach (var area in areaInfos)
		{
			var areaInfoUIElem = area.uiElement;
			areaInfoUIElem.toggle.onValueChanged.RemoveListener((isOn) => OnAreaInspectionToggleChanged(area, isOn));
			areaInfoUIElem.button.onClick.RemoveListener(() => OnRemoveAreaInspection(area));
		}

		createAreaInspectionToggle.onValueChanged.RemoveListener(OnCreateInspectionChanged);
		removeAreaInspectionToggle.onValueChanged.RemoveListener(OnRemoveInspectionChanged);
		lineModeToggle.onValueChanged.RemoveListener(OnLinePanelToggleChange);
		areaModeToggle.onValueChanged.RemoveListener(OnAreaPanelToggleChange);

		gridLayerController.OnShowGrid -= OnShowGrid;

		// Remove and clear
		DeleteAllInspections();
		lineInspector.LineInspectorGrids.Clear();
		if(inspectorContainer != null)
			Destroy(inspectorContainer.gameObject);

		// Remove Output panel
		outputPanel.SetPanel(null);
		//outputPanel.RemovePanel(inspectorOutput.transform);
		if(inspectorOutput != null)
			Destroy(inspectorOutput.gameObject);

		// Reset values
		SetAction(Action.None);
		inspectorType = InspectorType.Line;
        lineInspectorPanel.lineInspectorPanelFirstActive = true;
    }

    private void FinishTurnOn()
    {
        patch = dataLayers.activeLayerPanels[0].DataLayer.loadedPatchesInView[0];

        // Inspector Container
		if(inspectorContainer == null)
		{
			inspectorContainer = Instantiate(inspectorContainerPrefab, canvas.transform);
			inspectorContainer.name = inspectorContainerPrefab.name;
		}

		if (infoPanel != null)
		{
			var infoPanelT = infoPanel.transform;
			int index = infoPanelT.GetSiblingIndex();
			inspectorContainer.SetSiblingIndex(index);
		}

		// Create output panel for tool
		if (inspectorOutput == null)
		{
			inspectorOutput = Instantiate(outputPrefab);
			inspectorOutput.name = "Inspector_OutputPanel";
			outputPanel.SetPanel(null);
		}

        lineInspectorPanel.InitComponentsAndListeners();
        lineInspectorPanel.gameObject.SetActive(false);

        lineInspector.LineInspectionCount = 0;
        lineInspector.CreatedLineInspectionCount = 0;
		lineInspector.CurrLineInspection = 0;
		SetCursorTexture(cursorDefault);

        // UI listeners
		createAreaInspectionToggle.onValueChanged.AddListener(OnCreateInspectionChanged);
		removeAreaInspectionToggle.onValueChanged.AddListener(OnRemoveInspectionChanged);
		lineModeToggle.onValueChanged.AddListener(OnLinePanelToggleChange);
		areaModeToggle.onValueChanged.AddListener(OnAreaPanelToggleChange);

		gridLayerController.OnShowGrid += OnShowGrid;
		// Initialize grids list with already visible grid layers
		foreach (var layer in gridLayerController.mapLayers)
        {
            lineInspector.LineInspectorGrids.Add(layer.Grid);
		}

		OnLinePanelToggleChange(true);
	}

	private void ActivatePanels(bool isOn)
	{
        if (inspectorType == InspectorType.Line)
		{
			//lineModeToggle.isOn = areaModeToggle.interactable = isOn; // Enable only when Area inspection is available
			lineModeToggle.interactable = areaModeToggle.isOn = !isOn;
			lineInspectorPanel.gameObject.SetActive(isOn);
			areaInspectorPanel.gameObject.SetActive(!isOn);
		}
		else
		{
			areaModeToggle.isOn = lineModeToggle.interactable = isOn;
			areaModeToggle.interactable = lineModeToggle.isOn = !isOn;
			lineInspectorPanel.gameObject.SetActive(!isOn);
			areaInspectorPanel.gameObject.SetActive(isOn);
		}
	}

	public void SetCursorTexture(Texture2D otherTexture)
	{
		Vector2 hotspot = new Vector2(1, 1);
		if (otherTexture == cursorDraw)
			hotspot = new Vector2(0, otherTexture.height - 1);
		else if (otherTexture == cursorMove)
			hotspot = new Vector2(otherTexture.width * 0.5f, otherTexture.height * 0.5f);

		Cursor.SetCursor(otherTexture, hotspot, CursorMode.Auto);
	}

    private void DeleteAllInspections()
    {
		lineInspector.DeleteAllLineInspection(lineInfos);
		areaInspector.DeleteAllAreaInspection(areaInfos);
	}

	private void AllowRemoveAreaInspections(bool allow)
	{
		for (int i = 0; i < maxInspectionCount; ++i)
		{
			if (areaInfos[i].line == null)
				continue;

			areaInfos[i].uiElement.AllowRemove(allow);

			var line = areaInfos[i].line;

			if (allow)
			{
				// Thinner line width
				line.widthMultiplier = HalfLineWidth;

				// Add Inspector Toggle event listeners
				areaInfos[i].uiElement.AddOnPointerEnterEvent();
				areaInfos[i].uiElement.AddOnPointerExitEvent();
			}
			else
			{
				// Remove Inspector Toggle event listeners
				areaInfos[i].uiElement.RemoveOnPointerExitEvent();
				areaInfos[i].uiElement.RemoveOnPointerEnterEvent();

				// Revert curr area
				if (i == areaInspector.CurrAreaInspection)
				{
					// Normal line width
					line.widthMultiplier = LineWidth;
				}
			}
		}
	}

	private void AllowRemoveInspections(bool allow)
    {
		if (inspectorType == InspectorType.Line)
			lineInspectorPanel.AllowRemoveLineInspections(allow);
		else
			AllowRemoveAreaInspections(allow);
    }

	private void AddAreaInspection()
	{
		if (areaInspector.AreaInspectionCount == maxInspectionCount)
			return;

		++areaInspector.AreaInspectionCount;
		areaInspector.CurrAreaInspection = areaInspector.AreaInspectionCount - 1;
		++areaInspector.CreatedAreaInspectionCount;

		var areaInfo = areaInfos[areaInspector.CurrAreaInspection];

		areaInspector.UpdateAreaInspectorToggleProperties(areaInfo, newAreaInspectionPrefix);

		UpdateAreasElements();

		removeAreaInspectionToggle.interactable = true;

		if (areaInspector.AreaInspectionCount == maxInspectionCount)
		{
			createAreaInspectionToggle.interactable = false;
			SetAction(Action.None);
		}
	}

	private bool changingAction = false;
    public void SetAction(Action newAction)
    {
        if (action == newAction || changingAction)
            return;

        changingAction = true;

        switch(action)
        {
            case Action.None:
                break;
            case Action.CreateLineInspection:
                lineInspectorPanel.createLineInspectionToggle.isOn = false;
                lineInspectorPanel.FinishCreateLineInspector();
                break;
            case Action.CreateAreaInspection:
				createAreaInspectionToggle.isOn = false;
				FinishCreateAreaInspector();
				break;
            case Action.RemoveInspection:
				if (inspectorType == InspectorType.Line)
                    lineInspectorPanel.removeLineInspectionToggle.isOn = false;
				else
					removeAreaInspectionToggle.isOn = false;
                AllowRemoveInspections(false);
                break;
        }

        switch(newAction)
        {
            case Action.None:
                break;
            case Action.CreateLineInspection:
                lineInspectorPanel.StartCreateLineInspector();
                break;
            case Action.CreateAreaInspection:
				StartCreateAreaInspector();
                break;
            case Action.RemoveInspection:
                AllowRemoveInspections(true);
                break;
        }

        action = newAction;
        changingAction = false;
    }

	private void StartCreateAreaInspector()
	{
		if (lassoTool == null)
		{
			lassoTool = Instantiate(lassoToolPrefab);
			lassoTool.name = lassoToolPrefab.name;
			lassoTool.CanDraw = true;
			lassoTool.OnDraw += OnDrawArea;
			//lassoTool.OnCancel += OnLineToolCancel;
		}

		lassoTool.Activate();
	}

	private void FinishCreateAreaInspector()
	{
		if (lassoTool != null)
		{
			lassoTool.Deactivate();
			lassoTool.CanDraw = false;
			lassoTool.OnDraw -= OnDrawArea;
			//lassoTool.OnCancel -= OnLineToolCancel;

			Destroy(lassoTool.gameObject);
			lassoTool = null;
		}
	}

	private void UpdateAreasElements()
	{
		for (int i = 0; i < maxInspectionCount; ++i)
		{
			if (areaInfos[i].line == null)
				continue;

			bool currArea = (i == areaInspector.CurrAreaInspection);
			areaInspector.UpdateAreaInspectorToggle(areaInfos[i], currArea);
			//UpdateTransectChartAndOutput(lineInfos[i], currArea);
			
			// Change line width according to current area inspection
			var line = areaInfos[i].line;
			line.widthMultiplier = (!currArea) ? HalfLineWidth : LineWidth;
		}
	}

	//
	// Public Methods
	//

	public bool IsPosWithinMapViewArea(Vector3 position)
	{
		return mapViewArea.Contains(position);
	}

	public bool IsInsidePatch(Vector3 position)
	{
		inputHandler.GetWorldPoint(position, out Vector3 point);
		Coordinate coorPoint = map.GetCoordinatesFromUnits(point.x, point.z);

		if (!(patch is GridedPatch))
			return false;

		GridData g = (patch as GridedPatch).grid;
		return coorPoint.Longitude >= g.west && coorPoint.Longitude <= g.east && coorPoint.Latitude >= g.south && coorPoint.Latitude <= g.north;
	}

	public void SetCurrInspection(int index)
    {
		areaInspector.CurrAreaInspection = index;
		UpdateAreasElements();
	}
}
