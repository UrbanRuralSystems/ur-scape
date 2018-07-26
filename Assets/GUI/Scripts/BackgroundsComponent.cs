// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class BackgroundsComponent : UrsComponent
{
    [Header("UI References")]
	public ToggleGroup backgroundsContainer;

	[Header("Prefabs")]
    public Toggle backgroundPrefab;

    private MapboxLayerController mapboxController;
    private int selectedIndex = -1;

    // Special background storage
    private Dictionary<string,GameObject> specialBackgrounds = new Dictionary<string, GameObject>(); 

    //
    // Unity Methods
    //

    private IEnumerator Start()
    {
        yield return WaitFor.Frames(WaitFor.InitialFrames);

        // Get Mapbox layer controller
       var map = ComponentManager.Instance.Get<MapController>();
        mapboxController = map.GetLayerController<MapboxLayerController>();
        int selectedMapboxIndex = mapboxController.BackgroundIndex;
        bool mapboxEnabled = selectedMapboxIndex >= 0;

        AddBackground(-1, "None", selectedMapboxIndex == -1);

		// Check if Mapbox is ready
		if (mapboxEnabled)
		{
			selectedIndex = selectedMapboxIndex;
			AddBackgrounds();
            AddSpecialBackgrounds();
        }
		else
		{
			mapboxController.OnReady += OnMapboxControllerReady;
		}

	}

	private void OnMapboxControllerReady()
    {
        int selected = selectedIndex == -1? mapboxController.BackgroundIndex + 1 : 0;

		AddBackgrounds();

		var container = backgroundsContainer.transform;
		var count = container.childCount;
		for (int i = 0; i < count; ++i)
        {
            var toggle = container.GetChild(i).GetComponent<Toggle>();
            if (i == selected)
                toggle.isOn = true;
        }
    }

    //
    // Inheritance Methods
    //

    public override bool HasBookmarkData()
    {
        return true;
    }

    public override void SaveToBookmark(BinaryWriter bw, string bookmarkPath)
    {
        bw.Write(selectedIndex);
    }

    public override void LoadFromBookmark(BinaryReader br, string bookmarkPath)
    {
        selectedIndex = br.ReadInt32();
        if (selectedIndex >= 0)
        {
			string selectedName = mapboxController.Backgrounds[selectedIndex].name;
            int count = backgroundsContainer.transform.childCount;
            for (int i = 0; i < count; i++)
            {
                var child = backgroundsContainer.transform.GetChild(i);
                if (child.name.Equals(selectedName))
                {
                    child.GetComponent<ToggleButton>().isOn = true;
                    break;
                }
            }
        }
    }
    // this can be called e.g. by prototype
    public void AddSpecialBackground(string name, Transform prefab)
    {
            specialBackgrounds.Add(name,prefab.gameObject);
    }

    //
    // Event Methods
    //
    private void OnSpecialBackgroundChanged(string name, bool show)
    {
        // if index is in special Layers use the object and ignore the rest
        if (specialBackgrounds.ContainsKey(name))
        {          
            specialBackgrounds[name].SetActive(show);
            return;
        }
    }

    private void OnBackgroundChanged(int index, bool show)
    {

        if (index >= 0)
        {
            if (show)
            {
                if (selectedIndex < 0)
                    mapboxController.ShowBackground(true);

                mapboxController.ChangeBackground(index);
            }
        }
        else
        {
            if (selectedIndex >= 0)
                mapboxController.ShowBackground(false);
        }

        if (show)
        {
            selectedIndex = index;
        }
    }


    //
    // Private Methods
    //

	private void AddBackgrounds()
	{
		var backgrounds = mapboxController.Backgrounds;
		for (int i = 0; i < backgrounds.Count; ++i)
		{
			AddBackground(i, backgrounds[i].name, i == selectedIndex);
		}       
    }

    private void AddSpecialBackgrounds()
    {
        if (specialBackgrounds.Count > 0)
        {
            // Get unique index as combination of mapbox bgs + special bgs + 1;
            foreach (var pair in specialBackgrounds)
            {
                // and index above mapbox 
                AddSpecialBackground(pair.Key, false);
            }
        }
    }

    private Toggle AddBackground(int index, string name, bool isOn)
    {
        var background = Instantiate(backgroundPrefab, backgroundsContainer.transform);
        background.isOn = isOn;
        background.name = name;
        background.group = backgroundsContainer;
        background.GetComponentInChildren<Text>().text = name;
        background.onValueChanged.AddListener((show) => OnBackgroundChanged(index, show));
        return background;
    }

    private Toggle AddSpecialBackground(string name, bool isOn)
    {
        var background = Instantiate(backgroundPrefab, backgroundsContainer.transform);
        background.isOn = isOn;
        background.name = name;
        background.group = backgroundsContainer;
        background.GetComponentInChildren<Text>().text = name;
        background.onValueChanged.AddListener((show) => OnSpecialBackgroundChanged(name, show));
        return background;
    }
}
