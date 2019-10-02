// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[System.Flags]
public enum DialogElements
{
	Title = 1,
	Close = 2,
	Yes = 4,
	No = 8,
	Ok = 16,
	Message = 32,
	Input = 64,
	Warning = 128,
}


public class PopupDialog : BasicPopupDialog
{
	[Header("UI References")]
	public Button yesButton;
	public Button noButton;
	public Text message;
	public InputField input;
	public GameObject warningIcon;
	public GameObject titleRow;
	public GameObject iconsRow;
	public GameObject messageRow;
	public GameObject buttonsRow;

	public string InputValue { get { return input.text; } }

	[EnumFlags]
	public DialogElements elements;

    private static int DialogID = 0;
    private int id = 0;

    //
    // Unity Methods
    //

    private void Awake()
    {
        id = ++DialogID;
    }

    private void OnDestroy()
    {
        DialogID--;
    }

    private void OnValidate()
	{
		if (transform.parent != null)
			InitUI();
	}

    private void Update()
    {
        if (id == DialogID)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
				if (elements.IsSet(DialogElements.Yes))
				{
					OnYesClicked();
				}
				else if (elements.IsSet(DialogElements.Ok))
                {
                    OnOkClicked();
                }
                else if (elements.IsSet(DialogElements.Close))
                {
                    OnCloseClicked();
                }
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (elements.IsSet(DialogElements.Close))
                {
                    closeButton.Select();
                    OnCloseClicked();
                }
				else if (elements.IsSet(DialogElements.No))
				{
					noButton.Select();
					OnNoClicked();
				}
			}
        }
    }

    //
    // Event Methods
    //

	private void OnYesClicked()
	{
		CloseDialog(DialogAction.Yes);
	}

	private void OnNoClicked()
	{
		CloseDialog(DialogAction.No);
	}

	private void OnInputChanged(string text)
	{
		UpdateConfirmationButtons();
	}


	//
	// Public Methods
	//

	public void ShowWarningMessage(string text, string title = null)
	{
		var elements = DialogElements.Close | DialogElements.Warning | DialogElements.Message;
		if (!string.IsNullOrEmpty(title))
		{
			elements |= DialogElements.Title;
			this.title.text = title;
		}
		message.text = text;

		Show(elements);
	}

	public void ShowWarningQuestion(string text)
	{
		Show(DialogElements.Warning | DialogElements.Message | DialogElements.Yes | DialogElements.No);
		message.text = text;
	}

	public void ShowInput(string title, string placeholder)
	{
		Show(DialogElements.Title | DialogElements.Close | DialogElements.Input | DialogElements.Ok);
		this.title.text = title;
		input.placeholder.GetComponent<Text>().text = placeholder;
	}

	public void Show(DialogElements elements)
	{
		this.elements = elements;

		// Deselect currently selected object
		if (!EventSystem.current.alreadySelecting)
			EventSystem.current.SetSelectedGameObject(null);

		InitUI();
	}


	//
	// Private Methods
	//

	protected override void InitEvents()
	{
		base.InitEvents();

		yesButton.onClick.RemoveAllListeners();
		yesButton.onClick.AddListener(OnYesClicked);

		noButton.onClick.RemoveAllListeners();
		noButton.onClick.AddListener(OnNoClicked);

		input.onValueChanged.RemoveAllListeners();
		input.onValueChanged.AddListener(OnInputChanged);
	}

	private void InitUI()
	{
		title.gameObject.SetActive(elements.IsSet(DialogElements.Title));

		if (closeButton != null)
			closeButton.gameObject.SetActive(elements.IsSet(DialogElements.Close));
		if (yesButton != null)
			yesButton.gameObject.SetActive(elements.IsSet(DialogElements.Yes));
		if (noButton != null)
			noButton.gameObject.SetActive(elements.IsSet(DialogElements.No));
		if (okButton != null)
			okButton.gameObject.SetActive(elements.IsSet(DialogElements.Ok));

		var isInputActive = elements.IsSet(DialogElements.Input);
		input.gameObject.SetActive(isInputActive);
		message.gameObject.SetActive(elements.IsSet(DialogElements.Message));
		message.alignment = isInputActive ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;

		titleRow.SetActive(elements.IsSet(DialogElements.Title));
		iconsRow.SetActive(elements.IsSet(DialogElements.Warning));
		messageRow.SetActive(elements.IsSet(DialogElements.Message));
		buttonsRow.SetActive(elements.IsSet(DialogElements.Yes | DialogElements.No | DialogElements.Ok));

		UpdateConfirmationButtons();

		if (isInputActive)
			input.Select();
	}

	private void UpdateConfirmationButtons()
	{
		bool isInputActive = elements.IsSet(DialogElements.Input);
		bool interactable = !(isInputActive && string.IsNullOrWhiteSpace(input.text));

		if (elements.IsSet(DialogElements.Ok) &&
            okButton.interactable != interactable)
			okButton.interactable = interactable;

        if (elements.IsSet(DialogElements.Yes) &&
            yesButton.interactable != interactable)
            yesButton.interactable = interactable;
	}
}


public static class Flags
{
	public static bool IsSet(this DialogElements flags, DialogElements flag)
	{
		return (flags & flag) != 0;
	}

	public static void Set(this DialogElements flags, DialogElements flag)
	{
		flags |= flag;
	}

	public static void Unset(this DialogElements flags, DialogElements flag)
	{
		flags &= ~flag;
	}
}
