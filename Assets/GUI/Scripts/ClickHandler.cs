// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)

using UnityEngine;
using UnityEngine.EventSystems;

public class ClickHandler : MonoBehaviour, IPointerClickHandler
{
    public delegate void OnClickDelegate(bool isLeft);
    public event OnClickDelegate OnClick;

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        
        if (pointerEventData.button == PointerEventData.InputButton.Left)
        {
            OnClick(true);
        }
    }
}
