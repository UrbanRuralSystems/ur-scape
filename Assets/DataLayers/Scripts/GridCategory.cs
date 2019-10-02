// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;
using UnityEngine;

public class GridCategory : Category, IEquatable<GridCategory>
{
	public GridData grid;

	public GridCategory(string name, GridData grid, Color color)
	{
		this.name = name;
		this.grid = grid;
		this.color = color;
	}

	public override bool Equals(object other)
	{
		return base.Equals(other); // Don't compare grid == ((GridCategory)other).grid;
	}

	public bool Equals(GridCategory other)
	{
		return base.Equals(other); // Don't compare grid == other.grid
	}

	public override int GetHashCode()
	{
		var hashCode = -726078169;
		hashCode = hashCode * -1521134295 + base.GetHashCode();
		hashCode = hashCode * -1521134295 + EqualityComparer<GridData>.Default.GetHashCode(grid);
		return hashCode;
	}

	public static bool operator ==(GridCategory category1, GridCategory category2)
	{
		return EqualityComparer<GridCategory>.Default.Equals(category1, category2);
	}

	public static bool operator !=(GridCategory category1, GridCategory category2)
	{
		return !(category1 == category2);
	}
}
