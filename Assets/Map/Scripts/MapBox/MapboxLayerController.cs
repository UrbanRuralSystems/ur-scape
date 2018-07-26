// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class MapboxBackground
{
	public string name;
	public string styleUrl;
}

public class MapboxLayerController : MapLayerControllerT<MapboxLayer>
{
	[Header("Settings")]
	public MapboxBackground defaultBackground;

	private int backgroundIndex = 0;
    public int BackgroundIndex { get { return string.IsNullOrEmpty(token)? -1 : backgroundIndex; } }

    private string token = null;
    private MapboxLayer backgroundLayer = null;

    public delegate void OnReadyDelegate();
    public event OnReadyDelegate OnReady;

	private List<MapboxBackground> backgrounds;
	public List<MapboxBackground> Backgrounds { get { return backgrounds; } }


	//
	// Unity Methods
	//

	private void Awake()
    {
        StartCoroutine(FileRequest.GetText(Paths.Data + "MapboxToken.txt", (sr) =>
        {
            token = sr.ReadLine();
			if (string.IsNullOrEmpty(token))
				Debug.LogWarning("Mapbox token is empty. Please paste your Mapbox token inside Data/MapboxToken.txt");
			FinishInit();
        }, () => Debug.LogError("Couldn't find Mapbox Token")));

		string backgroundsFile = Paths.Data + "backgrounds.csv";
		StartCoroutine(FileRequest.GetText(backgroundsFile, (sr) =>
		{
			backgrounds = Parse(sr);
			if (backgrounds.Count == 0)
			{
				CreateDefaultBackgrounds(backgroundsFile);
			}
			FinishInit();
		}, () =>
		{
			Debug.LogWarning("Couldn't find Mapbox backgrounds");
			CreateDefaultBackgrounds(backgroundsFile);
			FinishInit();
		}));
	}


	//
	// Inheritance Methods
	//

	public override void Init(MapController map)
    {
        base.Init(map);

		// Create background layer
		backgroundLayer = CreateLayer();

        FinishInit();
    }


    //
    // Public Methods
    //

    public void ChangeBackground(int newBackgroundIndex)
    {
        if (backgroundIndex != newBackgroundIndex)
        {
			if (backgrounds == null || backgrounds.Count == 0)
				return;

			if (newBackgroundIndex >= backgrounds.Count)
			{
				Debug.Log("Wrong background index " + newBackgroundIndex + ". Only " + backgrounds.Count + " available.");
			}

            backgroundIndex = Mathf.Clamp(newBackgroundIndex, 0, backgrounds.Count - 1);

            InitBackgroundLayer(backgrounds[backgroundIndex]);

            UpdateLayers();
        }
    }

    public void ShowBackground(bool show)
    {
        backgroundLayer.gameObject.SetActive(show);
    }


    //
    // Private Methods
    //

    private void FinishInit()
    {
		// Early out and wait for the token and backgrounds to arrive
		if (map == null || backgrounds == null || string.IsNullOrEmpty(token))
			return;

        // Add the background layer to the layer list
        mapLayers.Add(backgroundLayer);

        // Initialize the background
        InitBackgroundLayer(backgrounds[backgroundIndex]);

        if (OnReady != null)
            OnReady();
    }

    private void InitBackgroundLayer(MapboxBackground background)
    {
        backgroundLayer.gameObject.name = background.name;

        if (string.IsNullOrEmpty(token))
            return;

		string cachePath = Paths.Backgrounds + background.name + Path.DirectorySeparatorChar;

		backgroundLayer.Init(map, backgroundIndex, map.textureCache, new MapboxRequestGenerator(token, background.styleUrl, cachePath));
    }

    private MapboxLayer CreateLayer()
    {
        GameObject layerObj = new GameObject();
        layerObj.transform.SetParent(transform, false);
        return layerObj.AddComponent<MapboxLayer>();
    }

	private static List<MapboxBackground> Parse(StreamReader sr)
	{
		List<MapboxBackground> backgrounds = new List<MapboxBackground>();

		// Read/skip header
		string line = sr.ReadLine();

		while ((line = sr.ReadLine()) != null)
		{
			string[] cells = line.Split(',');
			if (cells.Length > 1 &&
				!string.IsNullOrEmpty(cells[0]) &&
				!string.IsNullOrEmpty(cells[1]))
			{
				var background = new MapboxBackground();
				background.name = cells[0];
				background.styleUrl = cells[1];

				backgrounds.Add(background);
			}
		}

		return backgrounds;
	}

	private void CreateDefaultBackgrounds(string filename)
	{
		backgrounds = new List<MapboxBackground>();
		backgrounds.Add(defaultBackground);

#if UNITY_STANDALONE
		using (var sw = new StreamWriter(File.Open(filename, FileMode.Create)))
		{
			sw.WriteLine("Name,StyleURL");
			sw.WriteLine(defaultBackground.name + "," + defaultBackground.styleUrl);
		}
#endif
	}
}
