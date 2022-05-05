// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//          Michael Joos(joos @arch.ethz.ch)
//          Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class OutputPanel : UrsComponent
{
    [Header("Prefabs")]
    public Transform defaultPanelPrefab;
    public Transform outputContainer;

	[Header("UI References")]
	[SerializeField] private Dropdown outputDropdown = default;

    private Transform defaultPanel;
    private Transform currentPanel;
	private readonly Dictionary<string, Transform> outputPanels = new Dictionary<string, Transform>();

    //
    // Unity Methods
    //

    private void Start()
    {
		LocalizationManager.WaitAndRun(() => LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged);

		defaultPanel = Instantiate(defaultPanelPrefab, outputContainer, false);
		defaultPanel.name = defaultPanelPrefab.name;
		currentPanel = defaultPanel;

		outputDropdown.ClearOptions();
		AddPanel("Default", defaultPanel);
		outputDropdown.onValueChanged.AddListener(OnOutputDropdownChanged);
	}

	//
	// Event Methods
	//

	private void OnLanguageChanged()
    {
		int count = outputDropdown.options.Count;
		var outputPanelNames = outputPanels.Keys.ToArray();

		for (int i = 0; i < count; ++i)
		{
			outputDropdown.options[i].text = Translator.Get(outputPanelNames[i]);
		}
		outputDropdown.captionText.text = outputDropdown.options[outputDropdown.value].text;
		outputDropdown.RefreshShownValue();
	}

	private void OnOutputDropdownChanged(int option)
	{
		SetPanel(option);
	}

	//
	// Private Methods
	//

	private void SetPanel(int option)
	{
		// deactive previus panel
		if (currentPanel != null)
			currentPanel.gameObject.SetActive(false);

		SetPanel(outputContainer.GetChild(option));
	}

	//
	// Public Methods
	//

	public void AddPanel(string name, Transform panel)
    {
		if (!outputPanels.ContainsKey(name))
        {
			outputPanels.Add(name, panel);

			// Update dropdown
			outputDropdown.options.Add(new Dropdown.OptionData(name));
			outputDropdown.RefreshShownValue();
			outputDropdown.gameObject.SetActive(outputDropdown.options.Count > 1);

			SetPanel(panel);
			outputDropdown.SetValueWithoutNotify(panel.GetSiblingIndex());
		}
	}

    public void SetPanel(Transform panel)
	{
		// deactive previus panel
		if (currentPanel != null)
			currentPanel.gameObject.SetActive(false);

		currentPanel = panel;

        if (currentPanel == null)
		{
			currentPanel = defaultPanel;
			outputDropdown.SetValueWithoutNotify(0);
		}
		currentPanel.SetParent(outputContainer, false);
		outputDropdown.SetValueWithoutNotify(currentPanel.GetSiblingIndex());
		currentPanel.gameObject.SetActive(true);
	}

	public void DestroyPanel(string name)
    {
		outputPanels.TryGetValue(name, out Transform panel);

		if (panel == currentPanel)
			SetPanel(null);

		outputPanels.Remove(name);

		// Update dropdown
		outputDropdown.options.RemoveAt(outputDropdown.options.FindIndex(option => string.Equals(option.text, name)));
		outputDropdown.RefreshShownValue();
		outputDropdown.gameObject.SetActive(outputDropdown.options.Count > 1);

		Destroy(panel.gameObject);
	}
}
