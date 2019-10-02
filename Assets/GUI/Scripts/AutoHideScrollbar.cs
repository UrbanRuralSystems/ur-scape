// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AutoHideScrollbar : MonoBehaviour
{
    private const float VISIBLE_SCROLL_TIME = 0.5f;
    private Coroutine scrollCheck = null;
    private float lastScrollTime;


	//
	// Unity Methods
	//

	private void OnEnable()
	{
		var scrollBar = GetComponent<Scrollbar>();
		if (scrollBar != null)
		{
			scrollBar.onValueChanged.AddListener(OnValueChanged);

			// Force show the scrollbar briefly if needed
			if (scrollBar.size > 0 && scrollBar.size < 0.999f)
				OnValueChanged(scrollBar.value);
		}
	}

	private void OnDisable()
	{
		var scrollBar = GetComponent<Scrollbar>();
		if (scrollBar != null)
		{
			scrollBar.onValueChanged.RemoveListener(OnValueChanged);

			if (scrollCheck != null)
			{
				StopCoroutine(scrollCheck);
				scrollCheck = null;
			}
		}
	}


	//
	// Event Methods
	//

	private void OnValueChanged(float value)
	{
		lastScrollTime = Time.time + VISIBLE_SCROLL_TIME;
		if (scrollCheck == null && isActiveAndEnabled && GetComponent<Scrollbar>().size < 0.999f)
		{
			ChangeNormalTransparency(1f);
			scrollCheck = StartCoroutine(RepeatingFunction());
		}
	}

    private void OnStopScrolling()
    {
        ChangeNormalTransparency(0f);
    }


	//
	// Event Methods
	//

    private IEnumerator RepeatingFunction()
    {
        while (lastScrollTime > Time.time)
        {
            yield return null;
        }
        OnStopScrolling();
        scrollCheck = null;
    }

    private void ChangeNormalTransparency(float alpha)
    {
        ColorBlock cb = GetComponent<Scrollbar>().colors;
        Color color = cb.normalColor;
        color.a = alpha;
        cb.normalColor = color;
        GetComponent<Scrollbar>().colors = cb;
    }

}
