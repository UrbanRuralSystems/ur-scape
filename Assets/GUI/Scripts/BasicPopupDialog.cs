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

public enum DialogAction
{
	Close,
	Yes,
	No,
	Ok,
	Cancel
}

public class DialogResult
{
	public DialogAction action = DialogAction.Close;
	public bool shouldClose = true;
}

public class BasicPopupDialog : MonoBehaviour
{
	[Header("Dialog UI References")]
	public Text title;
	public Button closeButton;
	public Button okButton;


	public event UnityAction<DialogResult> OnCloseDialog;

	//
	// Unity Methods
	//

	protected virtual void Start()
	{
		InitEvents();
	}

	//
	// Unity Methods
	//

	protected virtual void OnCloseClicked()
	{
		CloseDialog(DialogAction.Close);
	}

	protected virtual void OnOkClicked()
	{
		CloseDialog(DialogAction.Ok);
	}


	//
	// Public Methods
	//

	public void CloseDialog(DialogAction action)
	{
		DialogResult result = new DialogResult() { action = action };

		OnCloseDialog?.Invoke(result);

		if (result.shouldClose)
			Close();
	}
	
	
	//
	// Private Methods
	//

	protected virtual void InitEvents()
	{
		if (closeButton != null)
		{
			closeButton.onClick.RemoveAllListeners();
			closeButton.onClick.AddListener(OnCloseClicked);
		}
		if (okButton != null)
		{
			okButton.onClick.RemoveAllListeners();
			okButton.onClick.AddListener(OnOkClicked);
		}
	}


	private void Close()
	{
		Destroy(gameObject);
	}
}
