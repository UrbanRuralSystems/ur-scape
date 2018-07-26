// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TilePool))]
public class TilePoolEditor : Editor
{
    private TilePool pool;

    void OnEnable()
    {
        pool = (TilePool)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        string l = "0 / 0";
        float p = 0;
        if (Application.isPlaying)
        {
            l = pool.Available + " / " + pool.Total;
            p = (float)pool.Available / pool.Total;
        }

        EditorGUILayout.Space();

        GUILayout.Label("Available", EditorStyles.boldLabel);
        Rect r = EditorGUILayout.BeginVertical();
        EditorGUI.ProgressBar(r, p, l);
        GUILayout.Space(16);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
    }

    override public bool RequiresConstantRepaint() { return Application.isPlaying; }
}
