// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          David Neudecker(neudecker@arch.ethz.ch)

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ContoursTool : Tool
{
    private class Snapshot
    {
        public GridMapLayer mapLayer;
        public RectTransform uiTransform;
        public string name;
    }

    [Header("General Setup")]
    public int snapshotCount = 5;

    [Header("Prefabs")]
    public ContoursMapLayer contoursLayerPrefab;
    public GridMapLayer snapshotLayerPrefab;
    public RectTransform snapshotPrefab;
    public RectTransform emptySnapshotPrefab;
    public ContoursOutput contoursOutputPrefab;

    [Header("UI References")]
	public Toggle showContoursToggle;
	public Toggle filteredToggle;
    public Toggle lockToggle;
    public Toggle analysisToggle;
	public Scrollbar analysisProgress;
	public Toggle excludeNoDataToggle;
	public Button addButton;
    public ToggleButton deleteToggle;
    public Transform SnapshotList;

	// Prefab Instances
    private ContoursOutput contoursOutput;
	private ContoursMapLayer contoursLayer;
    public ContoursMapLayer ContoursLayer { get { return contoursLayer; } }

    // Component References
    private DataLayers dataLayers;
    private DataManager dataManager;
    private InputHandler inputHandler;

    // UI References
    private readonly List<Snapshot> snapshots = new List<Snapshot>();
    private readonly List<RectTransform> emptySnapshots = new List<RectTransform>();

	// Misc
	private List<GridData> grids = new List<GridData>();
    private GridMapLayer lockedContours;
    private ContoursAnalyzer analyzer;
	private int runningSnapshotCounter;


    //
    // Inheritance Methods
    //

    protected override void OnComponentRegistrationFinished()
    {
        base.OnComponentRegistrationFinished();

        // Get Components
        dataLayers = ComponentManager.Instance.Get<DataLayers>();
        dataManager = ComponentManager.Instance.Get<DataManager>();
        inputHandler = ComponentManager.Instance.Get<InputHandler>();
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
    }

	private void OnShowFilteredChanged(bool isOn)
    {
		CancelRemoveSnapshotMode();

		dataManager.EnableSiteFilters(!filteredToggle.isOn);
    }

    private void OnLockChanged(bool isOn)
    {
		CancelRemoveSnapshotMode();

		if (isOn)
        {
			contoursLayer.FetchGridValues();
			lockedContours = CreateMapLayer(snapshotLayerPrefab, "LockedContours", contoursLayer.Grid);
            lockedContours.Show(showContoursToggle.isOn);
        }
        else
        {
            DeleteMapLayer(ref lockedContours);
        }

        // Show/hide contours layer at the end!
        if (showContoursToggle.isOn)
            ShowContoursLayer(!isOn);
    }

    public override void OnToggleTool(bool isOn)
    {
        if (isOn)
        {
			runningSnapshotCounter = 1;

			// Create contours layer
			contoursLayer = CreateMapLayer(contoursLayerPrefab, "Contours");
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
				mapLayer.Grid.OnFilterChange += OnGridFilterChange;
                grids.Add(mapLayer.Grid);
            }

            // Reset buttons
            showContoursToggle.isOn = true;
            lockToggle.isOn = false;
			//filteredToggle.isOn = true;		// Don't reset, user wants to keep this setting
			//excludeNoDataToggle.isOn = true;	// Don't reset, user wants to keep this setting
			deleteToggle.isOn = false;
			deleteToggle.interactable = false;
			addButton.interactable = true;

			// Initialize listeners
			addButton.onClick.AddListener(OnAddButtonClick);
            deleteToggle.onValueChanged.AddListener(OnDeleteToggleChange);
            showContoursToggle.onValueChanged.AddListener(OnShowContoursChanged);
			filteredToggle.onValueChanged.AddListener(OnShowFilteredChanged);
            lockToggle.onValueChanged.AddListener(OnLockChanged);
            analysisToggle.onValueChanged.AddListener(OnAnalysisChanged);
			excludeNoDataToggle.onValueChanged.AddListener(OnExcludeNoDataChanged);
            
            // Show contours layer
            ShowContoursLayer(true);

            // Create output panel
            contoursOutput = Instantiate(contoursOutputPrefab);
            contoursOutput.name = "Contours_OutputPanel";
			var outputPanel = ComponentManager.Instance.Get<OutputPanel>();
			outputPanel.SetPanel(contoursOutput.transform);
            contoursLayer.FetchGridValues();
            contoursOutput.AddGroup("All Contours", contoursLayer, true, false);

			// Create empty snapshots
			for (int i = 0; i < snapshotCount; ++i)
			{
				emptySnapshots.Add(Instantiate(emptySnapshotPrefab, SnapshotList, false));
			}
            GuiUtils.RebuildLayout(SnapshotList);
        }
        else
        {
			// Remove listeners
			addButton.onClick.RemoveAllListeners();
			showContoursToggle.onValueChanged.RemoveAllListeners();
			filteredToggle.onValueChanged.RemoveAllListeners();
			lockToggle.onValueChanged.RemoveAllListeners();
			analysisToggle.onValueChanged.RemoveAllListeners();
			excludeNoDataToggle.onValueChanged.RemoveAllListeners();
			dataLayers.OnLayerVisibilityChange -= OnLayerVisibilityChange;
			dataLayers.OnLayerAvailabilityChange -= OnLayerAvailabilityChange;

			if (map)
            {
                var gridController = map.GetLayerController<GridLayerController>();
                if (gridController)
                {
                    gridController.OnShowGrid -= OnShowGrid;
                }
            }

            // for output update
            foreach (var grid in contoursLayer.grids)
                grid.OnFilterChange -= OnGridFilterChange;

			var outputPanel = ComponentManager.Instance.Get<OutputPanel>();
			outputPanel.RemovePanel(contoursOutput.transform);

            // Remove map layers
            DeleteAllLayers();

			// Remove snapshots UI
			foreach (var snapshot in snapshots)
			{
				Destroy(snapshot.uiTransform.gameObject);
			}
			snapshots.Clear();
			foreach (var emptySnapshot in emptySnapshots)
            {
                Destroy(emptySnapshot.gameObject);
            }
			emptySnapshots.Clear();

			grids.Clear();

			dataLayers.ResetToolOpacity();

			CancelRemoveSnapshotMode();
		}

		// Enable/disable map layer filters
		dataManager.EnableSiteFilters(!(isOn && filteredToggle.isOn));
	}

    public override void OnActiveTool(bool isActive)
    {
        if (isActive && contoursOutput != null)
        {
			var outputPanel = ComponentManager.Instance.Get<OutputPanel>();
			outputPanel.SetPanel(contoursOutput.transform);
        }
    }

    private void OnGridFilterChange(GridData grid)
    {
		contoursOutput.Grid_OnFilterChange();
    }

    private void OnLayerVisibilityChange(DataLayer layer, bool visible)
    {
		dataLayers.AutoReduceToolOpacity();
	}

	private void OnLayerAvailabilityChange(DataLayer layer, bool visible)
	{
		if (dataLayers.IsLayerActive(layer))
		{
			dataLayers.AutoReduceToolOpacity();
		}
	}

	private void OnAddButtonClick()
    {
		CancelRemoveSnapshotMode();

		contoursLayer.FetchGridValues();
		CreateSnapshot(snapshots.Count, contoursLayer.Grid);

		if (snapshots.Count == snapshotCount)
		{
            addButton.interactable = false;
        }
        deleteToggle.interactable = true;
		GuiUtils.RebuildLayout(transform);
    }

    private void OnDeleteToggleChange(bool isOn)
    {
        foreach (var snapshot in snapshots)
        {
            var toggle = snapshot.uiTransform.GetComponentInChildren<Toggle>(true);
			toggle.gameObject.SetActive(!isOn);

			var deleteButton = snapshot.uiTransform.GetComponentInChildren<Button>(true);
			deleteButton.gameObject.SetActive(isOn);
		}
    }

	private void OnRemoveSnapshot(Snapshot snapshot)
	{
        RemoveSnapshot(snapshot);
        emptySnapshots[snapshots.Count].gameObject.SetActive(true);
 
		addButton.interactable = true;
		if (snapshots.Count == 0)
		{
			deleteToggle.isOn = false;
			deleteToggle.interactable = false;
		}
    }

	private void OnEndEdit(string value, InputField input, Snapshot snapshot)
    {
        if (string.IsNullOrEmpty(value))
        {
            input.text = "Snapshot";
        }

		contoursOutput.RenameGroup(snapshot.name, input.text);

        snapshot.name = input.text;
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
            DestroyAnalyzer();
            inputHandler.OnLeftMouseUp -= OnLeftMouseUp;
         }
    }

	private void OnExcludeNoDataChanged(bool isOn)
	{
		CancelRemoveSnapshotMode();

		contoursLayer.ExcludeCellsWithNoData(isOn);
		contoursLayer.Refresh();
	}


	//
	// Event Methods
	//

	private void OnLeftMouseUp()
    {
        if (!inputHandler.IsDraggingLeft)
        {
            Vector3 worldPos;
            if (inputHandler.GetWorldPoint(Input.mousePosition, out worldPos))
            {
                Coordinate coords = map.GetCoordinatesFromUnits(worldPos.x, worldPos.z);
                var grid = contoursLayer.Grid;
                if (grid.values != null && grid.IsInside(coords.Longitude, coords.Latitude))
                {
                    contoursLayer.SetSelectedContour((int)grid.GetValue(coords.Longitude, coords.Latitude));
                }
            }
        }

		if (contoursLayer.grids.Count > 0)
		{
			contoursLayer.FetchGridValues();
			contoursOutput.AddGroup("Selected Contours", contoursLayer, false, true);
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
				contoursLayer.Add(grid);
				contoursLayer.Refresh();
			}
			else
				grids.Add(grid);

            grid.OnFilterChange += OnGridFilterChange;
        }
        else
        {
			grid.OnFilterChange -= OnGridFilterChange;

			// Remove from contours layer
			if (contoursLayer.IsVisible())
			{
				contoursLayer.Remove(grid);
				contoursLayer.Refresh();
			}
			else
				grids.Remove(grid);
            grid.patch.dataLayer.SetToolOpacity(1);
        }

        if (contoursLayer.grids.Count > 0) 
            contoursLayer.FetchGridValues();

        contoursOutput.UpdateGroup("All Contours");
        dataLayers.AutoReduceToolOpacity();
    }

    private void OnContoursGridChange(GridData grid)
    {
        if (analyzer != null && analyzer)
        {
            if (grid.values != null)
            {
                StartAnalysis();
            }
            else
            {
                StopAnalysis();
            }
        }
    }

    private void OnAnalysisProgress(float progress)
    {
        if (progress >= 1)
        {
            // Hide progress
            analysisProgress.gameObject.SetActive(false);

            if (contoursLayer)
            {
                contoursLayer.Grid.OnGridChange -= OnContoursGridChange;
                contoursLayer.Grid.ValuesChanged();
				contoursLayer.Grid.OnGridChange += OnContoursGridChange;
            }
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
                contoursLayer.Grid.OnGridChange += OnContoursGridChange;
            }

            // Update contours' data
            contoursLayer.Refresh();

			dataLayers.AutoReduceToolOpacity();
		}
		else
        {
            // Hide at the beginning
            contoursLayer.Show(false);
            contoursLayer.Grid.OnGridChange -= OnContoursGridChange;

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
		var snapshot = new Snapshot();
		snapshot.name = "Snapshot" + runningSnapshotCounter;
        emptySnapshots[i].gameObject.SetActive(false);

		// Create Snapshot
		snapshot.uiTransform = Instantiate(snapshotPrefab, SnapshotList, false);
        snapshot.uiTransform.SetSiblingIndex(i);

		// Setup toggle button
		var toggleButton = snapshot.uiTransform.GetComponentInChildren<Toggle>();
		toggleButton.onValueChanged.AddListener((isOn) => OnSnapshotToggleChanged(snapshot, isOn));

		// Setup delete button
		var deleteButton = snapshot.uiTransform.GetComponentInChildren<Button>();
		deleteButton.gameObject.SetActive(false);
		deleteButton.onClick.AddListener(() => OnRemoveSnapshot(snapshot));

		// Setup label
		snapshot.uiTransform.GetComponentInChildren<Text>().text = runningSnapshotCounter.ToString();

		// Setup intput text
		var input = snapshot.uiTransform.GetComponentInChildren<InputField>();
		input.text = "Snapshot\n" + runningSnapshotCounter;
		input.onEndEdit.AddListener((value) => OnEndEdit(value, input, snapshot));

		// Create snapshot's map layer
		snapshot.mapLayer = CreateMapLayer(snapshotLayerPrefab, "ContoursSnapshot", gridData);

		// Set Snapshot color
		SetColorsToSnapshot(snapshot.mapLayer, toggleButton, deleteButton);

		// Update output
		contoursOutput.AddGroup(snapshot.name, contoursLayer, false, false);

		snapshots.Add(snapshot);

		runningSnapshotCounter++;
	}

    private void RemoveSnapshot(Snapshot snapshot)
    {
		// Delete the UI element
		Destroy(snapshot.uiTransform.gameObject);

        // Delete the map layer
        DeleteMapLayer(ref snapshot.mapLayer);

        contoursOutput.RemoveGroup(snapshot.name);

		snapshots.Remove(snapshot);
    }

    private T CreateMapLayer<T>(T prefab, string layerName, GridData gridData = null) where T : GridMapLayer
    {
        T mapLayer = Instantiate(prefab);
		var grid = gridData == null ? new GridData() : new GridData(gridData);
        toolLayers.Add(mapLayer, grid, layerName, Color.white);
        return mapLayer;
    }

    private void DeleteAllLayers()
    {
        contoursLayer.Grid.OnGridChange -= OnContoursGridChange;
        contoursLayer.Clear();

        var layer = contoursLayer as GridMapLayer;
        DeleteMapLayer(ref layer);

        if (lockedContours != null)
        {
            DeleteMapLayer(ref lockedContours);
        }

		for (int i = 0; i < snapshots.Count; ++i)
        {
            if (snapshots[i].mapLayer != null)
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

		contoursLayer.FetchGridValues();
		analyzer.Analyze(contoursLayer.Grid, OnAnalysisProgress);
    }

    private void StopAnalysis()
    {
        analyzer.Stop();
        analysisProgress.gameObject.SetActive(false);
        contoursOutput.RemoveGroup("Selected Contours");
    }

	private void SetColorsToSnapshot(GridMapLayer mapLayer, Toggle toggle, Button deleteButton)
	{
		float snapshotHue = (runningSnapshotCounter * 0.2f) % 1f;

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
}
