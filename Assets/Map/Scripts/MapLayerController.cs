// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;


public abstract class MapLayerController : MonoBehaviour
{
    protected MapController map;

    public virtual void Init(MapController map)
    {
        this.map = map;

        map.AddLayerController(this);
    }

    public abstract void UpdateLayers();

}

public abstract class MapLayerControllerT<T> : MapLayerController where T : MapLayer
{
    public readonly List<T> mapLayers = new List<T>();

    public override void UpdateLayers()
    {
        foreach (var layer in mapLayers)
        {
            layer.UpdateContent();
        }
    }

}
