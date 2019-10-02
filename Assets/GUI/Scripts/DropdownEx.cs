// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class DropdownEx : Dropdown
{
    [Header("UI References")]
    public Image arrow;
    public Text placeholder;
    //public Button[] buttons;

    [Header("Settings")]
    public bool allowEmptySelection;
    public Color disabledArrow = Color.gray;

    public bool HasSelected { get; private set; }

    public event UnityAction<GameObject> OnItemCreated;

    private SelectionState previousState;

	private static readonly OptionData EmptyOption = new OptionData( (string)null );


	//
	// Unity Methods
	//

	protected override void Awake()
    {
        base.Awake();

        onValueChanged.RemoveListener(OnValueChanged);
        onValueChanged.AddListener(OnValueChanged);

        HasSelected = !allowEmptySelection;
    }


	//
	// Inherited Methods
	//

	public new void AddOptions(List<string> options)
    {
		base.AddOptions(options);
		PostAddOptions(options);
	}

	public new void AddOptions(List<OptionData> options)
	{
		base.AddOptions(options);
		PostAddOptions(options);
	}

	private void PostAddOptions<T>(List<T> options) where T : class
	{
		if (allowEmptySelection)
		{
			ResetSelectionAndAddEmptyOption();
		}
	}


	protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);

        if (state == SelectionState.Disabled)
        {
            //UpdateButtons(false);
            UpdateDrowdownArrow(disabledArrow);
        }
        else if (previousState == SelectionState.Disabled)
        {
            //UpdateButtons(true);
            UpdateDrowdownArrow(colors.normalColor);
        }

        previousState = state;
    }

    // Dropdown creates the dropdown first, then the blocker
    protected override GameObject CreateDropdownList(GameObject template)
    {
        // Temporarily remove the last (empty) option
        if (allowEmptySelection && !HasSelected && options.Count > 0)
            options.RemoveAt(options.Count - 1);

        return base.CreateDropdownList(template);
    }

    protected override DropdownItem CreateItem(DropdownItem itemTemplate)
    {
        var item = base.CreateItem(itemTemplate);
		OnItemCreated?.Invoke(item.gameObject);
		return item;
    }

    protected override void DestroyDropdownList(GameObject dropdownList)
    {
        base.DestroyDropdownList(dropdownList);

        // Re-add empty option if user hasn't selected anything yet
        if (allowEmptySelection && !HasSelected)
			options.Add(EmptyOption);
	}

    // Dropdown creates the dropdown first, then the blocker
    //private GameObject blocker;
    //protected override GameObject CreateBlocker(Canvas rootCanvas)
    //{
    //    blocker = base.CreateBlocker(rootCanvas);
    //    var button = blocker.GetComponent<Button>();
    //    if (button != null)
    //    {
    //        button.onClick.AddListener(OnCancelDropdown);
    //    }
    //    return blocker;
    //}


    //
    // Event Methods
    //

    private void OnValueChanged(int option)
    {
        if (allowEmptySelection && !HasSelected && !IsEmptyOption(option))
        {
            HasSelected = true;

            if (placeholder != null)
                placeholder.gameObject.SetActive(false);

			if (options.Count > 0)
			{
				int lastIndex = options.Count - 1;
				if (options[lastIndex].text == null)
					options.RemoveAt(lastIndex);
			}
		}
    }


    //
    // Public Methods
    //

	public void Deselect()
	{
		if (allowEmptySelection)
		{
			ResetSelectionAndAddEmptyOption();
		}
	}

	public bool IsEmptyOption(int option)
	{
		return option == options.Count - 1 && options[option].text == null;
	}

	public delegate string ItemText<T>(T item);
    public void SetOptions<T>(List<T> list, ItemText<T> toText)
    {
        SetOptions(list, list == null ? 0 : list.Count, toText);
    }

    public void SetOptions<T>(T[] list, ItemText<T> toText)
    {
        SetOptions(list, list == null ? 0 : list.Length, toText);
    }

    public void SetOptions<T>(IEnumerable<T> list, int count, ItemText<T> toText)
    {
        ClearOptions();

        bool hasOptions = list != null && count > 0;
        if (hasOptions)
        {
            var options = new List<string>();
            foreach (var item in list)
            {
                options.Add(toText(item));
            }
            AddOptions(options);
        }
    }


    //
    // Private Methods
    //

    //private void UpdateButtons(bool interactable)
    //{
    //    if (buttons != null)
    //    {
    //        foreach (var b in buttons)
    //        {
    //            b.interactable = interactable;
    //        }
    //    }
    //}

    private void UpdateDrowdownArrow(Color color)
    {
        if (gameObject.activeInHierarchy && arrow != null && transition == Transition.ColorTint)
        {
            color *= colors.colorMultiplier;
            arrow.CrossFadeColor(color, 0f, true, true);
        }
    }

	private void ResetSelectionAndAddEmptyOption()
	{
		HasSelected = false;

		if (placeholder != null)
			placeholder.gameObject.SetActive(true);

		int lastIndex = options.Count - 1;
		if (options[lastIndex].text != null)
		{
			options.Add(EmptyOption);
			lastIndex++;
		}

		value = lastIndex;
	}
}
