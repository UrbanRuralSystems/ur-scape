// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TransectLocator : UrsComponent
{
	[Header("Settings")]
	public bool updateAllLayers = true;

	[Header("UI References")]
	public Slider slider;

    public event UnityAction<float> OnLocatorChange;

    private GridLayerController gridLayerController;

	public float Locator {  get { return slider.value; } }


    //
    // Unity Methods
    //

    protected void OnEnable()
    {
        var map = ComponentManager.Instance.Get<MapController>();
        if (map != null)
        {
            gridLayerController = map.GetLayerController<GridLayerController>();
            if (updateAllLayers && gridLayerController != null)
            {
                gridLayerController.OnShowGrid += OnShowGridMapLayer;
                foreach (var mapLayer in gridLayerController.mapLayers)
                {
                    mapLayer.SetTransect(slider.value);
                    mapLayer.ShowTransect(true);
                }
            }
        }

		slider.onValueChanged.AddListener(OnSliderChanged);
    }


    protected void OnDisable()
    {
		slider.onValueChanged.RemoveAllListeners();

		if (gridLayerController != null)
        {
            gridLayerController.OnShowGrid -= OnShowGridMapLayer;

            foreach (var mapLayer in gridLayerController.mapLayers)
            {
                mapLayer.ShowTransect(false);
            }
        }
    }
	

    //
    // Event Methods
    //

    private void OnShowGridMapLayer(GridMapLayer mapLayer, bool show)
    {
        if (show)
        {
            mapLayer.SetTransect(slider.value);
            mapLayer.ShowTransect(true);
        }
    }

	private void OnSliderChanged(float value)
	{
		// Update map layers
		foreach (var mapLayer in gridLayerController.mapLayers)
		{
			mapLayer.SetTransect(value);
		}

		if (OnLocatorChange != null)
			OnLocatorChange(value);
	}

}
