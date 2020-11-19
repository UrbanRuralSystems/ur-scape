// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//			Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;

public class MunicipalityData
{
	public readonly double west;
	public readonly double east;
	public readonly double north;
	public readonly double south;
	public readonly int countX;
	public readonly int countY;

	public readonly int[] ids;
	public readonly Dictionary<int, string> idToName;

	public MunicipalityData(int[] ids, Dictionary<int, string> idToName, double north, double east, double south, double west, int countX, int countY)
	{
		this.west = west;
		this.east = east;
		this.north = north;
		this.south = south;
		this.countX = countX;
		this.countY = countY;
		this.ids = ids;
		this.idToName = idToName;
	}
}
