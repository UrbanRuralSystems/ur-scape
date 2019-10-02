// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;

public class LayerSite
{
	// Link to Site
	public Site Site { get; private set; }

	public readonly Dictionary<int, SiteRecord> records = new Dictionary<int, SiteRecord>();
	public SiteRecord LastRecord { get; private set; }

	public float minValue;
	public float maxValue;
	public float mean;

	public LayerSite(Site site, Patch patch)
	{
		Site = site;

		Add(patch);
	}

	public SiteRecord Add(Patch patch)
	{
		if (records.TryGetValue(patch.Year, out SiteRecord siteRecord))
		{
			siteRecord.Add(patch);
		}
		else
		{
			siteRecord = new SiteRecord(this, patch);
			Add(siteRecord);
		}

		return siteRecord;
	}

	private void Add(SiteRecord record)
    {
        records.Add(record.Year, record);

        if (LastRecord == null || record.Year > LastRecord.Year )
        {
            LastRecord = record;
        }
    }

	public void ChangeSite(Site newSite)
	{
		if (newSite == Site)
			return;

		Site = newSite;

		var siteDir = Site.GetDirectory();

		foreach (var record in records)
		{
			foreach (var patch in record.Value.patches)
			{
				patch.ChangeSiteName(Site.Name, siteDir);
			}
		}
	}

	public void ChangeYear(int oldYear, int newYear)
	{
		if (!records.TryGetValue(oldYear, out SiteRecord record))
			return;

		records.Remove(oldYear);
		records.Add(newYear, record);
		record.ChangeYear(newYear);

		// Update last record
		foreach (var pair in records)
		{
			if (pair.Key > LastRecord.Year)
				LastRecord = pair.Value;
		}
	}
}
