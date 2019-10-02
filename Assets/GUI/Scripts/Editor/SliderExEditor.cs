// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEditor;
using UnityEditor.UI;

[CustomEditor(typeof(SliderEx))]
public class SliderExEditor : SliderEditor
{
	private SliderEx slider;

	protected override void OnEnable()
	{
		base.OnEnable();
		slider = (SliderEx)target;
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		EditorGUILayout.Space();

		slider.useCenter = EditorGUILayout.Toggle("Use Center", slider.useCenter);
	}
}
