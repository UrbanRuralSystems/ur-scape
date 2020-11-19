// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
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

#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

public class DataManager : UrsComponent
{
	[Header("Debug")]
	public bool findUnusedLayers;
	public bool findUnusedPatches;

	// Component references
	private DataLayers dataLayers;
    private MapController map;
	private ModalDialogManager dialogManager;
	private ProgressDialog progressDialog;
    private LoadingComponent loadingComponent;

    public readonly List<LayerGroup> groups = new List<LayerGroup>();

	public readonly List<Site> sites = new List<Site>();
	private readonly Dictionary<string, Site> nameToSite = new Dictionary<string, Site>();

    private List<GridMapLayer> visibleGrids = new List<GridMapLayer>();
    private Dictionary<Patch, PatchRequest> requestedPatches = new Dictionary<Patch, PatchRequest>();

    private GridLayerController gridLayers;

    private bool showFilteredDataOnly = true;
    private int maxRequests;

	public event UnityAction OnDataLoaded;

	public Site ActiveSite { get; private set; }


	//
	// Unity Methods
	//

	private IEnumerator Start()
    {
		dialogManager = FindObjectOfType<ModalDialogManager>();

		progressDialog = dialogManager.NewProgressDialog();
		progressDialog.SetMessage(Translator.Get("Loading") + " ...");
		progressDialog.SetProgress(0);

#if UNITY_WEBGL
		// Show welcome screen for online version
		dialogManager.ShowWebWelcomeMessage();
#endif

		yield return WaitFor.Frames(WaitFor.InitialFrames - 1);     // -1 to initialize before all other components

        // Mandatory components
        var componentManager = ComponentManager.Instance;
        dataLayers = componentManager.Get<DataLayers>();
        loadingComponent = componentManager.Get<LoadingComponent>();
        map = componentManager.Get<MapController>();

		dialogManager.UpdateUI();

		InitMapControllers();

		// Load config (groups & layers)
		StartCoroutine(Init());
	}


    //
    // Public Methods
    //

    public void ShowOnlyFilteredData(bool show)
    {
        if (showFilteredDataOnly != show)
        {
            showFilteredDataOnly = show;

            foreach (var g in visibleGrids)
            {
                g.EnableFilters(show);
            }
        }
    }

	public void ChangeActiveSite(Site site)
	{
		ActiveSite = site;
	}

	public bool HasSite(string siteName) => nameToSite.ContainsKey(siteName);
	public Site GetSite(string siteName) => nameToSite[siteName];
	public bool TryGetSite(string siteName, out Site site) => nameToSite.TryGetValue(siteName, out site);
	public bool TryGetSiteIgnoreCase(string siteName, out Site site)
	{
		if (TryGetSite(siteName, out site))
			return true;

		foreach (var pair in nameToSite)
		{
			if (pair.Key.EqualsIgnoreCase(siteName))
			{
				site = pair.Value;
				return true;
			}
		}
		site = null;
		return false;
	}

	public Site GetOrAddSite(string siteName)
	{
		if (!TryGetSite(siteName, out Site site))
			site = AddSite(siteName);
		return site;
	}

	public void RemoveSite(Site site)
	{
		nameToSite.Remove(site.Name);
		sites.Remove(site);
	}

	public void RenameSite(Site site, string newSiteName)
	{
		if (nameToSite.ContainsKey(newSiteName))
		{
			Debug.LogError("Can't rename site " + site.Name + ". New site name already exists: " + newSiteName);
			return;
		}

		nameToSite.Remove(site.Name);
		nameToSite.Add(newSiteName, site);

		site.ChangeName(newSiteName);
	}

	public void RemoveEmptySites()
	{
		for (int i = sites.Count - 1; i >= 0; --i)
		{
			if (sites[i].layers.Count == 0)
			{
				nameToSite.Remove(sites[i].Name);
				sites.RemoveAt(i);
			}
		}
	}

	public LayerGroup AddLayerGroup(string groupName)
	{
		var group = new LayerGroup(groupName);
		groups.Add(group);
		return group;
	}

	public void RemoveLayerGroup(LayerGroup group)
	{
		group.layers.Clear();
		groups.Remove(group);
	}

	public bool HasLayerGroup(string groupName)
	{
		return GetLayerGroup(groupName) != null;
	}

	public LayerGroup GetLayerGroup(string groupName)
	{
		return groups.Find((g) => g.name.EqualsIgnoreCase(groupName));
	}

	public int IndexOf(LayerGroup group)
	{
		return groups.IndexOf(group);
	}

#if !UNITY_WEBGL
	public List<string> GetDataDirectories()
	{
		List<string> dataDirs = new List<string>();
		int index = Paths.Sites.Length;
		var dirs = Directory.GetDirectories(Paths.Sites);
		foreach (var dir in dirs)
		{
			// Ignore site directories that start with an underscore
			if (dir[index] != '_')
				dataDirs.Add(dir);
		}
		return dataDirs;
	}
#endif

	// Called from DataLayer. Don't call directly
	public void ShowPatch(Patch patch)
	{
		if (patch.Data is GridData)
		{
			CreateGridMapLayer(patch.Data as GridData);

			// Enable these lines to show the patch id in the GameObject's name
			//Patch.SplitFileName(patch.Filename, out _, out _, out int patchId, out _, out _, out _, out _);
			//visibleGrids[visibleGrids.Count - 1].name += "_" + patchId;
		}
		else if (patch.Data is MultiGridData)
		{
			CreateMultiGridMapLayer(patch.Data as MultiGridData);
		}

		RemoveFromCache(patch);
	}

	// Called from DataLayer. Don't call directly
	public void HidePatch(Patch patch)
	{
		CancelRequest(patch);

		// Add it to cache
		if (patch.Data.IsLoaded())
			AddToCache(patch);

		// Remove map layer
		if (patch is GridedPatch)
		{
			var gridMapLayer = patch.GetMapLayer() as GridMapLayer;
			if (gridMapLayer != null)
			{
				gridLayers.Remove(gridMapLayer);
				visibleGrids.Remove(gridMapLayer);
			}
			patch.SetMapLayer(null);
		}
		else if (patch is MultiGridPatch)
		{
			var multigrid = patch as MultiGridPatch;
			var mapLayers = new List<GridMapLayer>(multigrid.GetMapLayers());
			foreach (var mapLayer in mapLayers)
			{
				gridLayers.Remove(mapLayer);
				visibleGrids.Remove(mapLayer);
			}
			multigrid.ClearMapLayers();
		}
	}

	public bool IsRequesting(Patch patch)
	{
		return requestedPatches.ContainsKey(patch);
	}

	public void RequestPatch(Patch patch, PatchLoadRequestCallback callback)
	{
#if SAFETY_CHECK
		if (requestedPatches.ContainsKey(patch))
		{
			Debug.LogWarning("This shouldn't happen: this patch is already being requested: " + patch.Filename);
			return;
		}
#endif

		var request = new PatchRequest(patch, (r) => RequestFinished(r, callback));
		requestedPatches.Add(patch, request);

		// Queue the patch request
		map.patchRequestHandler.Add(request);

		UpdatePatchLoadingMessage();
		if (map.patchRequestHandler.TotalCount == 1)
			loadingComponent.Show(true);
	}

	public void UpdateLayerConfig()
	{
#if !UNITY_WEBGL
		LayerConfig.Save(groups);
#endif
	}

	public void ReloadPatches(DataLayer layer, string siteName, int level, int year)
	{
		var site = GetSite(siteName);
		layer.RemovePatches(site, level, year);
		ClearPatchCache();

		if (dataLayers.IsLayerActive(layer))
		{
			layer.UpdatePatches(ActiveSite, map.CurrentLevel, map.MapCoordBounds);
		}
	}

	public void ClearPatchCache()
	{
		map.patchRequestHandler.cache.Clear();
	}


	//
	// Private Methods
	//

	private IEnumerator Init()
    {
		// Load groups
		yield return LayerConfig.Load((g) => groups.AddRange(g));

		// Rebuild data layer GUI
		yield return InitLayers();
	}

	private const float MaxProcessingTimePerFrame = 0.03f;
	private IEnumerator InitLayers()
    {
        if (dataLayers == null)
        {
			CloseProgressDialog();
			yield break;
        }

#if !UNITY_WEBGL
		if (!Directory.Exists(Paths.Sites))
		{
			Debug.LogError("Data path '" + Paths.Sites + "' doesn't exist. Trying to create it.");
			CloseProgressDialog();
			Directory.CreateDirectory(Paths.Sites);
			yield break;
		}
#endif


#if UNITY_WEBGL
		// Show progress message
		progressDialog.SetMessage(Translator.Get("Downloading data") + "...");
		progressDialog.SetProgress(0);

        yield return PatchDataIO.StartReadingHeaders();

		progressDialog.SetProgress(0.5f);
		yield return null;

		var paths = new List<string>();
		yield return FileRequest.GetText(Paths.DataWebDBPatches, (tr) =>
        {
			string line;
            while ((line = tr.ReadLine()) != null)
                paths.Add(line);
        });
#else
        // Find valid data directories
        var paths = GetDataDirectories();
#endif

		// Show progress message
        progressDialog.SetMessage(Translator.Get("Loading") + " ...");
        progressDialog.SetProgress(0);

        yield return null;

		// Count the layers
        int layerCount = 0;
		foreach (var group in groups)
			foreach (var layer in group.layers)
				layerCount++;

		// Rebuild the UI
		dataLayers.Show(false);

		// Create the patches/sites for each layer
		float index = 0;
        float time = 0;
        float invLayerCount = 1f / layerCount;
        var bounds = map.MapCoordBounds;
		foreach (var group in groups)
        {
			var groupController = dataLayers.AddLayerGroup(group.name);
			foreach (var layer in group.layers)
            {
				dataLayers.AddLayer(layer, groupController);

				List<string> patchFiles = null;
				var filesIt = layer.GetPatchFiles(paths, (pf) => patchFiles = pf);
				if (filesIt.MoveNext())
				{
					do { yield return null; } while (filesIt.MoveNext());
					time = Time.realtimeSinceStartup + MaxProcessingTimePerFrame;
				}
#if UNITY_EDITOR
				if (findUnusedLayers && patchFiles.Count == 0)
				{
					Debug.LogWarning("Layer " + layer.Name + " doesn't have any files.");
				}
#endif

				if (patchFiles.Count > 0)
                {
					int patchCount = 0;
                    float invPatchCount = 1f / patchFiles.Count;
                    var patches = layer.CreatePatches(patchFiles);
                    while (patches.MoveNext())
                    {
                        patchCount++;
						progressDialog.SetProgress((index + (patchCount * invPatchCount)) * invLayerCount);
						if (patches.RunThruChildren())
                        {
                            do
							{
								if (Time.realtimeSinceStartup > time)
								{
									yield return null;
									time = Time.realtimeSinceStartup + MaxProcessingTimePerFrame;
								}
							}
							while (patches.RunThru());
                        }
                        else if (Time.realtimeSinceStartup > time)
                        {
                            yield return null;
							time = Time.realtimeSinceStartup + MaxProcessingTimePerFrame;
                        }
                    }
                }
				else if (Time.realtimeSinceStartup > time)
				{
					yield return null;
					time = Time.realtimeSinceStartup + MaxProcessingTimePerFrame;
				}
				index++;
			}
		}

		PatchDataIO.StopParsingThread();

#if UNITY_EDITOR && !UNITY_WEBGL
		if (findUnusedPatches)
		{
			var extensions = new string[] { "*.csv", "*.bin" };
			var dirs = GetDataDirectories();
			foreach (var dir in dirs)
			{
				foreach (var ext in extensions)
				{
					var files = Directory.GetFiles(dir, ext);
					foreach (var file in files)
					{
						if (Path.GetFileName(file).StartsWith("_"))
							continue;
						var layerName = Patch.GetFileNameLayer(file);
						if (!dataLayers.HasLayer(layerName))
							Debug.LogWarning("Patch is being ignored: " + file);
					}
				}
			}
		}
#endif

		dataLayers.Show(true);
        yield return null;

        // Update DataLayers UI (activate those layers that have sites/patches within view bounds)
        dataLayers.UpdateLayers();

		// Hide Message
		CloseProgressDialog();

		// Clean up
#if UNITY_WEBGL
		PatchDataIO.FinishReadingHeaders();
#endif

		OnDataLoaded?.Invoke();
	}

	private void CloseProgressDialog()
	{
		progressDialog.Close();
		progressDialog = null;
	}

	private void InitMapControllers()
    {
        gridLayers = map.GetLayerController<GridLayerController>();
    }

    private void CreateGridMapLayer(GridData grid)
    {
        // Add map layer
        GridMapLayer mapLayer = gridLayers.Add(grid);
		visibleGrids.Add(mapLayer);

		mapLayer.EnableFilters(showFilteredDataOnly);
	}

	private void CreateMultiGridMapLayer(MultiGridData multigrid)
	{
		int count = multigrid.categories.Length;
		for (int i = 0; i < count; i++)
		{
			if (multigrid.gridFilter.IsSet(i))
			{
				CreateGridMapLayer(multigrid.categories[i].grid);

				// Change the map layer's color to the category's one (by default it will have the datalayer's color)
				visibleGrids[visibleGrids.Count - 1].SetColor(multigrid.categories[i].color);
			}
		}
	}

	private void AddToCache(Patch patch)
    {
		map.patchRequestHandler.cache.Add(patch);
	}

    private void RemoveFromCache(Patch patch)
    {
        // Remove it from the cache (if it's there)
        map.patchRequestHandler.cache.TryRemove(patch);
    }

	private void RequestFinished(ResourceRequest r, PatchLoadRequestCallback callback)
    {
        var request = r as PatchRequest;

        if (request.State == RequestState.Canceled)
		{
			// Canceled patch requests may still have a loaded patch. Add it to the cache since its no longer needed
			if (request.patch.Data.IsLoaded())
				AddToCache(request.patch);
		}
		else
        {
			requestedPatches.Remove(request.patch);

			if (request.State == RequestState.Succeeded)
			{
				var bounds = map.MapCoordBounds;
				var patch = request.patch;
				bool isInView = patch.Level == map.CurrentLevel && patch.Data.Intersects(bounds.west, bounds.east, bounds.north, bounds.south);
				callback(patch, isInView);
			}
#if SAFETY_CHECK
			else
            {
                Debug.LogError("Requested patch '" + request.file + "' finished with state: " + request.State);
                if (!string.IsNullOrEmpty(request.Error))
                {
                    Debug.LogError("Requesting error: " + request.Error);
                }
            }
#endif
        }

		UpdatePatchLoadingMessage();

		if (map.patchRequestHandler.TotalCount == 0)
		{
			maxRequests = 0;
			loadingComponent.Show(false);
		}
	}

	private void CancelRequest(Patch patch)
    {
        if (requestedPatches.TryGetValue(patch, out PatchRequest request))
        {
			requestedPatches.Remove(patch);
            map.patchRequestHandler.Cancel(request);

			if (map.patchRequestHandler.TotalCount == 0)
			{
				maxRequests = 0;
				loadingComponent.Show(false);
			}
		}
	}

	private void UpdatePatchLoadingMessage()
	{
		int currentRequests = requestedPatches.Count;
		maxRequests = Mathf.Max(maxRequests, currentRequests);
		int finished = maxRequests - currentRequests;

		loadingComponent.SetProgress((float)finished / maxRequests);

#if UNITY_EDITOR
		// This is only for debuging purposes. Non-editor builds will only see "Loading ..."
		loadingComponent.SetLabel(Translator.Get("Loading") + " ...  (" + finished + " / " + maxRequests + ")");
#endif
	}

	private Site AddSite(string siteName)
	{
		if (TryGetSite(siteName, out Site site))
		{
			Debug.LogWarning("Trying to add site that already exists: " + siteName);
			return site;
		}

		site = new Site(siteName);
		nameToSite.Add(siteName, site);
		sites.Add(site);

		// Sort the list alphabetically 
		sites.Sort((site1, site2) => site1.Name.CompareTo(site2.Name));

		return site;
	}

#if UNITY_EDITOR
	private class ReportSite
	{
		public readonly Dictionary<string, ReportLayer> layers = new Dictionary<string, ReportLayer>();
	}

	private class ReportLayer
	{
		public HashSet<string> sources = new HashSet<string>();
	}

	private static void Debug_GenerateReport()
	{
		var sites = new Dictionary<string, ReportSite>();
		var folders = Directory.GetDirectories(Path.Combine(Paths.Sites, "_all"));
		foreach (var folder in folders)
		{
			var files = Directory.GetFiles(folder, "*.csv");
			foreach (var file in files)
			{
				var layerName = Patch.SplitFileName(file, out _, out string siteName, out _, out _, out _, out _, out _);
				if (!sites.TryGetValue(siteName, out ReportSite site))
				{
					site = new ReportSite();
					sites.Add(siteName, site);
				}

				if (!site.layers.TryGetValue(layerName, out ReportLayer layer))
				{
					layer = new ReportLayer();
					site.layers.Add(layerName, layer);
				}

				using (var sr = new StreamReader(file))
				{
					for (int i = 0; i < 100; i++)
					{
						var line = sr.ReadLine();
						if (string.IsNullOrWhiteSpace(line))
							break;
						else if (line.StartsWithIgnoreCase("Source,"))
						{
							var source = line.Substring(7).Trim();
							if (!string.IsNullOrWhiteSpace(source))
								layer.sources.AddOnce(source);
						}
						else if (line.StartsWithIgnoreCase("Author,"))
						{
							var author = line.Substring(7).Trim();
							if (!string.IsNullOrWhiteSpace(author))
								layer.sources.AddOnce(author);
						}
					}
				}
			}
		}

		using (var sw = new StreamWriter("report.csv", false, System.Text.Encoding.Unicode))
		{
			var siteNames = new List<string>(sites.Keys);
			siteNames.Sort();
			foreach (var siteName in siteNames)
			{
				sw.WriteLine(siteName);

				var site = sites[siteName];

				var layerNames = new List<string>(site.layers.Keys);
				layerNames.Sort();
				foreach (var layerName in layerNames)
				{
					var layer = site.layers[layerName];
					sw.WriteLine("\t" + layerName + "\t\"" + string.Join(",", layer.sources) + "\"");
				}

				sw.WriteLine();
			}
		}
	}
#endif
}
