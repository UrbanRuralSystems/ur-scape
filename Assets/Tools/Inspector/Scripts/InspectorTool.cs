// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli

using UnityEngine;
using UnityEngine.UI;

using LineInfo = LineInspector.LineInspectorInfo;
using AreaInfo = AreaInspector.AreaInspectorInfo;

public class InspectorTool : Tool
{
	[Header("Prefabs")]
    public RectTransform inspectorContainerPrefab;
    public RectTransform inspectionPrefab;
    // Line Renderer for line
    public LineRenderer linePrefab;
    public LineRenderer areaPrefab;
    // Output
    public InspectorOutput outputPrefab;

    [Header("UI References")]
	public Toggle lineModeToggle;
	public Toggle areaModeToggle;
    public LineInspectorPanel lineInspectorPanel;
    public AreaInspectorPanel areaInspectorPanel;

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
	private AreaInspector areaInspector;
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

        // Initialize line inspector
		lineInspectorPanel.Init(toolLayers, canvas, maxInspectionCount);
        lineInspector = lineInspectorPanel.lineInspector;
        lineInfos = lineInspectorPanel.lineInfos;

        // Initialize area inspector
        areaInspectorPanel.Init(toolLayers, maxInspectionCount);
		areaInspector = areaInspectorPanel.areaInspector;
        areaInfos = areaInspectorPanel.areaInfos;
	}

	protected override void OnToggleTool(bool isOn)
    {
        if (isOn)
        {
            TurnOn();
            
            var contoursTool = ComponentManager.Instance.Get<ContoursTool>();
            if (contoursTool != null && inspectorOutput != null)
				inspectorOutput.AreaOutput?.ShowAreaTypeHeaderAndDropdown(contoursTool.IsToggled);
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

            if (inspectorType == InspectorType.Area && areaInspector.CurrAreaInspection >= 0)
            {
                var currArea = areaInfos[areaInspector.CurrAreaInspection];
				areaInspectorPanel.UpdateGridsAndOutput(currArea, true);

                var contoursTool = ComponentManager.Instance.Get<ContoursTool>();
                if (contoursTool != null)
                    inspectorOutput.AreaOutput.ShowAreaTypeHeaderAndDropdown(contoursTool.IsToggled);
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
			inspectorOutput.ActivateOutputPanel(inspectorType);

            ActivatePanels(isOn);
            lineInspectorPanel.UpdateControlPtsAndInspectionDel();
			lineInspector.ShowLinesAndControlPts(isOn, lineInfos);

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
            if (lineInspector.CurrLineInspection >= 0)
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
			inspectorOutput.ActivateOutputPanel(inspectorType);

			ActivatePanels(isOn);
            areaInspectorPanel.UpdateAreasPtsInspectionDel();
			areaInspector.ShowAreaLines(isOn, areaInfos);

			// for (int i = 0; i < maxInspectionCount; ++i)
			// {
			// 	areaInspector.UpdateAreaInspectorToggle(areaInfos[i], i == areaInspector.CurrAreaInspection);
			// }

            foreach (var layer in gridLayerController.mapLayers)
			{
				OnShowGrid(layer, true);
			}
		}
		lineInspector.ShowLinesAndControlPts(false, lineInfos);
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
        else
            areaInspectorPanel.AreaShowGridAndUpdateOutput(mapLayer, show);
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
		// foreach (var area in areaInfos)
		// {
		// 	var areaInfoUIElem = area.uiElement;
		// 	areaInfoUIElem.toggle.onValueChanged.RemoveListener((isOn) => areaInspectorPanel.OnAreaInspectionToggleChanged(area, isOn));
		// 	areaInfoUIElem.button.onClick.RemoveListener(() => areaInspectorPanel.OnRemoveAreaInspection(area));
		// }

		areaInspectorPanel.createAreaInspectionToggle.onValueChanged.RemoveListener(OnCreateInspectionChanged);
		areaInspectorPanel.removeAreaInspectionToggle.onValueChanged.RemoveListener(OnRemoveInspectionChanged);
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

        areaInspectorPanel.InitComponentsAndListeners();
        areaInspectorPanel.gameObject.SetActive(false);
        areaInspector.AreaInspectionCount = 0;
        areaInspector.CreatedAreaInspectionCount = 0;
		areaInspector.CurrAreaInspection = -1;

		SetCursorTexture(cursorDefault);

        // UI listeners
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
			lineModeToggle.isOn = areaModeToggle.interactable = isOn;
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

	private void AllowRemoveInspections(bool allow)
    {
		if (inspectorType == InspectorType.Line)
			lineInspectorPanel.AllowRemoveLineInspections(allow);
		else
			areaInspectorPanel.AllowRemoveAreaInspections(allow);
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
				areaInspectorPanel.createAreaInspectionToggle.isOn = false;
				areaInspectorPanel.FinishCreateAreaInspector();
				break;
            case Action.RemoveInspection:
				if (inspectorType == InspectorType.Line)
                    lineInspectorPanel.removeLineInspectionToggle.isOn = false;
				else
					areaInspectorPanel.removeAreaInspectionToggle.isOn = false;
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
				areaInspectorPanel.StartCreateAreaInspector();
                break;
            case Action.RemoveInspection:
                AllowRemoveInspections(true);
                break;
        }

        action = newAction;
        changingAction = false;
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
}
