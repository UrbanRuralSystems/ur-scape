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
    private float[] divisors;
    public bool useFilters = false;
    private readonly Dictionary<DataLayer, float> weights = new Dictionary<DataLayer, float>();

    public float[] Divisors => divisors;


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

    public void ResetGrid()
    {
        _UpdateGrid(0, 0, 0, 0, 0, 0);
    }

    public void UpdateGrid(double north, double east, double south, double west, int countX, int countY)
    {
        if (east <= west || north <= south)
        {
            Debug.LogError("Invalid area!");
            return;
        }

        _UpdateGrid(north, east, south, west, countX, countY);
    }

    public void UpdateData()
    {
        bool hasGrids = grids.Count > 0;

        if (hasGrids && grid.values != null)
        {
            CalculateValues();
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

    private void InitArrays()
    {
        int length = grid.countX * grid.countY;

        if (length > 0)
        {
            if (grid.values == null || grid.values.Length != length)
            {
                grid.values = new float[length];
                grid.valuesMask = GridData.CreateMaskBuffer(length);
                divisors = new float[length];
            }
        }
        else
        {
            grid.values = null;
            grid.valuesMask = null;
            divisors = null;
        }
    }

    private void ResetArrays()
    {
        var length = grid.values.Length;

        // Set all values to 0
        Array.Clear(grid.values, 0, length);
        Array.Clear(divisors, 0, length);

        // Set all values to 1 (True)
        for (int i = 0; i < length; ++i)
            grid.valuesMask[i] = 1;
    }
    
    private void _UpdateGrid(double north, double east, double south, double west, int countX, int countY)
    {
        grid.countX = countX;
        grid.countY = countY;
        grid.ChangeBounds(west, east, north, south);

        UpdateResolution();

        InitArrays();
    }

    private void CalculateValues()
    {
        ResetArrays();

        double thisDegreesPerCellX = (grid.east - grid.west) / grid.countX;
		double thisDegreesPerCellY = (grid.south - grid.north) / grid.countY;

        for (int index = 0; index < grids.Count; index++)
        {
            var otherGrid = grids[index];
            
			var patchCellsPerDegreeX = otherGrid.countX / (otherGrid.east - otherGrid.west);
			var patchCellsPerDegreeY = otherGrid.countY / (otherGrid.south - otherGrid.north);
            var cellSizeInDegressX = 1.0 / patchCellsPerDegreeX;
            var cellSizeInDegressY = 1.0 / patchCellsPerDegreeY;
            var absPatchCellsPerDegreeX = Math.Abs(patchCellsPerDegreeX);
            var absPatchCellsPerDegreeY = Math.Abs(patchCellsPerDegreeY);

            double scaleX = patchCellsPerDegreeX * thisDegreesPerCellX;
			double scaleY = patchCellsPerDegreeY * thisDegreesPerCellY;
            double invScaleX = 1.0 / scaleX;
            double invScaleY = 1.0 / scaleY;

            double offsetX = (grid.west - otherGrid.west) * patchCellsPerDegreeX ;
			double offsetY = (grid.north - otherGrid.north) * patchCellsPerDegreeY ;

            double startX = (grid.west - otherGrid.west) * patchCellsPerDegreeX;
            double startY = (grid.north - otherGrid.north) * patchCellsPerDegreeY;
            double endX = (grid.east - otherGrid.west) * patchCellsPerDegreeX;
            double endY = (grid.south - otherGrid.north) * patchCellsPerDegreeY;

            // Get clear start and end
            startX = (int)Math.Max(Math.Floor(startX), 0);
            startY = (int)Math.Max(Math.Floor(startY), 0);
            endX = (int)Math.Min(Math.Ceiling(endX), otherGrid.countX);
            endY = (int)Math.Min(Math.Ceiling(endY), otherGrid.countY);

            float weight = 1;
			if (otherGrid.patch != null && weights.ContainsKey(otherGrid.patch.DataLayer))
			{
				weight = weights[otherGrid.patch.DataLayer];
			}

			SetValueDelegate SetValue;

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

            for (int y = (int)startY; y < (int)endY; y++)
            {
                // Get Data grid index
                var otherGridIndex = y * otherGrid.countX + (int)startX;
                for (int x = (int)startX; x < (int)endX; x++, otherGridIndex++)
                {
                    // Get Extent of the Data Grid cell
                    var otherCellxMin = otherGrid.west + x * cellSizeInDegressX;
                    var otherCellxMax = otherGrid.west + (x + 1) * cellSizeInDegressX;
                    var otherCellyMax = otherGrid.north + y * cellSizeInDegressY;
                    var otherCellyMin = otherGrid.north + (y + 1) * cellSizeInDegressY;

                    // IMPORTANT: The following code block has been optimized. Original code is commented out for reference
                    // Floor equivalent:   var f = (int)(value + 32768.0) - 32768;
                    // Ceiling equivalent: var c = 32768 - (int)(32768.0 - value);

                    // Get range of affected cells
                    int fromX = (int)((x - offsetX) * invScaleX + 50000.0) - 50000; // (int)Math.Floor((x - offsetX) * invScaleX);
                    int fromY = (int)((y - offsetY) * invScaleY + 50000.0) - 50000; // (int)Math.Floor((y - offsetY) * invScaleY);
                    int toX = 50000 - (int)(50000.0 - (x - offsetX + 1) * invScaleX); // (int)Math.Ceiling((x - offsetX + 1) * invScaleX);
                    int toY = 50000 - (int)(50000.0 - (y - offsetY + 1) * invScaleY); // (int)Math.Ceiling((y - offsetY + 1) * invScaleY);

                    // if data cell is bigger then affected cell and also the extent
                    // it might call negative index, therefore the range check
                    fromX = fromX > 0 ? fromX : 0; // Math.Max(0, fromX);
                    fromY = fromY > 0 ? fromY : 0; // Math.Max(0, fromY);
                    toX = toX < grid.countX ? toX : grid.countX; // Math.Min(grid.countX, toX);
                    toY = toY < grid.countY ? toY : grid.countY; // Math.Min(grid.countY, toY);

                    // Go thru all grid cells inside of other grid cell (other grid cell can be bigger)
                    for (int yI = fromY; yI < toY; yI++)
                    {
                        var thisGridIndex = yI * grid.countX + fromX;
                        for (int xI = fromX; xI < toX; xI++, thisGridIndex++)
                        {
                            //Get municipal grid extent
                            var thisCellxMin = grid.west + xI * thisDegreesPerCellX;
                            var thisCellxMax = grid.west + (xI + 1) * thisDegreesPerCellX;
                            var thisCellyMax = grid.north + yI * thisDegreesPerCellY;
                            var thisCellyMin = grid.north + (yI + 1) * thisDegreesPerCellY;

                            // IMPORTANT: The following code block has been optimized. Original code with Min/Max is commented out for reference

                            // Get area from XY range overlap 
                            var right = otherCellxMax < thisCellxMax ? otherCellxMax : thisCellxMax; // Math.Min(otherCellxMax, thisCellxMax);
                            var left = otherCellxMin > thisCellxMin ? otherCellxMin : thisCellxMin; // Math.Max(otherCellxMin, thisCellxMin);
                            var top = otherCellyMax < thisCellyMax ? otherCellyMax : thisCellyMax; // Math.Min(otherCellyMax, thisCellyMax);
                            var bottom = otherCellyMin > thisCellyMin ? otherCellyMin : thisCellyMin; // Math.Max(otherCellyMin, thisCellyMin);
                            var overlapX = right > left ? right - left : 0; // Math.Max(right - left, 0);
                            var overlapY = top > bottom ? top - bottom : 0; // Math.Max(top - bottom, 0);
                            var areaRatio = (float)(overlapX * absPatchCellsPerDegreeX * overlapY * absPatchCellsPerDegreeY);

                            // Assign values to grid if values available
                            SetValue(grid, thisGridIndex, otherGrid, otherGridIndex, weight, areaRatio);
                        }
                    }
                }
            }
        }

        int gridCount = grid.countX * grid.countY;
        float min =  gridCount > 0? grid.values[0] : 0;
        float max = min;
		for (int i = 1; i < gridCount; ++i)
		{
            float value = grid.values[i];

            // IMPORTANT: The following code block has been optimized.

            if (value > max) max = value;
            else if (value < min) min = value;
        }

		grid.maxValue = max;
        grid.minValue = min < max ? min : 0; // case for categorized, when all categories enabled
        grid.ValuesChanged();
    }

    private delegate void SetValueDelegate(GridData grid, int thisIndex, GridData otherGrid, int otherIndex, float weight, float areaRatio);

	private void CategorizedMasked(GridData grid, int gridIndex, GridData otherGrid, int otherIndex, float weight, float areaRatio)
	{
        int value = (int)otherGrid.values[otherIndex];
        if (otherGrid.valuesMask[otherIndex] == 1) // 3th case of ignoring noData from TS #68
        {
            grid.values[gridIndex] += otherGrid.categoryFilter.IsSetAsInt(value) * weight * areaRatio;
            //Assign the divisor -  to be used for whole admin area
            divisors[gridIndex] += areaRatio;
        }
    }

	private void CategorizedUnmasked(GridData grid, int gridIndex, GridData otherGrid, int otherIndex, float weight, float areaRatio)
	{   
        int value = (int)otherGrid.values[otherIndex];
		grid.values[gridIndex] += otherGrid.categoryFilter.IsSetAsInt(value) * weight * areaRatio;

        //Assign the divisor -  to be used for whole admin area
        divisors[gridIndex] += areaRatio;
    }

	private void FilteredMasked(GridData grid, int gridIndex, GridData otherGrid, int otherIndex, float weight, float areaRatio )
	{
        float value = otherGrid.values[otherIndex];
        if (otherGrid.valuesMask[otherIndex] == 1 && value >= otherGrid.minFilter && value <= otherGrid.maxFilter)
        {
            grid.values[gridIndex] += (value - otherGrid.minValue) * weight * areaRatio;
            //Assign the divisor -  to be used for whole admin area
            divisors[gridIndex] += areaRatio;
        }
    }

	private void UnfilteredMasked(GridData grid, int gridIndex, GridData otherGrid, int otherIndex, float weight, float areaRatio)
	{
        if (otherGrid.valuesMask[otherIndex] == 1)  // 3th case of ignoring noData from TS #68
        {
            grid.values[gridIndex] += (otherGrid.values[otherIndex] - otherGrid.minValue) * weight * areaRatio;
            //Assign the divisor -  to be used for whole admin area
            divisors[gridIndex] += areaRatio;
        }
    }

    private void FilteredUnmasked(GridData grid, int gridIndex, GridData otherGrid, int otherIndex, float weight, float areaRatio)
    {
        float value = otherGrid.values[otherIndex];
        if (value >= otherGrid.minFilter && value <= otherGrid.maxFilter)
        {
            grid.values[gridIndex] += (value - otherGrid.minValue) * weight * areaRatio;
        }
        //Assign the divisor -  to be used for whole admin area
        divisors[gridIndex] += areaRatio;
    }

	private void UnfilteredUnmasked(GridData grid, int gridIndex, GridData otherGrid, int otherIndex, float weight, float areaRatio)
	{
		grid.values[gridIndex] += (otherGrid.values[otherIndex] - otherGrid.minValue) * weight* areaRatio;

        //Assign the divisor -  to be used for whole admin area
        divisors[gridIndex] += areaRatio;
    }

}
