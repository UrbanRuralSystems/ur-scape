// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LanguageSelector : MonoBehaviour
{
	[Header("UI References")]
	public Button closeButton;
	public Button okButton;
	public ScrollRect languagesList;

	[Header("Prefabs")]
	public GameObject listItemPrefab;

	//
	// Unity Methods
	//
	
	private IEnumerator Start()
	{
		// Wait one frame to allow list shadows to work properly
		yield return null;

		closeButton.onClick.AddListener(OnCloseClick);
		okButton.onClick.AddListener(OnOkClick);
		UpdateList();
	}

	
	//
	// Event Methods
	//

	private void OnCloseClick()
	{
		Close();
	}

	private void OnOkClick()
	{
		var active = languagesList.content.GetComponent<ToggleGroup>().ActiveToggles();
		foreach (var item in active)
		{
			int index = item.transform.GetSiblingIndex();
			LocalizationManager.Instance.ChangeLanguage(index);
		}
		Close();
	}


	//
	// Private Methods
	//

	private void UpdateList()
	{
		var languageManager = LocalizationManager.Instance;
		var currentLanguage = languageManager.Current;
		var toggleGroup = languagesList.content.GetComponent<ToggleGroup>();

		foreach (var language in languageManager.languages)
		{
			var item = Instantiate(listItemPrefab, languagesList.content);
			var toggle = item.GetComponent<Toggle>();
			toggle.group = toggleGroup;
			if (language == currentLanguage)
				toggle.isOn = true;

			item.GetComponentInChildren<Text>().text = language;
		}
	}

	private void Close()
	{
		Destroy(gameObject);
	}
	
}
