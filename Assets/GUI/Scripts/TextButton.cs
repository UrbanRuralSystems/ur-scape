// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class TextButton : Button
{

    private Text text;
    private Color initialColor;


    //
    // Unity Methods
    //

    protected override void Awake()
    {
        base.Awake();

        text = GetComponentInChildren<Text>();
        if (text == null)
        {
            Debug.LogWarning("No text found for button " + name);
        }
        else
        {
            initialColor = text.color;
        }
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        text.color = interactable ? initialColor : initialColor * colors.disabledColor;
    }

}
