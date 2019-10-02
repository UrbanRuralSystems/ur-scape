// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DropdownWithInput : DropdownEx
{
    [Header("UI References")]
    public InputField input;

	public event UnityAction OnTextChangedWithoutValueChange;

    //
    // Unity Methods
    //

    protected override void Awake()
	{
        base.Awake();

		if (input != null && Application.isPlaying)
		{
			input.onEndEdit.AddListener(OnSiteInputEndEdit);
			onValueChanged.AddListener(OnValueChanged);
		}
	}

	//
	// Event Methods
	//

	private void OnSiteInputEndEdit(string text)
	{
		UpdateDropdownFromText(text);
	}

	private void OnValueChanged(int option)
    {
		if (!IsEmptyOption(option))
        {
            input.text = options[option].text;
        }
    }

	private void UpdateDropdownFromText(string text)
	{
		if (options.Count == 0)
		{
			OnTextChangedWithoutValueChange?.Invoke();
			return;
		}

		// Check if it matches the selected option
		var currentOption = options[value].text;
		if (text.EqualsIgnoreCase(currentOption))
		{
			OnTextChangedWithoutValueChange?.Invoke();
			return;
		}

		// Check if it matches any of the other options
		for (int i = 0; i < options.Count; i++)
		{
			if (text.EqualsIgnoreCase(options[i].text))
			{
				value = i;
				return;
			}
		}

		// The text didn't match any of the options
		Deselect();
		OnTextChangedWithoutValueChange?.Invoke();
	}
}
