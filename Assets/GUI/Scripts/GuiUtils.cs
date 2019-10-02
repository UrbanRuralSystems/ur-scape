// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public static class GuiUtils
{
    public static void RebuildLayout(Transform parent)
    {
        if (parent != null && parent.GetComponent<ContentSizeFitter>() != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent.GetComponent<RectTransform>());

            parent = parent.parent;
            while (parent.GetComponent<ContentSizeFitter>() != null)
                parent = parent.parent;

            LayoutRebuilder.MarkLayoutForRebuild(parent.GetComponent<RectTransform>());
        }
    }

	public static char ValidateNameInput(string input, int charIndex, char addedChar)
	{
		// Check if new character is valid
		if (char.IsLetterOrDigit(addedChar) || (addedChar == ' ' && charIndex > 0))
			return addedChar;

		// Otherwise return an empty character
		return '\0';
	}

}
