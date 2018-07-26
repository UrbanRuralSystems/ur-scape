// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using ExtensionMethods;
using UnityEngine;
using UnityEngine.UI;

public class LegendLayerEntry : CollapsibleList
{
    [Header("UI References")]
	public Image groupIcon;
	public Image layerIcon;
    public Text layerValue;
    public Text layerCredit;

    private const int MaxNameLength = 17;
    private const int MaxValueLength = 13;


    //
    // Public Methods
    //

    public void Init(string name, Color color)
    {
		if (name.Length > MaxNameLength)
			name = name.Remove(MaxNameLength - 3) + "...";

		base.Init(name);

        layerIcon.color = color;
        layerValue.text = "N/A";
		groupToggle.interactable = false;
		groupIcon.gameObject.SetActive(false);
    }

    public void SetValue(string value)
    {
        if (value.Length > MaxValueLength)
            value = value.Remove(MaxValueLength - 3) + "...";

        layerValue.text = value;
    }

	public void SetCredit(string credit)
	{
		if (string.IsNullOrEmpty(credit) || credit.EqualsIgnoreCase("N/A"))
		{
			groupIcon.gameObject.SetActive(false);
			groupToggle.interactable = false;
		}
		else
		{
			layerCredit.text = credit;
			groupIcon.gameObject.SetActive(true);
			groupToggle.interactable = true;
		}
		GuiUtils.RebuildLayout(layerCredit.transform);
    }

    public void Expand(bool expand)
    {
		groupToggle.isOn = expand;
	}

}
