// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum DialogStatus
{
    OK,
    Canceled
}

public delegate void OnDialogClosed(DialogStatus status);

public class TargetPanel : MonoBehaviour
{
    [Header("Prefabs")]
    public Transform atributeItemPrefab;

    [Header("UI Referebces")]
    public Transform contentPanel;
    public Button okButton;

    private Dictionary<string, float> targetValues;
    private Dictionary<Transform, string> itemToKey = new Dictionary<Transform, string>();

    private OnDialogClosed callback;


    //
    // Unity Methods
    //

    void Start()
    {
        okButton.onClick.AddListener(OnOkClick);
    }


    //
    // Public Methods
    //

    public void Show(Dictionary<string, float> targetValues, Typology typology, OnDialogClosed callback)
    {
        this.targetValues = targetValues;
        this.callback = callback;

        int childIndex = 0;
        foreach (var pair in typology.values)
        {
            var item = Instantiate(atributeItemPrefab, contentPanel);
            var toggle = item.GetComponentInChildren<Toggle>();

            bool hasTarget = targetValues.ContainsKey(pair.Key);
            toggle.isOn = hasTarget;
            toggle.onValueChanged.AddListener((isOn) => OnToggleChange(toggle));

            item.GetChild(1).gameObject.SetActive(hasTarget);

            if (itemToKey.Count != typology.values.Count)
                itemToKey.Add(item, pair.Key);

            if (Typology.info.TryGetValue(pair.Key, out TypologyInfo info))
                toggle.GetComponentInChildren<Text>().text = pair.Key + " (" + info.units + ")";
            else
                toggle.GetComponentInChildren<Text>().text = pair.Key;

            if (hasTarget)
            {
                var input = item.GetComponentInChildren<InputField>();
                input.text = targetValues[pair.Key].ToString();
            }

            if (contentPanel.childCount == typology.values.Count)
                ++childIndex;
        }

        gameObject.SetActive(true);
    }


    //
    // Private Methods
    //

    private void OnToggleChange(Toggle toggle)
    {
        toggle.transform.parent.GetChild(1).gameObject.SetActive(toggle.isOn);
        GuiUtils.RebuildLayout(toggle.transform.parent);
    }

    private void OnOkClick()
    {
		//gameObject.SetActive(false);

		targetValues.Clear();
		for (int i = contentPanel.childCount - 1; i >= 0; i--)
		{
			var item = contentPanel.GetChild(i);
			if (item.GetComponentInChildren<Toggle>().isOn)
			{
				var input = item.GetComponentInChildren<InputField>();
				if (!string.IsNullOrEmpty(input.text))
				{
					float value = float.Parse(item.GetComponentInChildren<InputField>().text);
					targetValues.Add(itemToKey[item], value);
				}
			}

			Destroy(item.gameObject);
		}

		itemToKey.Clear();
		targetValues = null;

		if (callback != null)
		{
			callback(DialogStatus.OK);
		}

		Destroy(gameObject);
    }
}
