// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class RoadToggle : MonoBehaviour
{
	[Header("UI References")]
	public Toggle toggle;
	public Button button;
    public Text letter;
	public InputField label;

	[Header("Images for each state")]
	public Sprite disabled;
	public Sprite toggledOn;
	public Sprite toggledOff;


	//
	// Unity Methods
	//

	void Awake()
	{
		toggle.onValueChanged.AddListener(OnToggleValueChanged);
	}

	void OnEnable()
	{
		UpdateImage();
	}


	//
	// Event Methods
	//

	private void OnToggleValueChanged(bool on)
	{
		UpdateImage();
	}


	//
	// Public Methods
	//

	public bool IsInteractable
	{
		set
		{
			if (!value && button.gameObject.activeSelf)
				AllowRemove(false);

			toggle.interactable = value;
			label.interactable = value;
			UpdateImage();
		}
	}

	public void AllowRemove(bool allow)
	{
		if (!toggle.interactable)
			return;

		toggle.image.enabled = !allow;
        letter.gameObject.SetActive(!allow);
		button.gameObject.SetActive(allow);
	}


	//
	// Private Methods
	//

	private void UpdateImage()
	{
		if (toggle.interactable)
			toggle.image.sprite = toggle.isOn ? toggledOn : toggledOff; 
		else
			toggle.image.sprite = disabled;
	}
}
