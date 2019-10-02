// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(DropdownEx))]
public class DropdownExEditor : DropdownEditor
{
    private SerializedProperty arrowProp;
    private SerializedProperty placeholderProp;
    //private SerializedProperty buttonsProp;
    private SerializedProperty allowEmptySelectionProp;
    private SerializedProperty disabledArrowProp;

    protected override void OnEnable()
	{
		base.OnEnable();

        arrowProp = serializedObject.FindProperty("arrow");
        placeholderProp = serializedObject.FindProperty("placeholder");
        //buttonsProp = serializedObject.FindProperty("buttons");
        allowEmptySelectionProp = serializedObject.FindProperty("allowEmptySelection");
        disabledArrowProp = serializedObject.FindProperty("disabledArrow");
    }

	public override void OnInspectorGUI()
	{
        base.OnInspectorGUI();

        EditorGUILayout.Space();

        serializedObject.Update();

        EditorGUILayout.PropertyField(arrowProp);
        EditorGUILayout.PropertyField(placeholderProp);
        //EditorGUILayout.PropertyField(buttonsProp, true);

        EditorGUILayout.PropertyField(allowEmptySelectionProp);
        EditorGUILayout.PropertyField(disabledArrowProp);

        serializedObject.ApplyModifiedProperties();
    }
}
