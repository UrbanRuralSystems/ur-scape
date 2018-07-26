// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine.Events;
using UnityEngine.UI;

public class SiteGroup : CollapsibleList
{
	public Button siteButton;

	//
	// Public Methods
	//

	public void AddListener(UnityAction<bool> call)
	{
		siteButton.onClick.AddListener(() => call(true));
	}

}
