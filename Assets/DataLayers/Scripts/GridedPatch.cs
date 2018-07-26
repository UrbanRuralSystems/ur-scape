// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

public abstract class GridedPatch : Patch
{
    public readonly GridData grid;
    public override PatchData Data { get { return grid; } }

    public GridedPatch(DataLayer dataLayer, string name, int level, int year, GridData grid, string filename)
        : base(dataLayer, name, level, year, filename)
    {
        this.grid = grid;
        grid.patch = this;
    }

    public void SetMinMaxFilter(float min, float max)
    {
        grid.minFilter = min;
        grid.maxFilter = max;
        grid.FilterChanged();
    }

    public void SetCategoryMask(uint mask)
    {
        grid.categoryMask = mask;
		grid.FilterChanged();
    }

    public override void UnloadData()
    {
        grid.UnloadData();
    }
}
