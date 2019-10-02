// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
public class RangeDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		if (property.type.Equals("Vector2"))
		{
			// Using BeginProperty / EndProperty on the parent property means that
			// prefab override logic works on the entire property.
			EditorGUI.BeginProperty(position, label, property);

			var range = attribute as MinMaxRangeAttribute;
			var min = property.FindPropertyRelative("x");
			var max = property.FindPropertyRelative("y");
			var newMin = min.floatValue;
			var newMax = max.floatValue;

			var rect = EditorGUI.PrefixLabel(position, label);
			float left = rect.xMin;

			rect.xMin = left + 40;
			rect.xMax = position.xMax - 40;
			EditorGUI.MinMaxSlider(rect, ref newMin, ref newMax, range.min, range.max);
			newMin = Mathf.Clamp(newMin, range.min, range.max);
			newMax = Mathf.Clamp(newMax, range.min, range.max);

			rect.xMin = left;
			rect.xMax = rect.xMin + 40;
			EditorGUI.LabelField(rect, newMin.ToString("0.##"));

			rect.xMax = position.xMax;
			rect.xMin = rect.xMax - 35;
			EditorGUI.LabelField(rect, newMax.ToString("0.##"));

			min.floatValue = newMin;
			max.floatValue = newMax;

			EditorGUI.EndProperty();
		}
		else
		{
			//base.OnGUI(position, property, label);
			EditorGUI.PropertyField(position, property, label, true);
		}
	}
}