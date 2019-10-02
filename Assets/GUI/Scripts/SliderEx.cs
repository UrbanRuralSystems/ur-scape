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

public class SliderEx : Slider
{
    public delegate void OnClickDelegate();
    public event OnClickDelegate OnClick;

	public bool useCenter = false;

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);

        if (!eventData.dragging)
        {
            if (OnClick != null)
                OnClick();
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
	{
		base.OnValidate();

		if (wholeNumbers && useCenter && handleRect != null)
		{
			UpdateHandleRange();
		}
	}
#endif

    public void ChangeCount(int count)
	{
		maxValue = System.Math.Max(0, count - 1);
		UpdateHandleRange();
	}

	private void UpdateHandleRange()
	{
		float k = 0.5f / (1 + maxValue - minValue);
		var slideArea = handleRect.parent.GetComponent<RectTransform>();
		Vector2 offset = slideArea.anchorMin;
		offset.x = k;
		slideArea.anchorMin = offset;
		offset = slideArea.anchorMax;
		offset.x = 1f - k;
		slideArea.anchorMax = offset;
		//slideArea.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, )
	}

}
