// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class AutoHideShadow : MonoBehaviour
{
	[Header("UI References")]
	public ScrollRect scrollRect;

	[Header("Settings")]
	public bool top;


	//
	// Unity Methods
	//

	private void Start()
	{
		// Hide by default and wait for the scrollrect to send the event
		gameObject.SetActive(false);

		if (top)
			scrollRect.onValueChanged.AddListener(OnScrollChangedTop);
		else
			scrollRect.onValueChanged.AddListener(OnScrollChangedBottom);
	}

	//
	// Event Methods
	//

	private void OnScrollChangedTop(Vector2 value)
	{
		if (scrollRect.verticalScrollbar.size == 1 || value.y > 0.999f && gameObject.activeSelf)
		{
			gameObject.SetActive(false);
		}
		else if (value.y <= 0.999f && !gameObject.activeSelf)
		{
			gameObject.SetActive(true);
		}
	}

	private void OnScrollChangedBottom(Vector2 value)
	{
		if (value.y <= 0.001f && gameObject.activeSelf)
		{
			gameObject.SetActive(false);
		}
		else if (value.y > 0.001f && !gameObject.activeSelf)
		{
			gameObject.SetActive(true);
		}
	}

}
