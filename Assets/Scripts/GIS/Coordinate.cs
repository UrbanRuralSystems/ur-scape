// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Globalization;

public struct Coordinate
{
    public static readonly Coordinate Zero = new Coordinate();

    public double Longitude;        // In degrees
    public double Latitude;         // In degrees

    public Coordinate(double longitude, double latitude)
    {
        Longitude = longitude;
        Latitude = latitude;
    }

    public override string ToString()
    {
        return string.Format(NumberFormatInfo.InvariantInfo, "{0:F5},{1:F5}", Longitude, Latitude);
    }

    public void Lerp(Coordinate newCoords, float t)
    {
        Longitude = (1 - t) * Longitude + t * newCoords.Longitude;
        Latitude = (1 - t) * Latitude + t * newCoords.Latitude;
    }

    public double DistanceTo(Coordinate other)
    {
        return Math.Sqrt(Math.Pow(other.Latitude - Latitude, 2) + Math.Pow(other.Longitude - Longitude, 2));
    }

    public double SqDistanceTo(Coordinate other)
    {
        return Math.Pow(other.Latitude - Latitude, 2) + Math.Pow(other.Longitude - Longitude, 2);
    }

}
