// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;

public class SiteBrowser : UrsComponent
{
	[Header("Prefabs")]
	public Toggle siteGroupPrefab;

	[Header("UI References")]
	public Transform sitesContainer;
	public GameObject message;
	public Toggle visibilityToggle;

	[Header("Settings")]
	public string defaultSiteName = "World";

	[Header("Selected Font Settings")]
	public Font font;

	public event UnityAction<Site, Site> OnBeforeActiveSiteChange;
	public event UnityAction<Site, Site> OnAfterActiveSiteChange;

    private Site activeSite;
	private Toggle selectedToggle;
	private Font originalFont;

	private Site defaultSite;
	public Site DefaultSite { get { return defaultSite; } }

	public Site ActiveSite { get { return activeSite; } }

	private DataManager dataManager;
	private MapController map;

	private readonly Dictionary<Site, Toggle> siteToToggle = new Dictionary<Site, Toggle>();

	//
	// Unity Methods
	//

	private IEnumerator Start()
	{
		yield return WaitFor.Frames(WaitFor.InitialFrames);

		originalFont = siteGroupPrefab.GetComponentInChildren<Text>().font;

        dataManager = ComponentManager.Instance.Get<DataManager>();
		map = ComponentManager.Instance.Get<MapController>();

		dataManager.OnDataLoaded += OnDataLoaded;
		visibilityToggle.onValueChanged.AddListener(OnVisibilityChanged);
	}


	//
	// Event Methods
	//

	private void OnDataLoaded()
	{
		dataManager.OnDataLoaded -= OnDataLoaded;

		BuildSitesList();

		if (defaultSite != null)
		{
			SetActiveSite(defaultSite);
		}
		else
		{
			// Couldn't find default site, try to load the first one on the list
			if (dataManager.sites.Count > 0)
			{
				Debug.LogWarning("Couldn't find default site " + defaultSiteName);
				SetActiveSite(dataManager.sites[0]);
			}
			else
			{
				Debug.LogWarning("No sites were found!");
			}
		}
	}

	private void OnSiteButtonPressed(Site site, bool isOn)
	{
		if (isOn)
		{
			SetActiveSite(site);
		}
	}

	private void OnVisibilityChanged(bool isOn)
	{
		map.GetLayerController<SiteBoundaryLayerController>().ShowBoundaries(isOn);
	}



	//
	// Public Methods
	//

	public void ChangeActiveSite(string siteName)
	{
		if (dataManager.HasSite(siteName))
		{
			ChangeActiveSite(dataManager.GetSite(siteName));
		}
		else
		{
			Debug.LogError("Can't find site " + siteName);
		}
    }

	public void ChangeActiveSite(Site site)
	{
		Toggle siteToggle;
		if (!siteToToggle.TryGetValue(site, out siteToggle))
			return;

		siteToggle.isOn = true;
	}

	private void SetActiveSite(Site site)
	{
		if (selectedToggle != null)
		{
			selectedToggle.GetComponentInChildren<Text>().font = originalFont;
			selectedToggle = null;
		}

		Toggle siteToggle;
		if (!siteToToggle.TryGetValue(site, out siteToggle))
			return;

		selectedToggle = siteToggle;
		selectedToggle.GetComponentInChildren<Text>().font = font;

		var previousSite = activeSite;
		activeSite = site;

		if (OnBeforeActiveSiteChange != null)
			OnBeforeActiveSiteChange(site, previousSite);

		GoToSite(site);

		UpdateMinMaxLevels(site);

		if (OnAfterActiveSiteChange != null)
			OnAfterActiveSiteChange(site, previousSite);
	}


	//
	// Private Methods
	//

	private void BuildSitesList()
	{
		var sites = dataManager.sites;
		if (sites.Count == 0)
		{
			message.SetActive(true);
		}
        else
		{
			var toggleGroup = GetComponent<ToggleGroup>();

			if (dataManager.HasSite(defaultSiteName))
			{
				defaultSite = dataManager.GetSite(defaultSiteName);
				AddSiteToList(defaultSite, toggleGroup);
			}

			// Sort the list alphabetically 
			sites.Sort((site1, site2) => site1.name.CompareTo(site2.name));

			foreach (var site in sites)
			{
				if (site != defaultSite)
					AddSiteToList(site, toggleGroup);
			}
		}

		GuiUtils.RebuildLayout(sitesContainer);
	}

	private void AddSiteToList(Site site, ToggleGroup toggleGroup)
	{
		var siteToggle = Instantiate(siteGroupPrefab);
		siteToggle.group = toggleGroup;
		siteToggle.transform.SetParent(sitesContainer, false);
		siteToggle.GetComponentInChildren<Text>().text = site.name;

		if (site == defaultSite)
			siteToggle.isOn = true;
		else
			siteToggle.isOn = false;

		siteToggle.onValueChanged.AddListener((isOn) => OnSiteButtonPressed(site, isOn));

		siteToToggle.Add(site, siteToggle);
	}

	private void UpdateMinMaxLevels(Site site)
	{
		int min = map.dataLevels.levels.Length - 1;
		int max = 0;

		foreach (var layer in site.dataLayers)
		{
			int count = layer.levels.Length;
			for (int l = 0; l < count; ++l)
			{
				foreach (var layerSite in layer.levels[l].layerSites)
				{
					if (layerSite.site == site)
					{
						max = Math.Max(max, l);
						min = Math.Min(min, l);
					}
				}
			}
		}

		map.SetMinMaxLevels(min, max);
	}

	private void GoToSite(Site site)
	{
		var bounds = site.bounds;
		if (bounds.east > bounds.west && bounds.north > bounds.south)
		{
			var max = GeoCalculator.LonLatToMeters(bounds.east, bounds.north);
			var min = GeoCalculator.LonLatToMeters(bounds.west, bounds.south);
			var center = GeoCalculator.MetersToLonLat((min.x + max.x) * 0.5, (min.y + max.y) * 0.5);

			map.SetCenter(center.Longitude, center.Latitude);
			map.ZoomToBounds(bounds, site != defaultSite);
		}
		else
		{
			map.SetCenter(0, 0);
			map.SetZoom(map.minZoomLevel);
		}
    }

}
