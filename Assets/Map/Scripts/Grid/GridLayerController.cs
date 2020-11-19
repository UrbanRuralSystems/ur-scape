// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;

public class GridLayerController : MapLayerControllerT<GridMapLayer>
{
	public GridMapLayer gridLayerPrefab;
	public GridMapLayer categoryGridLayerPrefab;
	public float offCenter = 0.15f;

	public delegate void OnShowGridDelegate(GridMapLayer mapLayer, bool show);
	public event OnShowGridDelegate OnShowGrid;

	private readonly HashSet<DataLayer> visibleDataLayers = new HashSet<DataLayer>();

	private static readonly float MeanThreshold = 0.2f;
	private static readonly float InvMeanThresholdLog = 1f / Mathf.Log(MeanThreshold);
	private float gamma = 1;


	//
	// Unity Methods
	//


	//
	// Public Methods
	//

	public GridMapLayer Add(GridData grid)
	{
		GridMapLayer layer = Instantiate(grid.IsCategorized ? categoryGridLayerPrefab : gridLayerPrefab);
		Add(layer, grid);
		return layer;
	}

	public void Add(GridMapLayer layer, GridData grid)
	{
		layer.Init(map, grid);

#if UNITY_EDITOR
		layer.name = grid.patch.DataLayer.Name;
		if (!string.IsNullOrWhiteSpace(grid.patch.Filename))
			layer.name += Patch.GetFileNamePatch(grid.patch.Filename);
#endif

		var dataLayer = grid.patch.DataLayer;
		layer.SetColor(dataLayer.Color);
		layer.transform.SetParent(transform, false);

		mapLayers.Add(layer);
		grid.patch.SetMapLayer(layer);

		if (!visibleDataLayers.Contains(dataLayer))
		{
			visibleDataLayers.Add(dataLayer);
		}

		OnShowGrid?.Invoke(layer, true);

		ArrangeLayers();

		if (GridMapLayer.ManualGammaCorrection)
			layer.SetGamma(gamma);
		else
			UpdateGamma(layer);
	}

	public void Remove(GridMapLayer layer)
	{
		mapLayers.Remove(layer);
		layer.Grid.patch.SetMapLayer(null);

		// If it's the last patch then remove the data layer
		var dataLayer = layer.Grid.patch.DataLayer;
		if (!dataLayer.HasLoadedPatchesInView())
		{
			visibleDataLayers.Remove(dataLayer);
		}

		OnShowGrid?.Invoke(layer, false);

		Destroy(layer.gameObject);

		ArrangeLayers();
	}

	public void SetGamma(float g)
	{
		gamma = g;
		foreach (var gridMapLayer in mapLayers)
		{
			gridMapLayer.SetGamma(gamma);
		}
	}

	public void AutoGamma()
	{
		foreach (var mapLayer in mapLayers)
		{
			UpdateGamma(mapLayer);
		}
	}

	public void AutoGamma(GridMapLayer mapLayer)
	{
		var mean = mapLayer.PatchData.patch.SiteRecord.layerSite.mean;
		if (mean < MeanThreshold)
		{
			mapLayer.SetGamma(Mathf.Log(mean) * InvMeanThresholdLog);
		}
		else
		{
			mapLayer.SetGamma(1f);
		}
	}


	//
	// Private Methods
	//

	private void ArrangeLayers()
	{
		if (mapLayers.Count == 1)
		{
			mapLayers[0].SetOffset(0, 0);
		}
		else if (mapLayers.Count > 1)
		{
			if (offCenter > 0)
			{
				int count = 0;
				foreach (var dataLayer in visibleDataLayers)
				{
					if (!dataLayer.IsTemp && dataLayer.patchesInView[0] is GridPatch)
						count++;
				}

				float invCount = 1f / count;
				float radians = Mathf.Deg2Rad * 360f * invCount;
				float distance = offCenter * (1f - invCount);
				int index = 0;
				foreach (var dataLayer in visibleDataLayers)
				{
					float xOffset, yOffset;
					if (dataLayer.IsTemp)
					{
						xOffset = yOffset = 0;
					}
					else
					{
						var rad = index * radians;
						xOffset = distance * Mathf.Cos(rad);
						yOffset = distance * Mathf.Sin(rad);
					}

					foreach (var patch in dataLayer.loadedPatchesInView)
					{
						var mapLayer = patch.GetMapLayer() as GridMapLayer;
						if (mapLayer != null && patch is GridPatch)
						{
							mapLayer.SetOffset(xOffset, yOffset);
						}
					}
					index++;
				}
			}
			else
			{
				foreach (var mapLayer in mapLayers)
				{
					mapLayer.SetOffset(0, 0);
				}
			}
		}
	}

	private void UpdateGamma(GridMapLayer mapLayer)
	{
		var patch = mapLayer.PatchData.patch;

		// Don't change gamma for dynamically created map layers
		if (string.IsNullOrEmpty(patch.Filename))
			return;

		AutoGamma(mapLayer);
	}

}
