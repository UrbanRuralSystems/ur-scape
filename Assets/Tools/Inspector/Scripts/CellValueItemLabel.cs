// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli

using UnityEngine;
using UnityEngine.UI;

public class CellValueItemLabel: MonoBehaviour
{
	[Header("UI References")]
	public Image dot;
	public Text nameLabel;

	public void SetDotColor(Color color)
	{
		this.dot.color = color;
	}

	public void SetName(string nameString)
    {
        this.nameLabel.text = nameString;
    }
}
