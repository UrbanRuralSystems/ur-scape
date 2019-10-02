// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class DistributionBar : MonoBehaviour
{
	[Header("UI References")]
	public Text percentage;
    
    //
    // Public Methods
    //

	public void SetColor(Color color)
	{
		Color bar = GetComponent<Image>().color = color;
		percentage.color = ColorExtensions.GetLuma(bar) > 0.5f ? Color.black : Color.white;
	}

	public void SetPercentageVal(float value, bool prefix = false)
	{
		string sign = (prefix && value > 0.0f) ? "+" : "";
		percentage.text = sign + Mathf.RoundToInt(value * 100.0f).ToString() + "%";
	}

	public void SetDistribBarWidth(float width)
	{
		RectTransform rectTransform = GetComponent<RectTransform>();
		float height = rectTransform.rect.height;
		rectTransform.sizeDelta = new Vector2(width, height);
	}
}