// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli
//          Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class LocalizedText : Text
{
	public string OriginalText { get; private set; }

	//
	// Unity Methods
	//

	protected override void Awake()
	{
		base.Awake();
		OriginalText = text;
	}

	protected override void OnEnable()
	{
		base.OnEnable();

		if (Application.isPlaying)
		{
			text = Translator.Get(OriginalText);
		}
	}
}