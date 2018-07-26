// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)


public class AreaBounds
{
    public double west;
    public double east;
    public double north;
    public double south;

    public AreaBounds(double west, double east, double north, double south)
    {
        this.west = west;
        this.east = east;
        this.north = north;
        this.south = south;
    }

	public AreaBounds(AreaBounds other) : this(other.west, other.east, other.north, other.south) {}

}
