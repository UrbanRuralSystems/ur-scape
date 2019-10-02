// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.Events;

public class ModalDialogManager : UrsComponent
{
	[Header("UI References")]
	public Transform background;
	public MapViewArea mapViewArea;
	public Canvas canvas;

	[Header("Prefabs")]
	public ProgressDialog progressPrefab;
	public PopupDialog popupPrefab;
	public GameObject webWelcomeMessagePrefab;

	private Vector2 offset;
	private int count;

	public bool HasVisibleDialogues => count > 1;

	//
	// Unity Methods
	//

	protected override void Awake()
	{
		base.Awake();

		count = transform.childCount;
		mapViewArea.OnMapViewAreaChange += OnMapViewAreaChange;
	}

	private void OnMapViewAreaChange()
	{
		UpdateOffset();
	}

	private void OnTransformChildrenChanged()
	{
		if (transform.childCount > count && count == 1)
		{
			background.gameObject.SetActive(true);
		}
		else if (transform.childCount < count && transform.childCount == 1)
		{
			background.gameObject.SetActive(false);
		}

		count = transform.childCount;

		if (count > 1)
			background.SetSiblingIndex(count - 2);
	}


	//
	// Public Methods
	//

	public PopupDialog NewPopupDialog()
	{
		return NewDialog(popupPrefab);
	}

#if UNITY_WEBGL
	public void ShowWebWelcomeMessage()
	{
		Instantiate(webWelcomeMessagePrefab, null, false);
	}
#endif

	public ProgressDialog NewProgressDialog()
	{
		return NewDialog(progressPrefab);
	}

	public T NewDialog<T>(T dialogPrefab) where T : Behaviour
	{
        return NewDialog(dialogPrefab.gameObject).GetComponent<T>();
	}

    public GameObject NewDialog(GameObject dialogPrefab)
    {
		var dialog = Instantiate(dialogPrefab);
		ShowDialog(dialog);
		return dialog;
	}

	public void ShowDialog(GameObject dialog)
	{
		UpdateOffset();
		dialog.transform.SetParent(transform, false);
		dialog.transform.localPosition = offset;
	}

	public void UpdateUI()
	{
		UpdateOffset();
	}

	public PopupDialog Ask(string question, UnityAction yes, UnityAction no = null)
	{
		var popup = NewPopupDialog();
		popup.ShowWarningQuestion(question);
		popup.OnCloseDialog += (result) =>
		{
			if (result.action == DialogAction.Yes)
				yes?.Invoke();
			else if (result.action == DialogAction.No)
				no?.Invoke();
		};
		return popup;
	}

	public PopupDialog Warn(string message, string title = null)
	{
		var popup = NewPopupDialog();
		popup.ShowWarningMessage(message, title);
		return popup;
	}


	//
	// Private Methods
	//

	private void UpdateOffset()
	{
		var invScaleFactor = 1f / canvas.scaleFactor;

		// Shift the top of the rect to always be below the title bar
		var backgroundOffset = mapViewArea.RectTransform.TransformPoint(0, mapViewArea.Rect.yMax, 0).y - Screen.height;
		var rt = transform as RectTransform;
		var o = rt.offsetMax;
		o.y = backgroundOffset * invScaleFactor;
		rt.offsetMax = o;

		// Adjust the vertical offset of the modal dialogs
		var newOffset = mapViewArea.WorldCenter();
		newOffset.x *= invScaleFactor;
		newOffset.y *= invScaleFactor;

		if (offset.x != newOffset.x || offset.y != newOffset.y)
		{
			offset = newOffset;
			for (int i = 0; i < count; i++)
			{
				var child = transform.GetChild(i);
				if (child != background)
					child.localPosition = offset;
			}
		}
	}
}
