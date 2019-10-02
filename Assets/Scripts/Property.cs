// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

public abstract class PropertyBase
{
	public delegate void OnPropertyValueChanged();
	public event OnPropertyValueChanged OnValueChanged;

	public bool IsSet { get; protected set; } = false;
	public abstract bool HasChanged { get; }

	public abstract void Revert();
	public abstract void Apply();

	protected void InvokeOnValueChanged() => OnValueChanged?.Invoke();
}

public class Property<T> : PropertyBase
{
	public T OriginalValue { get; private set; } = default;

	private T propValue = default;
	public T Value
	{
		get { return propValue; }
		set { propValue = value; IsSet = true; InvokeOnValueChanged(); }
	}

	public override bool HasChanged { get { return IsSet && !Value.Equals(OriginalValue); } }

	public void Init(T value = default)
	{
		propValue = OriginalValue = value;
		IsSet = false;
	}

	public override void Revert()
	{
		propValue = OriginalValue;
		IsSet = false;
	}

	public override void Apply()
	{
		OriginalValue = propValue;
		IsSet = false;
	}

	public static implicit operator T(Property<T> rhs)
	{
		return rhs.Value;
	}

}
