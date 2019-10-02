// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class OrphanFinder : EditorWindow
{
	private const string WindowName = "Orphan Finder";
	private const string PathsKey = "OrphanFinder_ExcludePaths";

	private GUIStyle listButtonStyle;
	private Vector2 m_scrollPosition = Vector2.zero;
	List<Object> orphans = new List<Object>();
	List<GameObject> references = new List<GameObject>();

	private string paths = "";

	private readonly HashSet<string> excludePaths = new HashSet<string>();

	[MenuItem("Assets/Find Orphans", false, 39)]
	static void FindObjectReferences()
	{
		var window = GetWindow<OrphanFinder>(true, WindowName, true);
		window.listButtonStyle = new GUIStyle(EditorStyles.miniButton);
		window.listButtonStyle.alignment = TextAnchor.MiddleLeft;

		window.Show();
	}

	private void OnEnable()
	{
		paths = EditorPrefs.GetString(PathsKey, "_Prototypes\nDiagrams\nGUI\\Icon\nMapbox\nPlugins\nWebGLTemplates");
	}

	private void OnDisable()
	{
		EditorPrefs.SetString(PathsKey, paths);
	}

	private void OnGUI()
	{
		EditorGUILayout.Space();
		GUILayout.Label("Directories to exclude:");
		paths = EditorGUILayout.TextArea(paths);
		EditorGUILayout.Space();

		GUILayout.BeginHorizontal();
		GUILayout.Label("Orphans: " + orphans.Count);
		if (GUILayout.Button("Refresh", EditorStyles.miniButton))
		{
			orphans.Clear();
			Refresh();
		}
		GUILayout.EndHorizontal();

		EditorGUILayout.Space();

		m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

		for (int i = orphans.Count - 1; i >= 0; --i)
		{
			LayoutItem(i, orphans[i]);
		}

		EditorGUILayout.EndScrollView();
	}

	void LayoutItem(int i, Object obj)
	{
		if (obj != null)
		{
			if (GUILayout.Button(obj.name, listButtonStyle))
			{
				Selection.activeObject = obj;
				EditorGUIUtility.PingObject(obj);
			}
		}
	}

	private void Refresh()
	{
		EditorUtility.DisplayProgressBar("Searching", "Generating asset list", 0);

		excludePaths.Clear();
		var pathsArray = paths.Split('\n');
		foreach (var path in pathsArray)
		{
			var p = path.Trim();
			if (!string.IsNullOrEmpty(p))
				excludePaths.Add(p);
		}

		List<string> assets = new List<string>();
		GetAssets("Assets", assets);

		EditorUtility.DisplayProgressBar("Searching", "Generating prefab list", 0);

		List<string> prefabs = new List<string>();
		GetPrefabs("Assets", prefabs);

		int count = assets.Count;
		float progressBarPos = 0;
		float progressDelta = 1f / count;
		for (int i = 0; i < count; i++)
		{
			var obj = AssetDatabase.LoadMainAssetAtPath(assets[i]);
			if (!HasReferences(obj, prefabs))
			{
				orphans.Add(obj);
				if (orphans.Count >= 100)
					break;
			}
			progressBarPos += progressDelta;
			EditorUtility.DisplayProgressBar("Searching", "Finding orphans", progressBarPos);
		}

		EditorUtility.ClearProgressBar();
	}

	private void GetAssets(string path, List<string> assets)
	{
		var items = Directory.GetFiles(path);
		int count = items.Length;
		for (int i = 0; i < count; ++i)
		{
			if (!items[i].EndsWith(".meta") &&
				!items[i].EndsWith(".cs") &&
				!items[i].EndsWith(".shader") &&
				!items[i].EndsWith(".cginc") &&
				!items[i].EndsWith(".unity"))
				assets.Add(items[i]);
		}

		items = Directory.GetDirectories(path);
		count = items.Length;
		for (int i = 0; i < count; ++i)
		{
			var subpath = items[i].Substring(7);		// 7 => "Assets/"
			if (!excludePaths.Contains(subpath))
				GetAssets(items[i], assets);
		}
	}

	private void GetPrefabs(string path, List<string> prefabs)
	{
		var items = Directory.GetFiles(path);
		int count = items.Length;
		for (int i = 0; i < count; ++i)
		{
			if (items[i].EndsWith(".prefab") ||
				items[i].EndsWith(".asset") || 
				items[i].EndsWith(".unity"))
				prefabs.Add(items[i]);
		}

		items = Directory.GetDirectories(path);
		count = items.Length;
		for (int i = 0; i < count; ++i)
		{
			GetPrefabs(items[i], prefabs);
		}
	}

	private bool HasReferences(Object toFind, List<string> prefabs)
	{
		Object[] tmpArray = new Object[1];
		references.Clear();

		int numPaths = prefabs.Count;
		for (int i = 0; i < numPaths; ++i)
		{
			tmpArray[0] = AssetDatabase.LoadMainAssetAtPath(prefabs[i]);
			if (tmpArray != null && tmpArray.Length > 0 && tmpArray[0] != toFind) // Don't add self
			{
				Object[] dependencies = EditorUtility.CollectDependencies(tmpArray);
				if (System.Array.Exists(dependencies, item => item == toFind))
				{
					// Don't add if another of the dependencies is already in there
					references.Add(tmpArray[0] as GameObject);
				}
			}
		}

		for (int i = references.Count - 1; i >= 0; i--)
		{
			tmpArray[0] = references[i];
			Object[] dependencies = EditorUtility.CollectDependencies(tmpArray);

			bool shouldRemove = false;

			for (int j = 0; j < dependencies.Length && shouldRemove == false; ++j)
			{
				Object dependency = dependencies[j];
				shouldRemove = (references.Find(item => item == dependency && item != tmpArray[0]) != null);
			}

			if (shouldRemove)
				references.RemoveAt(i);
		}

		return references.Count > 0;
	}
}
