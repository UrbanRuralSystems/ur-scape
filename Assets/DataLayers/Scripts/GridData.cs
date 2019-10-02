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

public class GridData : PatchData
{
	public const long MaxValuesCount = 500000000;	// 500 Mill floats	(2Gb)

    public int countX;
    public int countY;
    public float[] values;
    public byte[] valuesMask;
    public float minValue;
    public float maxValue;

	public string units = "";

	public readonly CategoryFilter categoryFilter = CategoryFilter.GetDefault();
	public IntCategory[] categories;
	public bool IsCategorized
	{
		get { return categories != null && categories.Length > 0; }
	}

	public float minFilter;
	public float maxFilter;

	private double? mean;

	public const uint DefaultCategoryFilter = 0xFFFFFFFF;
	private const int DefaultDistributionSize = 50;
	private static readonly float? NoValue = null;

	private int[] distributionValues;
    public int[] DistributionValues { get { return distributionValues; } }
    private int maxDistributionValue;
    public int MaxDistributionValue { get { return maxDistributionValue; } }

    public delegate void GridChangeDelegate(GridData grid);
    public event GridChangeDelegate OnGridChange;
	public event GridChangeDelegate OnValuesChange;
    public event GridChangeDelegate OnFilterChange;

	public enum Coloring
	{
		Single,
		ReverseSingle,
		Multi,
		ReverseMulti,
		Custom,
	}

	public Coloring coloring = Coloring.Single;

	public GridData() : base() { }
	public GridData(PatchData other) : base(other) { }
	public GridData(GridData other, bool withValues = true) : base(other)
    {
        countX = other.countX;
        countY = other.countY;

		if (withValues)
		{
			if (other.values != null)
			{
				values = (float[])other.values.Clone();
			}
			if (other.valuesMask != null)
			{
				valuesMask = (byte[])other.valuesMask.Clone();
			}
		}

		units = other.units;

		categoryFilter.CopyFrom(other.categoryFilter);
		if (other.categories != null)
		{
			categories = (IntCategory[])other.categories.Clone();
		}

		minValue = other.minValue;
        maxValue = other.maxValue;
        minFilter = other.minFilter;
        maxFilter = other.maxFilter;
		coloring = other.coloring;
	}

	public override bool IsLoaded()
	{
		return values != null;
	}

	public override void UnloadData()
    {
        values = null;
        valuesMask = null;
		mean = null;
	}

    public void InitGridValues(bool withMask = true)
    {
		int count = countX * countY;
		values = new float[count];
		if (withMask)
		{
			CreateMaskBuffer();
		}
	}

	public void CreateMaskBuffer()
	{
		valuesMask = CreateMaskBuffer(values.Length);
	}

	public static byte[] CreateMaskBuffer(int size)
	{
		// Mask buffer's size needs to be multiple of 4
		size = 4 * ((size + 3) / 4);
		return new byte[size];
	}

	public void GridChanged()
    {
        if (OnGridChange != null)
            OnGridChange(this);
    }

	public void ValuesChanged()
	{
		if (OnValuesChange != null)
			OnValuesChange(this);
	}

	public void FilterChanged()
    {
        if (OnFilterChange != null)
            OnFilterChange(this);
    }

	public int GetIndex(double longitude, double latitude)
	{
		int x = (int)(countX * (longitude - west) / (east - west));
		int y = (int)(countY * (latitude - north) / (south - north));
		int index = y * countX + x;

#if SAFETY_CHECK
		if (x < 0 || x >= countX || y < 0 || y >= countY)
		{
			Debug.LogError("Trying to get value outside the GridData boundaries");
			return 0;
		}
		else if (index < 0 || index >= values.Length)
		{
			Debug.LogError("Trying to get value with index outside the GridData values array");
			return 0;
		}
#endif
		return index;
	}

	public float GetValue(double longitude, double latitude)
    {
		return values[GetIndex(longitude, latitude)];
	}

	public float? GetCell(double longitude, double latitude)
	{
		int index = GetIndex(longitude, latitude);
		return valuesMask[index] == 1 ? values[index] : NoValue;
	}

	public void SetValue(double longitude, double latitude, float value)
    {
        int x = (int)Mathf.Floor((float)(countX * (longitude - west) / (east - west)));
        int y = (int)Mathf.Floor((float)(countY * (latitude - north) / (south - north)));
        values[y * countX + x] = value;
    }

    public void SnapToCenter(ref Coordinate coords)
    {
        int x = (int)(countX * (coords.Longitude - west) / (east - west));
        int y = (int)(countY * (coords.Latitude - north) / (south - north));

        coords.Longitude = (x + 0.5) * (east - west) / countX + west;
        coords.Latitude = (y + 0.5f) * (south - north) / countY + north;
    }

    public double GetLongitude(int index)
    {
        int x = index % countX;
        return (x + 0.5) * (east - west) / countX + west;
    }

    public double GetLatitude(int index)
    {
        int y = index / countX;
        return (y + 0.5) * (south - north) / countY + north;
    }

    public double GetCellWidth()
    {
		return (east - west) / countX;
	}

	public double GetCellSquareMeters(int row) // row 0 is first/top row
	{
		if (countX == 0 && values == null)
			return 0.0;

		double degreesX = (east - west) / countX;
		double degreesY = (south - north) / countY;

		double metersY1 = Math.Log(Math.Tan((90d + north + row * degreesY) * GeoCalculator.Deg2HalfRad)) * GeoCalculator.Rad2Meters;
		double metersX = degreesX * GeoCalculator.Deg2Meters;

		double metersY2 = Math.Log(Math.Tan((90d + (north + (row + 1) * degreesY)) * GeoCalculator.Deg2HalfRad)) * GeoCalculator.Rad2Meters;
		double metersY = metersY1 - metersY2;

		return metersX * metersY;
	}

	public double GetMean()
	{
		return GetMean(minValue, false);
	}

	public double GetMean(float noDataValue, bool recalculate)
	{
		if (!mean.HasValue || recalculate)
			CalculateMean(noDataValue);

		return mean.Value;
	}

	public void UpdateMinMaxValues()
	{
		minValue = float.MaxValue;
		maxValue = float.MinValue;
		if (valuesMask == null)
		{
			foreach (var value in values)
			{
				minValue = Mathf.Min(minValue, value);
				maxValue = Mathf.Max(maxValue, value);
			}
		}
		else
		{
			int count = values.Length;
			for (int i = 0; i < count; i++)
			{
				if (valuesMask[i] == 1)
				{
					minValue = Mathf.Min(minValue, values[i]);
					maxValue = Mathf.Max(maxValue, values[i]);
				}
			}
		}
	}

	public void SetDistribution(int[] distribution, int max)
    {
        if (distribution != null)
        {
            distributionValues = distribution;
            maxDistributionValue = max;
        }
    }

	public void UpdateDistribution(bool crop = false)
    {
		int distributionSize = DefaultDistributionSize;

		// Special case: create a smaller distribution for integer-based grids with a small range (3-20)
		int range = 1 + (int)(maxValue - minValue);
		if (!crop && range > 2 && range <= 20 && minValue == Mathf.Floor(minValue) && maxValue == Mathf.Floor(maxValue))
		{
			distributionSize = range;
		}

		if (distributionValues == null || distributionValues.Length != distributionSize)
		{
            distributionValues = new int[distributionSize];
        }
        else
        {
            Array.Clear(distributionValues, 0, distributionSize);
        }

        int lastChartIndex = distributionSize - 1;
		float invValueRange = 0;
		if (Math.Abs(maxValue - minValue) > 0.0001f)
			invValueRange = lastChartIndex / (maxValue - minValue);

		maxDistributionValue = 0;
        int count = values.Length;
		if (crop)
		{
			if (valuesMask == null)
			{
				for (int i = 0; i < count; i++)
				{
					int chartIndex = Mathf.RoundToInt((values[i] - minValue) * invValueRange);
					if (chartIndex >= 0 && chartIndex <= lastChartIndex)
					{
						distributionValues[chartIndex]++;
						maxDistributionValue = Math.Max(maxDistributionValue, distributionValues[chartIndex]);
					}
				}
			}
			else
			{
				for (int i = 0; i < count; i++)
				{
					int chartIndex = Mathf.RoundToInt((values[i] - minValue) * invValueRange);
					if (valuesMask[i] == 1 && chartIndex >= 0 && chartIndex <= lastChartIndex)
					{
						distributionValues[chartIndex]++;
						maxDistributionValue = Math.Max(maxDistributionValue, distributionValues[chartIndex]);
					}
				}

			}
		}
		else
		{
			if (valuesMask == null)
			{
				for (int i = 0; i < count; i++)
				{
					int chartIndex = (int)((values[i] - minValue) * invValueRange + 0.4999f);
					distributionValues[chartIndex]++;
					maxDistributionValue = Math.Max(maxDistributionValue, distributionValues[chartIndex]);
				}
			}
			else
			{
				for (int i = 0; i < count; i++)
				{
					if (valuesMask[i] == 1)
					{
						int chartIndex = (int)((values[i] - minValue) * invValueRange + 0.4999f);
						distributionValues[chartIndex]++;
						maxDistributionValue = Math.Max(maxDistributionValue, distributionValues[chartIndex]);
					}
				}
			}
		}
    }

	public void ResetMask(bool allValid)
	{
		byte value = allValid ? (byte)1 : (byte)0;
		valuesMask.Fill(value);
	}

	public void AddMaskValue(float value)
	{
		int count = values.Length;
		int i = 0;
		for (; i < count; i++)
		{
			if (values[i] == value)
			{
				if (valuesMask == null)
				{
					CreateMaskBuffer();
					ResetMask(true);
				}

				valuesMask[i] = 0;
				for (; i < count; i++)
				{
					if (values[i] == value)
					{
						valuesMask[i] = 0;
					}
				}
				break;
			}
		}
	}

	public void CalculateResolution(double outResX, double outResY, double dataResX, double dataResY, int dataWidth, int dataHeight)
	{
		if (dataResX.Similar(outResX) && dataResY.Similar(outResY))
		{
			countX = dataWidth;
			countY = dataHeight;
		}
		else
		{
			countX = Mathf.RoundToInt((float)((east - west) / outResX));
			countY = Mathf.RoundToInt((float)((north - south) / outResY));

			/*
			double newEast = west + countX * outResX;
			double newSouth = north - countY * outResY;
			if (!newEast.Similar(east, 0.000001))
			{
				Debug.LogWarning("Data East (" + east + ") different from imported East (" + newEast + ")");
				east = newEast;
			}
			if (!newSouth.Similar(south, 0.000001))
			{
				Debug.LogWarning("Data South (" + south + ") different from imported South (" + newSouth + ")");
				south = newSouth;
			}
			*/
		}
	}

	public void CalculateResolution(double degPerCellX, double degPerCellY, out int x, out int y)
	{
		x = Mathf.RoundToInt((float)((east - west) / degPerCellX));
		y = Mathf.RoundToInt((float)((north - south) / degPerCellY));
	}

	public void RemapCategories(string filename, bool removeUnused)
	{
		if (removeUnused)
			RemapCategoriesAndRemoveUnused(filename);
		else
			RemapCategories(filename);
	}


	//
	// Private Methods
	//

	private void CalculateMean(float noDataValue)
	{
		double total = 0;
		int count = countX * countY;
		if (valuesMask == null)
		{
			for (int i = 0; i < count; i++)
			{
				total += values[i];
			}
		}
		else
		{
			for (int i = 0; i < count; i++)
			{
				total += valuesMask[i] == 1 ? values[i] : noDataValue;
			}
		}
		mean = total / count;
	}

	private void RemapCategories(string filename)
	{
		Dictionary<int, int> map = new Dictionary<int, int>();

		int categoriesCount = categories.Length;
		if (categoriesCount < 1)
			return;

		bool needsRemapping = false;
		for (int i = 0; i < categoriesCount; i++)
		{
			needsRemapping |= categories[i].value != i;
			map.Add(categories[i].value, i);
			categories[i].value = i;
		}

		minValue = 0;
		maxValue = categoriesCount - 1;

		if (!needsRemapping)
			return;

		int index = -1;
		int valuesCount = values.Length;
		if (valuesMask == null)
		{
			for (int i = 0; i < valuesCount; i++)
			{
				int value = (int)values[i];
				if (!map.ContainsKey(value))
				{
					Debug.LogWarning("Missing category for value " + value + ". Remapped to " + index + ". File: " + filename);
					map.Add(value, index--);
				}
				values[i] = map[value];
			}
		}
		else
		{
			for (int i = 0; i < valuesCount; i++)
			{
				if (valuesMask[i] == 0)
					continue;

				int value = (int)values[i];
				if (!map.ContainsKey(value))
				{
					Debug.LogWarning("Missing category for value " + value + ". Remapped to " + index + ". File: " + filename);
					map.Add(value, index--);
				}
				values[i] = map[value];
			}
		}
	}

	private void RemapCategoriesAndRemoveUnused(string filename)
	{
		int categoriesCount = categories.Length;
		if (categoriesCount < 1)
			return;

		int valuesCount = values.Length;

		// Find all the unique values
		HashSet<int> uniqueValues = new HashSet<int>();
		if (valuesMask == null)
		{
			for (int i = 0; i < valuesCount; i++)
				uniqueValues.AddOnce((int)values[i]);
		}
		else
		{
			for (int i = 0; i < valuesCount; i++)
				if (valuesMask[i] != 0)
					uniqueValues.AddOnce((int)values[i]);
		}

		// Create the map and list of used categories
		Dictionary<int, int> map = new Dictionary<int, int>();
		List<IntCategory> usedCategories = new List<IntCategory>();
		bool needsRemapping = false;
		int usedIndex = 0;
		for (int i = 0; i < categoriesCount; i++)
		{
			var cat = categories[i];
			needsRemapping |= cat.value != usedIndex;
			if (uniqueValues.Contains(cat.value))
			{
				uniqueValues.Remove(cat.value);
				map.Add(cat.value, usedIndex);
				cat.value = usedIndex;
				usedCategories.Add(cat);
				usedIndex++;
			}
			else
			{
				Debug.LogWarning("Removing unused category " + cat.name + " (" + cat.value + ") in " + filename);
			}
		}

		// Keep only the used categories and discard the rest
		if (categories.Length != usedCategories.Count)
		{
			categories = usedCategories.ToArray();
			categoriesCount = categories.Length;
			if (categoriesCount < 1)
				return;
		}

		// Remaining unique values don't have a category
		if (uniqueValues.Count > 0)
		{
			needsRemapping = true;
			int unusedIndex = -2;
			foreach (var value in uniqueValues)
			{
				if (!map.ContainsKey(value))
				{
					Debug.LogWarning("Missing category for value " + value + ". Remapped to " + unusedIndex + ". File: " + filename);
					map.Add(value, unusedIndex--);
				}
			}
		}

		minValue = 0;
		maxValue = categoriesCount - 1;

		if (!needsRemapping)
			return;

		if (valuesMask == null)
		{
			for (int i = 0; i < valuesCount; i++)
			{
				values[i] = map[(int)values[i]];
			}
		}
		else
		{
			for (int i = 0; i < valuesCount; i++)
			{
				if (valuesMask[i] != 0)
					values[i] = map[(int)values[i]];
			}
		}
	}
}
