// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Globalization;
using UnityEngine;

public struct Distance
{
    public double x;
    public double y;

    public Distance(double x, double y)
    {
        this.x = x;
        this.y = y;
    }

    public override string ToString()
    {
        return string.Format(NumberFormatInfo.InvariantInfo, "{0:F5},{1:F5}", x, y);
    }

    public static Distance operator +(Distance d1, Distance d2)
    {
        return new Distance(d1.x + d2.x, d1.y + d2.y);
    }

    public static Distance operator -(Distance d1, Distance d2)
    {
        return new Distance(d1.x - d2.x, d1.y - d2.y);
    }

    public static Distance operator *(Distance d, double val)
    {
        return new Distance(d.x * val, d.y * val);
    }

    public Vector2 ToVector2()
    {
        return new Vector2((float) x, (float)y);
    }
}