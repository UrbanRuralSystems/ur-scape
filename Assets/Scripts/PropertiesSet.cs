// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine.Events;

public abstract class PropertiesSet
{
	public bool HaveChanged { get; private set; } = false;

	public event UnityAction OnPropertiesChanged;

	private readonly List<PropertyBase> properties = new List<PropertyBase>();

	protected void Add(PropertyBase property)
	{
		property.OnValueChanged += OnPropertyValueChanged;
		properties.Add(property);
	}

	public void Revert()
	{
		foreach (var property in properties)
			property.Revert();

		HaveChanged = false;
	}

	public void Apply()
	{
		foreach (var property in properties)
			property.Apply();

		HaveChanged = false;
	}

	private void OnPropertyValueChanged()
	{
		HaveChanged = false;
		foreach (var property in properties)
			HaveChanged |= property.HasChanged;

		OnPropertiesChanged?.Invoke();
	}

}
