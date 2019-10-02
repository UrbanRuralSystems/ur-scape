// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;

public static class Mathd
{
    public static double Clamp(double d, double min, double max)
    {
        return Math.Max(min, Math.Min(d, max));
    }

    public static double Clamp01(double d)
    {
        return Math.Max(0, Math.Min(d, 1));
    }
}
