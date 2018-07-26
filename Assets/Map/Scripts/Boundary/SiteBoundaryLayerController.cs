// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
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

	private Transform selectedTransform;
	private Color originalColor;

	private readonly Dictionary<Site, AreaMapLayer> siteToMapLayer = new Dictionary<Site, AreaMapLayer>();
	private AreaMapLayer currentSelected;


	//
	// Unity Methods
	//

	protected void Awake()
	{
		ComponentManager.Instance.OnRegistrationFinished += OnRegistrationFinished;
	}

	/*private void Update()
	{
		Transform hitTransform = null;

		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		RaycastHit hitInfo;
		if (Physics.Raycast(ray, out hitInfo))
		{
			hitTransform = hitInfo.transform;
		}

		if (hitTransform != selectedTransform)
		{
			if (selectedTransform != null)
			{
				selectedTransform.GetComponent<MeshRenderer>().material.color = originalColor;
			}

			selectedTransform = hitTransform;

			if (selectedTransform != null)
			{
				if (hitTransform.localScale.x > 0.09f && hitTransform.localScale.x < 2)
				{
					originalColor = selectedTransform.GetComponent<MeshRenderer>().material.color;
					selectedTransform.GetComponent<MeshRenderer>().material.color = originalColor * 1.3f;
				}
				else
				{
					selectedTransform = null;
				}
			}
		}
	}*/


	//
	// Event Methods
	//

	private void OnRegistrationFinished()
	{
		ComponentManager.Instance.OnRegistrationFinished -= OnRegistrationFinished;

		var siteBrowser = ComponentManager.Instance.Get<SiteBrowser>();
		siteBrowser.OnAfterActiveSiteChange += OnFirstSiteSelection;
		siteBrowser.OnAfterActiveSiteChange += OnAfterActiveSiteChange;
	}

	private void OnFirstSiteSelection(Site site, Site previousSite)
	{
		var siteBrowser = ComponentManager.Instance.Get<SiteBrowser>();
		siteBrowser.OnAfterActiveSiteChange -= OnFirstSiteSelection;

		CreateSiteBondaries();
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
		layer.name = site.name;
		layer.Init(map, bounds.north, bounds.east, bounds.south, bounds.west);
		layer.transform.SetParent(transform, false);
		mapLayers.Add(layer);
		siteToMapLayer.Add(site, layer);
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
		gameObject.SetActive(show);
	}


	//
	// Private Methods
	//

	private void SetSelected(Site site)
	{
		if (currentSelected != null)
		{
			currentSelected.GetComponent<MeshRenderer>().material = defaultMaterial;
			currentSelected = null;
		}

		AreaMapLayer mapLayer;
		if (siteToMapLayer.TryGetValue(site, out mapLayer))
		{
			mapLayer.GetComponent<MeshRenderer>().material = selectedMaterial;
			currentSelected = mapLayer;
		}
	}

	private void CreateSiteBondaries()
	{
		var dataManager = ComponentManager.Instance.Get<DataManager>();
		var defaultSite = ComponentManager.Instance.Get<SiteBrowser>().DefaultSite;
		foreach (var site in dataManager.sites)
		{
			if (site != defaultSite)
			{
				Add(site, site.bounds);
			}
		}
	}
}
