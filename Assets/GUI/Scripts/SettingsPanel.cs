// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
	[Header("References")]
	public DataLayers dataLayers;

	[Header("UI References")]
	public RectTransform neck;
	public Button importButton;
	public Button dataMgrButton;
	public Button languageButton;
	public Toggle visibilityToggle;
	public Button resetButton;

	public Toggle hideLayersToggle;
	public Toggle hideEmptyGroupsToggle;
	public Toggle noDataToggle;
	public Toggle logarithmicToggle;
	public Toggle siteExtentsToggle;
	public Toggle layerExtentsToggle;
	public Toggle valueTypeToggle;
    public Toggle manualGammaToggle;
	public GameObject gammaPanel;
	public Slider gammaSlider;

	[Header("Prefabs")]
	public GameObject importDataPrefab;
	public GameObject dataManagerPrefab;
	public GameObject languageSelectionPrefab;

	private RectTransform thisRT;


	//
	// Unity Methods
	//

	private IEnumerator Start()
	{
		yield return WaitFor.Frames(WaitFor.InitialFrames);

		var map = ComponentManager.Instance.Get<MapController>();
		var siteBoundaryController = map.GetLayerController<SiteBoundaryLayerController>();
		var patchBoundaryController = map.GetLayerController<PatchBoundaryLayerController>();

		thisRT = transform as RectTransform;

#if UNITY_WEBGL
		importButton.interactable = false;
#else
		importButton.onClick.AddListener(OnImportClick);
#endif
		dataMgrButton.onClick.AddListener(OnDataManagerClick);
		languageButton.onClick.AddListener(OnLanguageClick);
		visibilityToggle.onValueChanged.AddListener(OnVisibilityToggleChanged);
		resetButton.onClick.AddListener(OnResetClick);

		hideLayersToggle.isOn = dataLayers.hideInactiveLayers;
		hideEmptyGroupsToggle.isOn = dataLayers.hideEmptyGroups;
		noDataToggle.isOn = GridMapLayer.ShowNoData;
		logarithmicToggle.isOn = FilterPanel.UseNonlinearDistribution;
		siteExtentsToggle.isOn = siteBoundaryController.IsShowingBoundaries;
		layerExtentsToggle.isOn = patchBoundaryController.IsShowingBoundaries;
		manualGammaToggle.isOn = GridMapLayer.ManualGammaCorrection;
		gammaSlider.onValueChanged.AddListener(OnGammaSliderChanged);

		hideLayersToggle.onValueChanged.AddListener(OnHideLayers);
		hideEmptyGroupsToggle.onValueChanged.AddListener(OnHideGroups);
		noDataToggle.onValueChanged.AddListener(OnShowNoData);
		logarithmicToggle.onValueChanged.AddListener(OnFilterDistiributionToggle);
		siteExtentsToggle.onValueChanged.AddListener(OnSiteExtentsToggle);
		layerExtentsToggle.onValueChanged.AddListener(OnLayerExtentsToggle);
		valueTypeToggle.onValueChanged.AddListener(OnValueTypeToggle);
        manualGammaToggle.onValueChanged.AddListener(OnManualGammaToggle);
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.mouseScrollDelta.y != 0)
		{
			if (!thisRT.IsMouseInside() && !neck.IsMouseInside())
				Close();
		}
	}


	//
	// Event Methods
	//

	private void OnResetClick()
	{
		dataLayers.ResetLayers();
		Close();
	}

	private void OnVisibilityToggleChanged(bool isOn)
	{
		dataLayers.ShowLayers(isOn);
		Close();
	}

	private void OnImportClick()
	{
		ComponentManager.Instance.Get<ModalDialogManager>().NewDialog(importDataPrefab);
		Close();
	}

	private void OnDataManagerClick()
	{
		ComponentManager.Instance.Get<ModalDialogManager>().NewDialog(dataManagerPrefab);
        Close();
	}

	private void OnLanguageClick()
	{
		ComponentManager.Instance.Get<ModalDialogManager>().NewDialog(languageSelectionPrefab);
		Close();
	}

	private void OnHideLayers(bool hide)
	{
		dataLayers.SetHideLayersWithoutVisiblePatches(hide);
	}

	private void OnHideGroups(bool hide)
	{
		dataLayers.SetHideEmptyGroups(hide);
	}

	private void OnShowNoData(bool show)
	{
		GridMapLayer.ShowNoData = show;

		var grids = ComponentManager.Instance.Get<MapController>().GetLayerController<GridLayerController>();
		foreach (var mapLayer in grids.mapLayers)
		{
			mapLayer.SetShowNoData(show);
		}
	}

	private void OnSiteExtentsToggle(bool isOn)
	{
		var map = ComponentManager.Instance.Get<MapController>();
		map.GetLayerController<SiteBoundaryLayerController>().ShowBoundaries(isOn);
	}

	private void OnLayerExtentsToggle(bool isOn)
	{
		var map = ComponentManager.Instance.Get<MapController>();
		map.GetLayerController<PatchBoundaryLayerController>().ShowBoundaries(isOn);
	}

    private void OnValueTypeToggle(bool isOn)
    {
        // Update All Active Filter panels
        var activeLayerPanels = dataLayers.activeLayerPanels;
        foreach (var dataLayerPanel in activeLayerPanels)
        {
            var panel = dataLayerPanel.Panel as FilterPanel;
            if (panel != null && panel.IsActive)
            {
                panel.RefreshValueType(isOn);
            }
        }
    }

    private void OnFilterDistiributionToggle(bool isOn)
	{
		// Update variable for all the Filter Panels
		FilterPanel.UseNonlinearDistribution = isOn;

		// Update All Active Filter panels
		var activeLayerPanels = dataLayers.activeLayerPanels;
		foreach (var dataLayerPanel in activeLayerPanels)
		{
			var panel = dataLayerPanel.Panel as FilterPanel;
			if (panel != null && panel.IsActive)
			{
				panel.RefreshDistributionChart();
			}
		}
	}

	private void OnManualGammaToggle(bool isOn)
	{
		gammaPanel.SetActive(isOn);

		GridMapLayer.ManualGammaCorrection = isOn;

		var map = ComponentManager.Instance.Get<MapController>();
		var gridLayerController = map.GetLayerController<GridLayerController>();

		if (isOn)
		{
			gridLayerController.SetGamma(gammaSlider.value);
		}
		else
		{
			gridLayerController.AutoGamma();
		}

		GuiUtils.RebuildLayout(gammaPanel.transform.parent as RectTransform);
	}

	private void OnGammaSliderChanged(float value)
	{
		var map = ComponentManager.Instance.Get<MapController>();
		var gridLayerController = map.GetLayerController<GridLayerController>();
		gridLayerController.SetGamma(value);
	}


	//
	// Public Methods
	//

	public void Close()
	{
		Show(false);
	}

	public void Show(bool show)
	{
		languageButton.interactable = LocalizationManager.Instance.languages.Count > 1;

		gameObject.SetActive(show);
	}

	public bool IsVisible()
	{
		return gameObject.activeSelf;
	}

}
