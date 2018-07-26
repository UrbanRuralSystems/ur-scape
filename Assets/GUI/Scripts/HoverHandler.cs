// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.EventSystems;

public class HoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public delegate void OnHoverDelegate(bool isHovering);
	public event OnHoverDelegate OnHover;

    public void OnPointerEnter(PointerEventData eventData)
	{
		if (OnHover != null)
			OnHover(true);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (OnHover != null)
			OnHover(false);
	}
}
