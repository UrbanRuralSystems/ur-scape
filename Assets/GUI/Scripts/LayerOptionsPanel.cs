// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public abstract class LayerOptionsPanel : MonoBehaviour
{
    protected DataLayer dataLayer;

    public bool IsActive
    {
        get { return gameObject.activeSelf; }
    }
    
    //
    // Unity Mehods
    //

    protected virtual void OnDestroy()
    {
        if (dataLayer != null)
        {
            EnableListeners(false);
        }
    }

    //
    // Event Methods
    //

    protected virtual void OnPatchVisibilityChange(DataLayer dataLayer, Patch patch, bool visible)
    {
		if (ChangePanelVisibility(true))
		{
			GuiUtils.RebuildLayout(transform.parent);
		}
    }

    //
    // Public Mehods
    //

    public virtual void Init(DataLayer dataLayer)
    {
        this.dataLayer = dataLayer;
        ActivatePanel(true);
	}

    public virtual void Show(bool show)
    {
		ActivatePanel(show);
    }


	//
	// Private Mehods
	//
	private void ActivatePanel(bool activate)
	{
		EnableListeners(activate);

		ChangePanelVisibility(activate);
	}

	private bool ChangePanelVisibility(bool visible)
	{
		visible &= dataLayer.HasLoadedPatchesInView();
		if (visible != gameObject.activeSelf)
		{
			gameObject.SetActive(visible);
			OnPanelVisibilityChange(visible);
			return true;
		}
		return false;
	}

	protected virtual void EnableListeners(bool enable)
    {
        if (enable)
            dataLayer.OnPatchVisibilityChange += OnPatchVisibilityChange;
        else
            dataLayer.OnPatchVisibilityChange -= OnPatchVisibilityChange;
    }

	protected virtual void OnPanelVisibilityChange(bool visible) { }
}
