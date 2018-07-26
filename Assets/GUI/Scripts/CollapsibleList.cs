// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class CollapsibleList : MonoBehaviour
{
	[Header("UI References")]
	public Text label;
	public Toggle groupToggle;
	public RectTransform container;


	//
	// Unity Methods
	//

	protected virtual void Start()
	{
		groupToggle.onValueChanged.AddListener(OnToggleChange);
	}


	//
	// Event Methods
	//

	private void OnToggleChange(bool isOn)
	{
		container.gameObject.SetActive(isOn);
		GuiUtils.RebuildLayout(container);
	}


	//
	// Public Methods
	//

	public void Init(string name)
	{
		SetGroupName(name);
	}

	public void SetGroupName(string name)
	{
		label.text = name;
	}

	public T AddItem<T>(T prefab) where T : Object
	{
		return Instantiate(prefab, container, false);
	}

	public void Clear()
	{
		for (int i = container.childCount - 1; i >= 0; i--)
		{
			Destroy(container.GetChild(i).gameObject);
		}
	}

}
