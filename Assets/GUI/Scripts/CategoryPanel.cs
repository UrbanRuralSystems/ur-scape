// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
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
	private readonly CategoryFilter newFilter = new CategoryFilter();


	//
	// Inheritance Methods
	//

	public override void Init(DataLayer dataLayer)
    {
        base.Init(dataLayer);

        if (dataLayer.HasLoadedPatchesInView() && AllPatchesHaveSameCategories())
        {
			UpdateList();
        }

		LayoutRebuilder.ForceRebuildLayoutImmediate(transform.GetComponent<RectTransform>());
	}

    public override void Show(bool show)
    {
        base.Show(show);

        ClearList();
        if (show && dataLayer.HasLoadedPatchesInView() && AllPatchesHaveSameCategories())
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
            if (AllPatchesHaveSameCategories())
            {
                if (toggles.Count == 0)
                {
                    UpdateList();
                    rebuildLayout = true;
                }
                else
                {
					if (patch is GridPatch)
					{
						(patch as GridPatch).SetCategoryFilter(newFilter);
					}
					else if (patch is MultiGridPatch)
					{
						(patch as MultiGridPatch).SetCategoryFilter(newFilter);
					}
                }
            }
            else
            {
                ClearList();
                rebuildLayout = true;
            }

			if (patch.Data is GridData)
			{
				(patch.Data as GridData).OnGridChange += OnGridChange;
			}
			else if (patch.Data is MultiGridData)
			{
				(patch.Data as MultiGridData).OnGridChange += OnMultiGridChange;
			}
        }
        else
        {
            if (dataLayer.HasLoadedPatchesInView() && AllPatchesHaveSameCategories())
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

			if (patch.Data is GridData)
			{
				(patch.Data as GridData).OnGridChange -= OnGridChange;
			}
			else if (patch.Data is MultiGridData)
			{
				(patch.Data as MultiGridData).OnGridChange -= OnMultiGridChange;
			}
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
		OnCategoriesChange(grid.categories);
    }

	private void OnMultiGridChange(MultiGridData grid)
	{
		OnCategoriesChange(grid.categories);
	}

	private void OnCategoriesChange<T>(T[] categories) where T : Category
	{
		if (categories.Length != toggles.Count)
		{
			ClearList();
			UpdateList();
			GuiUtils.RebuildLayout(transform);
		}
	}


	private bool avoidToggleChange = false;
    private void OnToggleChange(Toggle toggle, int bitIndex)
    {
        if (avoidToggleChange)
            return;

		if (toggle.isOn)
			newFilter.Set(bitIndex);
		else
			newFilter.Remove(bitIndex);

		if (lastToggle == toggle && doubleClickTime >= Time.time)
        {
            avoidToggleChange = true;
            if (newFilter.IsOnlySet(bitIndex))
            {
				foreach (var t in toggles)
				{
					t.isOn = true;
				}
				newFilter.ResetToDefault();
			}
			else
            {
				foreach (var t in toggles)
				{
					t.isOn = false;
				}
				toggle.isOn = true;
				newFilter.RemoveAll();
				newFilter.Set(bitIndex);
			}
			avoidToggleChange = false;
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
		Patch patch = dataLayer.loadedPatchesInView[0];
		var patchData = patch.Data;
		if (patchData is GridData)
		{
			var gridData = patch.Data as GridData;
			UpdateCategories(gridData.categories, gridData.categoryFilter);
		}
		else if (patchData is MultiGridData)
		{
			var multigrid = patchData as MultiGridData;
			UpdateCategories(multigrid.categories, multigrid.gridFilter);
		}
	}

	private void UpdateCategories<T>(T[] categories, CategoryFilter filter) where T : Category
	{
		int count = (categories == null) ? 0 : categories.Length;
		newFilter.CopyFrom(filter);

		if (count > 0)
		{
			for (int i = 0; i < count; i++)
			{
				UpdateCategory(categories[i], newFilter, i);
			}

			UpdateSitesMask();
		}
	}

	private void UpdateCategory(Category cat, CategoryFilter filter, int index)
	{
		bool isOn = filter.IsSet(index);

		int bIndex = index;

		var newToggle = Instantiate(classTogglePrefab, transform, false);
		newToggle.isOn = isOn;
		newToggle.onValueChanged.AddListener(delegate { OnToggleChange(newToggle, bIndex); });
		newToggle.graphic.color = cat.color;
		var label = newToggle.GetComponentInChildren<Text>();
		if (string.IsNullOrEmpty(cat.name))
			label.text = "N/A";
		else
			label.text = cat.name;

		AddHoverEvent(newToggle.transform.GetChild(0).gameObject, index);

		toggles.Add(newToggle);
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
			var patch = dataLayer.loadedPatchesInView[i];
			if (patch is GridPatch)
			{
				var mapLayer = patch.GetMapLayer() as GridMapLayer;
				mapLayer.HighlightCategory(index);
			}
			else if (patch is MultiGridPatch)
			{
				(patch as MultiGridPatch).HighlightCategory(index);
			}
		}
	}
	
	private void UpdateSitesMask()
    {
        for (int i = 0; i < dataLayer.loadedPatchesInView.Count; i++)
        {
			var patch = dataLayer.loadedPatchesInView[i];
            if (patch is GridPatch)
            {
				(patch as GridPatch).SetCategoryFilter(newFilter);
            }
			else if (patch is MultiGridPatch)
			{
				(patch as MultiGridPatch).SetCategoryFilter(newFilter);
			}
		}

		// Also update other patches from the same site but different record
		if (dataLayer.loadedPatchesInView.Count > 0)
		{
			var siteRecord = dataLayer.loadedPatchesInView[0].SiteRecord;
			foreach (var record in siteRecord.layerSite.records)
			{
				if (record.Value == siteRecord)
					continue;

				foreach (var patch in record.Value.patches)
				{
					(patch as GridPatch).SetCategoryFilter(newFilter);
				}
			}
		}
	}

    private bool AllPatchesHaveSameCategories()
    {
        int count = dataLayer.loadedPatchesInView.Count;
        if (count == 0)
            return false;

		var patchData = dataLayer.loadedPatchesInView[0].Data;
		if (patchData == null)
			return false;

		if (patchData is GridData)
		{
			var gridData = patchData as GridData;

			var categories = gridData.categories;
			if (categories == null)
				return false;

			for (int i = 1; i < count; i++)
			{
				gridData = dataLayer.loadedPatchesInView[i].Data as GridData;
				if (gridData == null)
					return false;

				var otherCategories = gridData.categories;
				if (otherCategories == null || categories.Length != otherCategories.Length || !categories[0].name.Equals(otherCategories[0].name))
				{
					return false;
				}
			}
		}
		else if (patchData is MultiGridData)
		{
			var multigrid = patchData as MultiGridData;

			var categories = multigrid.categories;
			if (categories == null)
				return false;

			for (int i = 1; i < count; i++)
			{
				multigrid = dataLayer.loadedPatchesInView[i].Data as MultiGridData;
				if (multigrid == null)
					return false;

				var otherCategories = multigrid.categories;
				if (otherCategories == null || categories.Length != otherCategories.Length || !categories[0].name.Equals(otherCategories[0].name))
				{
					return false;
				}
			}
		}

		return true;
    }

}
