// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiGridPatch : Patch
{
    public readonly MultiGridData multigrid;
    public override PatchData Data { get { return multigrid; } }

	private readonly List<GridMapLayer> mapLayers = new List<GridMapLayer>();

	public MultiGridPatch(DataLayer dataLayer, int level, int year, MultiGridData multigrid, string filename)
        : base(dataLayer, level, year, filename)
    {
        this.multigrid = multigrid;
		multigrid.patch = this;
    }

	public static MultiGridPatch Create<D>(DataLayer dataLayer, int level, int year, D data, string filename) where D : MultiGridData
	{
		return new MultiGridPatch(dataLayer, level, year, data, filename);
	}

	//
	// Inherit Methods
	//

	public override void UnloadData()
	{
		multigrid.UnloadData();
	}

	public override IEnumerator LoadData(PatchLoadedCallback callback)
	{
		bool loaded = false;
		yield return multigrid.LoadBin(Filename, (g) => loaded = true);

		if (loaded)
		{
			foreach (var c in multigrid.categories)
			{
				var grid = c.grid;
				if (grid.minFilter == 0 && grid.maxFilter == 0)
				{
					grid.minFilter = Mathf.Max(0.5f, grid.minValue);
					grid.maxFilter = grid.maxValue;
				}
			}

			GridPatch.AssignCategoryColors(multigrid.categories, DataLayer.Color, multigrid.coloring);

			multigrid.GridChanged();

			callback(this);
		}
	}

	public override void Save(string filename, PatchDataFormat format)
	{
		switch (format)
		{
			case PatchDataFormat.BIN:
				multigrid.SaveBin(filename);
				SetFilename(filename);
				break;
			default:
				Debug.LogError("Not implemented");
				break;
		}
	}

	public override void SetMapLayer(MapLayer layer)
	{
		if (layer != null)
		{
			if (layer is GridMapLayer)
				mapLayers.Add(layer as GridMapLayer);
		}
	}

	public override MapLayer GetMapLayer()
	{
		return null;
	}

	public GridMapLayer GetMapLayer(GridData grid)
	{
		foreach (var mapLayer in mapLayers)
			if (mapLayer.Grid == grid)
				return mapLayer;
		return null;
	}

	public override bool IsVisible()
	{
		return mapLayers.Count > 0;
	}


	//
	// Public Methods
	//

	public List<GridMapLayer> GetMapLayers()
	{
		return mapLayers;
	}

	public void ClearMapLayers()
	{
		mapLayers.Clear();
	}

	public void SetCategoryFilter(CategoryFilter filter)
	{
		multigrid.gridFilter.CopyFrom(filter);
		multigrid.FilterChanged();

		UpdateLayers();
	}

	public void ResetFilter()
	{
		multigrid.gridFilter.ResetToDefault();
		multigrid.FilterChanged();

		UpdateLayers();
	}

	public void HighlightCategory(int index)
	{
		if (index == -1 || !multigrid.gridFilter.IsSet(index))
		{
			// Show all enabled categories
			UpdateLayers();
		}
		else
		{
			// Show only the highlighted one
			int count = mapLayers.Count;
			for (int i = 0; i < count; i++)
				mapLayers[i].Show(i == index);
		}
	}

	public void UpdateLayers()
	{
		int count = mapLayers.Count;
		for (int i = 0; i < count; i++)
			mapLayers[i].Show(multigrid.gridFilter.IsSet(i));
	}
}
