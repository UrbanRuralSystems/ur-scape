// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          David Neudecker (neudecker@arch.ethz.ch)

//#define  USE_DEG_MIN_SEC

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScaleComponent : MonoBehaviour
{
	[Header("UI References")]
	public Text distanceText;
	public Text latitude;
	public Text longitude;
	public RectTransform scalePanel;
	public RectTransform compas;

	[Header("Sizes")]
	public int minSize = 50;
	public int maxSize = 125;

	[Header("Coordinates")]
	[Tooltip("Time to wait before updating values, measured in seconds")]
	[Range(0f, 2f)]
	public float updateInterval = 0.2f;
	private float nextUpdate;


	private MapController map;
	private MapCamera mapCamera;
	private InputHandler inputHandler;
	private RectTransform mapViewTransform;

	//- private bool waiting;

	private static readonly string[] coordsFormat =
	{
		"0.0",
		"0.00",
		"0.000",
		"0.0000",
		"0.00000"
	};


	//
	// Unity Methods
	//

	private IEnumerator Start()
	{
		// Avoid updates while loading
		nextUpdate = int.MaxValue;

		latitude.text = "";
		longitude.text = "";

		yield return WaitFor.Frames(WaitFor.InitialFrames);

		map = ComponentManager.Instance.Get<MapController>();
		mapCamera = ComponentManager.Instance.Get<MapCamera>();
		inputHandler = ComponentManager.Instance.Get<InputHandler>();
		var mapViewArea = ComponentManager.Instance.Get<MapViewArea>();
		if (mapViewArea != null)
			mapViewTransform = mapViewArea.transform as RectTransform;


		map.OnZoomChange += OnMapZoomChange;
		map.OnBoundsChange += OnBoundsChange;
		compas.GetComponent<Button>().onClick.AddListener(OnCompasClick);

		UpdateScale();

		nextUpdate = Time.time + updateInterval;
	}

	private void Update()
	{
		if (nextUpdate < Time.time)
		{
			nextUpdate += updateInterval;
			UpdateCoordinates();
		}
	}


	//
	// Event Methods
	//

	private void OnMapZoomChange(float zoom)
	{
		UpdateScale();
	}

	private void OnBoundsChange()
	{
		float cameraAngleY = WrapAngle(mapCamera.pivot.transform.rotation.eulerAngles.y);
		Quaternion quat = Quaternion.Euler(0, 0, cameraAngleY);
		compas.transform.rotation = quat;
	}

	private void OnCompasClick()
	{
		mapCamera.ResetRotation();
		OnBoundsChange();
	}


	//
	// Private Methods
	//

	private static float WrapAngle(float angle)
	{
		angle %= 360;
		if (angle > 180)
			return angle - 360;

		return angle;
	}

	private void UpdateScale()
	{
		float d1 = (float)GeoCalculator.PixelsToMetes(minSize, map.zoom);
		float d2 = (float)GeoCalculator.PixelsToMetes(maxSize, map.zoom);

		int number;
		string unit;
		float d = GetNumberAndUnit(d2, out number, out unit);

		distanceText.text = number + unit;

		var size = scalePanel.sizeDelta;
		size.x = Mathf.Lerp(minSize, maxSize, Mathf.InverseLerp(d1, d2, d));
		scalePanel.sizeDelta = size;
	}

	private void UpdateCoordinates()
	{
		// Check if mouse is within map view
		var localPos = mapViewTransform.InverseTransformPoint(Input.mousePosition);
		if (!mapViewTransform.rect.Contains(localPos))
		{
			latitude.text = "";
			longitude.text = "";
			return;
		}

		// Check if mouse is hitting something, and transform screen to world position
		Vector3 worldPos;
		if (!inputHandler.GetWorldPoint(Input.mousePosition, out worldPos))
		{
			latitude.text = "";
			longitude.text = "";
			return;
		}

		// Update coordinates
		Coordinate coords = map.GetCoordinatesFromUnits(worldPos.x, worldPos.z);

#if USE_DEG_MIN_SEC
		int latDeg = (int)coords.Latitude;
		int lonDeg = (int)coords.Longitude;
		double latDecimals = System.Math.Abs(coords.Latitude - latDeg);
		double lonDecimals = System.Math.Abs(coords.Longitude - lonDeg);
		int latMin = (int)(latDecimals * 60);
		int lonMin = (int)(lonDecimals * 60);
		float latSec = (float)((latDecimals - latMin / 60d) * 3600d);
		float lonSec = (float)((lonDecimals - lonMin / 60d) * 3600d);

		latitude.text = latDeg + "°" + latMin + "'" + latSec.ToString("00.00") + "\" N";
		longitude.text = lonDeg + "°" + lonMin + "'" + lonSec.ToString("00.00") + "\" E";
#else
		var format = coordsFormat[(int)(map.zoom * 0.24f)];
		latitude.text = coords.Latitude.ToString(format) + " N";
		longitude.text = coords.Longitude.ToString(format) + " E";
#endif
	}

	private static readonly int[] PowerOfTen = { 1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000 };

	private static float GetNumberAndUnit(float value, out int number, out string unit)
	{
		int count = 0;
		while (value >= 10f)
		{
			count++;
			value *= 0.1f;
		}

		if (value >= 5)
			number = 5;
		else if (value >= 2)
			number = 2;
		else
			number = 1;

		unit = " m";
		if (count > 2)
		{
			number *= PowerOfTen[count - 3];
			value = number * 1000;
			unit = " km";
		}
		else
		{
			number *= PowerOfTen[count];
		}
		return value;
	}

}
