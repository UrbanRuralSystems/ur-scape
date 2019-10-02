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
public class ToggleGraphic : MonoBehaviour
{
	public Graphic graphicOn;
    public Graphic graphicOff;

	//
	// Unity Methods
	//

	void OnEnable()
    {
		var toggle = GetComponent<Toggle>();
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
		if (graphicOn != null)
			graphicOn.enabled = on;
		if (graphicOff != null)
			graphicOff.enabled = !on;
		//graphicOn.gameObject.SetActive(on);
		//graphicOff.gameObject.SetActive(!on);
	}
}
