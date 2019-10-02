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

public class MultiTargetToggle : Toggle
{
	private Graphic[] graphics;

	protected override void OnEnable()
	{
		graphics = transform.GetComponentsInChildren<Graphic>();
		if (graphic != null)
		{
			int index = Array.IndexOf(graphics, graphic);
			graphics[index] = graphics[graphics.Length - 1];
			Array.Resize(ref graphics, graphics.Length - 1);
		}

		base.OnEnable();
	}

	protected override void DoStateTransition(SelectionState state, bool instant)
	{
		Color color;
		switch (state)
		{
			case SelectionState.Normal:
				color = colors.normalColor;
				break;
			case SelectionState.Highlighted:
				color = colors.highlightedColor;
				break;
			case SelectionState.Pressed:
				color = colors.pressedColor;
				break;
			case SelectionState.Disabled:
				color = colors.disabledColor;
				break;
			default:
				color = Color.black;
				break;
		}

		if (gameObject.activeInHierarchy && targetGraphic != null && transition == Transition.ColorTint)
		{
			color *= colors.colorMultiplier;
			foreach (var g in graphics)
			{
				g.CrossFadeColor(color, (!instant) ? colors.fadeDuration : 0f, true, true);
			}
			if (graphic != null && isOn)
				graphic.CrossFadeColor(color, (!instant) ? colors.fadeDuration : 0f, true, true);
		}
	}
}
