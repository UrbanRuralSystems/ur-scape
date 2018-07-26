// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegendComponent : MonoBehaviour
{
    public static readonly string CITATIONS_FILE = "citations.csv";


    [Header("UI References")]
    public Transform container;

    [Header("Prefabs")]
    public LegendLayerEntry layerEntryPrefab;

    [Header("Legend")]
    [Tooltip("Time to wait before updating legend values, measured in seconds")]
    [Range(0.1f, 2.0f)]
    public float updateInterval = 0.2f;
    private float nextUpdate;

    private InputHandler inputHandler;
    private DataLayers dataLayers;
    private MapController map;
    private RectTransform mapViewTransform;
    private Dictionary<DataLayer, LegendLayerEntry> entries = new Dictionary<DataLayer, LegendLayerEntry>();

    private LegendLayerEntry resolution;
    private Citations citations;


    //
    // Unity Methods
    //

    private IEnumerator Start()
	{
		// Avoid updates while loading
		nextUpdate = int.MaxValue;

		Loader.Create(LoadCitations(), true);

		yield return WaitFor.Frames(WaitFor.InitialFrames);

		inputHandler = ComponentManager.Instance.Get<InputHandler>();
        map = ComponentManager.Instance.Get<MapController>();
        var mapViewArea = ComponentManager.Instance.Get<MapViewArea>();
        if (mapViewArea != null)
            mapViewTransform = mapViewArea.transform as RectTransform;

        dataLayers = ComponentManager.Instance.Get<DataLayers>();
        dataLayers.OnLayerVisibilityChange += OnDataLayerVisibilityChange;

        resolution = Instantiate(layerEntryPrefab, container, false);
		resolution.Init("Resolution", Color.clear);

		nextUpdate = Time.time + updateInterval;
	}

	private IEnumerator LoadCitations()
    {
		yield return CitationsConfig.Load(Paths.Data + CITATIONS_FILE, (t) => citations = t);
    }

    private void OnDestroy()
    {
        dataLayers.OnLayerVisibilityChange -= OnDataLayerVisibilityChange;
    }

    private void Update()
    {
        if (nextUpdate < Time.time)
        {
            nextUpdate += updateInterval;

            UpdateLegendValues();
        }
    }


    //
    // Event Methods
    //

    private void OnDataLayerVisibilityChange(DataLayer layer, bool visible)
    {
        if (visible)
        {
            int index = 0;
            foreach (var pair in entries)
            {
                if (pair.Key.index < layer.index)
                    index++;
            }

            var newLegendEntry = Instantiate(layerEntryPrefab, container, false);
            newLegendEntry.Init(layer.name, layer.color);
            entries.Add(layer, newLegendEntry);

            newLegendEntry.transform.SetSiblingIndex(index + 2);

			layer.OnPatchVisibilityChange += OnPatchVisibilityChange;
        }
        else
        {
			layer.OnPatchVisibilityChange -= OnPatchVisibilityChange;
			Destroy(entries[layer].gameObject);
            entries.Remove(layer);
        }
    }

	private void OnPatchVisibilityChange(DataLayer dataLayer, Patch patch, bool visible)
	{
		if (visible)
		{
			var entry = entries[dataLayer];
			var gridData = patch.Data as GridData;
			if (gridData != null && gridData.metadata != null)
			{
				var key = dataLayer.name + "|" + gridData.metadata.source;

				entry.Expand(citations.mandatory.Contains(key));

				if (citations.map.ContainsKey(key))
					entry.SetCredit(citations.map[key]);
			}
		}
	}


	//
	// Private Methods
	//

	private void UpdateLegendValues()
    {
        var localPos = mapViewTransform.InverseTransformPoint(Input.mousePosition);
        if (!mapViewTransform.rect.Contains(localPos))
        {
            ResetLegendEntries();
            return;
        }

        Vector3 worldPos;
        if (!inputHandler.GetWorldPoint(Input.mousePosition, out worldPos))
        {
            ResetLegendEntries();
            return;
        }

		float cellSize = float.MaxValue;

        Coordinate coords = map.GetCoordinatesFromUnits(worldPos.x, worldPos.z);
        foreach (var pair in entries)
        {
            string legendValue = "N/A";
            foreach (GridedPatch patch in pair.Key.loadedPatchesInView)
            {
                var gridData = patch.grid;
                if (gridData.values != null && gridData.IsInside(coords.Longitude, coords.Latitude))
                {
					float? cell;
					if (gridData.valuesMask == null)
						cell = gridData.GetValue(coords.Longitude, coords.Latitude);
					else 
						cell = gridData.GetCell(coords.Longitude, coords.Latitude);

                    if (cell.HasValue)
                    {
                        if (gridData.IsCategorized)
                        {
                            int category = Mathf.RoundToInt(cell.Value);
                            if (category >= 0 && category < gridData.categories.Length)
                                legendValue = gridData.categories[category].name;
                            else
                                legendValue = "Unknown (" + category + ")";
                        }
                        else
                        {
							if (gridData.maxValue >= 10)
								legendValue = Mathf.RoundToInt(cell.Value) + " " + gridData.units;
							else
								legendValue = cell.Value.ToString("0.##") + " " + gridData.units;
						}
                    }
                }


				if (gridData.countX > 0)
				{
					float meters = (float)(GeoCalculator.Deg2Meters * gridData.GetCellSize());
					if (meters < cellSize)
					{
						cellSize = meters;
					}
				}
			}
			pair.Value.SetValue(legendValue);
        }

		if (cellSize == float.MaxValue)
		{
			resolution.SetValue("N/A");
		}
		else
		{
			string unit = "m";
			if (cellSize > 1000)
			{
				unit = "km";
				cellSize *= 0.001f;
			}

			int number = Mathf.RoundToInt(cellSize);
			resolution.SetValue(number + "x" + number + unit);
		}
	}

	private void ResetLegendEntries()
    {
		resolution.SetValue("N/A");

        foreach (var entry in entries.Values)
        {
            entry.SetValue("N/A");
        }
    }

}
