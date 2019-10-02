// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
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

    public GridedPatch(DataLayer dataLayer, int level, int year, GridData grid, string filename)
        : base(dataLayer, level, year, filename)
    {
        this.grid = grid;
        grid.patch = this;
    }

    public override void UnloadData()
    {
        grid.UnloadData();
    }
}
