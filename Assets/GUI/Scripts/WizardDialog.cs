// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public interface IWizardController
{
	GameObject OnWizardNext();
	bool OnWizardFinish();
	bool OnWizardBack();
    bool OnWizardClose();
    void OnWizardPanelChanged(GameObject previous, GameObject current);
}

public class WizardDialog : BasicPopupDialog
{
	[Header("Wizzard UI References")]
    public Button previousButton;
    public Button nextButton;
    public Button finishButton;
    public Transform container;

    [Header("Settings")]
    [SerializeField] private bool previous;
    public bool ShowPreviousButton { get { return previous; } set { previous = value; UpdateUI(); } }

    [SerializeField] private bool isLast;
    public bool IsLast { get { return isLast; } set { if (isLast != value) { isLast = value; UpdateUI(); } } }

    private IWizardController controller;
    private Stack<GameObject> previousPanels = new Stack<GameObject>();
    public GameObject Current { get; private set; }
    public GameObject Previous { get { return previousPanels.Count == 0 ? null : previousPanels.Peek(); } }


    //
    // Unity Methods
    //

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
#endif
        {
            UpdateUI();
        }
    }

	//
	// Event Methods
	//

	protected override void OnCloseClicked()
	{
        if (controller.OnWizardClose())
            base.OnCloseClicked();
	}

	private void OnPreviousClicked()
	{
        if (controller.OnWizardBack())
            Back();
    }

    private void OnNextClicked()
	{
		Next(controller.OnWizardNext());
    }

	private void OnFinishClicked()
	{
		if (controller.OnWizardFinish())
			CloseDialog(DialogAction.Ok);
	}
	


	//
	// Public Methods
	//

	public void Show(IWizardController controller, GameObject panel)
    {
        ClearEvents();

        this.controller = controller;
        if (controller == null)
            return;

		RegisterEvents();

        Current = panel;
        panel.SetActive(true);
        previousPanels.Clear();

        previous = false;
        UpdateUI();
    }

    public void Next(GameObject panel)
	{
		if (panel == null)
			return;

		var previousPanel = Current;
        previousPanel.SetActive(false);
        previousPanels.Push(previousPanel);

        Current = panel;
        panel.SetActive(true);

        previous = true;
        UpdateUI();

        if (controller != null)
            controller.OnWizardPanelChanged(previousPanel, Current);
    }

    public void Back()
    {
        if (previousPanels.Count == 0)
        {
            previous = false;
            UpdateUI();
            return;
        }

        var previousPanel = Current;
        previousPanel.SetActive(false);

        Current = previousPanels.Pop();
        Current.SetActive(true);

        previous = previousPanels.Count > 0;
        isLast = false;
        UpdateUI();

        if (controller != null)
            controller.OnWizardPanelChanged(previousPanel, Current);
    }


	//
	// Private Methods
	//

	private void RegisterEvents()
	{
		closeButton.onClick.AddListener(OnCloseClicked);
		previousButton.onClick.AddListener(OnPreviousClicked);
		nextButton.onClick.AddListener(OnNextClicked);
		finishButton.onClick.AddListener(OnFinishClicked);
	}

	private void ClearEvents()
    {
        closeButton.onClick.RemoveAllListeners();
        previousButton.onClick.RemoveAllListeners();
        nextButton.onClick.RemoveAllListeners();
        finishButton.onClick.RemoveAllListeners();
    }

    private void UpdateUI()
    {
        previousButton.gameObject.SetActive(previous);
        nextButton.gameObject.SetActive(!isLast);
        finishButton.gameObject.SetActive(isLast);
    }
}
