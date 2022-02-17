// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections;
using UnityEngine;

public class PointPatch : Patch
{
	public readonly PointData pointData;
	public override PatchData Data { get { return pointData; } }

	public PointPatch(DataLayer dataLayer, int level, int year, PointData pointData, string filename)
		: base(dataLayer, level, year, filename)
	{
		this.pointData = pointData;
		pointData.patch = this;
	}

	public override void UnloadData()
	{
		pointData.UnloadData();
	}



    //
    // Public Methods
    //

    public static PointPatch Create<D>(DataLayer dataLayer, int level, int year, D data, string filename) where D: PointData
    {
        return new PointPatch(dataLayer, level, year, data, filename);
    }

    public override IEnumerator LoadData(PatchLoadedCallback callback)
    {
		bool loaded = false;
        yield return pointData.LoadBin(Filename, (g) => loaded = true);

        if (loaded)
        {
			if (pointData.minFilter == 0 && pointData.maxFilter == 0)
			{
				pointData.minFilter = pointData.minValue;
				pointData.maxFilter = pointData.maxValue;
			}

			if (pointData.IsCategorized)
			{
				AssignCategoryColors();
			}

			pointData.PointDataChanged();

            callback(this);
        }
    }

    public override void Save(string filename, PatchDataFormat format)
    {
        switch (format)
        {
            case PatchDataFormat.BIN:
				pointData.SaveBin(filename);
				SetFilename(filename);
                break;
            default:
                Debug.LogError("Not implemented");
                break;
        }
    }

	public void ResetFilter()
	{
		if (pointData.IsCategorized)
		{
			ResetCategoryFilter();
		}
		else
		{
			SetMinMaxFilter(pointData.minValue, pointData.maxValue);
		}
	}

	public void SetMinMaxFilter(float min, float max)
	{
		pointData.minFilter = min;
		pointData.maxFilter = max;
		pointData.FilterChanged();
	}

	public void SetCategoryFilter(CategoryFilter filter)
	{
		pointData.categoryFilter.CopyFrom(filter);
		pointData.FilterChanged();
	}

	public void ResetCategoryFilter()
	{
		pointData.categoryFilter.ResetToDefault();
		pointData.FilterChanged();
	}


	//
	// Private Methods
	//



	private void AssignCategoryColors()
	{
		AssignCategoryColors(pointData.categories, DataLayer.Color, pointData.coloring);
	}

	public static void AssignCategoryColors<T>(T[] categories, Color color, PointData.Coloring coloring) where T : Category
	{
		if (coloring == PointData.Coloring.Custom)
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

		if (coloring == PointData.Coloring.Single ||
			coloring == PointData.Coloring.ReverseSingle)
		{
			// Single hue progression

			Color.RGBToHSV(color, out float h, out float s, out float v);
			float start = 0.25f; // Start at 1/4
			float k = (1f - start) / (categoriesCount - 1);
			if (coloring == PointData.Coloring.ReverseSingle)
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
		else if (coloring == PointData.Coloring.Multi ||
				 coloring == PointData.Coloring.ReverseMulti)
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
