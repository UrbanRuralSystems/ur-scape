// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEditor;

[CustomEditor(typeof(DropdownWithInput))]
public class DropdownWithInputEditor : DropdownExEditor
{
	private DropdownWithInput dropdown;
    //private SerializedProperty valueProp;
    private SerializedProperty inputProp;
    

    protected override void OnEnable()
	{
		base.OnEnable();
        dropdown = target as DropdownWithInput;
        //valueProp = serializedObject.FindProperty("m_Value");
        inputProp = serializedObject.FindProperty("input");
    }

    public override void OnInspectorGUI()
	{
        base.OnInspectorGUI();

        EditorGUILayout.Space();

        serializedObject.Update();
        EditorGUILayout.PropertyField(inputProp);
        serializedObject.ApplyModifiedProperties();

        /*
        var ptr = typeof(SelectableEditor).GetMethod("OnInspectorGUI").MethodHandle.GetFunctionPointer();
        var method = (Action)Activator.CreateInstance(typeof(Action), this, ptr);
        method();

        EditorGUILayout.Space();

        dropdown.input = EditorGUILayout.ObjectField("Input", dropdown.input, typeof(InputField), true) as InputField;

        EditorGUILayout.Space();

        dropdown.template = EditorGUILayout.ObjectField("Template", dropdown.template, typeof(RectTransform), true) as RectTransform;
        dropdown.itemText = EditorGUILayout.ObjectField("Item Text", dropdown.itemText, typeof(Text), true) as Text;

        EditorGUILayout.Space();

        serializedObject.Update();
        int newValue = EditorGUILayout.IntField("Value", valueProp.intValue);
        if (newValue != valueProp.intValue)
        {
            valueProp.intValue = newValue;
            serializedObject.ApplyModifiedProperties();
        }
        */
    }
}
