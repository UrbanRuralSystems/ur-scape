// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)


public class CategoryFilter
{
	public const int MaxCategories = 256;
	public const int MaxCategoryIndex = MaxCategories - 1;
	public const int ArraySize = (MaxCategories + 31) / 32;
	public const int DefaultValue = -1;

	public readonly int[] bits = new int[ArraySize];

	public void ResetToDefault()
	{
		for (int i = 0; i < ArraySize; ++i)
		{
			bits[i] = DefaultValue;
		}
	}

	public void CopyFrom(CategoryFilter other)
	{
		for (int i = 0; i < ArraySize; ++i)
		{
			bits[i] = other.bits[i];
		}
	}

	public int IsSetAsInt(int index)
	{
		if (index >= MaxCategories)
			return 0;

		int arrIndex = index >> 5;
		int shift = index - arrIndex * 32;
		return (bits[arrIndex] >> shift) & 1;
	}

	public bool IsSet(int index)
	{
		if (index >= MaxCategories)
			return false;

		int arrIndex = index >> 5;
		int shift = index - arrIndex * 32;
		return ((bits[arrIndex] >> shift) & 1) == 1;
	}

	public bool IsOnlySet(int index)
	{
		if (index >= MaxCategories)
			return false;

		int arrIndex = index >> 5;
		int shift = index - arrIndex * 32;
		int value = 1 << shift;
		for (int i = 0; i < ArraySize; ++i)
		{
			if ((i != arrIndex && bits[i] != 0) ||
				(i == arrIndex && bits[i] != value))
				return false;
		}
		return true;
	}

	public void Set(int index)
	{
		if (index >= MaxCategories)
			return;

		int arrIndex = index >> 5;
		int shift = index - arrIndex * 32;
		bits[arrIndex] |= (1 << shift);
	}

	public void Remove(int index)
	{
		if (index >= MaxCategories)
			return;

		int arrIndex = index >> 5;
		int shift = index - arrIndex * 32;
		bits[arrIndex] &= ~(1 << shift);
	}

	public void RemoveAll()
	{
		for (int i = 0; i < ArraySize; ++i)
		{
			bits[i] = 0;
		}
	}

	public bool Equals(CategoryFilter other)
	{
		if (ReferenceEquals(null, other))
			return false;
		if (ReferenceEquals(this, other))
			return true;

		for (int i = 0; i < ArraySize; ++i)
		{
			if (bits[i] != other.bits[i])
				return false;
		}
		return true;
	}

	public static CategoryFilter GetDefault()
	{
		var filter = new CategoryFilter();
		filter.ResetToDefault();
		return filter;
	}
}
