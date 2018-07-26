// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TileMapLayer), true)]
public class MapTileLayerEditor : Editor
{

    private TileMapLayer layer;

    void OnEnable()
    {
        layer = (TileMapLayer)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        GUILayout.Label("Used / Unused ratio", EditorStyles.boldLabel);
        string label = "0 / 0";
        float percent = 0;
        if (Application.isPlaying)
        {
            int used = layer.UsedTiles;
            label = used + " / " + layer.TotalTiles;
            percent = used / (float)layer.TotalTiles;
        }

        EditorGUILayout.Space();

        Rect r = EditorGUILayout.BeginVertical();
        EditorGUI.ProgressBar(r, percent, label);
        GUILayout.Space(16);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
    }

    override public bool RequiresConstantRepaint() { return Application.isPlaying; }

}
