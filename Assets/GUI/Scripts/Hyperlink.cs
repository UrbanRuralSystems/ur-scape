// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Hyperlink : MonoBehaviour
{
	public Button button;
	public Text text;
	public string link;

	private void Start()
	{
		if (button != null)
		{
#if UNITY_WEBGL && !UNITY_EDITOR
			var trigger = button.GetComponent<EventTrigger>();
			if (trigger == null)
				trigger = button.gameObject.AddComponent<EventTrigger>();

			var entry = new EventTrigger.Entry();
			entry.eventID = EventTriggerType.PointerDown;
			entry.callback.AddListener((_) => OnClick());
			trigger.triggers.Add(entry);
#else
			button.onClick.AddListener(OnClick);
#endif
		}

	}

	private void OnClick()
	{
		string url = link;
		if (string.IsNullOrWhiteSpace(url) && text != null)
			url = text.text;

#if UNITY_WEBGL && !UNITY_EDITOR
		Web.OpenUrlInTab(url);
#else
		Application.OpenURL(url);
#endif
	}
}
