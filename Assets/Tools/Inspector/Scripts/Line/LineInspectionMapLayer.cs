// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli

using System;
using System.Collections.Generic;
using UnityEngine;

public class LineInspectionMapLayer : GridMapLayer
{
    public readonly List<GridData> grids = new List<GridData>();
	public readonly Dictionary<GridData, List<float>> inspectedGridsData = new Dictionary<GridData, List<float>>();
    public bool useFilters = false;
	public int numOfSamples = 100;

	private const int StartPtIndex = 0;
	private const int EndPtIndex = 1;

    private List<Coordinate> points;

	//
	// Public Methods
	//

	public void Init(List<Coordinate> points)
    {
		Refresh(this.points);
    }

    public void Add(GridData otherGrid)
    {
        // Ignore Network patches
        if (otherGrid.patch != null && otherGrid.patch is GraphPatch)
            return;

        otherGrid.OnFilterChange += OnOtherGridFilterChange;

		if (!grids.Contains(otherGrid))
		{
			grids.Add(otherGrid);
		}
		else
		{
			int index = grids.FindIndex(a => a == otherGrid);
			grids[index] = otherGrid;
		}
		AddInspectedGridData(otherGrid);
	}

	public void Remove(GridData otherGrid)
    {
        otherGrid.OnFilterChange -= OnOtherGridFilterChange;

        grids.Remove(otherGrid);
		inspectedGridsData.Remove(otherGrid);
	}

	public void Clear()
    {
        foreach (var g in grids)
        {
            g.OnFilterChange -= OnOtherGridFilterChange;
        }
        grids.Clear();
		inspectedGridsData.Clear();
	}

    public void Refresh(List<Coordinate> points)
    {
        if (grids.Count > 0 && !UpdateBounds())
        {
            Debug.LogError("Invalid area!");
            return;
        }

        this.points = points;
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

	private void AddInspectedGridData(GridData otherGrid)
	{
		if(!inspectedGridsData.ContainsKey(otherGrid))
			inspectedGridsData.Add(otherGrid, new List<float>());
	}

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
			var g = grids[i];
			inspectedGridsData[g].Clear();

			var patchCellsPerDegreeX = g.countX / (g.east - g.west);
			var patchCellsPerDegreeY = g.countY / (g.south - g.north);

			double scaleX = patchCellsPerDegreeX * thisDegreesPerCellX;
			double scaleY = patchCellsPerDegreeY * thisDegreesPerCellY;

			double offsetX = (grid.west - g.west) * patchCellsPerDegreeX + 0.5 * scaleX;
			double offsetY = (grid.north - g.north) * patchCellsPerDegreeY + 0.5 * scaleY;

			Coordinate coorStart = points[StartPtIndex];
			Coordinate coorEnd = points[EndPtIndex];

			// The smaller range of longitude and latitude
			double startLon = (coorStart.Longitude > g.west) ? coorStart.Longitude : g.west;
			double startLat = (coorStart.Latitude < g.north) ? coorStart.Latitude : g.north;
			double endLon = (coorEnd.Longitude < g.east) ? coorEnd.Longitude : g.east;
			double endLat = (coorEnd.Latitude > g.south) ? coorEnd.Latitude : g.south;

			// Precompute values to be used in loop
			double diffX = endLon - startLon;
			double diffY = endLat - startLat;
			double diffXperSample = diffX / numOfSamples;
			double diffYperSample = diffY / numOfSamples;

			double srcX = (startLon - grid.west) * thisCellsPerDegreeX + 0.5;
			double srcY = (startLat - grid.north) * thisCellsPerDegreeY + 0.5;

			double pX = offsetX + srcX * scaleX;
			double pY = offsetY + srcY * scaleY;

			double srcXStep = diffXperSample * thisCellsPerDegreeX;
			double srcYStep = diffYperSample * thisCellsPerDegreeY;

			double srcXStepScaleX = srcXStep * scaleX;
			double srcYStepScaleY = srcYStep * scaleY;

			if(g.IsCategorized)
			{
				if (!useFilters)
					continue;

				if (g.values != null)
				{
					for (int j = 0; j < numOfSamples; ++j)
					{
						if (g.IsInside(startLon, startLat))
						{
							int thisIndex = (int)srcY * grid.countX + (int)srcX;
							int patchIndex = (int)pY * g.countX + (int)pX;
							if (patchIndex >= 0 && patchIndex < grid.values.Length)
							{
								int value = (int)g.values[patchIndex];
								byte mask = grid.valuesMask == null ? (byte)1 : grid.valuesMask[patchIndex];
								float valToAdd = mask == 1 ? g.categoryFilter.IsSetAsInt(value) : 0;
								grid.values[thisIndex] = valToAdd;
								inspectedGridsData[g].Add(valToAdd);
							}
						}

						startLon += diffXperSample;
						startLat += diffYperSample;

						srcX += srcXStep;
						srcY += srcYStep;

						pX += srcXStepScaleX;
						pY += srcYStepScaleY;
					}
				}
			}
			else
			{
				float gridMin = g.minValue;
				if (useFilters)
				{
					if (g.values != null)
					{
						for (int j = 0; j < numOfSamples; ++j)
						{
							if (g.IsInside(startLon, startLat))
							{
								int thisIndex = (int)srcY * grid.countX + (int)srcX;
								int patchIndex = (int)pY * g.countX + (int)pX;
								if (patchIndex >= 0 && patchIndex < grid.values.Length)
								{
									int value = (int)g.values[patchIndex];
									byte mask = grid.valuesMask == null ? (byte)1 : grid.valuesMask[patchIndex];
									if (mask == 1 && value >= g.minFilter && value <= g.maxFilter)
									{
										float valToAdd = (value - gridMin);
										grid.values[thisIndex] = valToAdd;
										inspectedGridsData[g].Add(valToAdd);
									}
								}
							}

							startLon += diffXperSample;
							startLat += diffYperSample;

							srcX += srcXStep;
							srcY += srcYStep;

							pX += srcXStepScaleX;
							pY += srcYStepScaleY;
						}
					}
				}
				else
				{
					if (g.values != null)
					{
						for (int j = 0; j < numOfSamples; ++j)
						{
							if (g.IsInside(startLon, startLat))
							{
								int thisIndex = (int)srcY * grid.countX + (int)srcX;
								int patchIndex = (int)pY * g.countX + (int)pX;
								if (patchIndex >= 0 && patchIndex < grid.values.Length)
								{
									byte mask = grid.valuesMask == null ? (byte)1 : grid.valuesMask[patchIndex];
									if (mask == 1)
									{
										float valToAdd = (g.values[patchIndex] - gridMin);
										grid.values[thisIndex] = valToAdd;
										inspectedGridsData[g].Add(valToAdd);
									}
								}
							}

							startLon += diffXperSample;
							startLat += diffYperSample;

							srcX += srcXStep;
							srcY += srcYStep;

							pX += srcXStepScaleX;
							pY += srcYStepScaleY;
						}
					}
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
}
