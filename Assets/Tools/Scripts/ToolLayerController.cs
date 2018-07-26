// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public class ToolLayerController : MapLayerControllerT<GridMapLayer>
{
    public void Add(GridMapLayer layer, GridData grid, string name, Color color)
    {
        layer.Init(map, grid);
        layer.name = name;
        layer.SetColor(color);
        layer.transform.SetParent(transform, false);
        mapLayers.Add(layer);
    }

    public void Remove(GridMapLayer layer)
    {
        mapLayers.Remove(layer);
    }
}
