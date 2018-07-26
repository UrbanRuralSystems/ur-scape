// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class ToggleGraphicSwap : MonoBehaviour
{
	public Image image;
	public Sprite imageOn;
    public Sprite imageOff;

	//
	// Unity Methods
	//

	void OnEnable()
    {
		var toggle = GetComponent<Toggle>();
		image = toggle.image;

		toggle.onValueChanged.AddListener(OnToggleValueChanged);
		OnToggleValueChanged(toggle.isOn);
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
