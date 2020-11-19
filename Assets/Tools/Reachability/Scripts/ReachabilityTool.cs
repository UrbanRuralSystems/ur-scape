// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MobilityMode
{
	public string name;
	public readonly float[] speeds = new float[ClassificationValue.Count];
	public readonly float[] invSpeeds = new float[ClassificationValue.Count];
}

public class ReachabilityTool : Tool
{
	class RoadInfo
	{
		public RoadLayer mapLayer;
		public RoadToggle uiElement;
	}

	public enum Action
	{
		None,
		PlaceStartSingle,
		PlaceStartMulti,
		CreateRoad,
		RemoveRoad,
	}

	private const string NetworkLayerOffError = "Network layer was turned off. Please turn it back on"/*translatable*/;
	private const string ReachabilityLayerOffError = "Reachability layer was turned off. Please turn it back on"/*translatable*/;
	private const string NetworkNotAvailableHereError = "Network data is not available in this area"/*translatable*/;
	private const string NetworkNotAvailableSiteError = "Network data is not available in this site"/*translatable*/;
	private const string CategorizedReachabilityError = "Reachability data should not be categorized"/*translatable*/;
	private static string BuildRequiredLayerMessage(object[] args) => Translator.Get("Required data layer is not available") + ": " + args[0];

	public int maxRoadCount = 3;

	public string reachabilityLayerName = "Reachability";
	public int travelTimeScale = 15;
	public float defaultWalkSpeed = 5; // km/h

    public string highwayRoadLetter = "H";
    public string primaryRoadLetter = "P";
    public string secondaryRoadLetter = "S";
    public string otherRoadLetter = "O";
    public string roadName = "Road";

    [Header("Prefabs")]
	public LineTool lineToolPrefab;
	public PlaceStart placeStartPrefab;
	public RoadLayer roadLayerPrefab;
	public RoadToggle roadTogglePrefab;
	public Toggle mobilityTogglePrefab;

	[Header("UI References")]
	public GameObject mainPanel;
	public Dropdown layerDropdown;
	public Toggle startSingleButton;
	public Toggle startMultiButton;
	public Button clearButton;
	public Toggle showRoadsToggle;
	public Toggle showIsochronToggle;
	public ToggleGroup mobilityGroup;
	public Slider traveTimeSlider;
	public Text timeLabel;
	public Button newRoadButton;

	public GameObject newRoadPanel;
	public Button finishNewRoadButton;
	public Toggle highwayButton;
	public Toggle primaryButton;
	public Toggle secondaryButton;
	public Toggle otherButton;
	public Toggle createRoadToggle;
	public Transform roadsList;
	public Toggle removeRoadToggle;

	// Component References
	private DataLayers dataLayers;
	private SiteBrowser siteBrowser;

	// Misc
	private string networkLayerName;
	private DataLayer networkLayer;
	private DataLayer reachabilityLayer;
	private GraphPatch networkPatch;
	private GridPatch reachabilityPatch;

	private ClassificationIndex classificationIndex = ClassificationIndex.Highway;

	private Action action = Action.None;
	private int mobilityMode = 0;
	private readonly List<MobilityMode> mobilityModes = new List<MobilityMode>();

	// Prefab Instances
	private LineTool lineTool;
	private PlaceStart placeStart;
	
	private RoadInfo[] roads;
	private int roadCount = 0;
	private int highwayRoadCount = 0;
	private int primaryRoadCount = 0;
	private int secondaryRoadCount = 0;
	private int otherRoadCount = 0;
    private readonly List<RoadLayer> activeRoadLayers = new List<RoadLayer>();

	private bool movedAwayError = false;
	private bool deactivatedLayerError = false;

	private readonly int stripeCount = 3;

    public const float kmPerHourToMetersPerMin = 1000.0f / 60.0f;

	private bool HasErrorMessage => currentErrorMessage != null || currentErrorBuilder != null;
	private string currentErrorMessage = null;
	private MessageBuilder currentErrorBuilder = null;



	//
	// Inheritance Methods
	//

	protected override void OnComponentRegistrationFinished()
	{
		base.OnComponentRegistrationFinished();

		networkLayerName = "Network"; //-

		roads = new RoadInfo[maxRoadCount];
		for (int i = 0; i < maxRoadCount; i++)
		{
			var road = new RoadInfo
			{
				mapLayer = null,
				uiElement = Instantiate(roadTogglePrefab, roadsList, false),
			};

			road.uiElement.toggle.onValueChanged.AddListener((isOn) => OnRoadToggleChanged(road, isOn));
			road.uiElement.button.onClick.AddListener(() => RemoveRoad(road));
			ResetRoadUI(road.uiElement);

			roads[i] = road;
		}

		// Get Components
		dataLayers = ComponentManager.Instance.Get<DataLayers>();
		siteBrowser = ComponentManager.Instance.Get<SiteBrowser>();

		// UI Listeners - Main Panel
		layerDropdown.onValueChanged.AddListener(OnSelectedLayerChanged);
		startSingleButton.onValueChanged.AddListener(OnToggleStartSingle);
		startMultiButton.onValueChanged.AddListener(OnToggleStartMulti);
		clearButton.onClick.AddListener(OnClearClicked);
		showRoadsToggle.onValueChanged.AddListener(OnToggleShowRoads);
		showIsochronToggle.onValueChanged.AddListener(OnToggleShowIsochrons);
		newRoadButton.onClick.AddListener(OnNewRoadClick);
		traveTimeSlider.onValueChanged.AddListener(OnTravelTimeSliderChanged);

		// UI Listeners - New Road Panel
		finishNewRoadButton.onClick.AddListener(OnFinishNewRoadClick);
		highwayButton.onValueChanged.AddListener((isOn) => OnRoadClassificationChange(highwayButton));
		primaryButton.onValueChanged.AddListener((isOn) => OnRoadClassificationChange(primaryButton));
		secondaryButton.onValueChanged.AddListener((isOn) => OnRoadClassificationChange(secondaryButton));
		otherButton.onValueChanged.AddListener((isOn) => OnRoadClassificationChange(otherButton));
		createRoadToggle.onValueChanged.AddListener(OnCreateRoadChanged);
		removeRoadToggle.onValueChanged.AddListener(OnRemoveRoadChanged);
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


	//
	// Event Methods
	//

	private void OnBeforeActiveSiteChange(Site nextSite, Site previousSite)
	{
		ResetTool(false);
	}

	private void OnAfterActiveSiteChange(Site nextSite, Site previousSite)
	{
		ResetErrorMessage();
		PrepareNetworkLayer();
	}

	private void OnLayerVisibilityChange(DataLayer layer, bool visible)
	{
		if (layer.Name == networkLayerName && !visible)
		{
			SetNetworkPatch(null);
		}

		if (!dataLayers.IsLayerActive(networkLayer))
		{
			if (movedAwayError)
				ResetErrorMessage();
			ShowError(NetworkLayerOffError);
			deactivatedLayerError = true;
		}
		else if (!dataLayers.IsLayerActive(reachabilityLayer) && reachabilityPatch != null)
		{
			ShowError(ReachabilityLayerOffError);
			deactivatedLayerError = true;
		}
		else if (deactivatedLayerError)
		{
			deactivatedLayerError = false;
			HideErrorMessage();
		}
	}

	private void OnPatchVisibilityChange(DataLayer dataLayer, Patch patch, bool visible)
	{
		if (dataLayer.Name == networkLayerName)
		{
			if (visible)
			{
				SetNetworkPatch(patch as GraphPatch);
			}
			else if (patch == networkPatch)
			{
				SetNetworkPatch(null);
			}
		}
		else if (dataLayer.Name == reachabilityLayerName)
		{
			if (visible)
			{
				reachabilityPatch = patch as GridPatch;
				GridMapLayer mapLayer = reachabilityPatch.GetMapLayer() as GridMapLayer;
				if (mapLayer != null)
				{
					mapLayer.SetStripes(stripeCount, true);
				}

				if (reachabilityPatch.grid.IsCategorized)
				{
					ShowError(CategorizedReachabilityError);
					ResetTool(true);
					return;
				}
				else if (placeStart != null)
				{
					placeStart.ReachabilityPatch = reachabilityPatch;
				}
			}
			else if (patch == reachabilityPatch)
			{
				reachabilityPatch = null;
			}
		}

		if (networkPatch == null)
		{
			ShowError(NetworkNotAvailableHereError);
			movedAwayError = true;
		}
		else if (movedAwayError)
		{
			movedAwayError = false;
			HideErrorMessage();
		}
	}

	private void OnMobilityModesLoaded()
	{
		var translator = LocalizationManager.Instance;
		for (int i = 0; i < mobilityModes.Count; ++i)
		{
			var mode = mobilityModes[i];

			// Override the speed for "No network" classfication to be walking speed
			mode.speeds[(int)ClassificationIndex.None] = defaultWalkSpeed * kmPerHourToMetersPerMin; //convert from km/h to m/min

            int count = mode.speeds.Length;
			for (int j = 0; j < count; ++j)
			{
				mode.invSpeeds[j] = mode.speeds[j] > 0? 1f / mode.speeds[j] : 600; // 600 = 10 hours, which is higher than the max travel time
			}

			var mobilityToggle = Instantiate(mobilityTogglePrefab, mobilityGroup.transform, false);
			mobilityToggle.name = mode.name;
			mobilityToggle.transform.GetChild(0).GetComponent<Text>().text = mode.name.Substring(0, 1);
			mobilityToggle.transform.GetChild(1).GetComponent<Text>().text = translator.Get(mode.name, false);
			mobilityToggle.group = mobilityGroup;

			int index = i;
			mobilityToggle.onValueChanged.AddListener((isOn) => OnMobilityToggleChanged(index, isOn));

			if (i == 0)
				mobilityToggle.isOn = true;
		}

		GuiUtils.RebuildLayout(mobilityGroup.transform);
	}

	private void OnLanguageChanged()
	{
		var translator = LocalizationManager.Instance;

		if (currentErrorMessage != null)
		{
			ShowMessage(translator.Get(currentErrorMessage), MessageType.Error);
		}
		else if (currentErrorBuilder != null)
		{
			ShowMessage(currentErrorBuilder.Message, MessageType.Error);
		}

		var mobilityList = mobilityGroup.transform;
		for (int i = 0; i < mobilityModes.Count; ++i)
		{
			var mode = mobilityModes[i];
			mobilityList.GetChild(i).GetChild(1).GetComponent<Text>().text = translator.Get(mode.name, false);
		}

		UpdateTravelTimeLabel(traveTimeSlider.value * travelTimeScale);
	}


	//
	// UI Event Methods
	//

	private void OnSelectedLayerChanged(int value)
	{
		ClearAllUserChanges();

		if (layerDropdown.options.Count > 0)
			networkLayerName = layerDropdown.options[value].text;
		else
			networkLayerName = null;

		if (networkLayer != null && networkLayer.Name != networkLayerName)
		{
			// Disable patches visibility events
			networkLayer.OnPatchVisibilityChange -= OnPatchVisibilityChange;
			if (dataLayers.IsLayerActive(networkLayer))
				dataLayers.ActivateLayer(networkLayer, false);

			networkLayer = null;
		}

		PrepareNetworkLayer();
	}

	private void OnToggleStartSingle(bool isOn)
	{
		HandleStartToggle(isOn, false);
	}

	private void OnToggleStartMulti(bool isOn)
	{
		HandleStartToggle(isOn, true);
	}

	private void HandleStartToggle(bool isOn, bool isMulti)
	{
		if (isOn)
			SetAction(isMulti? Action.PlaceStartMulti : Action.PlaceStartSingle);
		else
			SetAction(Action.None);
	}

	private void OnCreateRoadChanged(bool isOn)
	{
		if (isOn)
			SetAction(Action.CreateRoad);
		else
			SetAction(Action.None);
	}

	private void OnRemoveRoadChanged(bool isOn)
	{
		if (isOn)
			SetAction(Action.RemoveRoad);
		else
			SetAction(Action.None);
	}

	private void OnClearClicked()
	{
		SetAction(Action.None);
		ClearIsochrone();
	}

	private void OnMobilityToggleChanged(int index, bool isOn)
	{
		if (isOn)
		{
			ChangeMobilityMode(index);
		}
	}

	private void OnTravelTimeSliderChanged(float value)
	{
		float travelTime = value * travelTimeScale;
		UpdateTravelTimeLabel(travelTime);

        if (placeStart != null)
        {
			placeStart.SetTravelTime(travelTime);
            DelayedUpdateIsochrone();
		}
	}
	
	private void OnToggleShowRoads(bool isOn)
	{
		if (networkPatch != null)
		{
			networkPatch.GetMapLayer().Show(isOn);
			foreach (var activeRoad in activeRoadLayers)
			{
				activeRoad.Show(isOn);
			}
		}
	}

	private void OnToggleShowIsochrons(bool isOn)
	{
		if (reachabilityPatch != null)
		{
			reachabilityPatch.GetMapLayer().Show(isOn);
		}
	}

	private void OnRoadClassificationChange(Toggle toggle)
	{
		if (toggle.isOn)
		{
			if (toggle == highwayButton)
			{
				classificationIndex = ClassificationIndex.Highway;
			}
			else if (toggle == primaryButton)
			{
				classificationIndex = ClassificationIndex.Primary;
			}
			else if (toggle == secondaryButton)
			{
				classificationIndex = ClassificationIndex.Secondary;
			}
			else if (toggle == otherButton)
			{
				classificationIndex = ClassificationIndex.Other;
			}
		}
	}

	private void OnRoadToggleChanged(RoadInfo road, bool isOn)
	{
		if (isOn)
			activeRoadLayers.Add(road.mapLayer);
		else
			activeRoadLayers.Remove(road.mapLayer);

		road.mapLayer.Show(isOn);

		if (placeStart != null)
		{
			placeStart.SetNewRoads(activeRoadLayers);
			
			if (placeStart.HasStartPoints)
				placeStart.UpdateGrid();
		}
	}

	private void OnNewRoadClick()
	{
		SetAction(Action.None);

		removeRoadToggle.interactable = roadCount > 0;

		mainPanel.SetActive(false);
		newRoadPanel.SetActive(true);
		GuiUtils.RebuildLayout(transform);

		if (roadCount == 0)
			createRoadToggle.isOn = true;
	}

	private void OnFinishNewRoadClick()
	{
		SetAction(Action.None);

		newRoadPanel.SetActive(false);
		mainPanel.SetActive(true);
		GuiUtils.RebuildLayout(transform);
	}

	private void LineTool_OnFinishDrawing(List<Coordinate> points)
	{
		AddRoad(points);
	}

	private void LineTool_OnCancel()
	{
		createRoadToggle.isOn = false;
	}

	//
	// Private Methods
	//

	private void TurnOn()
	{
        var outputPanel = ComponentManager.Instance.Get<OutputPanel>();
        outputPanel.SetPanel(null);

        // Hide any previous message
        HideMessage();

		// Make sure the main panel is active
		mainPanel.SetActive(true);

		OnTravelTimeSliderChanged(traveTimeSlider.value);

		// Enable layers visibility event
		dataLayers.OnLayerVisibilityChange += OnLayerVisibilityChange;

		if (!FindLayer(networkLayerName, ref networkLayer))
			return;

		if (!FindLayer(reachabilityLayerName, ref reachabilityLayer, true))
			return;

		// Register for site change events
		siteBrowser.OnBeforeActiveSiteChange += OnBeforeActiveSiteChange;
		siteBrowser.OnAfterActiveSiteChange += OnAfterActiveSiteChange;

		LoadSiteMobilityModes();

		PrepareNetworkLayer();

		if (mobilityModes.Count > 0)
		{
			ChangeMobilityMode(0);
		}

		// Register for language change events
		LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
	}

	private void PrepareNetworkLayer()
	{
		// Enable patches visibility events (disable them first to avoid duplicates)
		networkLayer.OnPatchVisibilityChange -= OnPatchVisibilityChange;
		networkLayer.OnPatchVisibilityChange += OnPatchVisibilityChange;
		reachabilityLayer.OnPatchVisibilityChange -= OnPatchVisibilityChange;
		reachabilityLayer.OnPatchVisibilityChange += OnPatchVisibilityChange;

		if (networkLayer.HasLoadedPatchesInView())
		{
			// This only occurs when the Network layer is already active and the patch is visible & loaded
			SetNetworkPatch(networkLayer.loadedPatchesInView[0] as GraphPatch);
		}
		else
		{
			// Check if the network layer has patches in this area.
			// HasPatchesInView() doesn't work for unloaded data layer, use HasPatches()
			var bounds = map.MapCoordBounds;
			if (networkLayer.HasPatches(siteBrowser.ActiveSite, map.CurrentLevel, bounds.west, bounds.east, bounds.north, bounds.south))
			{
				ShowMessage(Translator.Get("Loading Data") + " ...", MessageType.Info);
			}
			else
			{
				if (ActiveSiteHasLayer(networkLayer))
				{

					ShowError(NetworkNotAvailableHereError);
					movedAwayError = true;
				}
				else
				{
					ShowError(NetworkNotAvailableSiteError);
				}
			}

			if (!dataLayers.IsLayerActive(networkLayer) && ActiveSiteHasLayer(networkLayer))
				dataLayers.ActivateLayer(networkLayer, true);
		}
	}

	private void TurnOff()
	{
		// Disable events
		siteBrowser.OnBeforeActiveSiteChange -= OnBeforeActiveSiteChange;
		siteBrowser.OnAfterActiveSiteChange -= OnAfterActiveSiteChange;
		LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;

		ResetTool(true);

		// Remove temp reachability layer
		if (reachabilityLayer != null && reachabilityLayer.IsTemp)
		{
			reachabilityLayer.Remove();
			dataLayers.RebuildList(ComponentManager.Instance.Get<DataManager>().groups);
		}

		// Reset layer references
		networkLayer = null;
		reachabilityLayer = null;

		// Disable layers visibility event AFTER ResetTool
		dataLayers.OnLayerVisibilityChange -= OnLayerVisibilityChange;
	}

	private void ClearAllUserChanges()
	{
		SetAction(Action.None);

		ClearIsochrone();

		DeleteAllRoads();
	}

	private void ResetTool(bool turnOff)
	{
		// Disable layers visibility event
		dataLayers.OnLayerVisibilityChange -= OnLayerVisibilityChange;

		// Clear layers dropdown
		if (!turnOff)
			layerDropdown.ClearOptions();

		ClearAllUserChanges();

		// Deactivate layers
		if (networkLayer != null)
		{
			// Disable patches visibility events
			networkLayer.OnPatchVisibilityChange -= OnPatchVisibilityChange;
			if (turnOff && dataLayers.IsLayerActive(networkLayer))
				dataLayers.ActivateLayer(networkLayer, false);
		}
		if (reachabilityLayer != null)
		{
			reachabilityLayer.OnPatchVisibilityChange -= OnPatchVisibilityChange;
			if (turnOff && dataLayers.IsLayerActive(reachabilityLayer))
			{
				dataLayers.ActivateLayer(reachabilityLayer, false);
			}
        }

		newRoadPanel.SetActive(false);
		mainPanel.SetActive(true);

		// Reset path references
		networkPatch = null;
		reachabilityPatch = null;

		ResetErrorMessage();
		movedAwayError = false;
		deactivatedLayerError = false;

		// Re-enable layers visibility event
		dataLayers.OnLayerVisibilityChange += OnLayerVisibilityChange;
	}

	private void ClearIsochrone()
	{
		if (placeStart != null)
		{
			placeStart.Clear();
			DestroyPlaceStart();
		}
	}

	private void RefreshLayersDropdown()
	{
		//+layerDropdown.onValueChanged.RemoveListener(OnSelectedLayerChanged);

		layerDropdown.ClearOptions();

		foreach (var layer in dataLayers.availableLayers)
		{
			foreach (var patch in layer.patchesInView)
			{
				if (patch is GraphPatch)
				{
					layerDropdown.options.Add(new Dropdown.OptionData(layer.Name));
					break;
				}
			}
		}

		//+layerDropdown.onValueChanged.AddListener(OnSelectedLayerChanged);
	}

	private void ChangeMobilityMode(int mode)
	{
		mobilityMode = mode;

		if (placeStart != null)
		{
			placeStart.SetMinutesPerMeter(mobilityModes[mode].invSpeeds);
			
			if (placeStart.HasStartPoints)
				placeStart.UpdateGrid();
		}
	}

	private bool FindLayer(string layerName, ref DataLayer layer, bool add = false)
	{
		if (dataLayers.HasLayer(layerName))
		{
			layer = dataLayers.GetLayer(layerName);
			if (layer != null)
				return true;

			Debug.LogError("Data layer '" + layerName + "' is null");
		}
		else if (add && networkLayer != null)
		{
			var dataManager = ComponentManager.Instance.Get<DataManager>();
			layer = new DataLayer(dataManager, layerName, networkLayer.Color, networkLayer.Group); //+ Don't use network Color
			layer.SetIsTemp(true);
			dataLayers.RebuildList(dataManager.groups);
			return true;
		}

		ShowError(new MessageBuilder(BuildRequiredLayerMessage, layerName));
		ResetTool(true);
		return false;
	}

	private bool changingAction = false;
	private void SetAction(Action newAction)
	{
		if (action == newAction || changingAction)
			return;

		if (newAction != Action.None && (networkPatch == null || reachabilityPatch == null))
		{
			if (networkPatch == null)
				Debug.LogWarning("Network data is not available.");
			if (reachabilityPatch == null)
				Debug.LogWarning("Reachability data is not available.");
			return;
		}

		changingAction = true;

		switch (action)
		{
			case Action.None:
				break;
			case Action.PlaceStartSingle:
				startSingleButton.isOn = false;
				placeStart.Deactivate();
				break;
			case Action.PlaceStartMulti:
				startMultiButton.isOn = false;
				placeStart.Deactivate();
				break;
			case Action.CreateRoad:
				createRoadToggle.isOn = false;
				FinishCreateRoad();
				break;
			case Action.RemoveRoad:
				removeRoadToggle.isOn = false;
				AllowRemoveRoads(false);
				break;
		}

		switch (newAction)
		{
			case Action.None:
				break;

			case Action.PlaceStartSingle:
			case Action.PlaceStartMulti:
				if (placeStart == null)
				{
					placeStart = Instantiate(placeStartPrefab);
					placeStart.name = placeStartPrefab.name;
				}
				float travelTime = traveTimeSlider.value * travelTimeScale;
				placeStart.Init(networkPatch, reachabilityPatch, mobilityModes[mobilityMode].invSpeeds, travelTime);
				placeStart.SetNewRoads(activeRoadLayers);
				placeStart.Activate();
				placeStart.isMultiStart = newAction == Action.PlaceStartMulti;
				break;

			case Action.CreateRoad:
				StartCreateRoad();
				break;

			case Action.RemoveRoad:
				AllowRemoveRoads(true);
				break;
		}

		action = newAction;
		changingAction = false;
	}

    private string UpdateRoadName(ClassificationIndex index, out string roadType)
    {
        if (index == ClassificationIndex.Highway)
        {
            roadType = (highwayRoadLetter + ++highwayRoadCount);
        }
        else if (index == ClassificationIndex.Primary)
        {
            roadType = (primaryRoadLetter + ++primaryRoadCount);
        }
        else if (index == ClassificationIndex.Secondary)
        {
            roadType = (secondaryRoadLetter + ++secondaryRoadCount);
        }
        else if (index == ClassificationIndex.Other)
        {
            roadType = (otherRoadLetter + ++otherRoadCount);
        }
        else
        {
            roadType = "";
        }

        return (roadName + " " + roadType);
    }

	private void AddRoad(List<Coordinate> points)
	{
		if (roadCount == roads.Length)
			return;

		var road = roads[roadCount];

		// Create snapshot's map layer
		road.mapLayer = Instantiate(roadLayerPrefab);
		int classification = classificationIndex == ClassificationIndex.None ? 0 : 1 << ((int)classificationIndex - 1);
		road.mapLayer.Init(networkPatch, classification, points);
		toolLayers.Add(road.mapLayer, road.mapLayer.Grid, "newRoad", networkLayer.Color);	//+ Don't use network color

		activeRoadLayers.Add(road.mapLayer);

		road.uiElement.IsInteractable = true;
		road.uiElement.label.placeholder.GetComponent<Text>().text = UpdateRoadName(classificationIndex, out string letter);
		road.uiElement.letter.GetComponent<Text>().text = letter;
        road.uiElement.letter.gameObject.SetActive(true);

        roadCount++;
		removeRoadToggle.interactable = true;

		if (roadCount == roads.Length)
		{
			createRoadToggle.interactable = false;
			SetAction(Action.None);
		}

		UpdateReachabilityGrid();
	}

	private void RemoveRoad(RoadInfo road)
	{
		activeRoadLayers.Remove(road.mapLayer);

		// Delete the map layer
		toolLayers.Remove(road.mapLayer);
		Destroy(road.mapLayer);
		road.mapLayer = null;

		ResetRoadUI(road.uiElement);

		roadCount--;

		if (roadCount == 0)
		{
			SetAction(Action.None);
		}
		else
		{
			// Reorder UI elements
			int index = road.uiElement.transform.GetSiblingIndex();
			if (index < roadCount)
			{
				// Move deleted road UI element to last position
				int lastIndex = roads.Length - 1;
				road.uiElement.transform.SetSiblingIndex(lastIndex);

				// Swap road with last one
				var temp = roads[lastIndex];
				roads[lastIndex] = roads[index];
				roads[index] = temp;
			}
		}

		createRoadToggle.interactable = true;

		UpdateReachabilityGrid();
	}

	private void ResetRoadUI(RoadToggle uiElement)
	{
		uiElement.IsInteractable = false;
		uiElement.label.placeholder.GetComponent<Text>().text = Translator.Get("Empty");
		uiElement.label.text = "";
	}

	private void DeleteAllRoads()
	{
		activeRoadLayers.Clear();

		foreach (var r in roads)
		{
			if (r.mapLayer != null)
			{
				toolLayers.Remove(r.mapLayer);

				Destroy(r.mapLayer.gameObject);
				r.mapLayer = null;
			}
		}

		roadCount = 0;
	}

	private void DestroyPlaceStart()
	{
		if (placeStart != null)
		{
			// Stop any ongoing grid generation
			placeStart.StopGridGeneration();

			Destroy(placeStart.gameObject);
			placeStart = null;
		}
	}

	private void StartCreateRoad()
	{
		if (lineTool == null)
		{
			lineTool = Instantiate(lineToolPrefab);
			lineTool.name = lineToolPrefab.name;
			lineTool.ForceDrawingMethod(LineTool.Method.Dragging);
			lineTool.OnFinishDrawing += LineTool_OnFinishDrawing;
			lineTool.OnCancel += LineTool_OnCancel;
		}
		lineTool.Init(networkPatch, map);
		lineTool.Activate();

		EnableClassificationToggles(true);
	}

	private void FinishCreateRoad()
	{
		if (lineTool != null)
		{
			lineTool.Deactivate();
			lineTool.OnFinishDrawing -= LineTool_OnFinishDrawing;
			lineTool.OnCancel -= LineTool_OnCancel;

			Destroy(lineTool.gameObject);
			lineTool = null;
		}
		EnableClassificationToggles(false);
	}

	private void AllowRemoveRoads(bool allow)
	{
		foreach (var road in roads)
		{
			road.uiElement.AllowRemove(allow);
		}
	}

	private void EnableClassificationToggles(bool enable)
	{
		var classificationList = highwayButton.transform.parent;
		foreach (Transform toggle in classificationList)
		{
			toggle.GetComponent<Toggle>().interactable = enable;
            if (!toggle.GetComponent<Toggle>().isOn)
                toggle.GetChild(0).GetComponent<Text>().color = (!enable) ? toggle.GetComponent<Toggle>().colors.disabledColor : toggle.GetComponent<ToggleTint>().colorOff;
        }
	}

	private void SetNetworkPatch(GraphPatch patch)
	{
		networkPatch = patch;

		if (placeStart != null)
			placeStart.NetworkPatch = networkPatch;

		if (patch == null)
		{
			ClearAllUserChanges();
			return;
		}

		var bounds = map.MapCoordBounds;
		if (!reachabilityLayer.HasPatches(siteBrowser.ActiveSite, map.CurrentLevel, bounds.west, bounds.east, bounds.north, bounds.south))
			CreateReachabilityPatch(patch);

		if (!dataLayers.IsLayerActive(reachabilityLayer))
			dataLayers.ActivateLayer(reachabilityLayer, true);

		if (placeStart != null)
			placeStart.ReachabilityPatch = reachabilityPatch;

		// Hide Loading Message
		if (!HasErrorMessage)
			HideMessage();
	}

	private void CreateReachabilityPatch(GraphPatch graphPatch)
	{
		GridData grid = new GridData(graphPatch.grid, false);
		grid.InitGridValues();
		grid.minValue = 0;
		grid.maxValue = traveTimeSlider.value * travelTimeScale;
		grid.minFilter = grid.maxFilter = 0;
		grid.metadata = null;

		reachabilityPatch = reachabilityLayer.CreateGridPatch(graphPatch.GetSiteName(), graphPatch.Level, DateTime.Now.Year, grid);
		dataLayers.UpdateLayer(reachabilityLayer);
    }

	private float nextUpdateTime = 0;
	private Coroutine isochroneUpdateCR = null;
	private void DelayedUpdateIsochrone()
	{
		nextUpdateTime = Time.time + 0.35f;
		if (isochroneUpdateCR == null)
		{
			isochroneUpdateCR = StartCoroutine(DelayedIsochroneUpdate());
		}
	}

	private IEnumerator DelayedIsochroneUpdate()
	{
		while (Time.time < nextUpdateTime)
		{
			yield return null;
		}
		UpdateReachabilityGrid();
		isochroneUpdateCR = null;
	}

	private void ShowError(string msg)
	{
		// Avoid overriding an error message
		if (HasErrorMessage)
			return;

		currentErrorMessage = msg;

		ShowMessage(Translator.Get(msg), MessageType.Error);
	}

	private void ShowError(MessageBuilder builder)
	{
		// Avoid overriding an error message
		if (HasErrorMessage)
			return;

		currentErrorBuilder = builder;

		ShowMessage(builder.Message, MessageType.Error);
	}

	private void HideErrorMessage()
	{
		HideMessage();
		ResetErrorMessage();
	}

	private void ResetErrorMessage()
	{
		currentErrorMessage = null;
		currentErrorBuilder = null;
	}

	private void UpdateReachabilityGrid()
	{
		if (placeStart != null && placeStart.HasStartPoints)
		{
			placeStart.UpdateGrid();
		}
	}

	private bool ActiveSiteHasLayer(DataLayer layer)
	{
		return siteBrowser.ActiveSite.HasDataLayer(layer);
	}

	private void ClearMobilityModes()
	{
		mobilityModes.Clear();

		// Remove UI
		var mobilityList = mobilityGroup.transform;
		for (int i = mobilityList.childCount - 1; i >= 0; --i)
		{
			Destroy(mobilityList.GetChild(i).gameObject);
		}
	}

	private void LoadSiteMobilityModes()
	{
		ClearMobilityModes();

		string activeSiteName = siteBrowser.ActiveSite.Name;
		string filename = Paths.Data + "Reachability" + Path.DirectorySeparatorChar + activeSiteName + ".csv";
		StartCoroutine(ReachabilityIO.Load(filename, (modes) =>
		{
			if (modes == null || modes.Count == 0)
			{
				CreateDefaultMobilityModes(filename);
			}
			else
			{
				mobilityModes.AddRange(modes);
			}
			OnMobilityModesLoaded();
		}, () =>
		{
			Debug.LogWarning("Couldn't find mobility data for " + activeSiteName);
			CreateDefaultMobilityModes(filename);
			OnMobilityModesLoaded();
		}));
	}

	private void CreateDefaultMobilityModes(string filename)
	{
		var mode = new MobilityMode() { name = "Car"/*translatable*/ };
		mode.speeds[(int)ClassificationIndex.Highway]		= 80;
		mode.speeds[(int)ClassificationIndex.HighwayLink]	= 50;
		mode.speeds[(int)ClassificationIndex.Primary]		= 50;
		mode.speeds[(int)ClassificationIndex.Secondary]		= 35;
		mode.speeds[(int)ClassificationIndex.Other]			= 15;
		mode.speeds[(int)ClassificationIndex.None]			= defaultWalkSpeed;
		mobilityModes.Add(mode);

		mode = new MobilityMode() { name = "Motorbike"/*translatable*/ };
		mode.speeds[(int)ClassificationIndex.Highway]		= 60;
		mode.speeds[(int)ClassificationIndex.HighwayLink]	= 40;
		mode.speeds[(int)ClassificationIndex.Primary]		= 30;
		mode.speeds[(int)ClassificationIndex.Secondary]		= 30;
		mode.speeds[(int)ClassificationIndex.Other]			= 15;
		mode.speeds[(int)ClassificationIndex.None]			= defaultWalkSpeed;
		mobilityModes.Add(mode);

		mode = new MobilityMode() { name = "Walk"/*translatable*/ };
		mode.speeds[(int)ClassificationIndex.Highway]		= 0;	// Can't walk on highway
		mode.speeds[(int)ClassificationIndex.HighwayLink]	= 0;    // Can't walk on highway link
		mode.speeds[(int)ClassificationIndex.Primary]		= defaultWalkSpeed;
		mode.speeds[(int)ClassificationIndex.Secondary]		= defaultWalkSpeed;
		mode.speeds[(int)ClassificationIndex.Other]			= defaultWalkSpeed;
		mode.speeds[(int)ClassificationIndex.None]			= defaultWalkSpeed;
		mobilityModes.Add(mode);

		ReachabilityIO.Save(mobilityModes, filename);
	}

	private void UpdateTravelTimeLabel(float travelTime)
	{
		timeLabel.text = travelTime + " " + Translator.Get("minutes");
	}
}
