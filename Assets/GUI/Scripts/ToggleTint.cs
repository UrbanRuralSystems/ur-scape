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
[RequireComponent(typeof(Toggle))]
public class ToggleTint : MonoBehaviour
{
	public Graphic graphic;
	public Color colorOn = Color.white;
    public Color colorOff = Color.gray;

	//
	// Unity Methods
	//

	private void Awake()
    {
		GetComponent<Toggle>().onValueChanged.AddListener(OnToggleValueChanged);
	}

	private void OnEnable()
    {
		OnToggleValueChanged(GetComponent<Toggle>().isOn);
    }

	private void OnValidate()
	{
		OnToggleValueChanged(GetComponent<Toggle>().isOn);
	}


	//
	// Private Methods
	//

	private void OnToggleValueChanged(bool on)
    {
		graphic.color = on ? colorOn : colorOff;
	}
}
