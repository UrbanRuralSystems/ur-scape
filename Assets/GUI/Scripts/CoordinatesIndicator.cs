// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using UnityEngine;
using UnityEngine.UI;

public class CoordinatesIndicator : MonoBehaviour
{
	[Header("UI References")]
	public Text latitude;
	public Text longitude;

	[Header("Options")]
	[Tooltip("Time to wait before updating values, measured in seconds")]
	[Range(0.1f, 2.0f)]
	public float updateInterval = 0.2f;

	private MapController map;
	private InputHandler inputHandler;
	private MapViewArea mapViewArea;
	private float nextUpdate;
	private bool toDMS = true;

	private static readonly string[] coordsFormat =
	{
		"0.0",
		"0.00",
		"0.000",
		"0.0000",
		"0.00000",
		"0.000000",
		"0.0000000"
	};

	//
	// Unity Methods
	//

	private void Start()
	{
		ResetValues();

		ComponentManager.Instance.OnRegistrationFinished += OnComponentRegistrationFinished;

		var button = GetComponent<Button>();
		if (button != null)
			button.onClick.AddListener(OnButtonClick);

		// Avoid update until components are registered;
		nextUpdate = float.MaxValue;
	}

	private void Update()
	{
		if (Time.time > nextUpdate)
		{
			nextUpdate += updateInterval;

			UpdateValues();
		}
	}

	//
	// Events
	//

	private void OnComponentRegistrationFinished()
	{
		map = ComponentManager.Instance.Get<MapController>();
		inputHandler = ComponentManager.Instance.Get<InputHandler>();
		mapViewArea = ComponentManager.Instance.Get<MapViewArea>();

		nextUpdate = Time.time;
	}

	private void OnButtonClick()
	{
		toDMS = !toDMS;
	}

	//
	// Private Methods
	//

	private void UpdateValues()
	{
		// Check if mouse is within map view
		if (!mapViewArea.IsMouseInside())
		{
			ResetValues();
			return;
		}

		// Check if mouse is hitting something, and transform screen to world position
		if (!inputHandler.GetWorldPoint(Input.mousePosition, out Vector3 worldPos))
		{
			ResetValues();
			return;
		}

		// Update coordinates
		Coordinate coords = map.GetCoordinatesFromUnits(worldPos.x, worldPos.z);

		latitude.text = ToString(coords.Latitude, "N", "S");
		longitude.text = ToString(coords.Longitude, "E", "W");
	}

	private void ResetValues()
	{
		latitude.text = "";
		longitude.text = "";
	}

	private string ToString(double degrees, string positive, string negative)
	{
		if (toDMS)
		{
			return degrees.ToDMS(positive, negative);
		}
		else
		{
			var side = degrees < 0 ? negative : positive;
			var format = coordsFormat[(int)(map.zoom * 0.26f)];
			return degrees.ToString(format) + "° " + side;
		}
	}

}
