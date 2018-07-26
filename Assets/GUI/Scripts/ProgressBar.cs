// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : Scrollbar
{

    public Text percentageText;

    //
    // Unity Methods
    //
	
	protected override void OnEnable()
	{
        base.OnEnable();

        if (percentageText != null)
        {
            onValueChanged.AddListener(OnPercentageChange);
        }
    }

    public void SetProgess(float p)
    {
        size = p;
        OnPercentageChange(p);
    }

    //
    // UI Events
    //

    public void OnPercentageChange(float p)
    {
        percentageText.text = Mathf.RoundToInt(size * 100f) + " %";
    }

}
