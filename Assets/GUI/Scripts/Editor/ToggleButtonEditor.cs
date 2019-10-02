// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEditor;
using UnityEditor.UI;

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

		toggle.pressedHasHighlight = EditorGUILayout.Toggle("Pressed Has Highlight", toggle.pressedHasHighlight);
	}
}
