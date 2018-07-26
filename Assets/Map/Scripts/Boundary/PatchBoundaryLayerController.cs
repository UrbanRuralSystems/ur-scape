// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using UnityEngine;

public class PatchBoundaryLayerController : MapLayerControllerT<PatchMapLayer>
{
	[Header("Prefabs")]
    public PatchMapLayer layerPrefab;

	private int boundariesLevel;


	//
	// Unity Methods
	//

	private IEnumerator Start()
	{
		yield return WaitFor.Frames(WaitFor.InitialFrames);

		map = ComponentManager.Instance.Get<MapController>();
		map.OnLevelChange += OnMapLevelChange;

		var dataLayers = ComponentManager.Instance.Get<DataLayers>();
		dataLayers.OnLayerVisibilityChange += OnLayerVisibilityChange;

		boundariesLevel = map.CurrentLevel + 1;
	}


	//
	// Event Methods
	//

	private void OnMapLevelChange(int level)
	{
		RemoveAllBoundaries();
		boundariesLevel = level + 1;
		CreateBoundaries(boundariesLevel);
	}

	private void OnLayerVisibilityChange(DataLayer dataLayer, bool visible)
	{
		if (visible)
		{
			CreateBoundaries(dataLayer, boundariesLevel);
		}
		else
		{
			RemoveBoundaries(dataLayer, boundariesLevel);
		}
	}


	//
	// Public Methods
	//

	public PatchMapLayer Add(PatchData patch, Color color)
    {
        PatchMapLayer layer = Instantiate(layerPrefab);
		Add(layer, patch, color);
		return layer;
    }

	public void Add(PatchMapLayer layer, PatchData patch, Color color)
	{
		layer.Init(map, patch);
		layer.SetColor(color);
		layer.transform.SetParent(transform, false);
		mapLayers.Add(layer);
	}

    public void Remove(PatchData siteData)
    {
        int count = mapLayers.Count;
        for (int i = 0; i < count; i++)
        {
            if (mapLayers[i].PatchData == siteData)
            {
                Destroy(mapLayers[i].gameObject);
                mapLayers.RemoveAt(i);
                break;
            }
        }
    }

    public void Clear()
    {
        foreach (var layer in mapLayers)
        {
            Destroy(layer.gameObject);
        }
        mapLayers.Clear();
    }


	//
	// Private Methods
	//

	private void CreateBoundaries(int level)
	{
		var dataLayers = ComponentManager.Instance.Get<DataLayers>();
		foreach (var layerPanel in dataLayers.activeLayerPanels)
		{
			CreateBoundaries(layerPanel.DataLayer, level);
		}
	}

	private void RemoveAllBoundaries()
	{
		Clear();
	}

	private void CreateBoundaries(DataLayer dataLayer, int level)
	{
		if (level < dataLayer.levels.Length)
		{
			var lowerSites = dataLayer.levels[level].layerSites;
			foreach (var site in lowerSites)
			{
				foreach (var patch in site.lastRecord.patches)
				{
					Add(patch.Data, dataLayer.color);
				}
			}
		}
	}

	private void RemoveBoundaries(DataLayer dataLayer, int level)
	{
		if (level < dataLayer.levels.Length)
		{
			var lowerSites = dataLayer.levels[level].layerSites;
			foreach (var site in lowerSites)
			{
				foreach (var patch in site.lastRecord.patches)
				{
					Remove(patch.Data);
				}
			}
		}
	}

}
