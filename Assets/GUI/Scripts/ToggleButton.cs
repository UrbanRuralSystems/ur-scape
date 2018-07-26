// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : Toggle
{
	public Text label;
	public Color textColor = Color.white;

	private Color onColor;
    private Color offColor;
    private Color hightlightOffset;


	//
	// Unity Methods
	//

	protected override void Awake()
    {
        switch (transition)
        {
            case Transition.ColorTint:
                offColor = colors.normalColor;
                onColor = colors.pressedColor;
                hightlightOffset = colors.highlightedColor - colors.normalColor;
                break;
        }
	}

    protected override void OnEnable()
    {
        base.OnEnable();

        onValueChanged.RemoveListener(OnToggle);
        if (Application.isPlaying)
        {
            onValueChanged.AddListener(OnToggle);
            OnToggle(isOn);
        }
    }


	//
	// Inheritance Methods
	//

	protected override void DoStateTransition(SelectionState state, bool instant)
	{
		base.DoStateTransition(state, instant);

		if (label != null)
		{
			if (interactable)
				label.color = isOn || state != SelectionState.Normal? textColor : textColor * 0.9f;
			else
				label.color = textColor * colors.disabledColor;
		}
	}


	//
	// Public Methods
	//

	public void SetText(string text)
	{
		transform.GetChild(0).GetComponent<Text>().text = text;
	}

	public void SetColor(Color newColor)
	{
		onColor = newColor;
		OnToggle(isOn);
	}

	public Color GetColor()
	{
		return onColor;
	}


	//
	// Private Methods
	//

	private void OnToggle(bool value)
    {
        switch (transition)
        {
            case Transition.ColorTint:
                var newColors = colors;
                newColors.normalColor = value ? onColor : offColor;
                newColors.highlightedColor = newColors.normalColor + hightlightOffset;
                newColors.pressedColor = newColors.highlightedColor;
                colors = newColors;
                break;
        }
    }
}
