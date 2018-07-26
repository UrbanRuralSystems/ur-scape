// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using System;

public class LevelsComponent : MonoBehaviour
{
    [Header("UI References")]
    public Text panelTitle;
    public GameObject sitesPanel;
    public RectTransform sitesContainer;
    public ToggleGroup buttonsContainer;
    public Slider zoomSlider;
    public ButtonEx minusButton;
    public ButtonEx plusButton;

    [Header("Prefabs")]
    public Transform siteEntryPrefab;
    public Transform dataLayerEntryPrefab;
    public Transform levelsSeparatorPrefab;
    public ToggleButton levelTogglePrefab;

    private DataManager dataManager;
    private MapController map;

    private float updateTime;
    private bool preventUpdate;
    private Coroutine checkCoroutine;


    //
    // Unity Methods
    //

    void Awake()
    {
        dataManager = ComponentManager.Instance.Get<DataManager>();
        map = ComponentManager.Instance.Get<MapController>();

        map.OnZoomChange += OnMapZoomChange;
        zoomSlider.onValueChanged.AddListener(OnSliderDrag);
        OnMapZoomChange(map.zoom);
    }

    private void Start()
    {
        sitesPanel.SetActive(false);
        if (!UpdateLevels())
        {
            Invoke("UpdateLevels", 0.3f);
        }
    }

    private bool UpdateLevels()
    {
        int levelCount = map.dataLevels.levels.Length;
        float invZoomRange = 1f / (map.maxZoomLevel - map.minZoomLevel);
        float containerHeight = buttonsContainer.GetComponent<RectTransform>().rect.height - (levelCount + 1);

        if (containerHeight <= 0)
            return false;

        Instantiate(levelsSeparatorPrefab, buttonsContainer.transform, false);
        for (int i = 0; i < levelCount; ++i)
        {
            var level = map.dataLevels.levels[i];

            var toggle = Instantiate(levelTogglePrefab, buttonsContainer.transform, false);
            var rt = toggle.GetComponent<RectTransform>();

            int index = i;
            toggle.group = buttonsContainer;
            toggle.onValueChanged.AddListener((show) => OnLevelToggleChanged(index, show));
            toggle.name = "Toggle_" + level.name;

            var size = rt.sizeDelta;
            size.y = containerHeight * (level.maxZoom - level.minZoom) * invZoomRange;
            rt.sizeDelta = size;

            var label = rt.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = level.name;
            }

            Instantiate(levelsSeparatorPrefab, buttonsContainer.transform, false);
        }

        return true;
    }

    private void Update()
    {
        if (updateTime < Time.time)
        {
            updateTime += 0.05f;
            if (minusButton.Pressed)
                map.ChangeZoom(-0.1f);
            if (plusButton.Pressed)
                map.ChangeZoom(0.1f);
        }
    }


    //
    // Private Methods
    //

    private void OnLevelToggleChanged(int levelIndex, bool isOn)
    {
        sitesPanel.SetActive(isOn);

        if (isOn)
        {
            panelTitle.text = "Level " + map.dataLevels.levels[levelIndex].name + " Sites";
            UpdateSitesList(levelIndex);
            checkCoroutine = StartCoroutine(CheckCollapse());
        }
        else
        {
            StopCoroutine(checkCoroutine);
            int childsNum = sitesContainer.childCount;
            for (int i = 0; i < childsNum; ++i)
            {
                Destroy(sitesContainer.GetChild(i).gameObject);
            }
        }
    }

    private IEnumerator CheckCollapse()
    {
        while (sitesPanel.activeSelf)
        {
            yield return null;

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
            {
                var localPos = transform.InverseTransformPoint(Input.mousePosition);
                bool insideComponent = (transform as RectTransform).rect.Contains(localPos);
                if (!insideComponent)
                {
                    buttonsContainer.GetComponent<ToggleGroup>().SetAllTogglesOff();
                }
            }
        }
    }

    private void UpdateSitesList(int levelIndex)
    {
        var sites = new Dictionary<string, Transform>();
        foreach (var group in dataManager.Groups)
        {
            foreach (var layer in group.layers)
            {
                var level = layer.levels[levelIndex];
				foreach (var site in level.layerSites)
                {
                    Transform siteEntry = null;
                    string siteName = site.lastRecord.patches[0].name;
                    if (sites.ContainsKey(siteName))
                    {
                        siteEntry = sites[siteName];
                    }
                    else
                    {
                        siteEntry = Instantiate(siteEntryPrefab, sitesContainer, false);
                        Button newSiteButton = siteEntry.GetChild(0).GetComponent<Button>();
                        newSiteButton.transform.GetChild(0).GetComponent<Text>().text = siteName;
                        newSiteButton.onClick.AddListener(() => OnSiteButtonPressed(site));

                        sites.Add(siteName, siteEntry);
                    }

                    var dataLayerContainer = siteEntry as RectTransform;

                    var dataLayerEntry = Instantiate(dataLayerEntryPrefab, dataLayerContainer, false);
                    dataLayerEntry.GetComponentInChildren<Image>().color = layer.color;
                    dataLayerEntry.GetComponentInChildren<Text>().text = layer.name;

                    GuiUtils.RebuildLayout(dataLayerContainer as RectTransform);
                }
            }
        }
    }

    private void OnSiteButtonPressed(LayerSite site)
	{
		double east = float.MinValue;
		double west = float.MaxValue;
		double north = float.MinValue;
		double south = float.MaxValue;

		foreach (var patch in site.lastRecord.patches)
		{
			east = Math.Max(east, patch.Data.east);
			west = Math.Min(west, patch.Data.west);
			north = Math.Max(north, patch.Data.north);
			south = Math.Min(south, patch.Data.south);
		}

		map.SetZoom(map.dataLevels.levels[site.lastRecord.patches[0].level].minZoom);
        map.SetCenter((west + east) * 0.5, (north + south) * 0.5);
    }

    private void OnMapZoomChange(float zoom)
    {
        if (!preventUpdate)
        {
            preventUpdate = true;
            zoomSlider.value = Mathf.InverseLerp(map.minZoomLevel, map.maxZoomLevel, zoom);
            preventUpdate = false;
        }
    }

    private void OnSliderDrag(float value)
    {
        if (!preventUpdate)
        {
            preventUpdate = true;
            map.SetZoom(Mathf.Floor(Mathf.Lerp(map.minZoomLevel, map.maxZoomLevel, value) * 10f) * 0.1f);
            preventUpdate = false;
        }
    }

}
