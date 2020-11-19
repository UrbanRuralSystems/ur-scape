// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker(neudecker@arch.ethz.ch)
//          Michael Joos  (joos@arch.ethz.ch)
//          Muhammad Salihin Bin Zaol-kefli

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MunicipalBudgetTool : Tool
{
	[Header("Source Files")]
	public string path = "Municipal Budget";

    [Header("Prefabs")]
    public WeightedMapLayer budgetLayerPrefab;
    public GridMapLayer highlightLayerPrefab;
    public MunicipalBudgetOutput outputPrefab;
    public WeightSlider sliderPrefab;

	[Header("UI References")]
    public Toggle filterToggle;
    public Toggle noDataToggle;

	// Prefab Instances
	private WeightedMapLayer budgetLayer;
	private GridMapLayer highlightLayer;
	private MunicipalBudgetOutput municipalBudgetOutput;

    // Component References
    private DataLayers dataLayers;
    private OutputPanel outputPanel;
    private SiteBrowser siteBrowser;
    private InputHandler inputHandler;

    // Misc
    private readonly List<GridData> grids = new List<GridData>();
    private readonly Dictionary<string, WeightSlider> sliders = new Dictionary<string, WeightSlider>();
	private MunicipalityData data;
	private Coroutine loadDataCR;
    private bool isHover;
    private int initChildsCount;

	//
	// Unity Methods
	//

	protected override void OnDestroy()
    {
		base.OnDestroy();
	}

	//
	// Inheritance Methods
	//

	protected override void OnComponentRegistrationFinished()
	{
		base.OnComponentRegistrationFinished();

		// Get Components
		dataLayers = ComponentManager.Instance.Get<DataLayers>();
		outputPanel = ComponentManager.Instance.Get<OutputPanel>();
        siteBrowser = ComponentManager.Instance.Get<SiteBrowser>();
        inputHandler = ComponentManager.Instance.Get<InputHandler>();
    }

	public override bool HasBookmarkData()
    {
        return false;
    }

    //
    // Event Methods
    //

    protected override void OnToggleTool(bool isOn)
    {
        if (isOn)
        {
            // Create budget layer
			budgetLayer = toolLayers.CreateMapLayer(budgetLayerPrefab, "WeightedLayer");
			highlightLayer = toolLayers.CreateMapLayer(highlightLayerPrefab, "HighlightLayer");
			highlightLayer.Show(false);

            // Store number of childs on start
            initChildsCount = transform.childCount + 1;

            // Add slider
            foreach (var panel in dataLayers.activeLayerPanels)
			{
				UpdateSlider(panel.DataLayer, true);
			}

			// Create output panel for tool
			municipalBudgetOutput = Instantiate(outputPrefab);
			municipalBudgetOutput.name = "MunicipalBudget_OutputPanel";
			outputPanel.SetPanel(municipalBudgetOutput.transform);
            municipalBudgetOutput.OnItemHovering += OnItemHover;

            // Add event listeners
            siteBrowser.OnBeforeActiveSiteChange += OnBeforeActiveSiteChange;
			siteBrowser.OnAfterActiveSiteChange += OnAfterActiveSiteChange;

            // Listen to any data layers being added/removed
            dataLayers.OnLayerAvailabilityChange += OnLayerAvailabilityChange;

			// Listen to any grids being added/removed
			var gridController = map.GetLayerController<GridLayerController>();
			gridController.OnShowGrid += OnShowGrid;

			// Initialize grids list with already visible grid layers
			foreach (var layer in gridController.mapLayers)
			{
				grids.Add(layer.Grid);
			}

			ShowBudgetLayer(false);

			// Add filter toggle event
			filterToggle.onValueChanged.AddListener(OnFilterToggleChange);
            noDataToggle.onValueChanged.AddListener(OnNoDataToggleChange);
            budgetLayer.useFilters = filterToggle.isOn;

            loadDataCR = StartCoroutine(LoadData());
        }
        else
        {
			if (loadDataCR != null)
				StopCoroutine(loadDataCR);

            // Remove listeners
            siteBrowser.OnBeforeActiveSiteChange -= OnBeforeActiveSiteChange;
			siteBrowser.OnAfterActiveSiteChange -= OnAfterActiveSiteChange;
			dataLayers.OnLayerAvailabilityChange -= OnLayerAvailabilityChange;
            if (map != null)
            {
                var gridController = map.GetLayerController<GridLayerController>();
                if (gridController)
                {
                    gridController.OnShowGrid -= OnShowGrid;
                }
            }

            // Remove Output panel
            outputPanel.DestroyPanel(municipalBudgetOutput.gameObject);
			municipalBudgetOutput = null;

			// Remove map layer
			DeleteAllLayers();

            // Remove sliders
            foreach (var pair in sliders)
            {
                Destroy(pair.Value.gameObject);
            }
			sliders.Clear();

			grids.Clear();

			dataLayers.ResetToolOpacity();
		}
	}

	private void OnBeforeActiveSiteChange(Site nextSite, Site previousSite)
	{

		if (loadDataCR != null)
			StopCoroutine(loadDataCR);

		data = null;
		highlightLayer.Grid.values = null;

		EnableSliders(false);
	}

	private void OnAfterActiveSiteChange(Site nextSite, Site previousSite)
	{
		loadDataCR = StartCoroutine(LoadData());
	}

	private void Update()
	{
		CheckMapAreaHover();
	}

	private void CheckMapAreaHover()
	{
        // All the conditions to disable area identification via hovering on map
        if (isHover || data == null || budgetLayer.Grid.values == null || budgetLayer.Grid.values.Length != data.ids.Length)
            return;

        var values = highlightLayer.Grid.values;
        if (values == null || values.Length != data.ids.Length)
            return;

        string name = "";

		if (inputHandler.GetWorldPoint(Input.mousePosition, out Vector3 worldPos))
		{
			Coordinate coords = map.GetCoordinatesFromUnits(worldPos.x, worldPos.z);
			var grid = highlightLayer.Grid;
			if (grid.values != null && grid.IsInside(coords.Longitude, coords.Latitude))
			{
				var id = grid.GetIndex(coords.Longitude, coords.Latitude);

				var ids = data.ids;
				var count = ids.Length;

				// No data matching any of the areas listed
				if (!data.idToName.TryGetValue(ids[id], out name))
				{
					highlightLayer.Show(false);
					municipalBudgetOutput.ShowBudgetLabel("");
					municipalBudgetOutput.UpdateSelectedAreaAndVal("");
					return;
				}

				for (int i = 0; i < count; ++i)
				{
					values[i] = ids[i] == ids[id] ? 1 : 0;
				}

				highlightLayer.Show(true);
				highlightLayer.Grid.ValuesChanged();
			}
			else
			{
				highlightLayer.Show(false);
			}
		}

		municipalBudgetOutput.ShowBudgetLabel(name);
        municipalBudgetOutput.UpdateSelectedAreaAndVal(name);
    }

    private void OnItemHover(string name, bool hover)
    {
		var values = highlightLayer.Grid.values;
		if (values == null || values.Length != data.ids.Length)
				return;

        if (hover)
        {
			var database = data;
            var id = database.idToName.FirstOrDefault(x => x.Value.Equals(name)).Key;
            
            var ids = data.ids;
			var count = ids.Length;
			for (int i = 0; i < count; ++i)
            {
				values[i] = ids[i] == id ? 1 : 0;
            }

            if (highlightLayer != null)
            {
                highlightLayer.Show(true);
                highlightLayer.Grid.ValuesChanged();
            }
		}
        else
        {
            if (highlightLayer != null)
			    highlightLayer.Show(false);
		}

        isHover = hover;
    }

    protected override void OnActiveTool(bool isActive)
    {
		if (municipalBudgetOutput != null)
        {
			outputPanel.SetPanel(isActive ? municipalBudgetOutput.transform : null);

			if (!isActive)
				municipalBudgetOutput.ClearAreas();
        }
    }

    private void OnLayerAvailabilityChange(DataLayer layer, bool visible)
	{
		// Change opacity of data layers
		dataLayers.AutoReduceToolOpacity();

		// Create a slider for each data layer
		UpdateSlider(layer, visible);

		if (!visible)
		{
			layer.SetToolOpacity(1f);
		}
	}

	private void OnShowGrid(GridMapLayer mapLayer, bool show)
    {
        var otherGrid = mapLayer.Grid;
        if (show)
        {
			// Add to budget layer
			if (budgetLayer.IsVisible())
			{
				Add(otherGrid);
				budgetLayer.UpdateData();
			}
			else
			{
				grids.Add(otherGrid);
			}

			if (highlightLayer.Grid.values == null)
			{
				UpdateHighlightGrid();
			}
        }
        else
        {
            filterToggle.onValueChanged.RemoveAllListeners();

			// Remove from budget layer
			if (budgetLayer.IsVisible())
			{

				Remove(otherGrid);
				budgetLayer.UpdateData();
			}
			else
			{
				grids.Remove(otherGrid);
			}
        }

		UpdateOutput();
        dataLayers.AutoReduceToolOpacity();
    }

	private void OnSliderChanged(DataLayer layer, float value)
	{
        budgetLayer.SetWeight(layer, value);
        budgetLayer.UpdateData();
        UpdateOutput();
    }

    private void OnOtherGridFilterChange(GridData grid)
	{
		if (filterToggle.isOn)
		{
			UpdateOutput();
		}
	}

	private void OnOtherGridChange(GridData grid)
	{
		UpdateOutput();
	}

	private void OnFilterToggleChange(bool isOn)
	{
		budgetLayer.useFilters = isOn;
        budgetLayer.UpdateData();
        UpdateOutput();
    }

    private void OnNoDataToggleChange(bool isOn)
    {
        municipalBudgetOutput.SetNoDataUse(isOn);
        UpdateOutput();
    }

    //
    // Private Methods
    //

    public void Add(GridData otherGrid)
	{
		// Ignore Network patches
		if (otherGrid.patch != null && otherGrid.patch is GraphPatch)
			return;

		budgetLayer.Add(otherGrid);

		otherGrid.OnGridChange += OnOtherGridChange;
		otherGrid.OnValuesChange += OnOtherGridChange;
		otherGrid.OnFilterChange += OnOtherGridFilterChange;
	}

	private void Remove(GridData otherGrid)
	{
		// Ignore Network patches
		if (otherGrid.patch != null && otherGrid.patch is GraphPatch)
			return;

		budgetLayer.Remove(otherGrid);

		otherGrid.OnGridChange -= OnOtherGridChange;
		otherGrid.OnValuesChange -= OnOtherGridChange;
		otherGrid.OnFilterChange -= OnOtherGridFilterChange;
	}

	private void UpdateSlider(DataLayer layer, bool visible)
	{
		if (visible)
		{
			// Create new tool slider
			var slider = Instantiate(sliderPrefab, transform, false);
            slider.transform.SetSiblingIndex(transform.childCount - initChildsCount);

            if (!sliders.ContainsKey(layer.Name))
				sliders.Add(layer.Name, slider);

			// Prepare slider
			slider.Init(layer.Name, layer.Color);
			slider.SetDefault();
			slider.OnSliderChanged += (value) => OnSliderChanged(layer, value);

			slider.Enable(data != null);

			// Pass key/value pair to budget layer for weighting formula
			budgetLayer.SetWeight(layer, slider.Value);
		}
		else
		{
			// Delete the slider
			if (sliders.ContainsKey(layer.Name))
			{
				Destroy(sliders[layer.Name].gameObject);
				sliders.Remove(layer.Name);
			}
			budgetLayer.RemoveWeight(layer);
		}
	}

    private void ShowBudgetLayer(bool show)
    {
        if (show)
        {
			// Add visible grids to the budget layer
			foreach (var g in grids)
			{
				Add(g);
			}
			grids.Clear();

			budgetLayer.Show(true);
			dataLayers.AutoReduceToolOpacity();
		}
        else
        {
            // Hide at the beginning
            budgetLayer.Show(false);

			// Move visible grids from budget layer to list
			if (budgetLayer.grids.Count > 0)
            {
                grids.AddRange(budgetLayer.grids);
                budgetLayer.Clear();
            }

            dataLayers.ResetToolOpacity();
        }
    }

	private void DeleteAllLayers()
    {
        budgetLayer.Clear();
        var layer = budgetLayer as GridMapLayer;
        DeleteMapLayer(ref layer);
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

    private IEnumerator LoadData()
    {
		var filename = Paths.Data + path + Path.DirectorySeparatorChar + siteBrowser.ActiveSite.Name + ".csv";

#if !UNITY_WEBGL
		if (File.Exists(filename))
#endif
		{
			municipalBudgetOutput.ShowMessage("Loading budget data for " + siteBrowser.ActiveSite.Name);

			yield return MunicipalityIO.Load(filename, (d) => data = d, OnNoData);

			if (data != null)
			{
				if (!budgetLayer.IsVisible())
					ShowBudgetLayer(true);

				budgetLayer.UpdateGrid(data.north, data.east, data.south, data.west, data.countX, data.countY);
                budgetLayer.UpdateData();
				UpdateOutput();
				UpdateHighlightGrid();
				EnableSliders(true);
            }
		}
#if !UNITY_WEBGL
		else
		{
			OnNoData();
		}
#endif

		loadDataCR = null;
	}

	private void OnNoData()
	{
		if (budgetLayer.IsVisible())
			ShowBudgetLayer(false);

		data = null;
		budgetLayer.ResetGrid();
		UpdateOutput();
	}

	private BudgetData budgetData;
	private void UpdateOutput()
    {
		if (data == null)
		{
			municipalBudgetOutput.ShowAreas(false);
			municipalBudgetOutput.ShowMessage("No budget data available for " + siteBrowser.ActiveSite.Name);
			return;
		}		
		if (budgetLayer.grids.Count == 0)
		{
			municipalBudgetOutput.ShowAreas(false);
			municipalBudgetOutput.ShowMessage("There are no active layers yet. Please select at least one layer in the Data Layer panel.");
			return;
		}

		if (budgetData == null)
			budgetData = new BudgetData(budgetLayer.Grid.values, budgetLayer.Grid.valuesMask, budgetLayer.Divisors, data);
		else
			budgetData.Update(budgetLayer.Grid.values, budgetLayer.Grid.valuesMask, budgetLayer.Divisors, data);

		municipalBudgetOutput.ShowAreas(true);
		municipalBudgetOutput.HideMessage();
        municipalBudgetOutput.SetData(budgetData);
    }

    private void UpdateHighlightGrid()
    {
		if (data == null)
			return;

		// Need at least one active layer
		if (budgetLayer.grids.Count == 0)
			return;

		// Check that the budget layer has the same size as the budget data
		if (budgetLayer.Grid.values.Length != data.ids.Length)
			return;

		GridData highlightGrid = highlightLayer.Grid;
		var grid = budgetLayer.Grid;

        highlightGrid.countX = grid.countX;
		highlightGrid.countY = grid.countY;
		highlightGrid.ChangeBounds(grid.west, grid.east, grid.north, grid.south);

		if (highlightGrid.values == null || highlightGrid.values.Length != data.ids.Length)
		{
			highlightGrid.values = new float[data.ids.Length];
		}
	}

	private void EnableSliders(bool enable)
	{
		foreach (var pair in sliders)
		{
			pair.Value.Enable(enable);
		}
	}

}
