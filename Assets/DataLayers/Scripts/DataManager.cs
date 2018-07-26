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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_WEBGL
using System.Runtime.InteropServices;
#endif
using ExtensionMethods;

public class DataManager : UrsComponent
{
	// Component references
	private DataLayers dataLayers;
    private MapController map;
    private MessageController messageController;
    private LoadingComponent loadingComponent;

    private List<LayerGroup> groups = new List<LayerGroup>();
    public List<LayerGroup> Groups { get { return groups; } }

#if UNITY_WEBGL
	private List<string> allPatchFiles = new List<string>();
	public List<string> AllPatchFiles { get { return allPatchFiles; } }
#else
	public string[] DataDirs { get; private set; }
#endif


	public readonly List<Site> sites = new List<Site>();
	private Dictionary<string, Site> nameToSite = new Dictionary<string, Site>();

	private Dictionary<string, DataLayer> layers = new Dictionary<string, DataLayer>();
    private List<GridMapLayer> visibleGrids = new List<GridMapLayer>();
    private Dictionary<Patch, PatchRequest> requestedPatches = new Dictionary<Patch, PatchRequest>();

    private GridLayerController gridLayers;

    private bool showFilteredDataOnly = true;
    private int maxRequests;

	public event UnityAction OnDataLoaded;


	//
	// Unity Methods
	//

	private IEnumerator Start()
    {
		messageController = FindObjectOfType<MessageController>();
        if (messageController != null)
        {
            messageController.Show(true);
            messageController.SetMessage("Loading ...");
        }

        yield return WaitFor.Frames(WaitFor.InitialFrames - 1);     // -1 to initialize before all other components

        // Mandatory components
        var componentManager = ComponentManager.Instance;
        dataLayers = componentManager.Get<DataLayers>();
        loadingComponent = componentManager.Get<LoadingComponent>();
        map = componentManager.Get<MapController>();

        InitMapControllers();

        // Load config (groups & layers)
        StartCoroutine(Init());
	}


    //
    // Public Methods
    //

    public void EnableSiteFilters(bool enable)
    {
        if (showFilteredDataOnly != enable)
        {
            showFilteredDataOnly = enable;

            foreach (var g in visibleGrids)
            {
                g.EnableFilters(enable);
            }
        }
    }

	public bool HasSite(string siteName)
	{
		return nameToSite.ContainsKey(siteName);
	}

	public Site GetSite(string siteName)
	{
		return nameToSite[siteName];
	}

	public GridPatch CreateGridPatch(DataLayer layer, string name, int level, int year, GridData data)
	{
		var newPatch = layer.CreateGridPatch(name, level, year, data);

		foreach (var layerSite in layer.levels[newPatch.level].layerSites)
		{
			if (layerSite.name.Equals(newPatch.name))
			{
				layerSite.site = nameToSite[layerSite.name];
				break;
			}
		}
		return newPatch;
	}

	//
	// Private Methods
	//

	private IEnumerator Init()
    {
		// Load groups
		yield return LayerConfig.Load(Paths.Data + "layers.csv", (g) => groups = g);

		// Rebuild data layer GUI
		yield return InitLayers();
    }

    private IEnumerator InitLayers()
    {
        if (dataLayers == null)
        {
            messageController.Show(false);
            yield break;
        }

#if !UNITY_WEBGL
		if (!Directory.Exists(Paths.Sites))
		{
			Debug.LogError("Data path '" + Paths.Sites + "' doesn't exist");
			messageController.Show(false);
			yield break;
		}
#endif

		// Show Message
		messageController.Show(true);
        messageController.SetProgress(0);

#if UNITY_WEBGL
        messageController.SetMessage("Downloading database ...");

        yield return PatchDataIO.StartReadingHeaders();

		messageController.SetProgress(0.5f);
		yield return null;

		allPatchFiles.Clear();
		yield return FileRequest.GetText(Paths.DataWebDBPatches, (tr) =>
        {
			string line;
            while ((line = tr.ReadLine()) != null)
                allPatchFiles.Add(line);
        });
#else
		// Find valid data directories
		InitValidDataDirs();
#endif

		messageController.SetMessage("Initializing database ...");
        messageController.SetProgress(0);
		yield return null;

		// Rebuild the UI
		int groupCount = 0;
        int layerCount = 0;
        dataLayers.Show(false);
        foreach (var group in groups)
        {
            var groupController = dataLayers.AddLayerGroup(group.name);
            foreach (var layer in group.layers)
            {
                dataLayers.AddLayer(layer, groupController);
                layer.BuildFileList();
                layerCount++;
			}
			groupCount++;
        }
        dataLayers.Show(true);

		yield return null;

        // Create the patches/sites for each layer
        float index = 0;
        int counter = 0;
        float invLayerCount = 1f / layerCount;
        var bounds = map.MapCoordBounds;
        foreach (var group in groups)
        {
			foreach (var layer in group.layers)
            {
                if (layer.PatchCount > 0)
                {
					int patchCount = 0;
                    float invPatchCount = 1f / layer.PatchCount;
                    var patches = layer.CreatePatches();
                    while (patches.MoveNext())
                    {
                        patchCount++;
						messageController.SetProgress((index + (patchCount * invPatchCount)) * invLayerCount);
						while (patches.RunThruChildren())
                        {
                            counter = 0;
							yield return null;
						}
                        if (++counter % 50 == 0)
                        {
                            yield return null;
                        }
                    }
                }

#if SAFETY_CHECK
                if (layers.ContainsKey(layer.name))
                    Debug.LogError(layer.name + " layer already exists. Looks like a duplicate line in layers.csv");
                else
#endif
                    layers.Add(layer.name, layer);

                index++;
            }
        }

		CreateSitesList();

        // Update DataLayers UI (activate those layers that have sites/patches within view bounds)
        dataLayers.UpdateLayers();

        // Hide Message
        messageController.Show(false);

		// Clean up the cached files & directories
#if UNITY_WEBGL
		PatchDataIO.FinishReadingHeaders();
		allPatchFiles.Clear();
		allPatchFiles = null;
#else
		DataDirs = null;
#endif

		if (OnDataLoaded != null)
			OnDataLoaded();
	}

	private void InitMapControllers()
    {
        gridLayers = map.GetLayerController<GridLayerController>();
    }

    public void ShowPatch(Patch patch)
    {
		var gridData = patch.Data as GridData;
		if (gridData != null)
        {
			CreateGridMapLayer(gridData);
        }

		RemoveFromCache(patch);
	}

	public void HidePatch(Patch patch)
    {
        CancelRequest(patch);

		// Add it to cache
		if (patch.Data.IsLoaded())
			AddToCache(patch);

		// Remove map layer
		var mapLayer = patch.GetMapLayer() as GridMapLayer;
		if (mapLayer != null)
		{
			gridLayers.Remove(mapLayer);
			visibleGrids.Remove(mapLayer);
		}
		patch.SetMapLayer(null);
    }

    private void CreateGridMapLayer(GridData grid)
    {
        // Add map layer
        GridMapLayer mapLayer = gridLayers.Add(grid);
		visibleGrids.Add(mapLayer);

		mapLayer.EnableFilters(showFilteredDataOnly);
	}

	public void AddToCache(Patch patch)
    {
		map.patchRequestHandler.cache.Add(patch);
	}

    private void RemoveFromCache(Patch patch)
    {
        // Remove it from the cache (if it's there)
        map.patchRequestHandler.cache.TryRemove(patch);
    }

	public bool IsRequesting(Patch patch)
	{
		return requestedPatches.ContainsKey(patch);
	}

	public void RequestPatch(Patch patch, PatchLoadedCallback callback)
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

	private void RequestFinished(ResourceRequest r, PatchLoadedCallback callback)
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
				callback(request.patch);
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

    public void CancelRequest(Patch patch)
    {
        PatchRequest request;
        if (requestedPatches.TryGetValue(patch, out request))
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
		loadingComponent.SetLabel("          Loading ...  (" + finished + " / " + maxRequests + ")");
#endif
	}

#if !UNITY_WEBGL
	private void InitValidDataDirs()
	{
		List<string> dataDirs = new List<string>();
		int index = Paths.Sites.Length;
		var dirs = Directory.GetDirectories(Paths.Sites);
		foreach (var dir in dirs)
		{
			// Ignore directories that start with an underscore
			if (dir[index] != '_')
				dataDirs.Add(dir);
		}
		DataDirs = dataDirs.ToArray();
	}
#endif

	private void CreateSitesList()
	{
		var siteCreators = new Dictionary<string, SiteCreator>();

		foreach (var group in groups)
		{
			foreach (var layer in group.layers)
			{
				foreach (var level in layer.levels)
				{
					foreach (var layerSite in level.layerSites)
					{
						string siteName = layerSite.name;

						SiteCreator site;
						if (!siteCreators.TryGetValue(siteName, out site))
						{
							site = new SiteCreator();
							site.name = siteName;
							site.bounds = new AreaBounds(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue);
							siteCreators.Add(siteName, site);
						}

						foreach (var patch in layerSite.lastRecord.patches)
						{
							site.bounds.east = Math.Max(site.bounds.east, patch.Data.east);
							site.bounds.west = Math.Min(site.bounds.west, patch.Data.west);
							site.bounds.north = Math.Max(site.bounds.north, patch.Data.north);
							site.bounds.south = Math.Min(site.bounds.south, patch.Data.south);
						}

						if (!site.layers.Contains(layer))
						{
							site.layers.Add(layer);
						}

						site.layerSites.Add(layerSite);
					}
				}
			}
		}

		// Check if a site is inside another
		foreach (var site1 in siteCreators)
		{
			var b1 = site1.Value.bounds;

			double minArea = double.MaxValue;
			foreach (var site2 in siteCreators)
			{
				var b2 = site2.Value.bounds;

				// Is Site2 larger than Site1?
				if (b2.west < b1.west && b2.east > b1.east && b2.north > b1.north && b2.south < b1.south)
				{
					double area = (b2.east - b2.west) * (b2.north - b2.south);
					if (area < minArea)
					{
						minArea = area;
						site1.Value.parent = site2.Key;
					}
				}
			}
		}

		foreach (var creator in siteCreators.Values)
		{
			CreateSite(creator, siteCreators);
		}
	}

	private Site CreateSite(SiteCreator creator, Dictionary<string, SiteCreator> siteCreators)
	{
		Site site;

		if (nameToSite.TryGetValue(creator.name, out site))
			return site;

		Site parent = null;
		if (!string.IsNullOrEmpty(creator.parent))
		{
			parent = CreateSite(siteCreators[creator.parent], siteCreators);
		}

		site = new Site(creator.name, parent, creator.bounds, creator.layers);

		sites.Add(site);
		nameToSite.Add(site.name, site);

		foreach (var layerSite in creator.layerSites)
		{
			layerSite.site = site;
		}

		return site;
	}

}
