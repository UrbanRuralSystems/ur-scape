// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridPatch : GridedPatch
{
    public GridPatch(DataLayer dataLayer, string name, int level, int year, GridData grid, string filename)
        : base(dataLayer, name, level, year, grid, filename)
    {
		if (grid.IsCategorized && grid.IsLoaded())
		{
			RemapCategories();
		}
	}


    //
    // Public Methods
    //

    public static GridPatch Create<D>(DataLayer dataLayer, string site, int level, int year, D data, string filename) where D: GridData
    {
        return new GridPatch(dataLayer, site, level, year, data, filename);
    }

    public override IEnumerator LoadData(PatchLoadedCallback callback)
    {
		bool loaded = false;
        yield return grid.LoadBin(Filename, (g) => loaded = true);

        if (loaded)
        {
			if (grid.minFilter == 0 && grid.maxFilter == 0)
			{
				grid.minFilter = grid.minValue;
				grid.maxFilter = grid.maxValue;
			}

			if (grid.IsCategorized)
			{
				AssignCategoryColors();
			}

			grid.GridChanged();

            callback(this);
        }
    }

    public override void Save(string filename, PatchDataFormat format)
    {
        switch (format)
        {
            case PatchDataFormat.BIN:
                grid.SaveBin(filename);
				Filename = filename;
                break;
            default:
                Debug.LogError("Not implemented");
                break;
        }
    }


	//
	// Private Methods
	//

	private void RemapCategories()
	{
		Dictionary<int, int> map = new Dictionary<int, int>();

		int categoriesCount = grid.categories.Length;
		if (categoriesCount < 1)
			return;

		bool needsRemapping = false;
		for (int i = 0; i < categoriesCount; i++)
		{
			needsRemapping |= grid.categories[i].value != i;
			map.Add(grid.categories[i].value, i);
			grid.categories[i].value = i;
		}

		grid.minValue = 0;
		grid.maxValue = categoriesCount - 1;

		if (!needsRemapping)
			return;

		int index = -1;
		int valuesCount = grid.values.Length;
		for (int i = 0; i < valuesCount; i++)
		{
			if (!grid.valuesMask[i])
				continue;

			int value = (int)grid.values[i];
			if (!map.ContainsKey(value))
			{
				Debug.LogWarning("Missing " + dataLayer.name + " category " + value + ", mapped to " + index);
				map.Add(value, index--);
			}
			grid.values[i] = map[value];
		}
	}

	private void AssignCategoryColors()
	{
		int categoriesCount = grid.categories.Length;
		if (categoriesCount < 1)
			return;

		if (grid.coloring == GridData.Coloring.Single ||
			grid.coloring == GridData.Coloring.ReverseSingle)
		{
			// Single hue progression

			float h, s, v;
			Color.RGBToHSV(dataLayer.color, out h, out s, out v);
			float start = 0.25f; // Start at 1/4
			float k = (1f - start) / (categoriesCount - 1);
			if (grid.coloring == GridData.Coloring.ReverseSingle)
			{
				start = 1;
				k = -k;
			}
			for (int i = 0; i < categoriesCount; ++i)
			{
				grid.categories[i].color = Color.HSVToRGB(h, s, Mathf.Clamp01(start + i * k));
				grid.categories[i].color.a = 1f;
			}
		}
		else if (grid.coloring == GridData.Coloring.Multi ||
				 grid.coloring == GridData.Coloring.ReverseMulti)
		{
			// Spectral color progression

			float h, s, v;
			Color.RGBToHSV(dataLayer.color, out h, out s, out v);

			float hueRange = Mathf.Min(categoriesCount * 0.005f + 0.01f, 0.06f);
			float invCount = categoriesCount > 1 ? 1f / (categoriesCount - 1) : 0;
			foreach (var cat in grid.categories)
			{
				float k = cat.value * invCount;
				float h2 = h + hueRange * (2f * k - 1f);
				if (h2 < 0)
					h2 += 1f;
				else if (h2 > 1f)
					h2 -= 1f;

				float v2 = Math.Abs(categoriesCount < 3 || cat.value % 2 == 0 ? v : v - 0.15f);

				cat.color = Color.HSVToRGB(h2, s, v2);
				cat.color.a = 1f;
			}
		}
	}
}
