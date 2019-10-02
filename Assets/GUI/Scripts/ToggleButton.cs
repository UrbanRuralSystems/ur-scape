// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
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
	public bool pressedHasHighlight = true;

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
	// Public Methods
	//

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
				newColors.highlightedColor = newColors.normalColor;
				if (!value || pressedHasHighlight)
					newColors.highlightedColor += hightlightOffset;
                newColors.pressedColor = newColors.highlightedColor;
                colors = newColors;
                break;
        }
    }
}
