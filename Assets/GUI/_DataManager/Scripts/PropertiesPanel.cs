// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.Events;

public abstract class PropertiesPanel : MonoBehaviour
{
	public event UnityAction OnPropertiesChanged;

	public abstract bool HasChanges();
	public abstract void DiscardChanges();
	public abstract bool SaveChanges();

	protected void InvokeOnPropertiesChanged() => OnPropertiesChanged?.Invoke();

	protected Site activeSite;
	protected DataManager dataManager;
	protected ModalDialogManager dialogManager;
	protected DataManagerPanel dataManagerPanel;


	//
	// Public Methods
	//

	public void Init(DataManagerPanel dataManagerPanel)
	{
		this.dataManagerPanel = dataManagerPanel;

		dialogManager = ComponentManager.Instance.Get<ModalDialogManager>();
	}


	//
	// Protected Methods
	//

	protected void Ask(string question, PropertyBase property, UnityAction yes, UnityAction no)
	{
		if (property.IsSet || activeSite == null || dataManager.sites.Count == 1)
		{
			yes();
		}
		else
		{
			dialogManager.Ask(question, yes, no);
		}
	}
}
