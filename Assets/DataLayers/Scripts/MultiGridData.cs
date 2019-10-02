// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine.Events;

public class MultiGridData : PatchData
{
	public GridCategory[] categories;

	public readonly CategoryFilter gridFilter = CategoryFilter.GetDefault();

	public GridData.Coloring coloring = GridData.Coloring.Single;

	public event UnityAction<MultiGridData> OnGridChange;
	public event UnityAction<MultiGridData> OnFilterChange;


	public MultiGridData() : base() { }
	public MultiGridData(GridData other) : base(other)
	{
		coloring = other.coloring;

		if (other.categories != null && other.categories.Length > 0)
		{
			int count = other.categories.Length;
			categories = new GridCategory[count];

			for (int i = 0; i < count; i++)
			{
				var c = other.categories[i];
				var category = new GridCategory(c.name, new GridData(other, false), c.color);
				category.grid.categories = null;
				category.grid.patch = patch;
				categories[i] = category;
			}
		}
	}
	public MultiGridData(MultiGridData other, bool withValues = true) : base(other)
    {
		if (other.categories != null)
		{
			int count = other.categories.Length;
			categories = new GridCategory[other.categories.Length];
			for (int i = 0; i < count; i++)
			{
				var c = other.categories[i];
				var category = new GridCategory(c.name, new GridData(c.grid, withValues), c.color);
				category.grid.patch = patch;
				categories[i] = category;
			}
		}

		gridFilter = other.gridFilter;
		coloring = other.coloring;
	}

	public override bool IsLoaded()
	{
		return categories != null;
	}

	public override void UnloadData()
	{
		categories = null;
	}

	public void GridChanged()
	{
		if (OnGridChange != null)
			OnGridChange(this);
	}

	public void FilterChanged()
	{
		if (OnFilterChange != null)
			OnFilterChange(this);
	}
}
