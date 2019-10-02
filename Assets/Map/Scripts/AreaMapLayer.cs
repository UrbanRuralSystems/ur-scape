// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class AreaMapLayer : MapLayer
{
    protected Distance areaCenterInMeters;
    protected Distance areaSizeInMeters;


    //
    // Inheritance Methods
    //

    public virtual void Init(MapController map, double north, double east, double south, double west)
    {
        base.Init(map);

		UpdateAreaCenterAndSize(north, east, south, west);
    }

    public override void UpdateContent()
    {
        // Update the position
        var offset = areaCenterInMeters - map.MapCenterInMeters;
        var units = GeoCalculator.RelativeMetersToPixels(offset, map.ZoomLevel) * map.PixelsToUnits;
        transform.localPosition = new Vector3((float)units.x, (float)units.y, -0.001f);

        // Update the size
        units = GeoCalculator.RelativeMetersToPixels(areaSizeInMeters, map.ZoomLevel) * map.PixelsToUnits;
        transform.localScale = new Vector3((float)units.x, (float)units.y, 1);
    }


    //
    // Public Methods
    //

    public void SetColor(Color color)
    {
        GetComponent<MeshRenderer>().material.SetColor("Tint", color);
    }


    //
    // Private/Protected Methods
    //

    protected void UpdateAreaCenterAndSize(double north, double east, double south, double west)
    {
        var max = GeoCalculator.LonLatToMeters(east, north);
        var min = GeoCalculator.LonLatToMeters(west, south);
        areaCenterInMeters = (min + max) * 0.5;
        areaSizeInMeters = max - min;

		UpdateContent();
	}

}
