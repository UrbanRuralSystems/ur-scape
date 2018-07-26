// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//          Michael Joos(joos @arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class OutputPanel : UrsComponent
{
    [Header("Prefabs")]
    public Transform defaultPanelPrefab;
    public Transform outputContainer;

    private Transform defaultPanel;
    private Transform currentPanel;

    //
    // Unity Methods
    //

    private void Start()
    {
        defaultPanel = Instantiate(defaultPanelPrefab, outputContainer, false);
		currentPanel = defaultPanel;

		defaultPanel.transform.GetChild(1).GetComponent<Text>().text = "Version " + Application.version;
	}

	//
	// Public Methods
	//

	public void SetPanel(Transform panel)
	{
		// deactive previus panel
		if (currentPanel != null)
			currentPanel.gameObject.SetActive(false);

		currentPanel = panel;

		if (currentPanel == null)
		{
			currentPanel = defaultPanel;
		}
		currentPanel.SetParent(outputContainer, false);
		currentPanel.gameObject.SetActive(true);
	}

	public void RemovePanel(Transform panel)
	{
		if (panel == currentPanel)
			SetPanel(null);
	}
}
