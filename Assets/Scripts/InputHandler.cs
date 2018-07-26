// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class InputEvent<T>
{
    private readonly List<T> handlers = new List<T>();

    public T LastHandler
    {
        get { return handlers[handlers.Count - 1]; }
    }

    public static InputEvent<T> operator +(InputEvent<T> ie, T del)
    {
        ie.handlers.Add(del);
        return ie;
    }

    public static InputEvent<T> operator -(InputEvent<T> ie, T del)
    {
        ie.handlers.Remove(del);
        return ie;
    }
}

public class InputHandler : UrsComponent
{
    public bool longPressAsRMB;
    public float longPressDelay = 1f;

    public bool LongPressAsRMB { get { return longPressAsRMB; } set { longPressAsRMB = value; } }

    // Dragging
    public bool IsLeftMouseDown { get; private set; }
    public bool IsRightMouseDown { get; private set; }
    public bool IsDraggingLeft { get; private set; }
    public bool IsDraggingRight { get; private set; }
    public Vector3 StartLeftDragPos { get; private set; }
    public Vector3 StartRightDragPos { get; private set; }
    public bool IsLongPress { get; private set; }

    public delegate void OnMouseEvent();
    public InputEvent<OnMouseEvent> OnLeftMouseDown = new InputEvent<OnMouseEvent>();
    public InputEvent<OnMouseEvent> OnRightMouseDown = new InputEvent<OnMouseEvent>();
    public InputEvent<OnMouseEvent> OnLeftMouseUp = new InputEvent<OnMouseEvent>();
    public InputEvent<OnMouseEvent> OnRightMouseUp = new InputEvent<OnMouseEvent>();
    public InputEvent<OnMouseEvent> OnLeftMouseDragStart = new InputEvent<OnMouseEvent>();
    public InputEvent<OnMouseEvent> OnRightMouseDragStart = new InputEvent<OnMouseEvent>();
    public InputEvent<OnMouseEvent> OnLeftMouseDrag = new InputEvent<OnMouseEvent>();
    public InputEvent<OnMouseEvent> OnRightMouseDrag = new InputEvent<OnMouseEvent>();
    public InputEvent<OnMouseEvent> OnLeftMouseDragEnd = new InputEvent<OnMouseEvent>();
    public InputEvent<OnMouseEvent> OnRightMouseDragEnd = new InputEvent<OnMouseEvent>();

    public delegate void OnMouseWheelEvent(float delta);
    public InputEvent<OnMouseWheelEvent> OnMouseWheel = new InputEvent<OnMouseWheelEvent>();

    private EventSystem eventSystem;
    private bool isPointerInUI;
	private bool ignoreIU;
	private Vector3 lastMousePosition;
    private float longPressTimeout;
    public Vector3 MouseDelta { get; private set; }
    public Vector3 PreviousMousePos { get { return lastMousePosition; } }

    // Ground plane (for raycasting)
    private readonly Plane plane = new Plane(Vector3.up, Vector3.zero);
    private Camera cam;

    public int PixelDragThreshold
    {
        get { return eventSystem.pixelDragThreshold; }
    }

    //
    // Unity Methods
    //

    protected override void Awake()
    {
        base.Awake();

        eventSystem = FindObjectOfType<EventSystem>();

        OnLeftMouseDown += DoNothing;
        OnRightMouseDown += DoNothing;
        OnLeftMouseUp += DoNothing;
        OnRightMouseUp += DoNothing;
        OnLeftMouseDragStart += DoNothing;
        OnRightMouseDragStart += DoNothing;
        OnLeftMouseDrag += DoNothing;
        OnRightMouseDrag += DoNothing;
        OnLeftMouseDragEnd += DoNothing;
        OnRightMouseDragEnd += DoNothing;
        OnMouseWheel += DoNothing;
    }

    private void Start()
    {
        cam = Camera.main;
    }

	private void Update()
    {
        MouseDelta = Input.mousePosition - lastMousePosition;

        if (Input.simulateMouseWithTouches)
        {
            if (Input.touchCount > 1)
            {
                if (IsLeftMouseDown)
                {
                    // Fake a LMB up event to stop single-touch drags/clicks
                    HandleLeftMouseUp();
                }
                else if (IsRightMouseDown)
                {
                    // Fake a RMB up event to stop single-touch drags/clicks
                    HandleRightMouseUp();
                }
            }
        }

        isPointerInUI = eventSystem.IsPointerOverGameObject() && !ignoreIU;
        for (int i = 0; i < Input.touchCount; i++)
        {
            isPointerInUI |= eventSystem.IsPointerOverGameObject(Input.GetTouch(i).fingerId);
        }

        // Can only click or start a drag when the cursor is outside the UI
        if (!isPointerInUI)
        {
            if (IsLeftMouseDown)
            {
                HandleLeftMouseDown();
            }
            else if (Input.GetMouseButtonDown(0))
            {
                HandleLeftMousePressed();
            }

            if (IsRightMouseDown)
            {
                HandleRightMouseDown();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                HandleRightMousePressed();
            }

			HandleMouseWheel();
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (IsLeftMouseDown)
            {
                HandleLeftMouseUp();
            }
            else if (IsLongPress)
            {
                // Fake a RMB up event
                HandleRightMouseUp();
            }
        }
        if (Input.GetMouseButtonUp(1) && IsRightMouseDown)
        {
            HandleRightMouseUp();
        }

        lastMousePosition = Input.mousePosition;
    }


	//
	// Public Methods
	//

	public void IgnoreUI(bool ignore)
	{
		ignoreIU = ignore;
	}


	//
	// Private Methods
	//

	private void HandleMouseWheel()
	{
		float deltaZ = Input.GetAxis("Mouse ScrollWheel");
		if (deltaZ != 0 && OnMouseWheel != null)
		{
			OnMouseWheel.LastHandler(deltaZ);
		}
	}

	private void HandleLeftMousePressed()
    {
        IsLeftMouseDown = true;
        StartLeftDragPos = Input.mousePosition;
        OnLeftMouseDown.LastHandler();

        longPressTimeout = Time.time + longPressDelay;
    }

    private void HandleRightMousePressed()
    {
        IsRightMouseDown = true;
        StartRightDragPos = Input.mousePosition;
        OnRightMouseDown.LastHandler();
    }

    private void HandleLeftMouseDown()
    {
        if (IsDraggingLeft)
        {
            OnLeftMouseDrag.LastHandler();
        }
        else
        {
            var offset = Input.mousePosition - StartLeftDragPos;
            if (Math.Abs(offset.x) > eventSystem.pixelDragThreshold ||
                Math.Abs(offset.y) > eventSystem.pixelDragThreshold)
            {
                IsDraggingLeft = true;
                OnLeftMouseDragStart.LastHandler();
            }
            else if (longPressAsRMB && Input.simulateMouseWithTouches && Time.time >= longPressTimeout)
            {
                // Fake a LMB up event
                HandleLeftMouseUp();

                IsLongPress = true;

                // Fake a RMB down event
                HandleRightMousePressed();
            }
        }
    }

    private void HandleRightMouseDown()
    {
        if (IsDraggingRight)
        {
            OnRightMouseDrag.LastHandler();
        }
        else
        {
            var offset = Input.mousePosition - StartRightDragPos;
            if (Math.Abs(offset.x) > eventSystem.pixelDragThreshold ||
                Math.Abs(offset.y) > eventSystem.pixelDragThreshold)
            {
                IsDraggingRight = true;
                OnRightMouseDragStart.LastHandler();
            }
        }
    }

    private void HandleLeftMouseUp()
    {
        if (!isPointerInUI || IsDraggingLeft)
        {
            OnLeftMouseUp.LastHandler();
        }
        if (IsDraggingLeft)
        {
            OnLeftMouseDragEnd.LastHandler();
            IsDraggingLeft = false;
        }

        IsLeftMouseDown = false;
    }

    private void HandleRightMouseUp()
    {
        if (!isPointerInUI || IsDraggingRight)
        {
            OnRightMouseUp.LastHandler();
        }
        if (IsDraggingRight)
        {
            OnRightMouseDragEnd.LastHandler();
            IsDraggingRight = false;
        }

        IsRightMouseDown = false;

        longPressTimeout = 0;
        IsLongPress = false;
    }

    public bool GetWorldPoint(Vector3 screenPos, out Vector3 pt)
    {
        float distance;
        Ray ray = cam.ScreenPointToRay(screenPos);
        if (plane.Raycast(ray, out distance))
        {
            pt = ray.GetPoint(distance);
            return true;
        }

        pt = Vector3.zero;
        return false;
    }

    private void DoNothing()
    {
        // Specifically do nothing
    }
    private void DoNothing(float delta)
    {
        // Specifically do nothing
    }

}
