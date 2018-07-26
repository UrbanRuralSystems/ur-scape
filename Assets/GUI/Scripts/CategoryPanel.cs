// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//          Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CategoryPanel : LayerOptionsPanel
{
    [Header("UI References")]
    public ToggleButton classTogglePrefab;

    private List<Toggle> toggles = new List<Toggle>();
    private Toggle lastToggle;
    private float doubleClickTime = 0;
    private uint mask = 0xFFFFFFFF;


    //
    // Inheritance Methods
    //

    public override void Init(DataLayer dataLayer)
    {
        base.Init(dataLayer);

        if (dataLayer.HasLoadedPatchesInView() && AllSitesHaveSameCategories())
        {
            UpdateList();
        }

		LayoutRebuilder.ForceRebuildLayoutImmediate(transform.GetComponent<RectTransform>());
	}

    public override void Show(bool show)
    {
        base.Show(show);

        ClearList();
        if (show && dataLayer.HasLoadedPatchesInView() && AllSitesHaveSameCategories())
        {
            UpdateList();
        }
    }

    protected override void OnPatchVisibilityChange(DataLayer dataLayer, Patch patch, bool visible)
    {
        base.OnPatchVisibilityChange(dataLayer, patch, visible);
        
        bool rebuildLayout = false;
        if (visible)
        {
            if (AllSitesHaveSameCategories())
            {
                if (toggles.Count == 0)
                {
                    UpdateList();
                    rebuildLayout = true;
                }
                else
                {
                    GridedPatch gridedPatch = patch as GridedPatch;
                    gridedPatch.SetCategoryMask(mask);
                }
            }
            else
            {
                ClearList();
                rebuildLayout = true;
            }

            (patch.Data as GridData).OnGridChange += OnGridChange;
        }
        else
        {
            if (dataLayer.HasLoadedPatchesInView() && AllSitesHaveSameCategories())
            {
                if (toggles.Count == 0)
                {
                    UpdateList();
                    rebuildLayout = true;
                }
            }
            else
            {
                ClearList();
                rebuildLayout = true;
            }

            (patch.Data as GridData).OnGridChange -= OnGridChange;
        }

        if (rebuildLayout)
        {
            GuiUtils.RebuildLayout(transform);
        }
    }

    //
    // Event Methods
    //

    private void OnGridChange(GridData grid)
    {
        if (grid.categories.Length != toggles.Count)
        {
            ClearList();
            UpdateList();
            GuiUtils.RebuildLayout(transform);
        }
    }

    private bool avoidToggleChange = false;
    private void OnToggleChange(Toggle toggle, uint bitIndex)
    {
        if (avoidToggleChange)
            return;

        uint newMask = toggle.isOn? mask | bitIndex : mask & ~bitIndex;

        if (lastToggle == toggle && doubleClickTime >= Time.time)
        {
            avoidToggleChange = true;
            if (newMask == bitIndex)
            {
                foreach (var t in toggles)
                {
                    t.isOn = true;
                }
                mask = 0xFFFFFFFF;
            }
            else
            {
                foreach (var t in toggles)
                {
                    t.isOn = false;
                }
                toggle.isOn = true;
                mask = bitIndex;
            }
            avoidToggleChange = false;
        }
        else
        {
            mask = newMask;
        }

        lastToggle = toggle;
        doubleClickTime = Time.time + 0.25f;

        UpdateSitesMask();
    }

    //
    // Private Methods
    //

    private void ClearList()
    {
        for (int i = toggles.Count - 1; i >= 0; i--)
        {
            DestroyImmediate(toggles[i].gameObject);
        }
        toggles.Clear();
    }

    private void UpdateList()
    {
        // List categories
        Patch patch = dataLayer.loadedPatchesInView[0];
        var categories = patch.Data.categories;
        int count = (categories == null) ? 0 : categories.Length;

        if (count > 0)
        {
            mask = 0xFFFFFFFF;
            uint bitIndex = 1;
            for (int i = 0; i < count; i++, bitIndex <<= 1)
            {
                var cat = categories[i];

                bool isOn = (patch.Data.categoryMask & bitIndex) != 0;

                uint bIndex = bitIndex;

                var newToggle = Instantiate(classTogglePrefab, transform, false);
                newToggle.isOn = isOn;
                newToggle.onValueChanged.AddListener(delegate { OnToggleChange(newToggle, bIndex); });
				newToggle.graphic.color = cat.color;
				var label = newToggle.GetComponentInChildren<Text>();
				if (string.IsNullOrEmpty(cat.name))
					label.text = "N/A";
                else
					label.text = cat.name;

				AddHoverEvent(newToggle.transform.GetChild(0).gameObject, i);

				toggles.Add(newToggle);

                if (!isOn)
                {
                    mask &= ~(1U << i);
                }
            }

            UpdateSitesMask();
        }
    }

	private void AddHoverEvent(GameObject go, int index)
	{
		go.AddComponent<HoverHandler>().OnHover += delegate (bool hover) { OnCategoryHover(index, hover); };
	}

	private void OnCategoryHover(int index, bool hover)
	{
		index = hover ? index : -1;

		for (int i = 0; i < dataLayer.loadedPatchesInView.Count; i++)
		{
			GridedPatch patch = dataLayer.loadedPatchesInView[i] as GridedPatch;
			if (patch != null)
			{
				var mapLayer = patch.GetMapLayer() as GridMapLayer;
				mapLayer.HighlightCategory(index);
			}
		}
	}
	
	private void UpdateSitesMask()
    {
        for (int i = 0; i < dataLayer.loadedPatchesInView.Count; i++)
        {
            GridedPatch patch = dataLayer.loadedPatchesInView[i] as GridedPatch;
            if (patch != null)
            {
                patch.SetCategoryMask(mask);
            }
        }

		// Also update other patches from the same site but different record
		if (dataLayer.loadedPatchesInView.Count > 0)
		{
			var siteRecord = dataLayer.loadedPatchesInView[0].siteRecord;
			foreach (var record in siteRecord.layerSite.records)
			{
				if (record.Value == siteRecord)
					continue;

				foreach (var patch in record.Value.patches)
				{
					(patch as GridedPatch).SetCategoryMask(mask);
				}
			}
		}
	}

    private bool AllSitesHaveSameCategories()
    {
        int count = dataLayer.loadedPatchesInView.Count;
        if (count == 0)
            return false;

        Category[] categories;
        categories = dataLayer.loadedPatchesInView[0].Data.categories;
        if (categories == null)
            return false;

        for (int i = 1; i < count; i++)
        {
            var otherCategories = dataLayer.loadedPatchesInView[i].Data.categories;
            if (otherCategories == null || categories.Length != otherCategories.Length || !categories[0].name.Equals(otherCategories[0].name))
            {
                return false;
            }
        }

        return true;
    }
}
