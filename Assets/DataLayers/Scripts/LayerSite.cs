// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;

public class SiteRecord
{
	public readonly LayerSite layerSite;
    public readonly int year;
    public readonly List<Patch> patches = new List<Patch>();

	public SiteRecord(LayerSite layerSite, Patch patch)
    {
		this.layerSite = layerSite;
        year = patch.year;

        patches.Add(patch);
    }
}

public class LayerSite
{
	public readonly Level level;
    public readonly Dictionary<int, SiteRecord> records = new Dictionary<int, SiteRecord>();
	public readonly string name;
	public SiteRecord lastRecord;
	public Site site;

	public float minValue;
	public float maxValue;
	public float mean;

	public LayerSite(Level level, string name, Patch patch)
	{
		this.level = level;
		this.name = name;

		lastRecord = new SiteRecord(this, patch);
		records.Add(lastRecord.year, lastRecord);
	}

	public SiteRecord Add(Patch patch)
	{
		SiteRecord siteRecord;
		if (records.ContainsKey(patch.year))
		{
			siteRecord = records[patch.year];
			siteRecord.patches.Add(patch);
		}
		else
		{
			siteRecord = new SiteRecord(this, patch);
			Add(siteRecord);
		}

		return siteRecord;
	}

	public void Add(SiteRecord record)
    {
        records.Add(record.year, record);

        if (lastRecord == null || record.year > lastRecord.year )
        {
            lastRecord = record;
        }
    }

}
