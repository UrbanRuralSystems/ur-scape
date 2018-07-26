// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;

public class TilePool : MonoBehaviour
{
    [SerializeField]
    private int initialSize = 10000;

    private Stack<Texture2D> tiles;
    
    public int Total { get; internal set; }
    public int Available { get { return tiles.Count; } }

    void Awake()
	{
        Total = initialSize;
        tiles = new Stack<Texture2D>(initialSize);
        for (int i = 0; i < initialSize; i++)
        {
            var tile = new Texture2D(MapTile.Size, MapTile.Size, TextureFormat.RGB24, false);
            tiles.Push(tile);
        }
    }

    public Texture2D Get()
    {
        if (tiles.Count > 0)
            return tiles.Pop();

        Total++;
        return new Texture2D(MapTile.Size, MapTile.Size, TextureFormat.RGB24, false);
    }

}
