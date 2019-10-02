// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

public class MapTileBounds
{
    public int North;
    public int South;
    public int West;
    public int East;

    public int HorizontalCount { get { return East - West; } }
    public int VerticalCount { get { return South - North; } }

    public MapTileBounds(int n, int s, int w, int e)
    {
        North = n;
        South = s;
        West = w;
        East = e;
    }

    public void Set(int n, int s, int w, int e)
    {
        North = n;
        South = s;
        West = w;
        East = e;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is MapTileBounds))
            return false;

        MapTileBounds bounds = (MapTileBounds)obj;

        return bounds.North == North && bounds.South == South && bounds.West == West && bounds.East == East;
    }

    public override int GetHashCode()
    {
        int hash = 269;
        hash = hash * 47 + North;
        hash = hash * 47 + South;
        hash = hash * 47 + West;
        hash = hash * 47 + East;
        return hash;
    }

    public static bool operator ==(MapTileBounds lhs, MapTileBounds rhs)
    {
        if (ReferenceEquals(lhs, null))
        {
            return ReferenceEquals(rhs, null);
        }

        return lhs.Equals(rhs);
    }

    public static bool operator !=(MapTileBounds lhs, MapTileBounds rhs)
    {
        return !(lhs == rhs);
    }
}
