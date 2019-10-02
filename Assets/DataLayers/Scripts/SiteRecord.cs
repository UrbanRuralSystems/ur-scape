// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;

public class SiteRecord
{
	// Link to parent LayerSite
	public readonly LayerSite layerSite;

	public int Year { get; private set; }
	public readonly List<Patch> patches = new List<Patch>();

	public SiteRecord(LayerSite layerSite, Patch patch)
	{
		this.layerSite = layerSite;
		Year = patch.Year;

		Add(patch);
	}

	public void Add(Patch patch)
	{
		patches.Add(patch);
		layerSite.Site.PatchWasAdded(patch);
	}

	public void ChangeYear(int year)
	{
		Year = year;

		foreach (var patch in patches)
		{
			if (patch.Filename != null)
			{
				patch.ChangeYear(year);
			}
		}
	}
}
