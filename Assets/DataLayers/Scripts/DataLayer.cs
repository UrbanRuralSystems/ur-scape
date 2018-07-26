// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

#if UNITY_EDITOR
#define SAFETY_CHECK
#endif

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public class Level
{
	public List<LayerSite> layerSites = new List<LayerSite>();
    
	public LayerSite AddSite(string name, Patch patch)
	{
		var layerSite = new LayerSite(this, name, patch);
		layerSites.Add(layerSite);
		return layerSite;
	}
}

public class DataLayer
{
    public readonly string name;
    public readonly Color color;
    public readonly int index;

    private float minFilter = 0;
    public float MinFilter { get { return minFilter; } }
    private float maxFilter = 1;
	public float MaxFilter { get { return maxFilter; } }

    public const int MaxLevels = 5;
    public readonly Level[] levels = new Level[MaxLevels];

    public readonly List<Patch> patchesInView = new List<Patch>();
	public readonly List<Patch> loadedPatchesInView = new List<Patch>();

	private float minVisibleValue = float.MaxValue;
    public float MinVisibleValue { get { return minVisibleValue; } }
    private float maxVisibleValue = float.MinValue;
    public float MaxVisibleValue { get { return maxVisibleValue; } }

	public int visibleYear = -1;

    public float UserOpacity { get; private set; }
	public float ToolOpacity { get; private set; }

	private readonly List<string> patchFiles = new List<string>();

	public int PatchCount { get { return patchFiles.Count; } }


	//
	// Events
	//

	public delegate void OnPatchVisibilityChangeDelegate(DataLayer dataLayer, Patch patch, bool visible);
    public event OnPatchVisibilityChangeDelegate OnPatchVisibilityChange;


	//
	// Public Methods
	//

	public DataLayer(string name, Color color, int index)
    {
        this.name = name;
        this.color = color;
        this.index = index;
        this.UserOpacity = 1;
		this.ToolOpacity = 1;

		for (int i = 0; i < levels.Length; i++)
        {
            levels[i] = new Level();
        }
    }

	// Called by the Filter Panel to set the values to all patches
    public void SetMinMaxFilters(float min, float max)
    {
        minFilter = min;
        maxFilter = max;

        foreach (var level in levels)
        {
			foreach (var site in level.layerSites)
				UpdateSitePatchesMinMaxFilters(site);
        }
    }

	public void ResetFiltersAndOpacity()
	{
		float opacity = 1;
		uint mask = 0xFFFFFFFF;
		minFilter = 0;
		maxFilter = 1;
		UserOpacity = opacity;
		ToolOpacity = opacity;

		foreach (var level in levels)
		{
			foreach (var site in level.layerSites)
			{
				foreach (var record in site.records.Values)
				{
					foreach (var patch in record.patches)
					{
						var mapLayer = patch.GetMapLayer() as GridMapLayer;
						if (mapLayer != null)
						{
							mapLayer.SetUserOpacity(opacity);
							mapLayer.SetToolOpacity(opacity);
						}
						var gridPatch = patch as GridPatch;
						if (gridPatch != null && gridPatch.grid.values != null)
						{
							var grid = gridPatch.grid;
							if (grid.IsCategorized)
							{
								gridPatch.SetCategoryMask(mask);
							}
							else
							{
								gridPatch.SetMinMaxFilter(
									Mathf.Lerp(grid.minValue, grid.maxValue, minFilter),
									Mathf.Lerp(grid.minValue, grid.maxValue, maxFilter));
							}
						}
					}
				}
			}
		}
	}

    public void SetMinMaxFilterToAllPatches(float min, float max)
    {
        minFilter = min;
        maxFilter = max;

        foreach (var level in levels)
        {
			foreach (var site in level.layerSites)
            {
                foreach (var record in site.records.Values)
                {
                    foreach (var patch in record.patches)
                    {
                        var gridPatch = patch as GridPatch;
                        if (gridPatch != null && gridPatch.grid.values != null)
                        {
                            var grid = gridPatch.grid;
                            gridPatch.SetMinMaxFilter(
                                Mathf.Lerp(grid.minValue, grid.maxValue, min),
                                Mathf.Lerp(grid.minValue, grid.maxValue, max));
                        }
                    }
                }
            }
        }
    }

    public void SetUserOpacity(float opacity)
    {
        UserOpacity = opacity;
        foreach (var level in levels)
        {
			foreach (var site in level.layerSites)
            {
                foreach (var record in site.records.Values)
                {
                    foreach (var patch in record.patches)
                    {
                        var mapLayer = patch.GetMapLayer() as GridMapLayer;
                        if (mapLayer != null)
                        {
                            mapLayer.SetUserOpacity(opacity);
                        }
                    }
                }
            }
        }
    }

    public void SetToolOpacity(float opacity)
	{
		opacity = Mathf.Clamp01(opacity);
		ToolOpacity = opacity;
		foreach (var level in levels)
		{
			foreach (var site in level.layerSites)
			{
				foreach (var record in site.records.Values)
				{
					foreach (var patch in record.patches)
					{
						var mapLayer = patch.GetMapLayer() as GridMapLayer;
						if (mapLayer != null)
						{
							mapLayer.SetToolOpacity(opacity);
                        }
					}
				}
			}
		}
	}

    public void BuildFileList()
    {
        patchFiles.Clear();

		var dataManager = ComponentManager.Instance.Get<DataManager>();

#if UNITY_WEBGL

		string start = name + "_";

		var allPatchFiles = dataManager.AllPatchFiles;
		int count = allPatchFiles.Count;
        for (int i = 0; i < count; i++)
        {
			string filepath = allPatchFiles[i];
			string filename = Path.GetFileName(filepath);

            if (filename.StartsWith(start))
			{
				patchFiles.Add(Paths.Sites + filepath);
				i++;

				for (; i < count; i++)
                {
					filepath = allPatchFiles[i];
					filename = Path.GetFileName(filepath);
                    if (!filename.StartsWith(start))
                        break;

					patchFiles.Add(Paths.Sites + filepath);
                }
                break;
            }
        }
#else
		var dirs = dataManager.DataDirs;
		foreach (var dir in dirs)
		{
			var binFiles = Directory.GetFiles(dir, name + "_*." + Patch.BIN_EXTENSION);
			patchFiles.AddRange(binFiles);

			HashSet<string> hs = new HashSet<string>(binFiles);
			var csvFiles = Directory.GetFiles(dir, name + "_*." + Patch.CSV_EXTENSION);
			foreach (var csvFile in csvFiles)
			{
				string binFile = Path.ChangeExtension(csvFile, Patch.BIN_EXTENSION);
				if (hs.Contains(binFile))
				{
					// Check if CSV is newer than the BIN
					if (File.GetLastWriteTime(csvFile) >= File.GetLastWriteTime(binFile))
					{
						patchFiles[patchFiles.IndexOf(binFile)] = csvFile;
					}
				}
				else
				{
					patchFiles.Add(csvFile);
				}
			}
		}
#endif

#if SAFETY_CHECK
		// Check if all patch filenames match exactly (without ignoring case)
		foreach (var patchFile in patchFiles)
		{
			if (!patchFile.Contains(name))
			{
				Debug.LogWarning("Patch " + patchFile + " has wrong case, should be: " + name);
			}
		}
#endif
	}

	public IEnumerator CreatePatches()
    {
#if SAFETY_CHECK
        HashSet<string> loadedFiles = new HashSet<string>();
#endif

        foreach (var patchFile in patchFiles)
        {
#if SAFETY_CHECK
            string fileWithoutExtension = Path.GetFileNameWithoutExtension(patchFile);
            if (loadedFiles.Contains(fileWithoutExtension))
            {
               Debug.LogError("This shouldn't happen! Data layer duplicate:" + fileWithoutExtension);
               continue;
            }
            loadedFiles.Add(fileWithoutExtension);
#endif

            string type = Patch.GetFileNameType(patchFile);

            if (type.Equals("grid"))
            {
                yield return Patch.Create(this, patchFile, GridPatch.Create, GridDataIO.GetPatchHeaderLoader, OnPatchCreated);
            }
            else if (type.Equals("graph"))
            {
                yield return Patch.Create(this, patchFile, GraphPatch.Create, GraphDataIO.GetPatchHeaderLoader, OnPatchCreated);
            }
            else
            {
                Debug.LogError("Found unknown data file type: " + patchFile);
            }
        }
    }

	public GridPatch CreateGridPatch(string name, int level, int year, GridData data)
	{
		var newPatch = new GridPatch(this, name, level, year, data, null);
		OnPatchCreated(newPatch);
		return newPatch;
	}

	public void Show(int levelIndex, AreaBounds bounds)
    {
        InitVisibleRange();
        UpdatePatches(levelIndex, bounds);
    }

    public bool HasPatchesInView()
    {
        return patchesInView.Count > 0;
    }

	public bool HasLoadedPatchesInView()
	{
		return loadedPatchesInView.Count > 0;
	}

	public bool HasPatches(int levelIndex, double west, double east, double north, double south)
    {
		var activeSite = ComponentManager.Instance.Get<SiteBrowser>().ActiveSite;

		foreach (var layerSite in levels[levelIndex].layerSites)
        {
			if (layerSite.site == activeSite)
			{
				foreach (var patch in layerSite.lastRecord.patches)
				{
					if (patch.Data.Intersects(west, east, north, south))
					{
						return true;
					}
				}
			}
        }
        return false;
    }

    public void UpdatePatches(int levelIndex, AreaBounds bounds)
    {
		var dataManager = ComponentManager.Instance.Get<DataManager>();
		var activeSite = ComponentManager.Instance.Get<SiteBrowser>().ActiveSite;

		bool removedPatches = false;

        if (visibleYear == -1)
        {
            // Hide visible patches that don't meet the criteria
            for (int i = patchesInView.Count - 1; i >= 0; i--)
            {
				var patchData = patchesInView[i].Data;
                if (patchesInView[i].level != levelIndex || !patchData.Intersects(bounds.west, bounds.east, bounds.north, bounds.south)
					|| patchesInView[i].siteRecord.layerSite.site != activeSite)
                {
					RemovePatchInView(i);
					removedPatches = true;
				}
            }

			// Show non-visible patches that meet the criteria
			foreach (var layerSite in levels[levelIndex].layerSites)
            {
				if (layerSite.site == activeSite)
				{
					foreach (var patch in layerSite.lastRecord.patches)
					{
						var patchData = patch.Data;
						if (!patch.IsVisible() &&
							patchData.Intersects(bounds.west, bounds.east, bounds.north, bounds.south) &&
							!dataManager.IsRequesting(patch))
						{
							AddPatchInView(patch);
						}
					}
				}
			}
        }
        else
        {
            // Hide visible patches that don't meet the criteria
            for (int i = patchesInView.Count - 1; i >= 0; i--)
            {
                var patchData = patchesInView[i].Data;
                if (patchesInView[i].level != levelIndex || !patchData.Intersects(bounds.west, bounds.east, bounds.north, bounds.south)
					|| patchesInView[i].year != visibleYear
					|| patchesInView[i].siteRecord.layerSite.site != activeSite)
				{
					RemovePatchInView(i);
					removedPatches = true;
				}
            }

			// Show non-visible sites that meet the criteria
			foreach (var layerSite in levels[levelIndex].layerSites)
            {
				if (layerSite.site == activeSite &&
					layerSite.records.ContainsKey(visibleYear))
                {
                    foreach (var patch in layerSite.records[visibleYear].patches)
                    {
                        var patchData = patch.Data;
						if (!patch.IsVisible() && 
							patchData.Intersects(bounds.west, bounds.east, bounds.north, bounds.south) &&
							!dataManager.IsRequesting(patch)) 
						{
							AddPatchInView(patch);
						}
					}
				}
            }
        }

		if (removedPatches)
		{
			UpdateLoadedVisibleRange();
		}
    }

	public void HidePatchesInView()
	{
		for (int i = patchesInView.Count - 1; i >= 0; i--)
		{
			RemovePatchInView(i);
		}
		UpdateLoadedVisibleRange();
	}


	public void HideLoadedPatches()
    {
        for (int i = loadedPatchesInView.Count - 1; i >= 0; i--)
        {
			RemoveLoadedPatch(i);
        }
    }


	//
	// Event Methods
	//

	private void OnPatchCreated(Patch patch)
	{
#if SAFETY_CHECK
		if (patch.level < 0 || patch.level >= levels.Length)
		{
			Debug.LogError("Patch " + patch.name + "(" + patch.Filename + ") has invalid level: " + patch.level);
			return;
		}
#endif
		var level = levels[patch.level];

		SiteRecord siteRecord = null;
		foreach (var layerSite in level.layerSites)
		{
			if (patch.name.Equals(layerSite.name))
			{
				siteRecord = layerSite.Add(patch);
				break;
			}
		}

		if (siteRecord == null)
		{
			var site = level.AddSite(patch.name, patch);
			siteRecord = site.lastRecord;
		}

		patch.siteRecord = siteRecord;
	}

	private void OnPatchLoaded(Patch patch)
	{
		var map = ComponentManager.Instance.Get<MapController>();
		var bounds = map.MapCoordBounds;

		// Check that the loaded patch is still visible, otherwise hide it
		if (patch.level == map.CurrentLevel && patch.Data.Intersects(bounds.west, bounds.east, bounds.north, bounds.south))
		{
			ShowPatch(patch);
		}
		else
		{
			// Patch has finished loading but is no longer in view :(
			RemovePatchInView(patch);
			UpdateLoadedVisibleRange();
		}
	}


	//
	// Private Methods
	//

	private void RequestPatch(Patch patch)
	{
		patchesInView.Add(patch);

		ComponentManager.Instance.Get<DataManager>().RequestPatch(patch, OnPatchLoaded);
	}

	private void AddPatchInView(Patch patch)
	{
		if (!patch.Data.IsLoaded())
		{
			RequestPatch(patch);
			return;
		}

		patchesInView.Add(patch);

		ShowPatch(patch);
	}

	private void ShowPatch(Patch patch)
	{
		if (patch is GraphPatch)
		{
			var graphPatch = patch as GraphPatch;
			if (graphPatch.grid.values == null)
					graphPatch.CreateDefaultGrid();
		}

		// Update ranges BEFORE adding patch
		UpdateValueRanges(patch);
		UpdateMapLayersVisibleRange();

		loadedPatchesInView.Add(patch);

		ComponentManager.Instance.Get<DataManager>().ShowPatch(patch);

		if (OnPatchVisibilityChange != null)
            OnPatchVisibilityChange(this, patch, true);
    }

	private void RemovePatchInView(int index)
	{
		Patch patch = patchesInView[index];
		patchesInView.RemoveAt(index);

		RemoveLoadedPatch(patch);
	}

	private void RemovePatchInView(Patch patch)
	{
		patchesInView.Remove(patch);

		RemoveLoadedPatch(patch);
	}

	private void RemoveLoadedPatch(int index)
	{
		Patch patch = loadedPatchesInView[index];
		loadedPatchesInView.RemoveAt(index);

		HidePatch(patch);
	}

	private void RemoveLoadedPatch(Patch patch)
	{
		loadedPatchesInView.Remove(patch);

		HidePatch(patch);
	}

	private void HidePatch(Patch patch)
	{
		bool isVisible = patch.IsVisible();

		ComponentManager.Instance.Get<DataManager>().HidePatch(patch);

		if (isVisible && OnPatchVisibilityChange != null)
            OnPatchVisibilityChange(this, patch, false);
	}

	private void UpdateValueRanges(Patch patch)
	{
		var gridData = patch.Data as GridData;
		if (gridData != null)
		{
			// Update site's value range
			var layerSite = patch.siteRecord.layerSite;
			if (layerSite.minValue == 0 && layerSite.maxValue == 0)
			{
				layerSite.minValue = gridData.minValue;
				layerSite.maxValue = gridData.maxValue;
			}
			else if (gridData.minValue < layerSite.minValue || gridData.maxValue > layerSite.maxValue)
			{
				layerSite.minValue = Mathf.Min(layerSite.minValue, gridData.minValue);
				layerSite.maxValue = Mathf.Max(layerSite.maxValue, gridData.maxValue);
			}

			if (patch is GridPatch)
				UpdateSiteMean(layerSite);
			else
				layerSite.mean = 1;

			if (minFilter != 0 || maxFilter != 1)
				UpdateSitePatchesMinMaxFilters(layerSite);


			// Update layer's value range
			minVisibleValue = Mathf.Min(minVisibleValue, gridData.minValue);
			maxVisibleValue = Mathf.Max(maxVisibleValue, gridData.maxValue);
		}
	}

	private void UpdateSiteMean(LayerSite layerSite)
	{
		layerSite.mean = 0;
		foreach (var record in layerSite.records.Values)
		{
			foreach (var patch in record.patches)
			{
				var grid = patch.Data as GridData;
				if (grid != null && grid.values != null)
				{
					float mean = 0;
					int count = grid.countX * grid.countY;
					for (int i = 0; i < count; i++)
					{
						mean += grid.valuesMask[i] ? grid.values[i] : layerSite.minValue;
					}
					mean /= count;

					float meanPercent = Mathf.InverseLerp(layerSite.minValue, layerSite.maxValue, mean);
					layerSite.mean = Mathf.Max(layerSite.mean, meanPercent);
				}
			}
		}
	}

    private void UpdateLoadedVisibleRange()
    {
		InitVisibleRange();
		if (loadedPatchesInView.Count > 0)
        {
            foreach (GridedPatch patch in loadedPatchesInView)
            {
				minVisibleValue = Mathf.Min(minVisibleValue, patch.grid.minValue);
				maxVisibleValue = Mathf.Max(maxVisibleValue, patch.grid.maxValue);
			}

			UpdateMapLayersVisibleRange();
		}
	}

	private void UpdateMapLayersVisibleRange()
	{
		foreach (GridedPatch patch in loadedPatchesInView)
		{
			(patch.GetMapLayer() as GridMapLayer).UpdateRange();
		}
	}

	private void InitVisibleRange()
    {
        minVisibleValue = float.MaxValue;
        maxVisibleValue = float.MinValue;
	}

	private void UpdateSitePatchesMinMaxFilters(LayerSite layerSite)
	{
		float siteMin = Mathf.Lerp(layerSite.minValue, layerSite.maxValue, minFilter);
		float siteMax = Mathf.Lerp(layerSite.minValue, layerSite.maxValue, maxFilter);

		foreach (var record in layerSite.records.Values)
		{
			foreach (GridedPatch patch in record.patches)
			{
				patch.SetMinMaxFilter(siteMin, siteMax);
			}
		}
	}

}

internal static class StringExtensions
{
    public static bool StartsWithIgnoreCase(this string filename, string start)
    {
        if (filename.StartsWith(start, System.StringComparison.CurrentCultureIgnoreCase))
        {
            if (!filename.StartsWith(start))
            {
                Debug.LogWarning(filename + " has a different case than layer name: " + start);
            }
            return true;
        }
        return false;
    }
}
