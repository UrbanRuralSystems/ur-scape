// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.Collections.Generic;
using UnityEngine;

public class LegendPanel : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How often the panel should be updated (in seconds)")]
    [SerializeField] private float updateInterval = 0.1f;

    [Header("UI References")]
    [SerializeField] private RectTransform legendListContainer = default;

    [Header("Prefabs")]
    [SerializeField] private UncategorisedLegendItem uncategorisedLegendItem = default;
    [SerializeField] private CategorisedLegendItem categorisedLegendItem = default;
    [SerializeField] private SnapshotLegendItem snapshotLegendItem = default;

    private DataLayers dataLayers;
	private float nextUpdate;

    public readonly Dictionary<string, LegendItem> LegendItems = new Dictionary<string, LegendItem>();

    //
    // Unity Methods
    //

    private void Start()
    {
        dataLayers = ComponentManager.Instance.Get<DataLayers>();

        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Time.time < nextUpdate)
            return;

        nextUpdate = Time.time + updateInterval;

        UpdatePanelValues();
    }

    private void OnDisable()
    {
        UpdatePanelValues();
    }

    //
    // Event Methods
    //



    //
    // Public Methods
    //

    public void AddSnapshotItem(string id, Color color, string name)
    {
        if (!LegendItems.ContainsKey(id))
        {
            var item = Instantiate(snapshotLegendItem, legendListContainer, false);

            // Update values
            item.name = name;
            item.SetDotColor(color);
            item.SetDisplayName(name);

            LegendItems.Add(id, item);
            item.transform.SetAsLastSibling();
        }
    }

    public void RemoveSnapshotItem(string id)
    {
        if (LegendItems.ContainsKey(id))
        {
            Destroy(LegendItems[id].gameObject);
            LegendItems.Remove(id);
        }
    }

    //
    // Private Methods
    //

    private void UpdateUncategorisedItem(DataLayer dataLayer)
    {
        string layerName = dataLayer.Name;
        // Get Data layer's filter values
        float diff = dataLayer.MaxVisibleValue - dataLayer.MinVisibleValue;
        float min = dataLayer.MinFilter * diff + dataLayer.MinVisibleValue;
        float max = dataLayer.MaxFilter * diff + dataLayer.MinVisibleValue;

        Color color = dataLayer.Color;

        if (!LegendItems.TryGetValue(layerName, out LegendItem item))
        {
            item = Instantiate(uncategorisedLegendItem, legendListContainer, false);

            // Update values
            item.name = item.LayerName = layerName;
            item.SetDotColor(color);
            item.SetDisplayName(layerName);

            LegendItems.Add(layerName, item);
            MoveSnapshotItemsToEnd();
        }

        var uncategorisedItem = item as UncategorisedLegendItem;

        bool reverse = false;
        if (dataLayer.HasLoadedPatchesInView())
        {
            var data = dataLayer.loadedPatchesInView[0].Data;
            var gridData = data as GridData;

            reverse = gridData.coloring == GridData.Coloring.ReverseSingle
                        || gridData.coloring == GridData.Coloring.ReverseMulti
                        || gridData.coloring == GridData.Coloring.Reverse;

            uncategorisedItem.SetUnits(gridData.units);
        }

        uncategorisedItem.SetMin(min);
        uncategorisedItem.SetMax(max);
        uncategorisedItem.SetMinAlpha(dataLayer.MinFilter, reverse);
        uncategorisedItem.SetMaxAlpha(dataLayer.MaxFilter, reverse);
    }

    private LegendItem AddCategorisedItem(DataLayer dataLayer)
    {
        string layerName = dataLayer.Name;
        Color color = dataLayer.Color;

        if (!LegendItems.TryGetValue(layerName, out LegendItem item))
        {
            item = Instantiate(categorisedLegendItem, legendListContainer, false);

            // Update values
            item.name = item.LayerName = layerName;
            item.SetDotColor(color);
            item.SetDisplayName(layerName);

            LegendItems.Add(layerName, item);
            MoveSnapshotItemsToEnd();
        }

        return item;
    }

    private void RemoveUnwantedItems()
    {
        for (int i = legendListContainer.childCount - 1; i >= 0; i--)
        {
            // Determine whether child item is part of active data layers
            // If not, destroy and remove from legendItems
            LegendItem item = legendListContainer.GetChild(i).GetComponent<LegendItem>();
            var dataLayer = dataLayers.activeLayerPanels.Find((dataLayerPanel) => Equals(dataLayerPanel.DataLayer.Name, item.LayerName));

            if (dataLayer == null)
            {
                string key = item.name;
                if (LegendItems.ContainsKey(key))
                {
                    Destroy(LegendItems[key].gameObject);
                    LegendItems.Remove(key);
                }
            }
        }
    }

    private void UpdatePanelValues()
    {
        RemoveUnwantedItems();

        foreach (var layerPanel in dataLayers.activeLayerPanels)
        {
            var dataLayer = layerPanel.DataLayer;
            if (!dataLayers.availableLayers.Contains(dataLayer))
                continue;

            if (!dataLayer.HasLoadedPatchesInView())
                continue;

            var data = dataLayer.loadedPatchesInView[0].Data;
            var gridData = data as GridData;
            bool isCategorized = gridData.IsCategorized;

            if (isCategorized)
            {
                int catCount = gridData.categories.Length;
                int selectedCount = gridData.categoryFilter.GetSetCount(catCount);
                var item = AddCategorisedItem(dataLayer) as CategorisedLegendItem;

                // Get data layer's active categories
                if (selectedCount > 1)
                {
                    for (int c = 0; c < catCount; ++c)
                    {
                        string name = gridData.categories[c].name;

                        // Add active categories if it does not exist in legendItems
                        // Destroy and remove inactive categories if it exists in legendItems
                        if (gridData.categoryFilter.IsSet(c))
                        {
                            Color color = gridData.categories[c].color;
                            item.AddCategory(name, color);
                        }
                        else
                        {
                            item.RemoveCategory(name);
                        }
                    }
                }
                else if (selectedCount == 1)
                {
                    for (int c = 0; c < catCount; ++c)
                    {
                        string name = gridData.categories[c].name;

                        // Add active categories if it does not exist in legendItems
                        // Destroy and remove inactive categories if it exists in legendItems
                        if (gridData.categoryFilter.IsSet(c))
                        {
                            Color color = gridData.categories[c].color;
                            item.AddCategory(name, color);
                            break;
                        }
                        else
                        {
                            item.RemoveCategory(name);
                        }
                    }
                }
                else
                {
                    for (int c = 0; c < catCount; ++c)
                    {
                        string name = gridData.categories[c].name;
                        item.RemoveCategory(name);
                    }
                }
            }
            else
            {
                UpdateUncategorisedItem(dataLayer);
            }
        }
    }

    private void MoveSnapshotItemsToEnd()
    {
        foreach (var item in LegendItems.Values)
        {
            if (item is SnapshotLegendItem snapshotLegendItem)
            {
                snapshotLegendItem.transform.SetAsLastSibling();
            }
        }
    }
}