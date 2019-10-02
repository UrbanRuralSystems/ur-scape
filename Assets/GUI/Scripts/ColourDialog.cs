// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public class ColourDialog : BasicPopupDialog
{
	[Header("UI References")]
	public ColourSelector colourSelector;

	public Color Color { get { return colourSelector.Color; } }


	//
	// Public Methods
	//

	public void Show(Color color)
	{
		colourSelector.SetColor(color);
	}

}
