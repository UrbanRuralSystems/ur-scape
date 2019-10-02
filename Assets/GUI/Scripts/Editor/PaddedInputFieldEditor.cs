// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEditor;
using UnityEditor.UI;

[CanEditMultipleObjects]
[CustomEditor(typeof(PaddedInputField), true)]
public class PaddedInputFieldEditor : InputFieldEditor
{
	private SerializedProperty m_Padding;

	protected override void OnEnable()
	{
		base.OnEnable();
		m_Padding = serializedObject.FindProperty("m_Padding");
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		serializedObject.Update();
		EditorGUILayout.PropertyField(m_Padding, true);
		serializedObject.ApplyModifiedProperties();

		EditorGUILayout.Space();
	}

}
