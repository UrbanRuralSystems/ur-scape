// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;

public class VectorLayerController : MapLayerControllerT<VectorMapLayer>
{
	public PointMapLayer pointLayerPrefab;
	//- public PointMapLayer categoryGridLayerPrefab;

	public delegate void OnShowMapLayerDelegate(VectorMapLayer mapLayer, bool show);
	public event OnShowMapLayerDelegate OnShowMapLayer;

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

	public PointMapLayer Add(PointData pointData)
	{
		var mapLayer = Instantiate(pointLayerPrefab);
		Add(mapLayer, pointData);
		return mapLayer;
	}

	public void Add(PointMapLayer mapLayer, PointData pointData)
	{
		mapLayer.transform.SetParent(transform, false);

		mapLayer.Init(map, pointData);

#if UNITY_EDITOR
		mapLayer.name = pointData.patch.DataLayer.Name;
		if (!string.IsNullOrWhiteSpace(pointData.patch.Filename))
			mapLayer.name += Patch.GetFileNamePatch(pointData.patch.Filename);
#endif

		var dataLayer = pointData.patch.DataLayer;
		mapLayer.SetColor(dataLayer.Color);

		mapLayers.Add(mapLayer);
		pointData.patch.SetMapLayer(mapLayer);

		if (!visibleDataLayers.Contains(dataLayer))
		{
			visibleDataLayers.Add(dataLayer);
		}

		OnShowMapLayer?.Invoke(mapLayer, true);

		if (VectorMapLayer.ManualGammaCorrection)
			mapLayer.SetGamma(gamma);
		else
			UpdateGamma(mapLayer);
	}

	public void Remove(VectorMapLayer mapLayer)
	{
		mapLayers.Remove(mapLayer);
		mapLayer.PatchData.patch.SetMapLayer(null);

		// If it's the last patch then remove the data layer
		var dataLayer = mapLayer.PatchData.patch.DataLayer;
		if (!dataLayer.HasLoadedPatchesInView())
		{
			visibleDataLayers.Remove(dataLayer);
		}

		OnShowMapLayer?.Invoke(mapLayer, false);

		Destroy(mapLayer.gameObject);
	}

	public void SetGamma(float g)
	{
		gamma = g;
		foreach (var mapLayer in mapLayers)
		{
			mapLayer.SetGamma(gamma);
		}
	}

	public void AutoGamma()
	{
		foreach (var mapLayer in mapLayers)
		{
			UpdateGamma(mapLayer);
		}
	}

	public void AutoGamma(VectorMapLayer mapLayer)
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


	private void UpdateGamma(VectorMapLayer mapLayer)
	{
		var patch = mapLayer.PatchData.patch;

		// Don't change gamma for dynamically created map layers
		if (string.IsNullOrEmpty(patch.Filename))
			return;

		AutoGamma(mapLayer);
	}

}
