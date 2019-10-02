// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class PressingEvents : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{

	public Action OnPressed;
	public Action OnPressing;
	public Action OnReleased;

	private Coroutine coroutine;

	//
	// Event Methods
	//

	public void OnPointerDown(PointerEventData eventData)
	{
		if (OnPressed != null)
			OnPressed();

		if (eventData.button == PointerEventData.InputButton.Left)
		{
			if (coroutine == null)
				coroutine = StartCoroutine(Pressing());
		}
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (OnReleased != null)
			OnReleased();

		if (eventData.button == PointerEventData.InputButton.Left)
		{
			StopCoroutine(coroutine);
			coroutine = null;
		}
	}

	//
	// Private Methods
	//

	private IEnumerator Pressing()
	{
		while (true)
		{
			yield return null;

			if (OnPressing != null)
				OnPressing();
		}
	}

}
