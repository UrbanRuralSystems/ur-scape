// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
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
#if UNITY_EDITOR
	protected override void OnValidate()
	{
		base.OnValidate();

		if (Application.isPlaying)
			SetProgess(size);
	}
#endif

	//
	// Public Methods
	//

	public void SetProgess(float p)
    {
        size = p;
		percentageText.text = Mathf.RoundToInt(size * 100f) + " %";
	}
}
