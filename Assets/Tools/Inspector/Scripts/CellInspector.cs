// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class CellInspector : MonoBehaviour
{
	[Header("UI References")]
	public Toggle toggle;
	public CellInspectorPanel panel;

	//[Header("Prefabs")]

	[Header("Settings")]
	[Tooltip("How often the panel should be updated (in seconds)")]
	public float updateInterval = 0.1f;
	public float paddingTop;
	public float paddingRight;
	public float paddingBottom;
	public float paddingLeft;


	private MapViewArea mapViewArea;
	private InputHandler inputHandler;
	private DataLayers dataLayers;
	private bool hidePanel = false;
	private bool flippedY;
	private bool flippedX;
	private float scaleFactor;


	//
	// Unity Methods
	//

	private void Awake()
	{
		toggle.onValueChanged.AddListener(OnToggleChanged);
		toggle.interactable = false;

		ComponentManager.Instance.OnRegistrationFinished += OnRegistrationFinished;
	}

	private void OnRegistrationFinished()
	{
		mapViewArea = ComponentManager.Instance.Get<MapViewArea>();
		inputHandler = ComponentManager.Instance.Get<InputHandler>();
		dataLayers = ComponentManager.Instance.Get<DataLayers>();

		dataLayers.OnLayerAvailabilityChange += OnLayerAvailabilityChange;
	}

	private void Update()
	{
		bool isInsideOtherPanel = inputHandler.IsPointerInUI;
		if (!isInsideOtherPanel && !hidePanel && mapViewArea.IsMouseInside() && dataLayers.availableLayers.Count > 0)
		{
			if (!panel.gameObject.activeSelf)
				panel.gameObject.SetActive(true);

			UpdatePanelPosition();
			UpdatePanelValues();
		}
		else
		{
			if (panel.gameObject.activeSelf)
				panel.gameObject.SetActive(false);
		}
	}

	//
	// Event Methods
	//

	private void OnLayerAvailabilityChange(DataLayer layer, bool available)
	{
		if (available && dataLayers.availableLayers.Count == 1)
		{
			toggle.interactable = true;
			if (toggle.isOn)
			{
				// Fake toggling on
				OnToggleChanged(true);
			}
		}
		else if (!available && dataLayers.availableLayers.Count == 0)
		{
			toggle.interactable = false;
			if (toggle.isOn)
			{
				// Fake toggling of
				OnToggleChanged(false);
			}
		}
	}

	private void OnToggleChanged(bool isOn)
	{
		enabled = isOn;

		if (isOn)
		{
			var canvas = GameObject.FindWithTag("Canvas").GetComponent<Canvas>();
			scaleFactor = canvas.scaleFactor;
		}
		else
		{
			panel.gameObject.SetActive(false);
		}
	}


	//
	// Public Methods
	//

	public void HidePanel(bool hide)
	{
		hidePanel = hide;
	}


	//
	// Private Methods
	//

	private float nextUpdate;
	private void UpdatePanelValues()
	{
		if (Time.time < nextUpdate)
			return;

		nextUpdate = Time.time + updateInterval;

		panel.UpdateValues();
	}

	private void UpdatePanelPosition()
	{
		Vector3 inputPos = Input.mousePosition;
		Vector2 pos = inputPos;
		pos.x += paddingRight;
		pos.y -= paddingBottom;

		if (flippedY)
			pos.y -= 40;
		if (flippedX)
			pos.x += 40;

		Vector2 localMousePosition = mapViewArea.WorldToLocal(pos);

		if ((localMousePosition.y - panel.container.rect.height) < mapViewArea.Rect.yMin)
		{
			pos.y = inputPos.y + paddingTop * scaleFactor + panel.container.rect.height;
			flippedY = true;
		}
		else if (flippedY)
		{
			pos.y += 40;
			flippedY = false;
		}

		if ((localMousePosition.x + panel.container.rect.width) > mapViewArea.Rect.xMax)
		{
			pos.x = inputPos.x - paddingLeft * scaleFactor - panel.container.rect.width;
			flippedX = true;
		}
		else if (flippedX)
		{
			pos.x -= 40;
			flippedX = false;
		}

		panel.transform.position = pos;
	}
}
