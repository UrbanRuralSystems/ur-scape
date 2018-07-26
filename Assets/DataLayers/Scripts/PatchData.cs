// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

public class Metadata
{
    public string name = string.Empty;
    public string date = string.Empty;
    public string source = string.Empty;
    public string accuracy = string.Empty;
    public float? mean;
    public float? stdDeviation;

    public Metadata() { }

    public Metadata(Metadata other)
    {
        name = other.name;
        date = other.date;
        source = other.source;
        accuracy = other.accuracy;
        mean = other.mean;
        stdDeviation = other.stdDeviation;
    }
}

public abstract class PatchData
{
    public Patch patch { get; set; }

	// Mandatory
    public double west;
    public double east;
    public double north;
    public double south;

	// Optional
    public string units = "";
    public Metadata metadata;
	public Category[] categories;

	public uint categoryMask = 0xFFFFFFFF;
    public bool IsCategorized
    {
        get { return categories != null && categories.Length > 0; }
    }

    public delegate void BoundsChangeDelegate(PatchData patchData);
    public event BoundsChangeDelegate OnBoundsChange;

    public PatchData() { }
    public PatchData(PatchData other)
    {
        west = other.west;
        east = other.east;
        north = other.north;
        south = other.south;
		units = other.units;

		if (other.metadata == null)
        {
            metadata = null;
        }
        else
        {
            metadata = new Metadata(other.metadata);
        }

        categoryMask = other.categoryMask;
        if (other.categories != null)
        {
            categories = (Category[])other.categories.Clone();
        }
    }

    public void ChangeBounds(double w, double e, double n, double s)
    {
        west = w;
        east = e;
        north = n;
        south = s;

        if (OnBoundsChange != null)
            OnBoundsChange(this);
    }

    public bool IsInside(double longitude, double latitude)
    {
        return longitude >= west && longitude <= east && latitude >= south && latitude <= north;
    }

    public bool Intersects(double w, double e, double n, double s)
    {
        return east > w && west < e && north > s && south < n;
    }

	public bool HasSameBounds(PatchData other)
	{
		return other.west == west && other.east == east && other.south == south && other.north == north;
	}

	public abstract bool IsLoaded();
	public abstract void UnloadData();
}
