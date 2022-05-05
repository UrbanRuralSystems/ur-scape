﻿// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          David Neudecker(neudecker@arch.ethz.ch)

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class ContoursTool : Tool
{
    public class Snapshot
    {
        public GridMapLayer mapLayer;
        public RectTransform uiTransform;
		public List<DataLayerPanel> activeLayerPanels;
        public string name;
		public string id;
    }

    [Header("General Setup")]
    public int snapshotCount = 5;

    [Header("Prefabs")]
    public ContoursMapLayer contoursLayerPrefab;
    public GridMapLayer snapshotLayerPrefab;
    public RectTransform snapshotPrefab;
    public RectTransform emptySnapshotPrefab;
	public ContoursInfoPanel infoPanelPrefab;
	public EditColourPanel editColourPanelPrefab;

	[Header("UI References")]
	public Toggle showContoursToggle;
	public Toggle cropToggle;
    public Toggle lockToggle;
    public Toggle analysisToggle;
	public Scrollbar analysisProgress;
	public Toggle excludeNoDataToggle;
	public Button addButton;
    public Toggle editToggle;
    public Toggle deleteToggle;
	public Transform SnapshotList;

	// Prefab Instances
    public ContoursInfoPanel InfoPanel { private set; get; }
	private ContoursMapLayer contoursLayer;
    public ContoursMapLayer ContoursLayer { get { return contoursLayer; } }
	private EditColourPanel editColourPanel;

	// Component References
	private DataLayers dataLayers;
    private InputHandler inputHandler;
	public LegendPanel LegendPanl { private set; get; }

    // UI References
    private Snapshot[] snapshots = null;
	private int usedSnapshots = 0;

	// Misc
	private List<GridData> grids = new List<GridData>();
    private GridMapLayer lockedContours;
    private ContoursAnalyzer analyzer;
	private int runningSnapshotCounter;
	private bool allowInfoUpdate = true;
	private Transform centerTopContainerT;

	public bool IsToggled { get; private set; }
	public Snapshot[] Snapshots { get { return snapshots; } }

	//
	// Inheritance Methods
	//

	protected override void OnComponentRegistrationFinished()
    {
        base.OnComponentRegistrationFinished();

        // Get Components
        dataLayers = ComponentManager.Instance.Get<DataLayers>();
        inputHandler = ComponentManager.Instance.Get<InputHandler>();

		centerTopContainerT = GameObject.Find("CenterTopContainer").transform;
		LegendPanl = centerTopContainerT.Find("Footer").Find("LegendPanel").GetComponent<LegendPanel>();

		// Initialize edit colour panel
		editColourPanel = Instantiate(editColourPanelPrefab, centerTopContainerT, false);
		editColourPanel.gameObject.SetActive(false);
	}

    public override bool HasBookmarkData()
    {
        return false;
    }

    public override void SaveToBookmark(BinaryWriter bw, string bookmarkPath)
    {
		/*
        bw.Write(showContoursToggle.isOn);
		bw.Write(filteredToggle.isOn);
        bw.Write(lockToggle.isOn);
        bw.Write(analysisToggle.isOn);
		bw.Write(excludeNoDataToggle.isOn);

		int count = snapshots.Count;
        bw.Write(count);

        for (int i = 0; i < count; i++)
        {
            var snapshot = snapshots[i];
            bw.Write(snapshot.mapLayer != null);
            if (snapshot.mapLayer != null)
            {
                bw.Write(snapshot.name);
                snapshot.mapLayer.Grid.SaveBin(bookmarkPath + "snapshot" + i);
            }
        }
		*/
    }

    public override void LoadFromBookmark(BinaryReader br, string bookmarkPath)
    {
		/*
        bool showContours = br.ReadBoolean();
        bool showFiltered = br.ReadBoolean();
        bool lockContours = br.ReadBoolean();
        bool analysisIsOn = br.ReadBoolean();
		bool excludeIsOn = br.ReadBoolean();

        int count = Mathf.Min(br.ReadInt32(), snapshots.Length);
        for (int i = 0; i < count; i++)
        {
            var snapshot = snapshots[i];

            // Delete the UI element
            Destroy(snapshot.uiTransform.gameObject);

            // Delete the map layer
            DeleteMapLayer(ref snapshot.mapLayer);

            bool hasMap = br.ReadBoolean();
            if (hasMap)
            {
                snapshot.name = br.ReadString();
                CreateSnapshot(i);
                var grid = snapshot.mapLayer.Grid;
				grid.LoadBin(bookmarkPath + "snapshot" + i);

                // Trigger callbacks
                grid.ChangeBounds(grid.west, grid.east, grid.north, grid.south);
                grid.GridChanged();
            }
            else
            {
                snapshot.name = null;
				snapshot.uiTransform = Instantiate(emptySnapshotPrefab, transform, false);
            }
        }

        filteredToggle.isOn = showFiltered;
        lockToggle.isOn = lockContours;
        showToggle.isOn = showContours;
        analysisToggle.isOn = analysisIsOn;
		excludeNoDataToggle.isOn = excludeIsOn;
		+*/
	}


	//
	// UI Events
	//

	private void OnShowContoursChanged(bool isOn)
    {
		CancelRemoveSnapshotMode();

		// Show/hide contours layer
		ShowContoursLayer(isOn && !lockToggle.isOn);

        if (lockedContours != null)
            lockedContours.Show(isOn && lockToggle.isOn);

		UpdateContoursInfo();
	}

	private void OnCropWithViewAreaChanged(bool isOn)
    {
		CancelRemoveSnapshotMode();

		contoursLayer.SetCropWithViewArea(isOn);
		contoursLayer.Refresh();
	}

    private void OnLockChanged(bool isOn)
    {
		CancelRemoveSnapshotMode();

		if (isOn)
        {
			contoursLayer.FetchGridValues();
			lockedContours = toolLayers.CreateGridMapLayer(snapshotLayerPrefab, "LockedContours", contoursLayer.Grid);
            lockedContours.Show(showContoursToggle.isOn);
        }
        else
        {
            DeleteMapLayer(ref lockedContours);
        }

        // Show/hide contours layer at the end!
        if (showContoursToggle.isOn)
            ShowContoursLayer(!isOn);

		UpdateContoursInfo();
	}

	protected override void OnToggleTool(bool isOn)
    {
        IsToggled = isOn;
        var inspectorTool = ComponentManager.Instance.Get<InspectorTool>();
        if (isOn)
        {
			runningSnapshotCounter = 1;

			// Create contours layer
			contoursLayer = toolLayers.CreateGridMapLayer(contoursLayerPrefab, "Contours");
			contoursLayer.ExcludeCellsWithNoData(excludeNoDataToggle.isOn);

			// Listen to any data layers being added/removed
			dataLayers.OnLayerVisibilityChange += OnLayerVisibilityChange;
			dataLayers.OnLayerAvailabilityChange += OnLayerAvailabilityChange;

			// Listen to any grid layers being added/removed
			var gridController = map.GetLayerController<GridLayerController>();
            gridController.OnShowGrid += OnShowGrid;

            // Initialize grids list with already visible grid layers
            grids.Clear();
            foreach (var mapLayer in gridController.mapLayers)
            {
                grids.Add(mapLayer.Grid);
            }

			// Reset buttons
			analysisToggle.isOn = false;
            showContoursToggle.isOn = true;
            lockToggle.isOn = false;
			//filteredToggle.isOn = true;		// Don't reset, user wants to keep this setting
			//excludeNoDataToggle.isOn = true;	// Don't reset, user wants to keep this setting
			deleteToggle.isOn = false;
			deleteToggle.interactable = false;
			addButton.interactable = true;
			cropToggle.isOn = contoursLayer.CropWithViewArea;

			// Initialize listeners
			addButton.onClick.AddListener(OnAddSnapshotClick);
            deleteToggle.onValueChanged.AddListener(OnDeleteToggleChange);
            showContoursToggle.onValueChanged.AddListener(OnShowContoursChanged);
			cropToggle.onValueChanged.AddListener(OnCropWithViewAreaChanged);
            lockToggle.onValueChanged.AddListener(OnLockChanged);
            analysisToggle.onValueChanged.AddListener(OnAnalysisChanged);
			excludeNoDataToggle.onValueChanged.AddListener(OnExcludeNoDataChanged);
			editToggle.onValueChanged.AddListener(OnEditColourToggleChanged);

			// Show contours layer
			ShowContoursLayer(true);

			// Create the info panel
			InfoPanel = Instantiate(infoPanelPrefab);
			InfoPanel.name = infoPanelPrefab.name;
			var outputPanel = ComponentManager.Instance.Get<OutputPanel>();
			outputPanel.AddPanel("Contours", InfoPanel.transform);

			var translator = LocalizationManager.Instance;
			InfoPanel.AddEntry("CC", translator.Get("Current Contours"));
			InfoPanel.AddEntry("SC", translator.Get("Selected Contours"));

			snapshots = new Snapshot[snapshotCount];
			usedSnapshots = 0;

			UpdateContoursInfo();

			translator.OnLanguageChanged += OnLanguageChanged;

			GuiUtils.RebuildLayout(SnapshotList);
        }
        else
        {
			editToggle.isOn = false;
			editToggle.gameObject.SetActive(false);

			// Remove listeners
			addButton.onClick.RemoveAllListeners();
			showContoursToggle.onValueChanged.RemoveAllListeners();
			cropToggle.onValueChanged.RemoveAllListeners();
			lockToggle.onValueChanged.RemoveAllListeners();
			analysisToggle.onValueChanged.RemoveAllListeners();
			excludeNoDataToggle.onValueChanged.RemoveAllListeners();
			dataLayers.OnLayerVisibilityChange -= OnLayerVisibilityChange;
			dataLayers.OnLayerAvailabilityChange -= OnLayerAvailabilityChange;
			inputHandler.OnLeftMouseUp -= OnLeftMouseUp;
			editToggle.onValueChanged.RemoveListener(OnEditColourToggleChanged);

			if (map)
            {
				var gridController = map.GetLayerController<GridLayerController>();
                if (gridController)
                {
                    gridController.OnShowGrid -= OnShowGrid;
                }
            }

			// Remove the info panel
			ComponentManager.Instance.Get<OutputPanel>().DestroyPanel("Contours");

			// Remove map layers
			DeleteAllLayers();

			// Remove snapshots UI
			foreach (var snapshot in snapshots)
			{
				if (snapshot != null)
				{
					Destroy(snapshot.uiTransform.gameObject);
					LegendPanl.RemoveSnapshotItem(snapshot.id);
				}
			}
			snapshots = null;
            usedSnapshots = 0;

			grids.Clear();

			dataLayers.ResetToolOpacity();

			CancelRemoveSnapshotMode();

			LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;

            if (inspectorTool.InspectOutput)
                inspectorTool.InspectOutput.AreaOutput.ResetAndClearContourOutput();
		}

        if (inspectorTool.InspectOutput)
            inspectorTool.InspectOutput.AreaOutput.ShowAreaTypeHeaderAndDropdown(isOn);
	}

	protected override void OnActiveTool(bool isActive)
    {
        if (InfoPanel != null)
        {
			ComponentManager.Instance.Get<OutputPanel>().SetPanel(isActive ? InfoPanel.transform : null);

            if (isActive)
				UpdateContoursInfo();
			else
				editToggle.isOn = false;
		}

		if (analysisToggle.isOn)
		{
			if (isActive)
				inputHandler.OnLeftMouseUp += OnLeftMouseUp;
			else
				inputHandler.OnLeftMouseUp -= OnLeftMouseUp;
		}
	}

    private void OnLayerVisibilityChange(DataLayer layer, bool visible)
    {
		UpdateLayersOpacity();
	}

	private void OnLayerAvailabilityChange(DataLayer layer, bool visible)
	{
		if (dataLayers.IsLayerActive(layer))
		{
			UpdateLayersOpacity();
		}
	}

	private void OnAddSnapshotClick()
    {
		CancelRemoveSnapshotMode();

		contoursLayer.FetchGridValues();
		for (int i = 0; i < snapshots.Length; i++)
		{
			if (snapshots[i] == null)
			{
				CreateSnapshot(i, contoursLayer.Grid);
				break;
			}
		}

		if (usedSnapshots == snapshotCount)
		{
            addButton.interactable = false;
        }

        deleteToggle.interactable = true;
    }

    private void OnDeleteToggleChange(bool isOn)
    {
        foreach (var snapshot in snapshots)
        {
			if (snapshot != null)
			{
				var toggle = snapshot.uiTransform.GetComponentInChildren<Toggle>(true);
				toggle.gameObject.SetActive(!isOn);

				var deleteButton = snapshot.uiTransform.GetComponentInChildren<Button>(true);
				deleteButton.gameObject.SetActive(isOn);
			}
		}
    }

	private void OnRemoveSnapshot(Snapshot snapshot)
	{
        RemoveSnapshot(snapshot);
 
		addButton.interactable = true;
		if (usedSnapshots == 0)
		{
			deleteToggle.isOn = false;
			deleteToggle.interactable = false;
		}
    }

	private void OnEndEdit(string value, InputField input, Snapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            input.text = snapshot.name;
        }

		var newSnapshotName = Regex.Replace(input.text, @"\n|\r", "");
		snapshot.name = newSnapshotName;
		InfoPanel.RenameEntry(snapshot.id, newSnapshotName);
		editColourPanel.RenameEntry(snapshot.uiTransform.GetSiblingIndex(), newSnapshotName);
    }

    private void OnSnapshotToggleChanged(Snapshot snapshot, bool isOn)
    {
        snapshot.mapLayer.Show(isOn);
    }

    private void OnAnalysisChanged(bool isOn)
    {
		CancelRemoveSnapshotMode();

		if (isOn)
        {
            CreateAnalyzer();
            StartAnalysis();
            inputHandler.OnLeftMouseUp += OnLeftMouseUp;
        }
        else
        {
			DeselectContour();
			DestroyAnalyzer();
            var inspectorTool = ComponentManager.Instance.Get<InspectorTool>();
            if (inspectorTool.InspectOutput)
                inspectorTool.InspectOutput.AreaOutput.ResetAndClearContourOutput();
            inputHandler.OnLeftMouseUp -= OnLeftMouseUp;
         }
    }

	private void OnExcludeNoDataChanged(bool isOn)
	{
		CancelRemoveSnapshotMode();

		contoursLayer.ExcludeCellsWithNoData(isOn);
		contoursLayer.Refresh();
	}

	private void OnLanguageChanged()
	{
		var translator = LocalizationManager.Instance;
		InfoPanel.RenameEntry("CC", translator.Get("Current Contours"));
		InfoPanel.RenameEntry("SC", translator.Get("Selected Contours"));

		//for (int i = 0; i < snapshotCount; ++i)
		//{
		//	if (snapshots[i] == null)
		//		continue;

		//	string newSnapshotName = translator.Get(snapshots[i].name, false);
		//	InputField snapshotLabel = snapshots[i].uiTransform.GetComponentInChildren<InputField>();

		//	snapshotLabel.text = newSnapshotName;
		//	InfoPanel.RenameEntry("S" + i, newSnapshotName);
		//	editColourPanel.RenameEntry(snapshots[i].uiTransform.GetSiblingIndex(), newSnapshotName);
		//}
	}

	private void OnEditColourToggleChanged(bool isOn)
	{
		editColourPanel.gameObject.SetActive(isOn);
	}


	//
	// Event Methods
	//

	private Coordinate contourSelectionCoords;
	private void OnLeftMouseUp()
    {
        if (!inputHandler.IsDraggingLeft)
        {
            if (inputHandler.GetWorldPoint(Input.mousePosition, out Vector3 worldPos))
            {
				contourSelectionCoords = map.GetCoordinatesFromUnits(worldPos.x, worldPos.z);
				UpdateSelectedContour(true);

                var inspectorTool = ComponentManager.Instance.Get<InspectorTool>();
                if (inspectorTool.InspectOutput)
                {
                    var areaInspector = inspectorTool.areaInspectorPanel.areaInspector;
                    
                    inspectorTool.InspectOutput.ShowHeader(InspectorTool.InspectorType.Area, true);
                    inspectorTool.InspectOutput.ShowPropertiesAndSummaryLabel(InspectorTool.InspectorType.Area, true);
                }
            }
        }
    }

    private void OnShowGrid(GridMapLayer mapLayer, bool show)
    {
        var grid = mapLayer.Grid;
        if (show)
        {
			// Add to contours layer
			if (contoursLayer.IsVisible())
			{
				// Temporarily disable updating info. Will be updated at the end of the method
				allowInfoUpdate = false;
				contoursLayer.Add(grid);
				contoursLayer.Refresh();
				allowInfoUpdate = true;
			}
			else
				grids.Add(grid);
        }
        else
        {
			// Remove from contours layer
			if (contoursLayer.IsVisible())
			{
				// Temporarily disable updating info. Will be updated at the end of the method
				allowInfoUpdate = false;
				contoursLayer.Remove(grid);
				contoursLayer.Refresh();
				allowInfoUpdate = true;
			}
			else
				grids.Remove(grid);
            grid.patch.DataLayer.SetToolOpacity(1);
        }

		UpdateContoursInfo();

		UpdateLayersOpacity();
    }

    private void OnContoursGridChange(GridData grid)
    {
		OnContoursChange();
	}

	private void OnContoursValuesChange(GridData grid)
	{
		OnContoursChange();
	}

	private void OnContoursChange()
	{
		if (analyzer != null)
		{
			if (contoursLayer.Grid.values != null)
			{
				StartAnalysis();
			}
			else
			{
				StopAnalysis();
			}
		}
		else
		{
			UpdateContoursInfo();
		}
	}

	private void OnAnalysisProgress(float progress)
    {
        if (progress >= 1)
        {
            // Hide progress
            analysisProgress.gameObject.SetActive(false);

            if (contoursLayer != null)
            {
				contoursLayer.SubmitGridValues();
			}

			UpdateSelectedContour(false);
		}
		else
        {
            analysisProgress.size = progress;
        }
    }

	//
	// Private Methods
	//

	private void ShowContoursLayer(bool show)
    {
        if (show)
        {
            // Add visible grids to the contours layer
            foreach (var g in grids)
                contoursLayer.Add(g);
            grids.Clear();

            // Show locked or normal
            if (lockedContours != null)
                lockedContours.Show(true);
            else
			{
                contoursLayer.Show(true);

				// Update contours' data
				contoursLayer.Refresh();

				// Add listeners after refreshing
				contoursLayer.Grid.OnGridChange += OnContoursGridChange;
				contoursLayer.Grid.OnValuesChange += OnContoursValuesChange;
			}

			dataLayers.AutoReduceToolOpacity();
		}
		else
        {
            // Hide at the beginning
            contoursLayer.Show(false);
            contoursLayer.Grid.OnGridChange -= OnContoursGridChange;
			contoursLayer.Grid.OnValuesChange -= OnContoursValuesChange;

			// Move visible grids from contours layer to list
			if (contoursLayer.grids.Count > 0)
            {
                grids.AddRange(contoursLayer.grids);
                contoursLayer.Clear();
            }

			dataLayers.ResetToolOpacity();
        }
    }

    private void CreateSnapshot(int i, GridData gridData = null)
    {
		var id = i + 1;
		var snapshotLabel = Translator.Get("Snapshot") + "\n" + id;
		var snapshotName = Regex.Replace(snapshotLabel, @"\n|\r", "");

		// Create Snapshot
		var snapshot = new Snapshot
		{
			id = "S" + i,
			name = snapshotName
		};
		snapshot.uiTransform = Instantiate(snapshotPrefab, SnapshotList, false);
		snapshot.uiTransform.name = snapshotName;
		snapshot.uiTransform.SetSiblingIndex(i);

		// Setup toggle button
		var toggleButton = snapshot.uiTransform.GetComponentInChildren<Toggle>();
		toggleButton.onValueChanged.AddListener((isOn) => OnSnapshotToggleChanged(snapshot, isOn));

		// Setup delete button
		var deleteButton = snapshot.uiTransform.GetComponentInChildren<Button>();
		deleteButton.gameObject.SetActive(false);
		deleteButton.onClick.AddListener(() => OnRemoveSnapshot(snapshot));

		// Setup label
		snapshot.uiTransform.GetComponentInChildren<Text>().text = id.ToString();

		// Setup intput text
		var input = snapshot.uiTransform.GetComponentInChildren<InputField>();
		input.text = snapshotLabel;
		input.onEndEdit.AddListener((value) => OnEndEdit(value, input, snapshot));

		// Create snapshot's map layer
		snapshot.mapLayer = toolLayers.CreateGridMapLayer(snapshotLayerPrefab, $"ContoursSnapshot_{snapshot.id}", gridData);

		// Set Snapshot color
		SetColorsToSnapshot(snapshot.mapLayer, toggleButton, deleteButton);

		// Update output and legend panel
		string titleLabel = $"{LocalizationManager.Instance.Get("Snapshot")} {i + 1}";
		Color snapshotColor = toggleButton.image.color;
		string snapshotID = snapshot.id;
		InfoPanel.AddEntry(snapshotID, titleLabel, true);
		LegendPanl.AddSnapshotItem(snapshotID, snapshotColor, titleLabel);

		if (gridData != null)
		{
			var sqm = ContourUtils.GetContoursSquareMeters(gridData);
			InfoPanel.UpdateEntry(snapshotID, sqm);
		}
		InfoPanel.SetSnapshotProperties(snapshotID, i + 1, toggleButton.image.color);

		snapshots[i] = snapshot;
		editColourPanel.AddSnapshotColour(i, toggleButton.image.color);
		usedSnapshots++;
		editToggle.gameObject.SetActive(usedSnapshots >= 1);

		runningSnapshotCounter++;

		InfoPanel.UpdateRelativeToSnapshotDropdown();

		GuiUtils.RebuildLayout(SnapshotList);
	}

    private void RemoveSnapshot(Snapshot snapshot)
    {
		// Delete snaphot item from legend panel
		LegendPanl.RemoveSnapshotItem(snapshot.id);

		// Delete the UI element
		editColourPanel.RemoveSnapshotColour(snapshot.uiTransform.GetSiblingIndex());
		Destroy(snapshot.uiTransform.gameObject);
		GuiUtils.RebuildLayout(SnapshotList);

		// Delete the map layer
		DeleteMapLayer(ref snapshot.mapLayer);

		//infoPanel.ClearEntry(snapshot.id);
		InfoPanel.RemoveEntry(snapshot.id);

		for (int i = 0; i < snapshotCount; i++)
		{
			if (snapshots[i] == snapshot)
			{
				snapshots[i] = null;
				usedSnapshots--;
				if (usedSnapshots < 1)
					editToggle.isOn = false;
				editToggle.gameObject.SetActive(usedSnapshots >= 1);
				break;
			}
		}

		InfoPanel.UpdateRelativeToSnapshotDropdown();
	}

    private void DeleteAllLayers()
    {
        contoursLayer.Grid.OnGridChange -= OnContoursGridChange;
		contoursLayer.Grid.OnValuesChange -= OnContoursValuesChange;
		contoursLayer.Clear();

        var layer = contoursLayer as GridMapLayer;
        DeleteMapLayer(ref layer);

        if (lockedContours != null)
        {
            DeleteMapLayer(ref lockedContours);
        }

		for (int i = 0; i < snapshotCount; ++i)
        {
            if (snapshots[i] != null && snapshots[i].mapLayer != null)
            {
                // Delete the map layer
                DeleteMapLayer(ref snapshots[i].mapLayer);
            }
        }
    }

    private void DeleteMapLayer(ref GridMapLayer mapLayer)
    {
        if (mapLayer != null)
        {
            toolLayers.Remove(mapLayer);
            Destroy(mapLayer.gameObject);
            mapLayer = null;
        }
    }

    private void CreateAnalyzer()
    {
        if (analyzer == null)
        {
            var go = new GameObject("ContoursAnalyzer");
            analyzer = go.AddComponent<ContoursAnalyzer>();
        }
    }

    private void DestroyAnalyzer()
    {
        if (analyzer != null)
        {
            Destroy(analyzer.gameObject);
            analyzer = null;
        }
    }

    private void StartAnalysis()
    {
        analysisProgress.size = 0;
        analysisProgress.gameObject.SetActive(true);

		analyzer.Analyze(contoursLayer, OnAnalysisProgress);
    }

    private void StopAnalysis()
    {
        analyzer.Stop();
        analysisProgress.gameObject.SetActive(false);
	}

	private void UpdateSelectedContour(bool updateInfo)
	{
		var grid = contoursLayer.Grid;

		if (grid.values != null && grid.IsInside(contourSelectionCoords.Longitude, contourSelectionCoords.Latitude))
		{
			int index = (int)grid.GetValue(contourSelectionCoords.Longitude, contourSelectionCoords.Latitude);
			if (index > 0)
			{
				contoursLayer.SetSelectedContour(index);

				if (isActive && updateInfo)
				{
					var sqm = ContourUtils.GetContoursSquareMeters(grid, true, index);
					InfoPanel.UpdateEntry("SC", sqm);

                    contoursLayer.CalculateSelectedContourValues();
                    var inspectorTool = ComponentManager.Instance.Get<InspectorTool>();
                    if (inspectorTool.InspectOutput)
                        inspectorTool.InspectOutput.AreaOutput.UpdateContourInspectorOutput(dataLayers);
				}
				return;
			}
		}

		DeselectContour();
	}

	private void DeselectContour()
	{
		if (contoursLayer.SelectedContour > 0)
		{
			contoursLayer.DeselectContour();
			if (isActive)
			{
				InfoPanel.ClearEntry("SC");
			}
		}
	}

	private void SetColorsToSnapshot(GridMapLayer mapLayer, Toggle toggle, Button deleteButton)
	{
		float scale = 1.0f / (snapshotCount + 1);
		float snapshotHue = (runningSnapshotCounter * scale) % 1f;

		Color color = Color.HSVToRGB(snapshotHue, 0.3f, 1f);
		toggle.image.color = color;
		toggle.GetComponent<ToggleTint>().colorOff = color;
		deleteButton.image.color = color;

		// set color for mapLayer
		mapLayer.SetColor(Color.HSVToRGB(snapshotHue, 1f, 1f));
	}

	private void CancelRemoveSnapshotMode()
	{
		if (deleteToggle.isOn)
			deleteToggle.isOn = false;
	}

	private void UpdateLayersOpacity()
	{
		if (showContoursToggle.isOn)
			dataLayers.AutoReduceToolOpacity();
	}

	private int nextUpdateFrame = 0;
	private Coroutine contoursInfoCoroutine = null;
	private bool needsUpdate = false;

	private void UpdateContoursInfo()
	{
		if (contoursInfoCoroutine == null)
		{
			if (Time.frameCount >= nextUpdateFrame)
			{
				_UpdateContoursInfo();
			}
			else
			{
				contoursInfoCoroutine = StartCoroutine(ContoursInfoDeferredUpdate());
			}
		}
		else
		{
			needsUpdate = true;
		}
	}

	private IEnumerator ContoursInfoDeferredUpdate()
	{
		do
		{
			needsUpdate = false;
			while (Time.frameCount < nextUpdateFrame) yield return null;
			_UpdateContoursInfo();
		}
		while (needsUpdate);

		contoursInfoCoroutine = null;
	}

	private void _UpdateContoursInfo()
	{
		// Contours can change while the tool is not active (e.g. by changing a data layer's filter)
		// Check if tool is still active (could have been deactivated while creating analysis)
		if (isActive && allowInfoUpdate)
		{
			if (showContoursToggle.isOn && (contoursLayer.grids.Count > 0 || lockedContours != null))
			{
				contoursLayer.FetchGridValues();
				var sqm = ContourUtils.GetContoursSquareMeters(contoursLayer.Grid);

				// Show the stats FIRST to ensure proper UI colors
				InfoPanel.ShowStats(true);
				InfoPanel.UpdateEntry("CC", sqm);

				// Avoid doing multiple updates per frame (in case it's called outside of coroutine)
				nextUpdateFrame = Time.frameCount + 10;
			}
			else if (grids.Count > 0)
			{
				// Show the stats FIRST to ensure proper UI colors
				InfoPanel.ShowStats(true);
				InfoPanel.ClearEntry("CC");
			}
			else
			{
				// Show the stats LAST to ensure proper UI colors
				InfoPanel.ClearEntry("CC");
				InfoPanel.ShowStats(false);
			}
		}
	}
}
