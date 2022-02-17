// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;

public class PatchBoundaryLayerController : MapLayerControllerT<BoundaryMapLayer>
{
	[Header("Prefabs")]
    public BoundaryMapLayer layerPrefab;

	[Header("Settings")]
	public bool allowMultiple = false;

	private DataLayer highlightedLayer;

	private bool showBoundaries = false;
	public bool IsShowingBoundaries { get { return showBoundaries; } }

	private readonly Dictionary<DataLayer, int> layerPatchCount = new Dictionary<DataLayer, int>();


	//
	// Unity Methods
	//

	private void Start()
	{
		ComponentManager.Instance.OnRegistrationFinished += OnRegistrationFinished;
	}


	//
	// Inheritance Methods
	//

	public override void UpdateLayers()
	{
		base.UpdateLayers();

		UpdateMapLayerZ();
	}


	//
	// Event Methods
	//

	private void OnRegistrationFinished()
	{
		var dataLayers = ComponentManager.Instance.Get<DataLayers>();
		dataLayers.OnLayerVisibilityChange += OnLayerVisibilityChange;
	}

	private void OnLayerVisibilityChange(DataLayer dataLayer, bool visible)
	{
		if (visible)
		{
			dataLayer.OnPatchVisibilityChange += OnPatchVisibilityChange;
		}
		else
		{
			dataLayer.OnPatchVisibilityChange -= OnPatchVisibilityChange;
		}
	}

	private void OnPatchVisibilityChange(DataLayer dataLayer, Patch patch, bool visible)
	{
		if (visible)
		{
			if (layerPatchCount.TryGetValue(dataLayer, out int count))
				layerPatchCount[dataLayer] = ++count;
			else
				layerPatchCount.Add(dataLayer, ++count);

			if (allowMultiple || count == 1)
			{
				// Only add the first patch if multiple patches are not allowed
				Add(patch.Data, dataLayer.Color);
				UpdateMapLayerZ();
			}
			else if (count == 2)
			{
				// Remove all patches from this layer if multiple patches are not allowed
				RemovePatchesForLayer(dataLayer);
			}
		}
		else
		{
			int count = layerPatchCount[dataLayer];
			if (count == 1)
				layerPatchCount.Remove(dataLayer);
			else
				layerPatchCount[dataLayer] = count - 1;

			Remove(patch.Data);
		}
	}


	//
	// Public Methods
	//

	public void ShowBoundaries(bool show)
	{
		showBoundaries = show;
		foreach (var layer in mapLayers)
		{
			layer.Show(show);
		}
	}

	public void HighlightBoundary(DataLayer layer, bool show)
	{
		int count = mapLayers.Count;
		int last = count - 1;
		for (int i = 0; i < count; i++)
		{
			var mapLayer = mapLayers[i];
			if (mapLayer.PatchData.patch.DataLayer == layer)
			{
				if (show)
				{
					if (!mapLayer.IsVisible())
						mapLayer.Show(true);

					if (i < last)
					{
						mapLayers[i] = mapLayers[last];
						mapLayers[last] = mapLayer;
						last--;
						count--;
						i--;
					}
				}
				else
				{
					if (!showBoundaries && mapLayer.IsVisible())
						mapLayer.Show(false);
				}
			}
		}

		if (show)
		{
			UpdateMapLayerZ();
		}

		highlightedLayer = show ? layer : null;
	}

	//
	// Private Methods
	//

	private BoundaryMapLayer Add(PatchData patch, Color color)
    {
		var layer = Instantiate(layerPrefab);
		layer.Init(map, patch);
		layer.SetColor(color);
		layer.transform.SetParent(transform, false);
		if (!showBoundaries && patch.patch.DataLayer != highlightedLayer)
			layer.Show(false);

		mapLayers.Add(layer);

		return layer;
	}

	private void Remove(PatchData siteData)
    {
		int count = mapLayers.Count;
        for (int i = 0; i < count; ++i)
        {
            if (mapLayers[i].PatchData == siteData)
            {
                Destroy(mapLayers[i].gameObject);
                mapLayers.RemoveAt(i);
                break;
            }
        }
	}

	private void RemovePatchesForLayer(DataLayer layer)
	{
		int last = mapLayers.Count - 1;
		for (int i = last; i >= 0; --i)
		{
			if (mapLayers[i].PatchData.patch.DataLayer == layer)
			{
				Destroy(mapLayers[i].gameObject);
				mapLayers[i] = mapLayers[last];
				mapLayers.RemoveAt(last--);
			}
		}
	}

	private void Clear()
    {
        foreach (var layer in mapLayers)
        {
            Destroy(layer.gameObject);
        }
        mapLayers.Clear();
		layerPatchCount.Clear();
	}

	private void UpdateMapLayerZ()
	{
		int index = 0;
		foreach (var layer in mapLayers)
		{
			var pos = layer.transform.localPosition;
			pos.z = -0.001f - 0.0001f * index++;
			layer.transform.localPosition = pos;
		}
	}

}
