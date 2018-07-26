// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RequestHandlerBase), true)]
public class RequestHandlerEditor : Editor
{
    private RequestHandlerBase handler;

    void OnEnable()
    {
        handler = (RequestHandlerBase) target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        int total = handler.TotalCount;
        float invMax = total == 0 ? 0 : 1f / total;
        int concurrent = handler.MaxConcurrent;
        int pending = handler.PendingCount;
        int running = handler.RunningCount;
        int finished = handler.FinishedCount;

        GUILayout.Label("Pending requests");
        Rect r = EditorGUILayout.BeginVertical();
        EditorGUI.ProgressBar(r, pending * invMax, pending + " / " + total);
        GUILayout.Space(16);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        GUILayout.Label("Running requests");
        r = EditorGUILayout.BeginVertical();
        EditorGUI.ProgressBar(r, (float)running / concurrent, running + " / " + concurrent);
        GUILayout.Space(16);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        GUILayout.Label("Finished requests");
        r = EditorGUILayout.BeginVertical();
        EditorGUI.ProgressBar(r, finished * invMax, finished + " / " + total);
        GUILayout.Space(16);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
    }

    override public bool RequiresConstantRepaint() { return Application.isPlaying; }

}
