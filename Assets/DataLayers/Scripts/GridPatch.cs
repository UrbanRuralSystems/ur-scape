// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
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
    public GridPatch(DataLayer dataLayer, int level, int year, GridData grid, string filename)
        : base(dataLayer, level, year, grid, filename)
    {
	}


    //
    // Public Methods
    //

    public static GridPatch Create<D>(DataLayer dataLayer, int level, int year, D data, string filename) where D: GridData
    {
        return new GridPatch(dataLayer, level, year, data, filename);
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
				SetFilename(filename);
                break;
            default:
                Debug.LogError("Not implemented");
                break;
        }
    }

	public void ResetFilter()
	{
		if (grid.IsCategorized)
		{
			ResetCategoryFilter();
		}
		else
		{
			SetMinMaxFilter(grid.minValue, grid.maxValue);
		}
	}

	public void SetMinMaxFilter(float min, float max)
	{
		grid.minFilter = min;
		grid.maxFilter = max;
		grid.FilterChanged();
	}

	public void SetCategoryFilter(CategoryFilter filter)
	{
		grid.categoryFilter.CopyFrom(filter);
		grid.FilterChanged();
	}

	public void ResetCategoryFilter()
	{
		grid.categoryFilter.ResetToDefault();
		grid.FilterChanged();
	}


	//
	// Private Methods
	//



	private void AssignCategoryColors()
	{
		AssignCategoryColors(grid.categories, DataLayer.Color, grid.coloring);
	}

	public static void AssignCategoryColors<T>(T[] categories, Color color, GridData.Coloring coloring) where T : Category
	{
		if (coloring == GridData.Coloring.Custom)
			return;

		int categoriesCount = categories.Length;
		if (categoriesCount <= 1)
		{
			if (categoriesCount == 1)
			{
				categories[0].color = color;
				categories[0].color.a = 1f;
			}
			return;
		}

		if (coloring == GridData.Coloring.Single ||
			coloring == GridData.Coloring.ReverseSingle)
		{
			// Single hue progression

			Color.RGBToHSV(color, out float h, out float s, out float v);
			float start = 0.25f; // Start at 1/4
			float k = (1f - start) / (categoriesCount - 1);
			if (coloring == GridData.Coloring.ReverseSingle)
			{
				start = 1;
				k = -k;
			}
			for (int i = 0; i < categoriesCount; ++i)
			{
				categories[i].color = Color.HSVToRGB(h, s, Mathf.Clamp01(start + i * k));
				categories[i].color.a = 1f;
			}
		}
		else if (coloring == GridData.Coloring.Multi ||
				 coloring == GridData.Coloring.ReverseMulti)
		{
			// Spectral color progression

			Color.RGBToHSV(color, out float h, out float s, out float v);

			float hueRange = Mathf.Min(categoriesCount * 0.005f + 0.01f, 0.06f);
			float invCount = categoriesCount > 1 ? 1f / (categoriesCount - 1) : 0;
			for (int i = 0; i < categoriesCount; ++i)
			{
				var cat = categories[i];
				float k = i * invCount;
				float h2 = h + hueRange * (2f * k - 1f);
				if (h2 < 0)
					h2 += 1f;
				else if (h2 > 1f)
					h2 -= 1f;

				float v2 = Math.Abs(categoriesCount < 3 || i % 2 == 0 ? v : v - 0.15f);

				cat.color = Color.HSVToRGB(h2, s, v2);
				cat.color.a = 1f;
			}
		}
	}
}
