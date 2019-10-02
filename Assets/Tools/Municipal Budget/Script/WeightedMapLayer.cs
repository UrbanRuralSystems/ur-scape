// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

#if UNITY_EDITOR
#define SAFETY_CHECK
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class WeightedMapLayer : GridMapLayer
{
    public readonly List<GridData> grids = new List<GridData>();
    public bool useFilters = false;
    // weight from UI to data layer
    private readonly Dictionary<DataLayer, float> weights = new Dictionary<DataLayer, float>();

    //
    // Public Methods
    //

    // update weight value for layer
	public void SetWeight(DataLayer layer, float weight)
	{
		if (weights.ContainsKey(layer))
			weights[layer] = weight;
		else
			weights.Add(layer, weight);
	}

	public void RemoveWeight(DataLayer layer)
	{
		weights.Remove(layer);
	}

	public void Add(GridData otherGrid)
    {
        // Ignore Network patches
        if (otherGrid.patch != null && otherGrid.patch is GraphPatch)
            return;

        otherGrid.OnFilterChange += OnOtherGridFilterChange;

        grids.Add(otherGrid);
    }

    public void Remove(GridData otherGrid)
    {
        otherGrid.OnFilterChange -= OnOtherGridFilterChange;

        grids.Remove(otherGrid);
    }

    public void Clear()
    {
        foreach (var g in grids)
        {
            g.OnFilterChange -= OnOtherGridFilterChange;
        }
        grids.Clear();
    }

    public void Refresh()
    {
        if (grids.Count > 0 && !UpdateBounds())
        {
            Debug.LogError("Invalid area!");
            return;
        }

        UpdateData();
    }

    public void UpdateData()
    {
        bool hasGrids = grids.Count > 0;

        if (hasGrids)
        {
            InitializeValues();
            CalculateValues();
		}
        else
        {
            grid.values = null;
            grid.countX = 0;
            grid.countY = 0;
        }

        if (hasGrids ^ GetComponent<MeshRenderer>().enabled)
        {
            GetComponent<MeshRenderer>().enabled = hasGrids;
        }
    }

    //
    // Event Methods
    //

	private void OnOtherGridFilterChange(GridData grid)
	{
		UpdateData();
	}

	//
	// Private Methods
	//

	private void InitializeValues()
    {
        int length = grid.countX * grid.countY;

        if (grid.values == null || grid.values.Length != length)
        {
            grid.values = new float[length];
        }

        // Set all values to 0
        for (int i = 0; i < length; ++i)
        {
            grid.values[i] = 0;
        }
    }

	private bool UpdateBounds()
	{
		double dotsPerDegreeX = 0;
		double dotsPerDegreeY = 0;

		double west = double.MaxValue;
		double east = double.MinValue;
		double north = double.MinValue;
		double south = double.MaxValue;

		foreach (var g in grids)
		{
			dotsPerDegreeX = Math.Max(g.countX / (g.east - g.west), dotsPerDegreeX);
			dotsPerDegreeY = Math.Max(g.countY / (g.north - g.south), dotsPerDegreeY);
			west = Math.Min(west, g.west);
			east = Math.Max(east, g.east);
			north = Math.Max(north, g.north);
			south = Math.Min(south, g.south);
		}

		if (east <= west || north <= south)
			return false;

		// Calculate grid resolution
		grid.countX = (int)Math.Round((east - west) * dotsPerDegreeX);
		grid.countY = (int)Math.Round((north - south) * dotsPerDegreeY);

		// Update the material
		UpdateResolution();

		if (grid.west != west || grid.east != east || grid.north != north || grid.south != south)
		{
			grid.ChangeBounds(west, east, north, south);
		}

        return true;
    }

    private void CalculateValues()
    {
		double thisDegreesPerCellX = (grid.east - grid.west) / grid.countX;
		double thisDegreesPerCellY = (grid.south - grid.north) / grid.countY;
		double thisCellsPerDegreeX = 1.0 / thisDegreesPerCellX;
		double thisCellsPerDegreeY = 1.0 / thisDegreesPerCellY;

		for (int i = 0; i < grids.Count; i++)
        {
            var otherGrid = grids[i];

			var patchCellsPerDegreeX = otherGrid.countX / (otherGrid.east - otherGrid.west);
			var patchCellsPerDegreeY = otherGrid.countY / (otherGrid.south - otherGrid.north);

			double scaleX = patchCellsPerDegreeX * thisDegreesPerCellX;
			double scaleY = patchCellsPerDegreeY * thisDegreesPerCellY;

			double offsetX = (grid.west - otherGrid.west) * patchCellsPerDegreeX + 0.5 * scaleX;
			double offsetY = (grid.north - otherGrid.north) * patchCellsPerDegreeY + 0.5 * scaleY;

			int startX = (int)((otherGrid.west - grid.west) * thisCellsPerDegreeX + 0.5);
			int startY = (int)((otherGrid.north - grid.north) * thisCellsPerDegreeY + 0.5);
			int endX = (int)((otherGrid.east - grid.west) * thisCellsPerDegreeX + 0.5);
			int endY = (int)((otherGrid.south - grid.north) * thisCellsPerDegreeY + 0.5);

			float weight = 1;
			if (otherGrid.patch != null && weights.ContainsKey(otherGrid.patch.DataLayer))
			{
				weight = weights[otherGrid.patch.DataLayer];
			}

			SetValueDelegate SetValue;

			int count = otherGrid.values.Length;
            if (otherGrid.IsCategorized)
            {
				if (!useFilters)
					continue;

				// Include cells which has selected category
				if (otherGrid.valuesMask == null)
					SetValue = CategorizedUnmasked;
				else
					SetValue = CategorizedMasked;
			}
            else
            {
				// Weighted Average formula = w1x1 + w2x2 + ... + wnxn

				weight /= otherGrid.maxValue;

				if (useFilters)
				{
					// Exlude filtered cells
					if (otherGrid.valuesMask == null)
						SetValue = FilteredUnmasked;
					else
						SetValue = FilteredMasked;
				}
				else
				{
					// Include filtered areas
					if (otherGrid.valuesMask == null)
						SetValue = UnfilteredUnmasked;
					else
						SetValue = UnfilteredMasked;
				}
            }

			for (int y = startY; y < endY; y++)
			{
				int gridIndex = y * grid.countX + startX;
				for (int x = startX; x < endX; x++, gridIndex++)
				{
					int pX = (int)(offsetX + x * scaleX);
					int pY = (int)(offsetY + y * scaleY);
					int otherIndex = pY * otherGrid.countX + pX;

					SetValue(grid, gridIndex, otherGrid, otherIndex, weight);
				}
			}
		}

		float min = float.MaxValue;
		float max = float.MinValue;
		int gridCount = grid.countX * grid.countY;
		for (int i = 0; i < gridCount; ++i)
		{
			float value = grid.values[i];
			min = Mathf.Min(min, value);
			max = Mathf.Max(max, value);
		}

		grid.maxValue = max;
		grid.minValue = min;

        grid.ValuesChanged();
    }

	private delegate void SetValueDelegate(GridData grid, int thisIndex, GridData otherGrid, int otherIndex, float weight);

	private static void CategorizedMasked(GridData grid, int thisIndex, GridData otherGrid, int otherIndex, float weight)
	{
		int value = (int)otherGrid.values[otherIndex];
		if (otherGrid.valuesMask[otherIndex] == 1)
			grid.values[thisIndex] += otherGrid.categoryFilter.IsSetAsInt(value) * weight;
	}

	private static void CategorizedUnmasked(GridData grid, int thisIndex, GridData otherGrid, int otherIndex, float weight)
	{
		int value = (int)otherGrid.values[otherIndex];
		grid.values[thisIndex] += otherGrid.categoryFilter.IsSetAsInt(value) * weight;
	}

	private static void FilteredMasked(GridData grid, int gridIndex, GridData otherGrid, int otherIndex, float weight)
	{
		float value = otherGrid.values[otherIndex];
		if (otherGrid.valuesMask[otherIndex] == 1 && value >= otherGrid.minFilter && value <= otherGrid.maxFilter)
			grid.values[gridIndex] += (value - otherGrid.minValue) * weight;
	}

	private static void UnfilteredMasked(GridData grid, int gridIndex, GridData otherGrid, int otherIndex, float weight)
	{
		if (otherGrid.valuesMask[otherIndex] == 1)
			grid.values[gridIndex] += (otherGrid.values[otherIndex] - otherGrid.minValue) * weight;
	}

	private static void FilteredUnmasked(GridData grid, int gridIndex, GridData otherGrid, int otherIndex, float weight)
	{
		float value = otherGrid.values[otherIndex];
		if (value >= otherGrid.minFilter && value <= otherGrid.maxFilter)
			grid.values[gridIndex] += (value - otherGrid.minValue) * weight;
	}

	private static void UnfilteredUnmasked(GridData grid, int gridIndex, GridData otherGrid, int otherIndex, float weight)
	{
		grid.values[gridIndex] += (otherGrid.values[otherIndex] - otherGrid.minValue) * weight;
	}
}
