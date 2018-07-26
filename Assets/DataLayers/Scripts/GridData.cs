// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
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
using UnityEngine;

public class Category
{
    public string name;
    public int value;
    public Color color;
}

public class GridData : PatchData
{
    public int countX;
    public int countY;
    public float[] values;
    public bool[] valuesMask;
    public float minValue;
    public float maxValue;

	public float minFilter;
	public float maxFilter;

    private static readonly int DefaultDistributionSize = 50;
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
	}

	public Coloring coloring = Coloring.Single;

	public GridData() : base() { }
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
				valuesMask = (bool[])other.valuesMask.Clone();
			}
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
    }

    public void InitGridValues(bool withMask = true)
    {
		int count = countX * countY;
		values = new float[count];
		if (withMask)
		{
			valuesMask = new bool[count];
		}
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
		return valuesMask[index] ? values[index] : NoValue;
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

    public double GetCellSize()
    {
		return (east - west) / countX;
	}

    public void SetDistribution(int[] distribution, int max)
    {
        if (distribution != null)
        {
            distributionValues = distribution;
            maxDistributionValue = max;
        }
    }

	public void UpdateDistribution(bool crop)
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
					if (valuesMask[i] && chartIndex >= 0 && chartIndex <= lastChartIndex)
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
					if (valuesMask[i])
					{
						int chartIndex = (int)((values[i] - minValue) * invValueRange + 0.4999f);
						distributionValues[chartIndex]++;
						maxDistributionValue = Math.Max(maxDistributionValue, distributionValues[chartIndex]);
					}
				}
			}
		}
    }
}
