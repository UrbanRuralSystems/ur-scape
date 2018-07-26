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

[CustomEditor(typeof(ProgressBar))]
public class ProgressBarEditor : ScrollbarEditor
{
    private ProgressBar progressBar;

    protected override void OnEnable()
    {
        base.OnEnable();
        progressBar = (ProgressBar)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space();
        progressBar.percentageText = EditorGUILayout.ObjectField("Percentage Text", progressBar.percentageText, typeof(Text), true) as Text;
        EditorGUILayout.Space();
    }
}
