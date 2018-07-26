// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEditor;
using UnityEditor.UI;
using UnityEngine.UI;

[CustomEditor(typeof(ToggleButton))]
public class ToggleButtonEditor : ToggleEditor
{
	private ToggleButton toggle;

	protected override void OnEnable()
	{
		base.OnEnable();
		toggle = (ToggleButton)target;
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		EditorGUILayout.Space();

		toggle.label = EditorGUILayout.ObjectField("Label", toggle.label, typeof(Text), true) as Text;
		toggle.textColor = EditorGUILayout.ColorField("Label Color", toggle.textColor);
	}
}
