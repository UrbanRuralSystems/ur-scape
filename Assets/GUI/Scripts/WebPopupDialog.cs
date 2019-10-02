// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public class WebPopupDialog : BasicPopupDialog
{
	private ModalDialogManager dialogMgr;
	private GameObject invisibleDlg;

	protected override void Start()
	{
		base.Start();

		var componentMgr = ComponentManager.Instance;
		dialogMgr = componentMgr.Get<ModalDialogManager>();
		if (dialogMgr == null)
			componentMgr.OnRegistrationFinished += OnRegistrationFinished;
		else
			CreateInvisibleDialog();
	}

	private void OnDestroy()
	{
		if (invisibleDlg != null)
		{
			DestroyImmediate(invisibleDlg);
			invisibleDlg = null;
		}
	}

	private void OnRegistrationFinished()
	{
		dialogMgr = ComponentManager.Instance.Get<ModalDialogManager>();
		CreateInvisibleDialog();
	}

	protected override void InitEvents()
	{
		base.InitEvents();
		if (okButton != null)
		{
			okButton.onClick.AddListener(OnOk);
		}
	}

	private void OnOk()
	{
#if UNITY_WEBGL
		if (!Screen.fullScreen)
			Web.GoFullScreen();
#endif
	}

	private void CreateInvisibleDialog()
	{
		invisibleDlg = new GameObject("InvisibleDialog");
		dialogMgr.ShowDialog(invisibleDlg);
	}
}
