// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;

public abstract class PatchData
{
    public Patch patch { get; set; }

	// Mandatory
    public double west;
    public double east;
    public double north;
    public double south;

	// Optional
    public List<MetadataPair> metadata;

    public delegate void BoundsChangeDelegate(PatchData patchData);
    public event BoundsChangeDelegate OnBoundsChange;

    public PatchData() { }
    public PatchData(PatchData other)
    {
        west = other.west;
        east = other.east;
        north = other.north;
        south = other.south;

		if (other.metadata == null)
        {
            metadata = null;
        }
        else
        {
            metadata = new List<MetadataPair>(other.metadata);
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

	public void AddMetadata(string key, string value)
	{
		if (metadata == null)
			metadata = new List<MetadataPair>();
		metadata.Add(key, value);
	}
}
