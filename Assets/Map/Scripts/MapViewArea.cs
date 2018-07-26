// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

public class MapViewArea : UrsComponent
{
    public delegate void OnMapViewAreaChangeDelegate();
    public event OnMapViewAreaChangeDelegate OnMapViewAreaChange;

    private void OnRectTransformDimensionsChange()
    {
        if (OnMapViewAreaChange != null)
            OnMapViewAreaChange();
    }
}
