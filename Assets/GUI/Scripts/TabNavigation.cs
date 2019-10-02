// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TabNavigation : MonoBehaviour
{
    public bool findFirstSelectable = false;

    //
    // Unity Methods
    //

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (EventSystem.current != null)
            {
                GameObject selected = EventSystem.current.currentSelectedGameObject;

                if (selected == null && findFirstSelectable)
                {
                    if (Selectable.allSelectablesArray.Length > 0)
                    {
                        selected = Selectable.allSelectablesArray[0].gameObject;
                    }
                }

                if (selected != null)
                {
                    Selectable current = (Selectable)selected.GetComponent("Selectable");
                    if (current != null)
                    {
                        Selectable next;
                        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                        {
							if (current.navigation.mode == Navigation.Mode.Explicit)
							{
								if ((next = current.navigation.selectOnLeft) != null)
									next.Select();
								else if ((next = current.navigation.selectOnUp) != null)
									next.Select();
							}
							else
							{
								if ((next = current.FindSelectableOnLeft()) != null)
									next.Select();
								else if ((next = current.FindSelectableOnUp()) != null)
									next.Select();
							}
						}
                        else
                        {
							if (current.navigation.mode == Navigation.Mode.Explicit)
							{
								if ((next = current.navigation.selectOnRight) != null)
									next.Select();
								else if ((next = current.navigation.selectOnDown) != null)
									next.Select();
							}
							else
							{
								if ((next = current.FindSelectableOnRight()) != null)
									next.Select();
								else if ((next = current.FindSelectableOnDown()) != null)
									next.Select();
							}
						}
                    }
                }
            }
        }
    }
}