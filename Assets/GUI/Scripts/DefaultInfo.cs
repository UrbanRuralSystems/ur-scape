// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class DefaultInfo : MonoBehaviour
{
	[Header("UI References")]
	public Text version;
	public Button moreInfoButton;

	[Header("Prefabs")]
	public GameObject aboutDialogPrefab;

	private void Start()
	{
		version.text = "Version " + Application.version;
		moreInfoButton.onClick.AddListener(OnMoreInfoClick);
	}

	private void OnMoreInfoClick()
    {
		var dlgManager = ComponentManager.Instance.Get<ModalDialogManager>();
		dlgManager.NewDialog(aboutDialogPrefab);
	}
}
