// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;

public class TypologyPanel : MonoBehaviour
{
	// Component References
    private RectTransform rectTransform;
	private PlanningTool planningTool;
	private HoverHandler hoverHandler;

	//
	// Unity Methods
	//

	private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
		planningTool = ComponentManager.Instance.Get<PlanningTool>();

		// Add hover event
		hoverHandler = GetComponent<HoverHandler>();
		hoverHandler.OnHover += OnPointerHover;
	}

	//
	// Private Methods
	//

	private bool IsMouseInside(RectTransform rt)
	{
		return rt.rect.Contains(rt.InverseTransformPoint(Input.mousePosition));
	}

	private void OnPointerHover(bool isHovering)
	{
		if (!isHovering && !IsMouseInside(rectTransform))
		{
			planningTool.ShowTypologyLabelAndCheckmark(false);
		}
		else
		{
			planningTool.ShowTypologyLabelAndCheckmark(true);
		}
	}
}