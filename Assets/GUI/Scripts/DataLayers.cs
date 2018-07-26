// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

#if UNITY_EDITOR
#define SAFETY_CHECK
#endif

using ExtensionMethods;
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
    public ScrollRect scrollRect;
    public RectTransform shadow;
    public Toggle optionsToggle;
	public Toggle visibilityToggle;
	public Button resetButton;

	[Header("Prefabs")]
    public DataLayerGroupPanel guiGroupPrefab;
    public DataLayerPanel layerPanelPrefab;
    public Transform preferencePanelPrefab;

    public event OnLayerChangeDelegate OnLayerVisibilityChange;
	public event OnLayerChangeDelegate OnLayerAvailabilityChange;

	private MapController map;

    private readonly Dictionary<string, DataLayerPanel> nameToLayer = new Dictionary<string, DataLayerPanel>();
    private readonly List<DataLayerPanel> layerPanels = new List<DataLayerPanel>();				// All layer-panels
	public readonly List<DataLayerPanel> activeSiteLayerPanels = new List<DataLayerPanel>();    // Only layer-panels available for the active site
	public readonly List<DataLayerPanel> activeLayerPanels = new List<DataLayerPanel>();		// Layer-panels that are toggled ON
	public readonly HashSet<DataLayer> availableLayers = new HashSet<DataLayer>();				// From the active layers, only those that are in view

	private bool updatingMap;
    private float updateUntilTime;
    private static readonly float MapUpdateInterval = 0.3f;
    private static readonly WaitForSeconds MapUpdateDelay = new WaitForSeconds(MapUpdateInterval + 0.02f);

    private Transform preferencePanel;
	private float originalTitleHeight;
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

        scrollRect.onValueChanged.AddListener(OnScrollChanged);
		optionsToggle.onValueChanged.AddListener(OnOptionsToggleChanged);
		visibilityToggle.onValueChanged.AddListener(OnVisibilityToggleChanged);
		resetButton.onClick.AddListener(OnResetClick);

		originalTitleHeight = optionsToggle.transform.parent.GetComponent<RectTransform>().rect.height;
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
						bw.Write(patch.Data.categoryMask);
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
						uint mask = br.ReadUInt32();

						if (l < dataLayer.levels.Length && s < dataLayer.levels[l].sites.Count)
						{
							var site = dataLayer.levels[l].sites[s];
							var record = site.lastRecord;

							foreach (var patch in record.patches)
							{
								var gridPatch = patch as GridPatch;
								if (gridPatch != null && gridPatch.grid.IsCategorized && gridPatch.name.Equals(patchName))
								{
									gridPatch.SetCategoryMask(mask);
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

	private void OnOptionsToggleChanged(bool isOn)
	{
		if (isOn)
		{
			preferencePanel = Instantiate(preferencePanelPrefab, optionsToggle.transform.parent, false);
            StartCoroutine(UpdateLayersListHeight());
		}
		else
		{
			Destroy(preferencePanel.gameObject);
			preferencePanel = null;

			SetLayersListTopOffset(originalTitleHeight);
		}
	}

	private void OnVisibilityToggleChanged(bool isOn)
	{
		var controller = map.GetLayerController<GridLayerController>();
		foreach (var layer in controller.mapLayers)
		{
			layer.Show(isOn);
		}
	}

	private void OnResetClick()
	{
		foreach (var panel in layerPanels)
		{
			panel.layerToggle.isOn = false;
			panel.DataLayer.ResetFiltersAndOpacity();
		}
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
		// Hide all the current patches in our view (loaded or not)
		foreach (var layerPanel in activeSiteLayerPanels)
		{
			layerPanel.DataLayer.HidePatchesInView();
		}

		// Update the list of layer panels available for the new active site
		activeSiteLayerPanels.Clear();
		foreach (var panel in layerPanels)
		{
			var dataLayer = panel.DataLayer;
			bool found = false;
			foreach (var level in dataLayer.levels)
			{
				foreach (var layerSite in level.layerSites)
				{
					if (layerSite.site == site)
					{
						found = true;
						break;
					}
				}

				if (found)
					break;
			}

			if (found)
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

        if (panel.filterToggle.isOn)
        {
            StartCoroutine(ShowOptionsPanel(panel, isOn));
        }
    }

	private void OnShowGrid(GridMapLayer mapLayer, bool show)
	{
		if (show && !visibilityToggle.isOn)
			mapLayer.Show(false);
	}


	//
	// Public Methods
	//

	public void Show(bool show)
    {
        groupsContainer.gameObject.SetActive(show);

        if (show)
        {
            GuiUtils.RebuildLayout(groupsContainer.transform);
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

	public void AddLayer(DataLayer layer, DataLayerGroupPanel groupController)
	{
		// Add new layer
		DataLayerPanel layerPanel = groupController.AddLayer(layerPanelPrefab, layer);
		layerPanel.layerToggle.onValueChanged.AddListener((isOn) => OnLayerToggleChange(layerPanel, isOn));
		layerPanel.filterToggle.onValueChanged.AddListener((isOn) => StartCoroutine(ShowOptionsPanel(layerPanel, isOn)));

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
		if (nameToLayer.ContainsKey(layer.name))
		{
			Debug.LogWarning(layer.name + " appears more than once in the layers list");
		}
		else
#endif
		{
			layerPanels.Add(layerPanel);
			nameToLayer.Add(layer.name, layerPanel);
		}
	}

	public bool IsLayerActive(DataLayer layer)
	{
#if SAFETY_CHECK
		if (!nameToLayer.ContainsKey(layer.name))
        {
            Debug.LogError("Layer " + layer.name + " could not be found");
            return false;
        }
#endif
        return nameToLayer[layer.name].IsLayerToggleOn;
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
        if (!nameToLayer.ContainsKey(layer.name))
        {
            Debug.LogError("Layer " + layer.name + " could not be found");
            return;
        }
#endif
		nameToLayer[layer.name].layerToggle.isOn = activate;
	}

	public void AutoReduceToolOpacity()
	{
		if (availableLayers.Count == 0)
			SetToolOpacity(1f);
		else
			SetToolOpacity(1f / (availableLayers.Count +1) );	
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

    // Helper function to show/hide or enable/disable layer depending on HideLayersWithoutVisiblePatches
    public void EnableLayerPanel(DataLayerPanel layerPanel, bool enable)
	{
		if (hideInactiveLayers)
		{
			// Show the group BEFORE the layer is shown
			if (hideEmptyGroups && enable && layerPanel.gameObject.activeSelf != enable)
				layerPanel.transform.parent.parent.GetComponent<DataLayerGroupPanel>().Show(true);

			if (layerPanel.ShowLayerPanel(enable))
			{
				// Hide the group AFTER the layer is hidden (only if no other layers are visible)
				if (hideEmptyGroups && !enable)
					layerPanel.transform.parent.parent.GetComponent<DataLayerGroupPanel>().UpdateVisibility();
			}
		}
		else
		{
			layerPanel.EnableLayerPanel(enable);
		}
	}

	private static readonly Vector2 bottomPoint = new Vector2(50, -1);
    private IEnumerator ShowOptionsPanel(DataLayerPanel layerCtrl, bool show)
    {
        // FIX: scrollbar won't retract if value is zero.
        if (!show && scrollRect.verticalScrollbar.value < 0.001f && scrollRect.verticalScrollbar.size < 1)
            scrollRect.verticalScrollbar.value = 0.001f;

        layerCtrl.ShowOptionsPanel(show);
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

    private void OnScrollChanged(Vector2 value)
    {
        if (value.y <= 0.01f && shadow.gameObject.activeSelf)
        {
            shadow.gameObject.SetActive(false);
        }
        if (value.y > 0.01f && !shadow.gameObject.activeSelf)
        {
            shadow.gameObject.SetActive(true);
        }
    }

    private void ShowDataLayer(DataLayerPanel layerPanel)
    {
#if SAFETY_CHECK
        if (activeLayerPanels.Contains(layerPanel))
        {
            Debug.LogWarning("Layer " + layerPanel.DataLayer.name + " is already active!");
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
		if (layerPanel.IsOptionsToggleOn && layerPanel.Panel is FilterPanel)
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
		if (!nameToLayer.ContainsKey(layer.name))
		{
			Debug.LogError("Layer " + layer.name + " could not be found");
			return;
		}
#endif

		int level = map.CurrentLevel;
		var bounds = map.MapCoordBounds;

		UpdateLayerPanel(nameToLayer[layer.name], level, bounds);
	}

	private void UpdateLayerPanel(DataLayerPanel layerPanel, int level, AreaBounds bounds)
	{
		var dataLayer = layerPanel.DataLayer;

        layerPanel.UpdateFilterIcon();

        bool hasPatches;
		if (layerPanel.IsLayerToggleOn)
		{
			dataLayer.UpdatePatches(level, bounds);
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
			hasPatches = dataLayer.HasPatches(level, bounds.west, bounds.east, bounds.north, bounds.south);
		}

		// Show/hide a layer panel when only visible layers are allowed, otherwise enable/disable the layer panel
		EnableLayerPanel(layerPanel, hasPatches);
    }

	private IEnumerator UpdateLayersListHeight()
	{
		// Need to wait 1 frame to get the title bar's height
		yield return null;
		var titlePanel = optionsToggle.transform.parent.GetComponent<RectTransform>();
		SetLayersListTopOffset(titlePanel.rect.height);
	}

	private void SetLayersListTopOffset(float topOffset)
	{
		var layersList = scrollRect.GetComponent<RectTransform>();
		var offset = layersList.offsetMax;
		offset.y = -topOffset;
		layersList.offsetMax = offset;
	}
}
