// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;

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

	public bool UpdateMinMax(float min, float max)
	{
		if (minValue == 0 && maxValue == 0)
		{
			minValue = min;
			maxValue = max;
			return true;
		}
		else if (min < minValue || max > maxValue)
		{
			minValue = Mathf.Min(minValue, min);
			maxValue = Mathf.Max(maxValue, max);
			return true;
		}
		return false;
	}

	public void RecalculateMinMax()
	{
		minValue = maxValue = 0;

		bool first = true;
		foreach (var record in records.Values)
		{
			foreach (var patch in record.patches)
			{
				if (patch.Data is GridData grid && grid.IsLoaded())
				{
					if (first)
					{
						first = false;
						minValue = grid.minValue;
						maxValue = grid.maxValue;
					}
					else
					{
						minValue = Mathf.Min(minValue, grid.minValue);
						maxValue = Mathf.Max(maxValue, grid.maxValue);
					}
				}
				else if (patch.Data is PointData pointData && pointData.IsLoaded())
				{
					if (first)
					{
						first = false;
						minValue = pointData.minValue;
						maxValue = pointData.maxValue;
					}
					else
					{
						minValue = Mathf.Min(minValue, pointData.minValue);
						maxValue = Mathf.Max(maxValue, pointData.maxValue);
					}
				}
			}
		}
	}

	public void RecalculateMean(bool recalculateGridsMean)
	{
		mean = 0;
		foreach (var record in records.Values)
		{
			foreach (var patch in record.patches)
			{
				if (patch.Data is GridData grid && grid.IsLoaded())
				{
					var gridMean = (float)grid.GetMean(minValue, recalculateGridsMean);
					var meanPercent = Mathf.InverseLerp(minValue, maxValue, gridMean);
					mean = Mathf.Max(mean, meanPercent);
				}
				else if (patch.Data is PointData pointData && pointData.IsLoaded())
				{
					var gridMean = (float)pointData.GetMean(recalculateGridsMean);
					var meanPercent = Mathf.InverseLerp(minValue, maxValue, gridMean);
					mean = Mathf.Max(mean, meanPercent);
				}
			}
		}
	}

	public void UpdatePatchesMinMaxFilters(float minFilter, float maxFilter)
	{
		float siteMin = Mathf.Lerp(minValue, maxValue, minFilter);
		float siteMax = Mathf.Lerp(minValue, maxValue, maxFilter);

		foreach (var record in records.Values)
		{
			foreach (var patch in record.patches)
			{
				if (patch is GridPatch gridPatch)
				{
					gridPatch.SetMinMaxFilter(siteMin, siteMax);
				}
				else if (patch is PointPatch pointPatch)
				{
					pointPatch.SetMinMaxFilter(siteMin, siteMax);
				}
			}
		}
	}
}
