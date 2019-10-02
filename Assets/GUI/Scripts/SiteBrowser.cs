// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
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
	public Toggle siteTogglePrefab;

	[Header("UI References")]
	public Transform sitesContainer;
	public GameObject message;

	[Header("Settings")]
	public string defaultSiteName = "World";

	public event UnityAction<Site, Site> OnBeforeActiveSiteChange;
	public event UnityAction<Site, Site> OnAfterActiveSiteChange;

	private Toggle selectedToggle;

	public Site DefaultSite { get; private set; }
    public Site ActiveSite { get; private set; }

	private DataManager dataManager;
	private MapController map;
	private SiteBoundaryLayerController boundariesController;

	private readonly Dictionary<Site, Toggle> siteToToggle = new Dictionary<Site, Toggle>();

	//
	// Unity Methods
	//

	private IEnumerator Start()
	{
		yield return WaitFor.Frames(WaitFor.InitialFrames);

        dataManager = ComponentManager.Instance.Get<DataManager>();
		map = ComponentManager.Instance.Get<MapController>();
		boundariesController = map.GetLayerController<SiteBoundaryLayerController>();

		dataManager.OnDataLoaded += OnDataLoaded;
	}


	//
	// Event Methods
	//

	private void OnDataLoaded()
	{
		dataManager.OnDataLoaded -= OnDataLoaded;

		BuildSitesList();
		ActivateDefaultSite();
	}

	private void OnSiteToggleHover(Site site, bool isHovering)
	{
		boundariesController.HighlightBoundary(site, isHovering);
	}

	private void OnSiteTogglePressed(Site site, bool isOn)
	{
		if (isOn)
		{
			SetActiveSite(site);
		}
	}


	//
	// Public Methods
	//

	public void ChangeActiveSite(string siteName)
	{
		if (dataManager.TryGetSite(siteName, out Site site))
		{
			ChangeActiveSite(site);
		}
		else
		{
			Debug.LogError("Can't find site " + siteName);
		}
    }

	public void ChangeActiveSite(Site site)
	{
		if (!siteToToggle.TryGetValue(site, out Toggle siteToggle))
			return;

		siteToggle.isOn = true;
	}

	public void UpdateMinMaxLevels()
	{
		if (ActiveSite != null)
			UpdateMinMaxLevels(ActiveSite);
	}

	private void SetActiveSite(Site site)
	{
		if (selectedToggle != null)
		{
            selectedToggle.GetComponentInChildren<Text>().fontStyle = FontStyle.Normal;
            selectedToggle = null;
		}

		if (!siteToToggle.TryGetValue(site, out Toggle siteToggle))
			return;

		selectedToggle = siteToggle;
        selectedToggle.GetComponentInChildren<Text>().fontStyle = FontStyle.Bold;

        var previousSite = ActiveSite;
        ActiveSite = site;

		if (site != previousSite)
		{
			dataManager.ChangeActiveSite(ActiveSite);

			if (OnBeforeActiveSiteChange != null)
				OnBeforeActiveSiteChange(site, previousSite);

		}

		UpdateMinMaxLevels(site);

		if (site != previousSite)
		{
			GoToSite(site);

			if (OnAfterActiveSiteChange != null)
				OnAfterActiveSiteChange(site, previousSite);
		}
	}

	public void RebuildList()
	{
		var oldActiveSite = ActiveSite;

		Clear();
		BuildSitesList();

		if (oldActiveSite != null && siteToToggle.ContainsKey(oldActiveSite))
		{
			ActiveSite = oldActiveSite;
			ChangeActiveSite(oldActiveSite);
		}
		else
		{
			ActivateDefaultSite();
		}
	}


	//
	// Private Methods
	//

	private void Clear()
	{
		siteToToggle.Clear();

		DefaultSite = null;
		ActiveSite = null;
		selectedToggle = null;

		for (int i = sitesContainer.childCount - 1; i >= 0; i--)
		{
			Destroy(sitesContainer.GetChild(i).gameObject);
		}
	}

	private void BuildSitesList()
	{
		var sites = dataManager.sites;
		if (sites.Count == 0)
		{
			message.SetActive(true);
		}
        else
		{
			message.SetActive(false);

			var toggleGroup = GetComponent<ToggleGroup>();

			if (dataManager.TryGetSiteIgnoreCase(defaultSiteName, out Site defaultSite))
			{
                DefaultSite = defaultSite;
				AddSiteToList(DefaultSite, toggleGroup);
			}

			foreach (var site in sites)
			{
				if (site != DefaultSite)
					AddSiteToList(site, toggleGroup);
			}
		}

		GuiUtils.RebuildLayout(sitesContainer);
	}

	private void AddSiteToList(Site site, ToggleGroup toggleGroup)
	{
		var siteToggle = Instantiate(siteTogglePrefab);
		siteToggle.group = toggleGroup;
		siteToggle.transform.SetParent(sitesContainer, false);
		siteToggle.GetComponentInChildren<Text>().text = site.Name;
		siteToggle.GetComponent<HoverHandler>().OnHover += delegate (bool hover) { OnSiteToggleHover(site, hover); };
		siteToggle.onValueChanged.AddListener((isOn) => OnSiteTogglePressed(site, isOn));

		siteToToggle.Add(site, siteToggle);
	}

	private void ActivateDefaultSite()
	{
		if (DefaultSite != null)
		{
			ChangeActiveSite(DefaultSite);
		}
		else
		{
			// Couldn't find default site, try to load the first one on the list
			if (dataManager.sites.Count > 0)
			{
				ChangeActiveSite(dataManager.sites[0]);
			}
		}
	}

	private void UpdateMinMaxLevels(Site site)
	{
		int min = map.dataLevels.levels.Length - 1;
		int max = 0;

		foreach (var layer in site.layers)
		{
			int count = layer.levels.Length;
			for (int l = 0; l < count; ++l)
			{
				foreach (var layerSite in layer.levels[l].layerSites)
				{
					if (layerSite.Site == site)
					{
						max = Math.Max(max, l);
						min = Math.Min(min, l);
					}
				}
			}
		}

		map.SetMinMaxLevels(min, max, false);
	}

	private void GoToSite(Site site)
	{
		var bounds = site.Bounds;
		if (bounds.east > bounds.west && bounds.north > bounds.south)
		{
			map.ZoomToBounds(bounds);
		}
		else
		{
			map.SetCenter(0, 0);
			map.SetZoom(map.minZoomLevel);
		}
    }

}
