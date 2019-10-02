// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          David Neudecker (neudecker@arch.ethz.ch)

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ZoomPanel : MonoBehaviour
{
	[Header("UI References")]
	public RectTransform compas;
	public PressingEvents plusButton;
	public PressingEvents minusButton;

	[Header("Options")]
	[Tooltip("Time to wait before updating values, measured in seconds")]
	[Range(0f, 2f)]
	public float updateInterval = 0.2f;
	private float nextUpdate;
	private float zoomingChange = 0;

	private MapController map;
	private MapCamera mapCamera;


	//
	// Unity Methods
	//

	private IEnumerator Start()
	{
		yield return WaitFor.Frames(WaitFor.InitialFrames);

		map = ComponentManager.Instance.Get<MapController>();
		mapCamera = ComponentManager.Instance.Get<MapCamera>();

		map.OnBoundsChange += OnBoundsChange;

        compas.GetComponent<Button>().onClick.AddListener(OnCompasClick);
		compas.gameObject.SetActive(false);

		plusButton.OnPressed += OnZoomInClick;
		plusButton.OnPressing += OnZooming;
		minusButton.OnPressed += OnZoomOutClick;
		minusButton.OnPressing += OnZooming;
	}


	//
	// Event Methods
	//

	private void OnZoomInClick()
	{
		zoomingChange = 0.1f;
		nextUpdate = Time.time + 0.25f;
		map.ChangeZoom(zoomingChange);
	}

	private void OnZoomOutClick()
	{
		zoomingChange = -0.1f;
		nextUpdate = Time.time + 0.25f;
		map.ChangeZoom(zoomingChange);
	}

	private void OnZooming()
	{
		if (Time.time > nextUpdate)
		{
			nextUpdate += updateInterval;
			map.ChangeZoom(zoomingChange);
		}
	}

	private void OnBoundsChange()
	{
		float cameraAngleY = WrapAngle(mapCamera.pivot.transform.rotation.eulerAngles.y);
		Quaternion quat = Quaternion.Euler(0, 0, cameraAngleY);
		compas.transform.rotation = quat;
		compas.gameObject.SetActive(cameraAngleY != 0.0f);
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

}
