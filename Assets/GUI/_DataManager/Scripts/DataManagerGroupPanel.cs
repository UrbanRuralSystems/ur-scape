// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;


public class DataManagerGroupPanel : PropertiesPanel
{
	[Header("UI References")]
	public InputField groupNameInput;


	[Header("UI References")]
	public bool autoApply = true;

	// Private Variables
	private LayerGroup activeGroup;
	private readonly GroupProperties properties = new GroupProperties();


	public class GroupProperties : PropertiesSet
	{
		public readonly Property<string> name = new Property<string>();

		public GroupProperties()
		{
			Add(name);
		}
	}
	

	//
	// Unity Methods
	//

	private void Start()
	{
		// Setup events
		properties.OnPropertiesChanged += InvokeOnPropertiesChanged;
		groupNameInput.onEndEdit.AddListener(OnEndGroupNameEdit);
		groupNameInput.onValidateInput += GuiUtils.ValidateNameInput;

		// Component references
		var componentManager = ComponentManager.Instance;
		dataManager = componentManager.Get<DataManager>();
	}


	//
	// Inheritance Methods
	//

	public override bool HasChanges()
	{
		return properties.HaveChanged;
	}

	public override void DiscardChanges()
	{
		properties.Revert();
	}

	public override bool SaveChanges()
	{
		if (activeGroup == null)
			return false;

		if (properties.name.HasChanged)
		{
			// Update group name
			activeGroup.ChangeName(properties.name);

			// Update accordion label
			dataManagerPanel.UpdateActiveGroup();

			if (!dataManagerPanel.UpdateBackend())
				return false;
		}

		properties.Apply();

		return true;
	}


	//
	// Event Methods
	//

	private void OnEndGroupNameEdit(string text)
	{
		var newName = text.Trim();
		if (newName.Equals(properties.name.Value))
			return;

		var translator = LocalizationManager.Instance;

		var question =
			translator.Get("Changing this group's name will affect all sites") +
			"\n\n" +
			translator.Get("Do you want to continue?");

		Ask(question, properties.name, () => SetGroupName(newName), RevertGroupName);
	}


	//
	// Public Methods
	//

	public void ShowProperties(LayerGroup group, Site site)
	{
		activeGroup = group;
		activeSite = site;
		if (activeGroup == null)
			return;

		properties.name.Init(activeGroup.name);

		groupNameInput.text = properties.name;
	}


	//
	// Private Methods
	//

	private void SetGroupName(string newName)
	{
		properties.name.Value = newName;
		groupNameInput.text = newName;

		if (autoApply)
		{
			SaveChanges();
			properties.name.Value = newName;
		}
	}

	private void RevertGroupName()
	{
		groupNameInput.text = properties.name;
	}

}
