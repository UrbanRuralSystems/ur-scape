// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;

[Serializable]
public class DataResolution
{
	public enum Units
	{
		Seconds,
		Meters,
	}

	public string name;
	public double size;
	public Units units;

	public override string ToString()
	{
		string text;
		switch (units)
		{
			case Units.Seconds:
				double d, m, s;
				double degrees = size / 3600d;
				degrees.ToDMS(out d, out m, out s);
				var meters = degrees * GeoCalculator.Deg2Meters;
				text = d + "° " + m + "\" " + s + "'  (~" + meters.Round().MetersToString() + ")";
				break;
			case Units.Meters:
				text = size.MetersToString();
				break;
			default:
				text = "N/A";
				break;
		}
		return text;
	}

	public double ToDegrees()
	{
		double degrees = 0;
		switch (units)
		{
			case Units.Seconds:
				degrees = size / 3600d;
				break;
			case Units.Meters:
				degrees = size * GeoCalculator.Meters2Deg;
				break;
		}
		return degrees;
	}

	public double ToMeters()
	{
		double meters = 0;
		switch (units)
		{
			case Units.Seconds:
				meters = size * GeoCalculator.Deg2Meters / 3600d;
				break;

			case Units.Meters:
				return size;
		}
		return meters;
	}

	public string ToMetersString(bool round, bool exceptMeters = true)
	{
		double meters = ToMeters();
		if (!round || (exceptMeters && units == Units.Meters))
			return meters.MetersToString();
		return "~" + meters.Round().MetersToString();
	}
}

