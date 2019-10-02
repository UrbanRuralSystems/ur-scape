// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;

public class SiteBoundaryLayerController : MapLayerControllerT<AreaMapLayer>
{
	[Header("Prefabs")]
	public AreaMapLayer sitePrefab;

	[Header("Materials")]
	public Material defaultMaterial;
	public Material selectedMaterial;
	public Material highlightMaterial;

	private readonly Dictionary<Site, AreaMapLayer> siteToMapLayer = new Dictionary<Site, AreaMapLayer>();
	private AreaMapLayer selectedLayer;
	private Site highlightedSite;

	private bool showBoundaries = false;
	public bool IsShowingBoundaries { get { return showBoundaries; } }


	//
	// Unity Methods
	//

	protected void Awake()
	{
		ComponentManager.Instance.OnRegistrationFinished += OnRegistrationFinished;
	}


	//
	// Event Methods
	//

	private void OnRegistrationFinished()
	{
		var siteBrowser = ComponentManager.Instance.Get<SiteBrowser>();
		siteBrowser.OnAfterActiveSiteChange += OnFirstSiteSelection;
		siteBrowser.OnAfterActiveSiteChange += OnAfterActiveSiteChange;
	}

	private void OnFirstSiteSelection(Site site, Site previousSite)
	{
		var siteBrowser = ComponentManager.Instance.Get<SiteBrowser>();
		siteBrowser.OnAfterActiveSiteChange -= OnFirstSiteSelection;

		CreateSiteBoundaries();
	}

	private void OnAfterActiveSiteChange(Site site, Site previousSite)
	{
		SetSelected(site);
	}


	//
	// Public Methods
	//

	public AreaMapLayer Add(Site site, AreaBounds bounds)
	{
		AreaMapLayer layer = Instantiate(sitePrefab);
		layer.name = site.Name;
		layer.Init(map, bounds.north, bounds.east, bounds.south, bounds.west);
		layer.transform.SetParent(transform, false);
		mapLayers.Add(layer);
		siteToMapLayer.Add(site, layer);

		if (!showBoundaries && site != highlightedSite)
			layer.Show(false);

		return layer;
	}

    public void Remove(Site site)
    {
		AreaMapLayer mapLayer;
		if (siteToMapLayer.TryGetValue(site, out mapLayer))
		{
			mapLayers.Remove(mapLayer);
			siteToMapLayer.Remove(site);
			Destroy(mapLayer.gameObject);
		}
    }

    public void Clear()
    {
        foreach (var layer in mapLayers)
        {
            Destroy(layer.gameObject);
        }
        mapLayers.Clear();
		siteToMapLayer.Clear();

	}

	public void ShowBoundaries(bool show)
	{
		showBoundaries = show;
		foreach (var layer in mapLayers)
		{
			layer.Show(show);
		}
	}

	public void HighlightBoundary(Site site, bool show)
	{
		AreaMapLayer mapLayer;
		if (siteToMapLayer.TryGetValue(site, out mapLayer))
		{
			if (show)
			{
				if (!mapLayer.IsVisible())
					mapLayer.Show(true);
				SetMaterial(mapLayer, highlightMaterial);
			}
			else
			{
				var mat = mapLayer == selectedLayer ? selectedMaterial : defaultMaterial;
				SetMaterial(mapLayer, mat);

				if (!showBoundaries && mapLayer.IsVisible())
					mapLayer.Show(false);
			}
		}

		highlightedSite = show ? site : null;
	}


	//
	// Private Methods
	//

	private void SetSelected(Site site)
	{
		if (selectedLayer != null)
		{
			SetMaterial(selectedLayer, defaultMaterial);
			selectedLayer = null;
		}

		AreaMapLayer mapLayer;
		if (siteToMapLayer.TryGetValue(site, out mapLayer))
		{
			SetMaterial(mapLayer, selectedMaterial);
			selectedLayer = mapLayer;
		}
	}

	private void SetMaterial(MapLayer mapLayer, Material mat)
	{
		mapLayer.GetComponent<MeshRenderer>().material = mat;
	}

	private void CreateSiteBoundaries()
	{
		var componentManager = ComponentManager.Instance;
		var dataManager = componentManager.Get<DataManager>();
		var defaultSite = componentManager.Get<SiteBrowser>().DefaultSite;

		foreach (var site in dataManager.sites)
		{
			if (site != defaultSite)
			{
				Add(site, site.Bounds);
			}
		}
	}
}
