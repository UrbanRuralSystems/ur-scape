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

public delegate void OnDrawPointsDelegate(List<Vector3> points);
public delegate void OnDrawAreaDelegate();

public abstract class DrawingInfo
{
}

public abstract class DrawingTool : MonoBehaviour
{
    public virtual event UnityAction<DrawingInfo> OnDraw;
    public event UnityAction OnCancel;

    protected bool isActive = false;
    protected bool canDraw = false;
    protected bool isDrawing = false;
    protected bool isErasing = false;

    // Component references
    protected InputHandler inputHandler;

    

    //
    // UnityMethods
    //

    protected virtual void Awake()
    {
        // Silly statement to avoid compiler throwing a CS0067 "The event 'DrawingTool.OnDraw' is never used" warning
        var dummy = OnDraw;

        inputHandler = ComponentManager.Instance.Get<InputHandler>();
    }

    protected virtual void OnDestroy()
    {
        if (isActive)
        {
            DisableInput();
        }
    }


    //
    // Public Methods
    //

    public bool IsActive { get { return isActive; } }
    public bool CanDraw
    {
        get { return canDraw; }
        set { canDraw = value; }
    }

    public bool Erasing
    {
        get { return isErasing; }
        set { isErasing = value; }
    }

    public virtual void Activate()
    {
        gameObject.SetActive(true);
        isActive = true;

        EnableInput();
    }

    public virtual void Deactivate()
    {
        if (isActive)
        {
            DisableInput();
        }

        gameObject.SetActive(false);
        isActive = false;
    }


    //
    // Protected Methods
    //

    protected virtual void EnableInput()
    {
        inputHandler.OnLeftMouseDragStart += OnLeftMouseDragStart;
        inputHandler.OnLeftMouseDrag += OnLeftMouseDrag;
        inputHandler.OnLeftMouseDragEnd += OnLeftMouseDragEnd;
        inputHandler.OnLeftMouseUp += OnLeftMouseUp;
        inputHandler.OnRightMouseUp += OnRightMouseUp;
    }

    protected virtual void DisableInput()
    {
        inputHandler.OnLeftMouseDragStart -= OnLeftMouseDragStart;
        inputHandler.OnLeftMouseDrag -= OnLeftMouseDrag;
        inputHandler.OnLeftMouseDragEnd -= OnLeftMouseDragEnd;
        inputHandler.OnLeftMouseUp -= OnLeftMouseUp;
        inputHandler.OnRightMouseUp -= OnRightMouseUp;
    }

    protected abstract void OnLeftMouseDragStart();
    protected abstract void OnLeftMouseDrag();
    protected abstract void OnLeftMouseDragEnd();
    protected abstract void OnLeftMouseUp();

    protected virtual void OnRightMouseUp()
    {
        if (!inputHandler.IsDraggingRight)
        {
            if (OnCancel != null)
                OnCancel();
        }
    }

}
