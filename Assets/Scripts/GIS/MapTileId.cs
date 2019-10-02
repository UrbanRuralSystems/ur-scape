// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using UnityEngine;

public class MapTileId
{
    public int X = 0;
    public int Y = 0;
    public int Z = 0;
    public int Layer = 0;

    public MapTileId() { }

    public MapTileId(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public MapTileId(int x, int y, int z, int layer) : this(x, y, z)
    {
        Layer = layer;
    }

    public MapTileId(long id)
    {
        Set(id);
    }

    public void Set(long id)
    {
        X = (int)id & 0xFFFFF;
        Y = (int)(id >> 20) & 0xFFFFF;
        Z = (int)(id >> 40) & 0x1F;
        Layer = (int)(id >> 45) & 0x1F;
    }

    public long ToId()
    {
        return ToId(X, Y, Z, Layer);
    }

    public static long ToId(long x, long y, long z, long layer)
    {
        return (layer << 45) | (z << 40) | (y << 20) | x;
    }

    public override string ToString()
    {
        return Z + "_" + X + "_" + Y;
    }

    public string ToURL()
    {
        return Z + "/" + X + "/" + Y;
    }

    public MapTileId Wrap()
    {
        return CreateWrapped(X, Y, Z, Layer);
    }

    public static MapTileId CreateWrapped(int x, int y, int z, int layer)
    {
        int pow = (1 << z);
        int wrap = (x < 0 ? x - pow + 1 : x) / pow;

        return new MapTileId(x - wrap * pow, y < 0 ? 0 : Math.Min(y, pow - 1), z, layer);
    }

    public static long CreateWrappedId(int x, int y, int z, int layer)
    {
        int pow = (1 << z);
        int wrap = (x < 0 ? x - pow + 1 : x) / pow;

        return ToId(x - wrap * pow, y < 0 ? 0 : Math.Min(y, pow - 1), z, layer);
    }

    public void ConvertToZoom(int zoomLevel)
    {
        float toMapZoom = Mathf.Pow(2, Z - zoomLevel);
        X = (int)(X * toMapZoom);
        Y = (int)(Y * toMapZoom);
        Z =zoomLevel;
    }

}