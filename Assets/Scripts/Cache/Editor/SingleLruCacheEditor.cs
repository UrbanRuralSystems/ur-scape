// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SingleLruCache), true)]
public class SingleLruCacheEditor : Editor
{
    private SingleLruCache cache;
    private float invMaxSize;

    void OnEnable()
    {
        cache = (SingleLruCache)target;
        invMaxSize = 1f / cache.MaxSize;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        int count = 0;
        if (Application.isPlaying)
        {
            count = cache.Count;
        }

        EditorGUILayout.Space();

        GUILayout.Label("Usage", EditorStyles.boldLabel);

        Rect r = EditorGUILayout.BeginVertical();
        EditorGUI.ProgressBar(r, count * invMaxSize, count + " / " + cache.MaxSize);
        GUILayout.Space(16);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
    }

    override public bool RequiresConstantRepaint() { return Application.isPlaying; }
}
