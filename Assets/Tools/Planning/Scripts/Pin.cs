// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public abstract class Pin : MonoBehaviour
{
    public static readonly float InitialHeadSize = 0.015f;
    public static Vector3 HeadSize;

    [Header("Pin Settings")]
    public float groundOffset;
    public float flexiForce;
    public float aplhaCoeff = 0.001f;

    protected Coordinate coords;
    public Coordinate Coords { get { return coords; } }

    // to make pin higher if user zoom out
    private float flexiHeight;

    protected void Init(Coordinate coords)
    {
        this.coords = coords;
        flexiHeight =  groundOffset / flexiForce;
    }

    public virtual void Show(bool show)
    {
        transform.GetChild(0).gameObject.SetActive(show);
    }

    public virtual void UpdatePosition(MapController map)
    {
        var pos = map.GetUnitsFromCoordinates(coords);
        pos.z = -groundOffset * map.MetersToUnits * 0.5f - flexiHeight;
        transform.localPosition = pos;

        // Update head size
        transform.GetChild(0).localScale = HeadSize;
    }

}
