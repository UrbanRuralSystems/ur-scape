// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DataLayersGammaCorrection : UrsComponent
{
    [Header("Prefabs")]
    public GridMapLayer gridLayerPrefab;

	[Header("UI References")]
	public GameObject panel;
    public Slider slider;
    public Text label;

    // Component References
    private GridLayerController gridLayerController;


	//
	// Unity Methods
	//

    private IEnumerator Start()
    {
        yield return WaitFor.Frames(WaitFor.InitialFrames);

        var map = ComponentManager.Instance.Get<MapController>();
        gridLayerController = map.GetLayerController<GridLayerController>();

        slider.onValueChanged.AddListener(OnSliderChanged);

		Show(GridMapLayer.ManualGammaCorrection);
	}


	//
	// Event Methods
	//

	private void OnSliderChanged(float value)
    {
        label.text = value.ToString();
		gridLayerController.SetGamma(value);
    }


	//
	// Public Methods
	//

	public void Show(bool show)
	{
		panel.SetActive(show);
		GridMapLayer.ManualGammaCorrection = show;

		if (show)
		{
			gridLayerController.SetGamma(slider.value);
		}
		else
		{
			gridLayerController.AutoGamma();
		}
	}

}
