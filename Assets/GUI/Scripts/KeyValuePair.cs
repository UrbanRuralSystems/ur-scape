// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class KeyValuePair : MonoBehaviour
{
	[Header("UI References")]
	public Text key;
	public Text value;

	[Header("Settings")]
	public Color disabledKey = Color.gray;
	public Color disabledValue = Color.gray;

	private Color originalKeyColor;
	private Color originalValueColor;

	//
	// Public Methods
	//

	private bool disabled = false;
	public bool Disabled
	{
		get
		{
			return disabled;
		}
		set
		{
			if (disabled != value)
			{
				disabled = value;
				if (isActiveAndEnabled)
				{
					if (disabled)
						SetDiabledColors();
					else
						SetOriginalColors();
				}
			}
		}
	}

	public void Awake()
	{
		originalKeyColor = key.color;
		originalValueColor = value.color;

		if (disabled)
			SetDiabledColors();
	}

	public void SetKey(string key)
	{
		this.key.text = key;
	}

	public void SetValue(string value)
	{
		this.value.text = value;
	}

	public void SetKeyValue(string key, string value)
	{
		this.key.text = key;
		this.value.text = value;
	}

	public string GetKey()
	{
		return key.text;
	}

	public string GetValue()
	{
		return value.text;
	}

	//
	// Private Methods
	//

	private void SetDiabledColors()
	{
		key.color = disabledKey;
		value.color = disabledValue;
	}

	private void SetOriginalColors()
	{
		key.color = originalKeyColor;
		value.color = originalValueColor;
	}

}
