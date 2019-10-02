// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;

public class MarkerContainer : MonoBehaviour
{
	public StartMarker markerPrefab;

	private MapController map;
	private MapCamera mapCamera;
	private readonly List<StartMarker> markers = new List<StartMarker>();

	public void Init()
	{
		map = ComponentManager.Instance.Get<MapController>();
		mapCamera = ComponentManager.Instance.Get<MapCamera>();

		map.OnMapUpdate += OnMapUpdate;
	}

	public void OnDestroy()
	{
		map.OnMapUpdate -= OnMapUpdate;
	}

	public void AddMarker(Coordinate coords)
	{
		StartMarker marker = Instantiate(markerPrefab, transform, true);
		marker.Init(coords);
		marker.UpdatePosition(map, mapCamera.transform.rotation);
		markers.Add(marker);
	}

	public void ClearMarkers()
	{
		foreach (var marker in markers)
		{
			Destroy(marker.gameObject);
		}

		markers.Clear();
	}

	private void OnMapUpdate()
	{
		UpdateMarkers();
	}

	private void UpdateMarkers()
	{
		var rotation = mapCamera.transform.rotation;
		foreach (var marker in markers)
		{
			marker.UpdatePosition(map, rotation);
		}
	}
}
