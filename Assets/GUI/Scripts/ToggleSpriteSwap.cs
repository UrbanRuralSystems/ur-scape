// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ToggleSpriteSwap : MonoBehaviour
{
	public Sprite imageOn;
    public Sprite imageOff;

	private Image image;

	//
	// Unity Methods
	//

	void OnEnable()
	{
		var toggle = GetComponent<Toggle>();
		image = toggle.image;

		OnToggleValueChanged(toggle.isOn);

		if (Application.isPlaying)
			toggle.onValueChanged.AddListener(OnToggleValueChanged);
	}

	void OnDisable()
	{
		GetComponent<Toggle>().onValueChanged.RemoveListener(OnToggleValueChanged);
	}


	//
	// Private Methods
	//

	private void OnToggleValueChanged(bool on)
    {
		image.sprite = on ? imageOn : imageOff;
	}
}
