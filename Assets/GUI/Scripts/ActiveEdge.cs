// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ActiveEdge : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public Button button;
    public Texture2D cursorTexture;
    public CursorMode cursorMode = CursorMode.Auto;
    public Vector2 hotSpot = Vector2.zero;

    private float scale;
    private bool inside = false;
    private bool dragging = false;

    private float startMouseY;
	private float previousStartHeight;
	private float nextStartHeight;

	private LayoutElement previousElement;
	private LayoutElement nextElement;


    //
    // Unity Methods
    //

    void Start()
    {  
        var canvas = FindObjectOfType<Canvas>();
        scale = 1f / canvas.scaleFactor;
        
        int index = transform.GetSiblingIndex();
		previousElement = transform.parent.GetChild(index - 1).GetComponent<LayoutElement>();
		nextElement = transform.parent.GetChild(index + 1).GetComponent<LayoutElement>();
    }

    //
    // Inheritance Methods
    //

    public void OnPointerEnter(PointerEventData eventData)
    {
        inside = true;
        if (!dragging)
            Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        inside = false;
        if (!dragging)
            Cursor.SetCursor(null, hotSpot, cursorMode);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (inside)
        {
            dragging = true;
            Cursor.SetCursor(cursorTexture, hotSpot, cursorMode);

            startMouseY = Input.mousePosition.y;

			previousStartHeight = previousElement.preferredHeight;
			if (previousStartHeight == -1)
				previousStartHeight = previousElement.GetComponent<RectTransform>().rect.height;

			nextStartHeight = nextElement.preferredHeight;
			if (nextStartHeight == -1)
				nextStartHeight = nextElement.GetComponent<RectTransform>().rect.height;
		}
	}

    public void OnPointerUp(PointerEventData eventData)
    {
        dragging = false;
        if (!inside)
        {
            Cursor.SetCursor(null, hotSpot, cursorMode);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
		var offset = (startMouseY - Input.mousePosition.y) * scale;
		offset = Mathf.Clamp(offset, previousElement.minHeight - previousStartHeight, nextStartHeight - nextElement.minHeight);

		if (previousElement.flexibleHeight == -1)
			previousElement.preferredHeight = previousStartHeight + offset;

		if (nextElement.flexibleHeight == -1)
			nextElement.preferredHeight = nextStartHeight - offset;
	}
}
