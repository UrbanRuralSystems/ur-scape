// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

#if UNITY_EDITOR
#define SAFETY_CHECK
#endif

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public delegate void OnLayerChangeDelegate(DataLayer layer, bool value);

public class DataLayers : UrsComponent
{
	[Header("Settings")]
	[Tooltip("When true: hide layers without visible sites. When false: show layers in inactive state")]
	public bool hideInactiveLayers = true;
	[Tooltip("Hide groups without any available layers")]
	public bool hideEmptyGroups = true;

	[Header("UI References")]
    public Transform groupsContainer;
	public GameObject hideLayersPanel;
	public Button unhideLayersButton;
	public ScrollRect scrollRect;
	public RectTransform shadowTop;
	public RectTransform shadowBottom;
    public Button settingsButton;
	public SettingsPanel settingsPanel;

	[Header("Prefabs")]
    public DataLayerGroupPanel guiGroupPrefab;
    public DataLayerPanel layerPanelPrefab;

    public float?[,] CorrCoeffs { set; get; }

    public event OnLayerChangeDelegate OnLayerVisibilityChange;
	public event OnLayerChangeDelegate OnLayerAvailabilityChange;

	private MapController map;

	private readonly Dictionary<string, DataLayerPanel> nameToLayer = new Dictionary<string, DataLayerPanel>();
    private readonly List<DataLayerPanel> layerPanels = new List<DataLayerPanel>();				// All layer-panels
	public readonly List<DataLayerPanel> activeSiteLayerPanels = new List<DataLayerPanel>();    // Only layer-panels available for the active site
	public readonly List<DataLayerPanel> activeLayerPanels = new List<DataLayerPanel>();		// Layer-panels that are toggled ON
	public readonly HashSet<DataLayer> availableLayers = new HashSet<DataLayer>();              // From the active layers, only those that are in view

	private bool hideLayers;
	private bool updatingMap;
    private float updateUntilTime;
    private static readonly float MapUpdateInterval = 0.3f;
    private static readonly WaitForSeconds MapUpdateDelay = new WaitForSeconds(MapUpdateInterval + 0.02f);

	private float toolOpacity = 1f;


    //
    // Unity Methods
    //

    private IEnumerator Start()
    {
		yield return WaitFor.Frames(WaitFor.InitialFrames);

        map = ComponentManager.Instance.Get<MapController>();
        map.OnBoundsChange += OnMapBoundsChange;
        map.OnLevelChange += OnMapLevelChange;
		map.GetLayerController<GridLayerController>().OnShowGrid += OnShowGrid;

		ComponentManager.Instance.Get<SiteBrowser>().OnAfterActiveSiteChange += OnAfterActiveSiteChange;

		settingsButton.onClick.AddListener(OnSettingsClick);
		unhideLayersButton.onClick.AddListener(OnUnhideButtonClick);
    }
	

	//
	// Inheritance Mehods
	//

	public override bool HasBookmarkData()
    {
        return false;
    }

    public override void SaveToBookmark(BinaryWriter bw, string bookmarkPath)
    {
		Debug.LogWarning("Saving to bookmark is currently under development");
		/*
		// Write Preferences
		bw.Write(DataLayerPanel.HideLayersWithoutVisiblePatches);
		bw.Write(GridMapLayer.ShowNoData);

		bw.Write(layerPanels.Count);
        foreach (var layer in layerPanels)
        {
            var dataLayer = layer.DataLayer;
            bw.Write(dataLayer.name);
            bw.Write(layer.layerToggle.isOn);
            bw.Write(layer.panelToggle.isOn);
            var panel = layer.Panel as FilterPanel;
            if (panel == null)
            {
                bw.Write(0f);
                bw.Write(1f);
            }
            else
            {
                bw.Write(panel.minSlider.value);
                bw.Write(panel.maxSlider.value);
            }

            bw.Write(dataLayer.levels.Length);
            foreach (var level in dataLayer.levels)
            {
                bw.Write(level.sites.Count);
                foreach (var site in level.sites)
                {
					var record = site.lastRecord;
					bw.Write(record.patches.Count);
					foreach (var patch in record.patches)
					{
						bw.Write(patch.name);
						bw.Write(patch.Data.categoryFilter);
					}
                }
            }
        }
        bw.Write(scrollRect.verticalScrollbar.value);
		+*/
	}

	public override void LoadFromBookmark(BinaryReader br, string bookmarkPath)
    {
		Debug.LogWarning("Loading from bookmark is currently under development");
		/*
		// Read Preferences
		DataLayerPanel.HideLayersWithoutVisiblePatches = br.ReadBoolean();
		GridMapLayer.ShowNoData = br.ReadBoolean();

		DeactivateAll();

        int count = br.ReadInt32();
        for (int i = 0; i < count; i++)
        {
            string layerName = br.ReadString();
            if (!nameToLayer.ContainsKey(layerName))
                continue;

            var layer = nameToLayer[layerName];
            bool showLayer = br.ReadBoolean();
            bool showPanel = br.ReadBoolean();

            var dataLayer = layer.DataLayer;

            float min = br.ReadSingle();
            float max = br.ReadSingle();


			//var panel = layer.Panel as FilterPanel;
			//if (panel == null)
			//{
			dataLayer.SetMinMaxFilterToAllPatches(min, max); 
			//}
			//else
			//{
			//	panel.SetMinMax(min, max);
			//}

            int levelCount = br.ReadInt32();
            for (int l = 0; l < levelCount; l++)
            {
				int siteCount = br.ReadInt32();
                for (int s = 0; s < siteCount; s++)
                {
					int patchCount = br.ReadInt32();
					for (int p = 0; p < patchCount; p++)
					{
						string patchName = br.ReadString();
						uint filter = br.ReadUInt32();

						if (l < dataLayer.levels.Length && s < dataLayer.levels[l].sites.Count)
						{
							var site = dataLayer.levels[l].sites[s];
							var record = site.lastRecord;

							foreach (var patch in record.patches)
							{
								var gridPatch = patch as GridPatch;
								if (gridPatch != null && gridPatch.grid.IsCategorized && gridPatch.name.Equals(patchName))
								{
									gridPatch.SetCategoryFilter(filter);
									break;
								}
							}
						}
					}
                }
            }

            layer.panelToggle.isOn = showLayer && showPanel;
            layer.layerToggle.isOn = showLayer;
        }

        scrollRect.verticalScrollbar.value = br.ReadSingle();

        // Check which layers have visible sites
        OnMapBoundsChange();
		+*/
	}


	//
	// Event Methods
	//

	private void OnSettingsClick()
	{
		settingsPanel.Show(!settingsPanel.IsVisible());
	}

	private void OnUnhideButtonClick()
	{
		ShowLayers(true);
		unhideLayersButton.gameObject.SetActive(false);
		settingsPanel.visibilityToggle.isOn = true;
	}

	private void OnMapBoundsChange()
    {
        updateUntilTime = Time.time + MapUpdateInterval;
        if (!updatingMap && isActiveAndEnabled)
        {
            updatingMap = true;
            StartCoroutine(DelayedMapUpdate());
        }
    }

    private void OnMapLevelChange(int level)
    {
        UpdateLayers();
    }

	private void OnAfterActiveSiteChange(Site site, Site previousSite)
	{
		// Update the list of layer panels available for the new active site
		activeSiteLayerPanels.Clear();
		foreach (var panel in layerPanels)
		{
			UpdateLayerPanelIsActive(panel, site.HasDataLayer(panel.DataLayer));
		}

		// Scroll the list up to avoid an "empty list" when the site only has a few layers
		scrollRect.verticalScrollbar.value = 1f;	

		// Show/load the new site's patches
		UpdateLayers();
	}

	private void OnLayerToggleChange(DataLayerPanel panel, bool isOn)
    {
        if (isOn)
        {
            ShowDataLayer(panel);
        }
        else
        {
            HideDataLayer(panel);
        }

        if (panel.IsFilterToggleOn)
        {
            StartCoroutine(ShowFilterPanel(panel, isOn));
        }
    }

	private void OnShowGrid(GridMapLayer mapLayer, bool show)
	{
		// Hide map layers that are loaded after hiding all layers
		if (show && hideLayers)
			mapLayer.Show(false);
	}


	//
	// Public Methods
	//

	/// //+ <summary>Very slow method. Use carefully.</summary>
	public void RebuildList(List<LayerGroup> groups)
	{
		var _activeLayerPanels = new List<DataLayerPanel>(activeLayerPanels);
		var _availableLayers = new HashSet<DataLayer>(availableLayers);

		Clear();

		var activeSite = ComponentManager.Instance.Get<SiteBrowser>().ActiveSite;

		foreach (var group in groups)
		{
			var groupController = AddLayerGroup(group.name);
			foreach (var layer in group.layers)
			{
				bool isLayerOn = false, isFilterOn = false;
				bool isLayerInActiveSite = activeSite.HasDataLayer(layer);

				var oldPanel = _activeLayerPanels.Find((p) => p.DataLayer == layer);

				if (oldPanel != null)
				{
					isLayerOn = oldPanel.IsLayerToggleOn;
					isFilterOn = oldPanel.IsFilterToggleOn;
				}

				var layerPanel = AddLayer(layer, groupController, isLayerOn);

				if (oldPanel != null)
					activeLayerPanels.Add(layerPanel);

				if (_availableLayers.Contains(layer))
					availableLayers.Add(layer);

				UpdateLayerPanelIsActive(layerPanel, isLayerInActiveSite);

				if (isLayerInActiveSite && isLayerOn && isFilterOn)
				{
					layerPanel.filterToggle.isOn = true;
				}
			}
		}

		// Scroll the list up to avoid an "empty list" when the site only has a few layers
		scrollRect.verticalScrollbar.value = 1f;

		// Show/load the new site's patches
		UpdateLayers();
	}

	public void Show(bool show)
    {
        groupsContainer.gameObject.SetActive(show);

        if (show)
        {
            GuiUtils.RebuildLayout(groupsContainer.transform);
        }
    }

	public void ShowLayers(bool show)
	{
		hideLayers = !show;

		hideLayersPanel.SetActive(hideLayers);
		unhideLayersButton.gameObject.SetActive(hideLayers);
		scrollRect.vertical = show;

		var controller = map.GetLayerController<GridLayerController>();
		foreach (var layer in controller.mapLayers)
		{
			layer.Show(show);
		}
	}

	public void ResetLayers()
	{
		foreach (var panel in layerPanels)
		{
			panel.layerToggle.isOn = false;
			panel.DataLayer.ResetFiltersAndOpacity();
		}
	}

	public DataLayerGroupPanel AddLayerGroup(string name)
    {
        // Create a new group and move it to the end of the list
        var group = Instantiate(guiGroupPrefab);
		if (hideEmptyGroups)
			group.gameObject.SetActive(false);

		group.Init(name);
		group.transform.SetParent(groupsContainer, false);

		return group;
    }

	public void RemoveLayerGroup(string groupName)
	{
		var group = GetLayerGroup(groupName);
		if (group != null)
		{
			Destroy(group.gameObject);
			GuiUtils.RebuildLayout(groupsContainer.transform);
		}
	}

	public DataLayerGroupPanel GetLayerGroup(string groupName)
	{
		for (int i = 0; i < groupsContainer.childCount; i++)
		{
			var group = groupsContainer.GetChild(i);
			if (group.name.Equals(groupName))
			{
				return group.GetComponent<DataLayerGroupPanel>();
			}
		}
		return null;
	}

	public void UpdateGroup(string newName, string oldName)
	{
		var group = GetLayerGroup(oldName);
		if (group != null)
		{
			group.UpdateName(newName);
		}
	}

	public bool HasLayer(string layerName)
	{
		return nameToLayer.ContainsKey(layerName);
	}

    public DataLayer GetLayer(string layerName)
    {
#if SAFETY_CHECK
		if (nameToLayer.ContainsKey(layerName))
		{
			return nameToLayer[layerName].DataLayer;
		}

		// Try to find layer without case sensitiveness
		foreach (var pair in nameToLayer)
		{
			if (pair.Key.EqualsIgnoreCase(layerName))
			{
				Debug.LogWarning("Layer " + layerName + " found with different case: " + pair.Key);
				return pair.Value.DataLayer;
			}
		}

		Debug.LogError("Layer " + layerName + " could not be found");
		return null;
#else
		return nameToLayer[layerName].DataLayer;
#endif
	}

	public DataLayerPanel AddLayer(DataLayer layer, DataLayerGroupPanel groupController, bool layerOn = false)
	{
		// Add new layer
		DataLayerPanel layerPanel = groupController.AddLayer(layerPanelPrefab, layer);

		if (layerOn)
			layerPanel.layerToggle.isOn = true;

		layerPanel.layerToggle.onValueChanged.AddListener((isOn) => OnLayerToggleChange(layerPanel, isOn));
		layerPanel.filterToggle.onValueChanged.AddListener((isOn) => StartCoroutine(ShowFilterPanel(layerPanel, isOn)));

		if (hideInactiveLayers)
		{
			layerPanel.gameObject.SetActive(false);
			layerPanel.EnableLayerPanel(true);
		}
		else
		{
			layerPanel.EnableLayerPanel(false);
			layerPanel.gameObject.SetActive(true);
		}

#if SAFETY_CHECK
		if (nameToLayer.ContainsKey(layer.Name))
		{
			Debug.LogWarning(layer.Name + " appears more than once in the layers list");
		}
		else
#endif
		{
			layerPanels.Add(layerPanel);
			nameToLayer.Add(layer.Name, layerPanel);
		}

		return layerPanel;
	}

	public void RemoveLayer(DataLayer layer)
	{
		var layerPanel = nameToLayer[layer.Name];
        nameToLayer.Remove(layer.Name);
        layerPanels.Remove(layerPanel);
		activeSiteLayerPanels.Remove(layerPanel);
		if (activeLayerPanels.Contains(layerPanel))
			HideDataLayer(layerPanel);
		else
			availableLayers.Remove(layer);

		var groupPanel = GetGroupPanel(layerPanel);

		DestroyImmediate(layerPanel.gameObject);

		groupPanel.UpdateVisibility();
	}

	public bool IsLayerActive(DataLayer layer)
	{
#if SAFETY_CHECK
		if (!nameToLayer.ContainsKey(layer.Name))
        {
            Debug.LogError("Layer " + layer.Name + " could not be found");
            return false;
        }
#endif
        return nameToLayer[layer.Name].IsLayerToggleOn;
    }

	private void DeactivateAll()
	{
		foreach (var panel in layerPanels)
		{
			panel.layerToggle.isOn = false;
			panel.filterToggle.isOn = false;
		}
	}

	// Manualy show/hide a data layer (the same as clicking on the layer button)
	public void ActivateLayer(DataLayer layer, bool activate)
    {
#if SAFETY_CHECK
        if (!nameToLayer.ContainsKey(layer.Name))
        {
            Debug.LogError("Layer " + layer.Name + " could not be found");
            return;
        }
#endif
		nameToLayer[layer.Name].layerToggle.isOn = activate;
	}

	public void AutoReduceToolOpacity()
	{
		if (availableLayers.Count == 0)
			SetToolOpacity(1f);
		else
			SetToolOpacity(0.1f + 1f / (availableLayers.Count + 1));
	}

	public void SetToolOpacity(float opacity)
	{
		toolOpacity = Mathf.Clamp01(opacity);
		foreach (var layer in availableLayers)
		{
			layer.SetToolOpacity(toolOpacity);
		}
	}

	public void ResetToolOpacity()
	{
		SetToolOpacity(1f);
	}

	public void SetHideLayersWithoutVisiblePatches(bool hide)
	{
		if (hide != hideInactiveLayers)
		{
			hideInactiveLayers = hide;
			foreach (var panel in layerPanels)
			{
				panel.UpdateBehaviour(hideInactiveLayers);
			}

			GuiUtils.RebuildLayout(groupsContainer.transform);
		}
	}

	public void SetHideEmptyGroups(bool hide)
	{
		if (hide != hideEmptyGroups)
		{
			hideEmptyGroups = hide;

			int count = groupsContainer.childCount;
			if (hide)
			{
				for (int i = 0; i < count; i++)
				{
					groupsContainer.GetChild(i).GetComponent<DataLayerGroupPanel>().UpdateVisibility();
				}
			}
			else
			{
				for (int i = 0; i < count; i++)
				{
					groupsContainer.GetChild(i).gameObject.SetActive(true);
				}
			}

			GuiUtils.RebuildLayout(groupsContainer.transform);
		}
	}

    //
    // Private Methods
    //

	private void Clear()
	{
		nameToLayer.Clear();
		layerPanels.Clear();
		activeSiteLayerPanels.Clear();
		activeLayerPanels.Clear();
		availableLayers.Clear();

		for (int i = groupsContainer.childCount - 1; i >= 0; i--)
		{
			var group = groupsContainer.GetChild(i);
			Destroy(group.gameObject);
		}
	}

	private void UpdateLayerPanelIsActive(DataLayerPanel panel, bool active)
	{
		if (active)
		{
			activeSiteLayerPanels.Add(panel);
			EnableLayerPanel(panel, true);
		}
		else
		{
			panel.layerToggle.isOn = false;
			EnableLayerPanel(panel, false);
		}
	}

	// Helper function to show/hide or enable/disable layer depending on HideLayersWithoutVisiblePatches
	public void EnableLayerPanel(DataLayerPanel layerPanel, bool enable)
	{
		if (hideInactiveLayers)
		{
			// Show the group BEFORE the layer is shown
			if (hideEmptyGroups && enable && layerPanel.gameObject.activeSelf != enable)
				GetGroupPanel(layerPanel).Show(true);

			if (layerPanel.ShowLayerPanel(enable))
			{
				// Hide the group AFTER the layer is hidden (only if no other layers are visible)
				if (hideEmptyGroups && !enable)
					GetGroupPanel(layerPanel).UpdateVisibility();
			}
		}
		else
		{
			layerPanel.EnableLayerPanel(enable);
		}
	}

	private DataLayerGroupPanel GetGroupPanel(DataLayerPanel layerPanel)
	{
		return layerPanel.transform.parent.parent.GetComponent<DataLayerGroupPanel>();
	}

	private static readonly Vector2 bottomPoint = new Vector2(50, -1);
    private IEnumerator ShowFilterPanel(DataLayerPanel layerCtrl, bool show)
    {
        // FIX: scrollbar won't retract if value is zero.
        if (!show && scrollRect.verticalScrollbar.value < 0.001f && scrollRect.verticalScrollbar.size < 1)
            scrollRect.verticalScrollbar.value = 0.001f;

		layerCtrl.ShowFilterPanel(show);

		yield return null;

		if (show)
        {
            float offset = 30f / (scrollRect.content.rect.height - scrollRect.viewport.rect.height);
            while (scrollRect.verticalScrollbar.size < 0.99f &&
                   RectTransformUtility.RectangleContainsScreenPoint(layerCtrl.GetComponent<RectTransform>(), bottomPoint))
            {
                scrollRect.verticalScrollbar.value -= offset;
                yield return null;
            }
        }
    }

    private void ShowDataLayer(DataLayerPanel layerPanel)
    {
#if SAFETY_CHECK
        if (activeLayerPanels.Contains(layerPanel))
        {
            Debug.LogWarning("Layer " + layerPanel.DataLayer.Name + " is already active!");
            return;
        }
#endif

		activeLayerPanels.Add(layerPanel);

		var dataLayer = layerPanel.DataLayer;

        if (OnLayerVisibilityChange != null)
            OnLayerVisibilityChange(dataLayer, true);

        dataLayer.Show(map.CurrentLevel, map.MapCoordBounds);

		bool hasPatches = dataLayer.HasPatchesInView();
		if (hasPatches && !availableLayers.Contains(dataLayer))
		{
			availableLayers.Add(dataLayer);
			if (OnLayerAvailabilityChange != null)
				OnLayerAvailabilityChange(dataLayer, true);
		}
	}

	private void HideDataLayer(DataLayerPanel layerPanel)
    {
#if SAFETY_CHECK
        if (!activeLayerPanels.Remove(layerPanel))
        {
            Debug.LogWarning("Layer " + layerPanel.name + " is not active!");
            return;
        }
#else
        activeLayerPanels.Remove(layerPanel);
#endif

		var dataLayer = layerPanel.DataLayer;

		if (availableLayers.Remove(dataLayer))
		{
			if (OnLayerAvailabilityChange != null)
				OnLayerAvailabilityChange(dataLayer, false);
		}

		dataLayer.HideLoadedPatches();

        if (OnLayerVisibilityChange != null)
            OnLayerVisibilityChange(dataLayer, false);
    }

    private IEnumerator DelayedMapUpdate()
    {
		do
		{
			yield return MapUpdateDelay;
			UpdateLayers();
		}
		while (Time.time < updateUntilTime);

		updatingMap = false;
	}

	private void UpdateLayersDistributionChart()
    {
        foreach (var layerPanel in activeLayerPanels)
        {
			UpdateLayerDistributionChart(layerPanel);
        }
    }

	private void UpdateLayerDistributionChart(DataLayerPanel layerPanel)
	{
		if (layerPanel.IsFilterToggleOn && layerPanel.Panel is FilterPanel)
		{
			var panel = layerPanel.Panel as FilterPanel;
			panel.UpdateDistributionChart();
		}
	}

	public void UpdateLayers()
    {
        int level = map.CurrentLevel;
        var bounds = map.MapCoordBounds;

        foreach (var layerPanel in activeSiteLayerPanels)
		{
			UpdateLayerPanel(layerPanel, level, bounds);
        }
	}

	public void UpdateLayer(DataLayer layer)
	{
#if SAFETY_CHECK
		if (!nameToLayer.ContainsKey(layer.Name))
		{
			Debug.LogError("Layer " + layer.Name + " could not be found");
			return;
		}
#endif

		int level = map.CurrentLevel;
		var bounds = map.MapCoordBounds;

		UpdateLayerPanel(nameToLayer[layer.Name], level, bounds);
	}

	private void UpdateLayerPanel(DataLayerPanel layerPanel, int level, AreaBounds bounds)
	{
		var dataLayer = layerPanel.DataLayer;

		var site = ComponentManager.Instance.Get<SiteBrowser>().ActiveSite;

		bool hasPatches;
		if (layerPanel.IsLayerToggleOn)
		{
			dataLayer.UpdatePatches(site, level, bounds);
			hasPatches = dataLayer.HasPatchesInView();

			if (hasPatches && !availableLayers.Contains(dataLayer))
			{
				availableLayers.Add(dataLayer);
				if (OnLayerAvailabilityChange != null)
					OnLayerAvailabilityChange(dataLayer, true);
			}
			else if (!hasPatches && availableLayers.Contains(dataLayer))
			{
				availableLayers.Remove(dataLayer);
				if (OnLayerAvailabilityChange != null)
					OnLayerAvailabilityChange(dataLayer, false);
			}
		}
		else
		{
			hasPatches = dataLayer.HasPatches(site, level, bounds.west, bounds.east, bounds.north, bounds.south);
		}

		// Show/hide a layer panel when only visible layers are allowed, otherwise enable/disable the layer panel
		EnableLayerPanel(layerPanel, hasPatches);
    }

}
