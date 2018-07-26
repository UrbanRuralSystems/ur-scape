// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public abstract class MapLayer : MonoBehaviour
{
    protected MapController map;

    public void Init(MapController map)
    {
        this.map = map;
    }

    public abstract void UpdateContent();

    public virtual void Show(bool show)
    {
        gameObject.SetActive(show);
    }

    public bool IsVisible()
    {
        return gameObject.activeSelf;
    }

}