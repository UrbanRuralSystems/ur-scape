// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.Events;

public abstract class Tool : UrsComponent
{
    [Header("Tool")]
    public ToolLayerController layerControllerPrefab;

    protected MapController map;
    protected ToolLayerController toolLayers;
	protected ToolMessenger messenger;

	protected bool isActive = false;

	//
	// Unity Methods
	//

	protected override void Awake()
    {
        base.Awake();
        ComponentManager.Instance.OnRegistrationFinished += OnComponentRegistrationFinished;
    }


    //
    // Inheritance Methods
    //

    protected virtual void OnComponentRegistrationFinished()
    {
        // Get Components
        map = ComponentManager.Instance.Get<MapController>();

        // Get or create the tool layer controller
        toolLayers = map.GetOrCreateLayerController(layerControllerPrefab);
    }


    //
    // UI Events
    //

    public abstract void OnToggleTool(bool isOn);

    public virtual void OnActiveTool(bool isActive)
	{
		this.isActive = isActive;
	}


    //
    // Private Methods
    //

    protected void ShowMessage(string msg, MessageType type)
	{
		if (messenger == null)
		{
			CreateMessenger(transform);
		}

		messenger.Show(msg, type);
	}

	protected void SetExample(string example, UnityAction action = null)
	{
		messenger.SetExample(example, action);
	}

	protected void HideMessage()
	{
		if (messenger != null)
		{
			messenger.Hide();
		}
	}

	protected virtual void CreateMessenger(Transform parent)
	{
		messenger = ComponentManager.Instance.Get<Toolbox>().CreateMessenger(parent);
	}

}
