// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          David Neudecker (neudecker@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class DataLayersOptions : MonoBehaviour
{

    [Header("UI References")]
    public Toggle hideLayersToggle;
	public Toggle hideEmptyGroupsToggle;
	public Toggle showNoDataToggle;
    public Toggle manualGammaToggle;
    public Toggle nonlinearFilterDistiribution;

    [Header("Prefabs")]
    public GridMapLayer gridLayerPrefab;


    //
    // Unity Methods
    //

    private void Start()
    {
		var dataLayers = ComponentManager.Instance.Get<DataLayers>();

        hideLayersToggle.isOn = dataLayers.hideInactiveLayers;
		hideEmptyGroupsToggle.isOn = dataLayers.hideEmptyGroups;
		showNoDataToggle.isOn = GridMapLayer.ShowNoData;
		manualGammaToggle.isOn = GridMapLayer.ManualGammaCorrection;
        nonlinearFilterDistiribution.isOn = FilterPanel.UseNonlinearDistribution; 

        hideLayersToggle.onValueChanged.AddListener(OnHideLayers);
		hideEmptyGroupsToggle.onValueChanged.AddListener(OnHideGroups);
		showNoDataToggle.onValueChanged.AddListener(OnShowNoData);
		manualGammaToggle.onValueChanged.AddListener(OnManualGammaToggle);
        nonlinearFilterDistiribution.onValueChanged.AddListener(OnAutomaticFilterDistiributionToggle);
    }

    private void OnAutomaticFilterDistiributionToggle(bool isOn)
    {
        // Update variable for all the Filter Panels
        FilterPanel.UseNonlinearDistribution = isOn;

        // Update All Active Filter panels
        var activeLayerPanels = ComponentManager.Instance.Get<DataLayers>().activeLayerPanels;
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
		ComponentManager.Instance.Get<DataLayersGammaCorrection>().Show(isOn);
    }

    private void OnHideLayers(bool hide)
	{
		ComponentManager.Instance.Get<DataLayers>().SetHideLayersWithoutVisiblePatches(hide);
	}

	private void OnHideGroups(bool hide)
	{
		ComponentManager.Instance.Get<DataLayers>().SetHideEmptyGroups(hide);
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

}
