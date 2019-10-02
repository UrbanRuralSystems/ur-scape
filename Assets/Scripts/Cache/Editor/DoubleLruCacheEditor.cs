// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DoubleLruCache), true)]
public class DoubleLruCacheEditor : Editor
{
    private static readonly Color UsedColor = new Color(0.42f, 0.62f, 0.97f);
    private static readonly Color UnusedColor = new Color(0.46f, 0.56f, 0.73f);
    private static readonly Color EmptyColor = new Color(0.72f, 0.72f, 0.72f);

    private DoubleLruCache cache;
    private float invMaxSize;

    void OnEnable()
    {
        cache = (DoubleLruCache)target;
        invMaxSize = 1f / cache.MaxSize;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        string lUsed = "0";
        string lUnused = "0";
        string lTotal = "0 (0%)";
        float pUsed = 0;
        float pUnused = 0;
        if (Application.isPlaying)
        {
            int total = cache.UnusedCount + cache.UsedCount;
            pUsed = cache.UsedCount * invMaxSize;
            pUnused = cache.UnusedCount * invMaxSize;
            int pTotal = Mathf.RoundToInt(100 * total * invMaxSize);

            lUsed = cache.UsedCount.ToString();
            lUnused = cache.UnusedCount.ToString();
            lTotal = total + " (" + pTotal + "%)";
        }

        EditorGUILayout.Space();

        GUILayout.Label("Usage", EditorStyles.boldLabel);

        Rect r = EditorGUILayout.BeginVertical();
        EditorGUI.DrawRect(r, Color.gray);
        r.xMax--; r.yMax--; r.xMin++; r.yMin++;
        if (Application.isPlaying)
        {
            float width = r.width;
            float xMax = r.xMax;
            r.xMax = r.xMin + pUsed * width;
            EditorGUI.DrawRect(r, UsedColor);
            r.xMin = r.xMax;
            r.xMax += pUnused * width;
            EditorGUI.DrawRect(r, UnusedColor);
            r.xMin = r.xMax;
            r.xMax = xMax;
        }
        EditorGUI.DrawRect(r, EmptyColor);
        GUILayout.Space(16);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Used", lUsed);
        EditorGUILayout.LabelField("Unused", lUnused);
        EditorGUILayout.LabelField("Total", lTotal);

        EditorGUILayout.Space();
    }

    override public bool RequiresConstantRepaint() { return Application.isPlaying; }
}
