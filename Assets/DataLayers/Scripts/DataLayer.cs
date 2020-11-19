// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

#if UNITY_EDITOR
#define SAFETY_CHECK
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public class DataLayer
{
	public string Name { get; private set; }
	public Color Color { get; private set; }
	public LayerGroup Group { get; private set; }

	public float MinFilter { get; private set; } = 0;
	public float MaxFilter { get; private set; } = 1;

	public const int MaxLevels = 5;
	public readonly LayerLevel[] levels = new LayerLevel[MaxLevels];

	public readonly List<Patch> patchesInView = new List<Patch>();
	public readonly List<Patch> loadedPatchesInView = new List<Patch>();

	public float MinVisibleValue { get; private set; } = float.MaxValue;
	public float MaxVisibleValue { get; private set; } = float.MinValue;

	public int visibleYear = -1;

	public float UserOpacity { get; private set; }
	public float ToolOpacity { get; private set; }
	public bool IsTemp { get; private set; }

	private readonly DataManager dataManager;


	//
	// Events
	//

	public delegate void OnPatchVisibilityChangeDelegate(DataLayer dataLayer, Patch patch, bool visible);
	public event OnPatchVisibilityChangeDelegate OnPatchVisibilityChange;


	//
	// Public Methods
	//

	public DataLayer(DataManager dataManager, string name, Color color, LayerGroup group)
	{
		Name = name;
		Color = color;
		Group = group;
		UserOpacity = 1;
		ToolOpacity = 1;

		for (int i = 0; i < levels.Length; i++)
		{
			levels[i] = new LayerLevel();
		}

		group.AddLayer(this);

		this.dataManager = dataManager;
	}

	public void Remove()
	{
		HidePatchesInView();

		var sites = GetSites();
		foreach (var site in sites)
		{
			site.RemoveLayer(this);
		}

		Group.RemoveLayer(this);
	}

	public void ChangeName(string newName)
	{
		if (string.IsNullOrWhiteSpace(newName) || Name == newName)
			return;

		RenamePatches(newName);

		Name = newName;
	}

	public void ChangeGroup(LayerGroup newGroup)
	{
		if (newGroup == null || newGroup == Group)
			return;

		Group.MoveLayerToGroup(this, newGroup);
		Group = newGroup;
	}

	public void ChangeColor(Color color)
	{
		Color = color;

		foreach (var patch in loadedPatchesInView)
		{
			var mapLayer = patch.GetMapLayer() as GridMapLayer;
			if (mapLayer != null)
			{
				mapLayer.SetColor(color);
			}
		}
	}

	public void ChangeYear(int oldYear, int newYear, Site site)
	{
		foreach (var level in levels)
		{
			foreach (var layerSite in level.layerSites)
			{
				if (layerSite.Site == site)
				{
					layerSite.ChangeYear(oldYear, newYear);
					break;
				}
			}
		}
	}

	public void SetIsTemp(bool temp)
	{
		IsTemp = temp;
	}

	// Called by the Filter Panel to set the values to all patches
	public void SetMinMaxFilters(float min, float max)
	{
		MinFilter = min;
		MaxFilter = max;

		foreach (var level in levels)
		{
			foreach (var layerSite in level.layerSites)
				layerSite.UpdatePatchesMinMaxFilters(min, max);
		}
	}

	public void ResetFiltersAndOpacity()
	{
		float opacity = 1;
		MinFilter = 0;
		MaxFilter = 1;
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
						if (patch is GridPatch gridPatch && gridPatch.grid.values != null)
						{
							gridPatch.ResetFilter();
						}
					}
				}
			}
		}
	}

	public void SetMinMaxFilterToAllPatches(float min, float max)
	{
		MinFilter = min;
		MaxFilter = max;

		foreach (var level in levels)
		{
			foreach (var site in level.layerSites)
			{
				foreach (var record in site.records.Values)
				{
					foreach (var patch in record.patches)
					{
						if (patch is GridPatch gridPatch && gridPatch.grid.values != null)
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

#if !UNITY_WEBGL
	private static int fileCounter = 0;
#endif

	public IEnumerator GetPatchFiles(List<string> paths, UnityAction<List<string>> callback)
	{
		var patchFiles = new List<string>();

#if UNITY_WEBGL
		string start = Name + "_";

		int count = paths.Count;
        for (int i = 0; i < count; i++)
        {
			string filepath = paths[i];
			string filename = Path.GetFileName(filepath);

            if (filename.StartsWith(start))
			{
				patchFiles.Add(Paths.Sites + filepath);
				i++;

				for (; i < count; i++)
                {
					filepath = paths[i];
					filename = Path.GetFileName(filepath);
                    if (!filename.StartsWith(start))
                        break;

					patchFiles.Add(Paths.Sites + filepath);
                }
                break;
            }
        }
#else
		const int MaxPatchesPerFrame = 100;
		HashSet<string> hs = new HashSet<string>();
		string binFilter = Name + "_*." + Patch.BIN_EXTENSION;
		string csvFilter = Name + "_*." + Patch.CSV_EXTENSION;
		foreach (var dir in paths)
		{
			hs.Clear();

			var binFiles = Directory.GetFiles(dir, binFilter);
			int count = binFiles.Length;
			for (int i = 0; i < count; i++)
			{
				var binFile = binFiles[i];
				if (PatchDataIO.CheckBinVersion(binFile))
				{
					patchFiles.Add(binFile);
					hs.Add(binFile);
				}
				if (++fileCounter % MaxPatchesPerFrame == 0)
					yield return null;
			}

			var csvFiles = Directory.GetFiles(dir, csvFilter);
			count = csvFiles.Length;
			for (int i = 0; i < count; i++)
			{
				var csvFile = csvFiles[i];
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
				if (++fileCounter % MaxPatchesPerFrame == 0)
					yield return null;
			}
		}
#endif

#if SAFETY_CHECK
		// Check if all patch filenames match exactly (without ignoring case)
		foreach (var patchFile in patchFiles)
		{
			if (!patchFile.Contains(Name))
			{
				Debug.LogWarning("Patch " + patchFile + " has wrong case, should be: " + Name);
			}
		}
#endif

		callback(patchFiles);

#if UNITY_WEBGL
		yield break;
#endif
	}

#if !UNITY_WEBGL
	public List<string> GetPatchFiles(List<string> directories, string site = null, string extension = "*")
	{
		var patchFiles = new List<string>();

		string filter1 = Name + "_*.*";
		string filter2 = null;
		if (site != null)
		{
			filter1 = Name + "_*_" + site + "@*." + extension;
			filter2 = Name + "_*_" + site + "_*." + extension;
		}

		foreach (var dir in directories)
		{
			patchFiles.AddRange(Directory.GetFiles(dir, filter1));
			if (filter2 != null)
				patchFiles.AddRange(Directory.GetFiles(dir, filter2));
		}

		return patchFiles;
	}
#endif

	public void DeleteFiles(string site = null)
	{
#if !UNITY_WEBGL
		var dirs = dataManager.GetDataDirectories();
		IOUtils.SafeDelete(GetPatchFiles(dirs, site));
#endif
	}

	public IEnumerator CreatePatches(List<string> patchFiles)
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

			yield return CreatePatch(patchFile);
		}
	}

	public IEnumerator CreatePatch(string filename)
	{
		string type = Patch.GetFileNameType(filename);

		if (type.Equals(GridDataIO.FileSufix))
		{
			yield return Patch.Create(this, filename, GridPatch.Create, GridDataIO.GetPatchHeaderLoader, OnPatchCreated);
		}
		else if (type.Equals(GraphDataIO.FileSufix))
		{
			yield return Patch.Create(this, filename, GraphPatch.Create, GraphDataIO.GetPatchHeaderLoader, OnPatchCreated);
		}
		else if (type.Equals(MultiGridDataIO.FileSufix))
		{
			yield return Patch.Create(this, filename, MultiGridPatch.Create, MultiGridDataIO.GetPatchHeaderLoader, OnPatchCreated);
		}
		else
		{
			Debug.LogError("Found unknown data file type: " + filename);
		}
	}

	public GridPatch CreateGridPatch(string siteName, int level, int year, GridData data)
	{
		var newPatch = new GridPatch(this, level, year, data, null);
		OnPatchCreated(newPatch, siteName);
		return newPatch;
	}

	public void Show(int levelIndex, AreaBounds bounds)
	{
		InitVisibleRange();
		UpdatePatches(dataManager.ActiveSite, levelIndex, bounds);
	}

	public bool HasPatchesInView()
	{
		return patchesInView.Count > 0;
	}

	public bool HasLoadedPatchesInView()
	{
		return loadedPatchesInView.Count > 0;
	}

	public bool HasPatches()
	{
		foreach (var level in levels)
		{
			foreach (var layerSite in level.layerSites)
			{
				if (layerSite.LastRecord.patches.Count > 0)
					return true;
			}
		}
		return false;
	}

	public bool HasPatches(Site site, int levelIndex, double west, double east, double north, double south)
	{
		foreach (var layerSite in levels[levelIndex].layerSites)
		{
			if (layerSite.Site == site)
			{
				foreach (var patch in layerSite.LastRecord.patches)
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

	public void UpdatePatches(Site site, int levelIndex, AreaBounds bounds)
	{
		bool removedPatches = false;

		if (visibleYear == -1)
		{
			// Hide visible patches that don't meet the criteria
			for (int i = patchesInView.Count - 1; i >= 0; i--)
			{
				var patch = patchesInView[i];
				if (patch.Level != levelIndex ||
					!patch.Data.Intersects(bounds.west, bounds.east, bounds.north, bounds.south) ||
					patch.SiteRecord.layerSite.Site != site)
				{
					RemovePatchInView(i);
					removedPatches = true;
				}
			}

			// Show non-visible patches that meet the criteria
			foreach (var layerSite in levels[levelIndex].layerSites)
			{
				if (layerSite.Site == site)
				{
					foreach (var patch in layerSite.LastRecord.patches)
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
				var patch = patchesInView[i];
				if (patch.Level != levelIndex || !patch.Data.Intersects(bounds.west, bounds.east, bounds.north, bounds.south)
					|| patch.Year != visibleYear
					|| patch.SiteRecord.layerSite.Site != site)
				{
					RemovePatchInView(i);
					removedPatches = true;
				}
			}

			// Show non-visible sites that meet the criteria
			foreach (var layerSite in levels[levelIndex].layerSites)
			{
				if (layerSite.Site == site &&
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

	public void RemovePatches(Site site, int level, int year)
	{
		bool removedPatches = false;
		for (int i = patchesInView.Count - 1; i >= 0; i--)
		{
			var patch = patchesInView[i];
			if (patch.Level == level &&
				patch.Year == year &&
				patch.SiteRecord.layerSite.Site == site)
			{
				RemovePatchInView(i);
				removedPatches = true;
			}
		}

		if (removedPatches)
			UpdateLoadedVisibleRange();
	}

	public List<Site> GetSites()
	{
		List<Site> sites = new List<Site>();
		foreach (var level in levels)
		{
			foreach (var layerSite in level.layerSites)
			{
				if (!sites.Contains(layerSite.Site))
				{
					sites.Add(layerSite.Site);
				}
			}
		}
		sites.Sort((s1, s2) => s1.Name.CompareTo(s2.Name));
		return sites;
	}

	public bool HasDataForSite(Site site)
	{
		foreach (var level in levels)
		{
			foreach (var layerSite in level.layerSites)
			{
				if (site == layerSite.Site)
					return true;
			}
		}
		return false;
	}

	public void UpdateAfterValuesChange(GridData gridData)
	{
		var layerSite = gridData.patch.SiteRecord.layerSite;
		layerSite.RecalculateMinMax();
		layerSite.RecalculateMean(true);
		layerSite.UpdatePatchesMinMaxFilters(MinFilter, MaxFilter);

		UpdateLoadedVisibleRange();
	}


	//
	// Event Methods
	//

	private void OnPatchCreated(Patch patch, string siteName)
	{
#if SAFETY_CHECK
		if (patch.Level < 0 || patch.Level >= levels.Length)
		{
			Debug.LogError("Patch " + patch.Filename + " has invalid level: " + patch.Level);
			return;
		}
#endif
		var level = levels[patch.Level];

		SiteRecord siteRecord = null;
		foreach (var layerSite in level.layerSites)
		{
			if (siteName == layerSite.Site.Name)
			{
				siteRecord = layerSite.Add(patch);
				break;
			}
		}

		if (siteRecord == null)
		{
			var site = dataManager.GetOrAddSite(siteName);
			var layerSite = level.AddSite(site, patch);
			siteRecord = layerSite.LastRecord;
		}

		patch.SetSiteRecord(siteRecord);
	}

	private void OnPatchLoaded(Patch patch, bool isInView)
	{
		// Check that the loaded patch is still visible, otherwise hide it
		if (isInView)
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
		dataManager.RequestPatch(patch, OnPatchLoaded);
	}

	private void AddPatchInView(Patch patch)
	{
		// A patch could be re-added if its layer is toggled on-off-on, as it is always in view.
		if (!patchesInView.Contains(patch))
			patchesInView.Add(patch);

		if (!patch.Data.IsLoaded())
		{
			RequestPatch(patch);
			return;
		}

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

		dataManager.ShowPatch(patch);

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

		dataManager.HidePatch(patch);

		if (isVisible && OnPatchVisibilityChange != null)
            OnPatchVisibilityChange(this, patch, false);
	}

	private void UpdateValueRanges(Patch patch)
	{
		if (patch.Data is GridData)
		{
			UpdateValueRanges(patch.Data as GridData);

		}
		else if (patch.Data is MultiGridData)
		{
			var multigrid = patch.Data as MultiGridData;
			foreach (var c in multigrid.categories)
			{
				UpdateValueRanges(c.grid);
			}
		}
	}

	private void UpdateValueRanges(GridData gridData)
	{
		// Update site's value range
		var layerSite = gridData.patch.SiteRecord.layerSite;
		bool siteValueRangeHasChanged = layerSite.UpdateMinMax(gridData.minValue, gridData.maxValue);

		if (gridData.patch is GridPatch)
			layerSite.RecalculateMean(siteValueRangeHasChanged);
		else
			layerSite.mean = 1;

		if (MinFilter != 0 || MaxFilter != 1)
			layerSite.UpdatePatchesMinMaxFilters(MinFilter, MaxFilter);

		// Update layer's value range
		MinVisibleValue = Mathf.Min(MinVisibleValue, gridData.minValue);
		MaxVisibleValue = Mathf.Max(MaxVisibleValue, gridData.maxValue);
	}

	private void UpdateLoadedVisibleRange()
    {
		InitVisibleRange();
		if (loadedPatchesInView.Count > 0)
        {
            foreach (var patch in loadedPatchesInView)
            {
				if (patch is GridPatch)
				{
					var gridPatch = patch as GridPatch;
					MinVisibleValue = Mathf.Min(MinVisibleValue, gridPatch.grid.minValue);
					MaxVisibleValue = Mathf.Max(MaxVisibleValue, gridPatch.grid.maxValue);
				}
			}

			UpdateMapLayersVisibleRange();
		}
	}

	private void UpdateMapLayersVisibleRange()
	{
		foreach (var patch in loadedPatchesInView)
		{
			if (patch is GridPatch)
			{
				(patch.GetMapLayer() as GridMapLayer).UpdateRange();
			}
		}
	}

	private void InitVisibleRange()
    {
        MinVisibleValue = float.MaxValue;
        MaxVisibleValue = float.MinValue;
	}

	private void RenamePatches(string newName)
	{
#if SAFETY_CHECK
		if (newName == Name)
		{
			Debug.LogWarning("Trying to rename files with the same name");
			return;
		}
#endif

		var oldValue = Path.DirectorySeparatorChar + Name + "_";
		var newValue = Path.DirectorySeparatorChar + newName + "_";
		foreach (var level in levels)
		{
			foreach (var layerSite in level.layerSites)
			{
				foreach (var record in layerSite.records)
				{
					foreach (var patch in record.Value.patches)
					{
						if (patch.Filename != null)
						{
							patch.RenameFile(patch.Filename.Replace(oldValue, newValue));
						}
					}
				}
			}
		}
	}

}
