// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEditor;
using UnityEngine;


[CustomPropertyDrawer(typeof(EnumFlagsAttribute))]
public class EnumFlagsAttributeDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		label = EditorGUI.BeginProperty(position, label, property);
		EditorGUI.BeginChangeCheck();
		int newValue = EditorGUI.MaskField(position, label, property.intValue, property.enumNames);
		if (EditorGUI.EndChangeCheck())
		{
			property.intValue = newValue;
		}
		EditorGUI.EndProperty();
	}
}