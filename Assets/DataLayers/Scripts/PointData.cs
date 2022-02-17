// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
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

public class PointData : PatchData
{
	public const long MaxValuesCount = 1000;

    public int count;
    public double[] lons;
    public double[] lats;
	public float[] values;      //+ What about string values?
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

	private int[] distributionValues;
    public int[] DistributionValues { get { return distributionValues; } }
    private int maxDistributionValue;
    public int MaxDistributionValue { get { return maxDistributionValue; } }

    public event Action<PointData> OnPointDataChange;
	public event Action<PointData> OnValuesChange;
    public event Action<PointData> OnFilterChange;

	//+
	public enum Coloring
	{
		Single,             // For categorised layer
		ReverseSingle,      // For categorised layer
		Multi,              // For categorised layer
		ReverseMulti,       // For categorised layer
		Custom,
		Reverse,            // Only applies to gradients
		Forward = Single,   // Default value for both categorised and gradients
	}

	public Coloring coloring = Coloring.Single;

	public PointData() : base() { }
	public PointData(PatchData other) : base(other) { }
	public PointData(PointData other, bool withValues = true) : base(other)
    {
        count = other.count;

		if (withValues)
		{
			if (other.values != null)
			{
				values = (float[])other.values.Clone();
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
		mean = null;
	}

    public void InitPointData()
    {
		lons = new double[count];
		lats = new double[count];
		values = new float[count];
	}

	public void PointDataChanged()
    {
		OnPointDataChange?.Invoke(this);
	}

	public void ValuesChanged()
	{
		OnValuesChange?.Invoke(this);
	}

	public void FilterChanged()
    {
		OnFilterChange?.Invoke(this);
	}

    public double GetLongitude(int index) => lons[index];
	public double GetLatitude(int index) => lats[index];
	public float GetValue(int index) => values[index];

	public double GetMean()
	{
		return GetMean(false);
	}

	public double GetMean(bool recalculate)
	{
		if (!mean.HasValue || recalculate)
			CalculateMean();

		return mean.Value;
	}

	public void UpdateMinMaxValues()
	{
		minValue = float.MaxValue;
		maxValue = float.MinValue;
		foreach (var value in values)
		{
			minValue = Mathf.Min(minValue, value);
			maxValue = Mathf.Max(maxValue, value);
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
		int chartIndex, value;
		if (crop)
		{
			for (int i = 0; i < count; i++)
			{
				chartIndex = (int)((values[i] - minValue) * invValueRange + 0.5f);
				if (chartIndex >= 0 && chartIndex <= lastChartIndex)
				{
					value = ++distributionValues[chartIndex];
					maxDistributionValue = value > maxDistributionValue? value : maxDistributionValue;
				}
			}
		}
		else
		{
			for (int i = 0; i < count; i++)
			{
				chartIndex = (int)((values[i] - minValue) * invValueRange + 0.5f);
				value = ++distributionValues[chartIndex];
				maxDistributionValue = value > maxDistributionValue ? value : maxDistributionValue;
			}
		}
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

	private void CalculateMean()
	{
		double total = 0;
		for (int i = 0; i < count; i++)
			total += values[i];
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

	private void RemapCategoriesAndRemoveUnused(string filename)
	{
		int categoriesCount = categories.Length;
		if (categoriesCount < 1)
			return;

		int valuesCount = values.Length;

		// Find all the unique values
		HashSet<int> uniqueValues = new HashSet<int>();
		for (int i = 0; i < valuesCount; i++)
			uniqueValues.AddOnce((int)values[i]);

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

		for (int i = 0; i < valuesCount; i++)
		{
			values[i] = map[(int)values[i]];
		}
	}
}
