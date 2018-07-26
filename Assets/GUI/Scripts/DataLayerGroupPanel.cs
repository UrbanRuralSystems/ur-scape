// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          David Neudecker  (neudecker@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class DataLayerGroupPanel : MonoBehaviour
{
    [Header("UI References")]
    public Text title;
    public RectTransform layersContainer;

    public void Init(string name)
    {
		this.name = name;
        title.text = name;
	}

    public DataLayerPanel AddLayer(DataLayerPanel prefab, DataLayer layer)
    {
        // Create a new instance of the layer prefab and initialize it
        var layerCtrl = Instantiate(prefab);
        // Important: set the parent AFTER Instantiate to allow toggles to awake
        // Parent could be disabled and prevent proper initialization
        layerCtrl.transform.SetParent(layersContainer, false);
        layerCtrl.Init(layer);
        return layerCtrl;
    }

	public void Show(bool show)
	{
		if (gameObject.activeSelf != show)
			gameObject.SetActive(show);
	}

	public void UpdateVisibility()
	{
		int count = layersContainer.childCount;
		for (int i = 0; i < count; ++i)
		{
			if (layersContainer.GetChild(i).gameObject.activeSelf)
			{
				if (!gameObject.activeSelf)
					gameObject.SetActive(true);
				return;
			}
		}
		if (gameObject.activeSelf)
			gameObject.SetActive(false);
	}
}
