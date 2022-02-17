// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Graticule : MonoBehaviour
{
	[Serializable]
	private class GraticuleIcon
	{
		[SerializeField] private Image image = default;
		[SerializeField] private HoverHandler hoverHandler = default;
		[SerializeField] private Color colorOnHover = default;
		[SerializeField] private Color colorOffHover = default;

		public HoverHandler HoverHandlr { get { return hoverHandler; } }

		public void ToggleImageColor(bool onHover)
        {
			image.color = onHover ? colorOnHover : colorOffHover;
        }
	}

	[Header("UI References")]
	[SerializeField] private GraticuleIcon graticuleIcon = default;
	public Image graticuleImageBG;
	public Text dimensions;
	public GraticulePanel graticulePanel;

	[Header("Prefabs")]
	public GraticuleMapLayer graticuleLayerPrefab;

	private MapController map;
	private ToolLayerController toolLayers;
	private GraticuleMapLayer graticuleLayer;
	private Color graticuleImageColor;
	private bool panelEnabled;
	private bool isInitialized;

    private const char Degree = '\u00B0';

	//
	// Unity Methods
	//

	private void Awake()
	{
		graticuleImageColor = graticuleImageBG.color;
		graticuleIcon.HoverHandlr.OnHover += OnPointerHover;
	}

    private void Start()
    {
		graticulePanel.DegreesToggle.onValueChanged.AddListener(OnDegreesToggleChanged);
		graticulePanel.LockedToggle.onValueChanged.AddListener(OnLockedToggleChanged);
		graticulePanel.ShowGridToggle.onValueChanged.AddListener(OnShowGridToggleChanged);
		graticulePanel.IntervalX.onEndEdit.AddListener(OnIntervalChangedX);
		graticulePanel.IntervalY.onEndEdit.AddListener(OnIntervalChangedY);

		OnDegreesToggleChanged(false);
	}

    //
    // Event Methods
    //

	private void OnPointerHover(bool isHovering)
	{
		if (isHovering)
		{
			graticuleIcon.ToggleImageColor(true);
			ToggleGraticule(true);
		}
		else
		{
			if (panelEnabled)
				StartCoroutine(CheckIfPointerIsOutsidePanel());
		}
	}

	private void OnDegreesToggleChanged(bool isOn)
	{
		if (graticuleLayer != null)
		{
			float x = float.Parse(graticulePanel.IntervalX.text);
			float y = float.Parse(graticulePanel.IntervalY.text);
			graticuleLayer.SetInterval(x, y, graticulePanel.IsDegrees);
			graticuleLayer.UpdateContent();

			UpdateDimensionsText(isOn, x, y);
		}
	}

	private void OnLockedToggleChanged(bool isOn)
	{
		if (graticuleLayer != null)
			graticuleLayer.SetLocked(isOn);
	}

	private void OnShowGridToggleChanged(bool isOn)
	{
		if (graticuleLayer != null)
		{
			graticuleLayer.gameObject.SetActive(isOn);
			dimensions.gameObject.SetActive(isOn);
		}
	}

	private void OnIntervalChangedX(string text)
	{
		if (graticuleLayer != null)
        {
			float intervalX = float.Parse(text);
			graticuleLayer.SetInterval(intervalX, graticuleLayer.IntervalY, graticulePanel.IsDegrees);
			graticuleLayer.UpdateContent();

			UpdateDimensionsText(graticulePanel.IsDegrees, intervalX, graticuleLayer.IntervalY);

			// Save interval x value
			if (graticulePanel.IsDegrees)
				graticulePanel.PrevDegreesX = intervalX;
			else
				graticulePanel.PrevMetersX = intervalX;
		}
	}

	private void OnIntervalChangedY(string text)
	{
		if (graticuleLayer != null)
		{
			float intervalY = float.Parse(text);
			graticuleLayer.SetInterval(graticuleLayer.IntervalX, intervalY, graticulePanel.IsDegrees);
			graticuleLayer.UpdateContent();

			UpdateDimensionsText(graticulePanel.IsDegrees, graticuleLayer.IntervalX, intervalY);

			// Save interval y value
			if (graticulePanel.IsDegrees)
				graticulePanel.PrevDegreesY = intervalY;
			else
				graticulePanel.PrevMetersY = intervalY;
		}
	}

	//
	// Private Methods
	//

	private bool Init()
	{
		// Get Components
		map = ComponentManager.Instance.Get<MapController>();
		if (map == null)
		{
			FailedInit();
			return false;
		}

		// Get tools layer-controller
		toolLayers = map.GetLayerController<ToolLayerController>();
		if (toolLayers == null)
		{
			FailedInit();
			return false;
		}

		return true;
	}

	private void FailedInit()
	{
		Debug.LogError("Failed to init Graticule tool");
	}

	private void ToggleGraticule(bool isOn)
	{
		if (isOn)
		{
			if (!isInitialized)
			{
				isInitialized = Init();

				if (graticuleLayer == null)
					graticuleLayer = toolLayers.CreateMapLayer(graticuleLayerPrefab, "Graticule");

				OnShowGridToggleChanged(false);

				// Initialize graticule content with intervals
				graticulePanel.GetPrevIntervals(out float? intervalX, out float? intervalY);
				float x = intervalX ?? graticuleLayer.IntervalX;
				float y = intervalY ?? graticuleLayer.IntervalY;
				graticuleLayer.SetInterval(x, y, graticulePanel.IsDegrees);
				graticuleLayer.UpdateContent();
			}
			graticuleImageColor.a = 1.0f;
			graticulePanel.SetIntervalX(graticuleLayer.IntervalX);
			graticulePanel.SetIntervalY(graticuleLayer.IntervalY);

			UpdateDimensionsText(graticulePanel.IsDegrees, graticuleLayer.IntervalX, graticuleLayer.IntervalY);
		}
		else
			graticuleImageColor.a = 0.0f;

		graticuleImageBG.color = graticuleImageColor;
		graticulePanel.gameObject.SetActive(isOn);
		panelEnabled = isOn;
	}

	private void UpdateDimensionsText(bool isDegrees, float x, float y)
    {
		string numbers;
		float factor = isDegrees ? 1.0f : 0.001f;
		string units = isDegrees ? Degree.ToString() : $"km";

		numbers = Equals(x, y) ? $"{x * factor}" : $"{x * factor} x {y * factor}";
		dimensions.text = $"{numbers} {units}";
	}

	private IEnumerator CheckIfPointerIsOutsidePanel()
	{
		do
		{
			if (graticulePanel.IsMousePosOutsideGraticulePanel())
			{
				graticuleIcon.ToggleImageColor(false);
				ToggleGraticule(false);
				break;
			}
			yield return null;
		}
		while (true);
	}

}
