// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEditor;
using UnityEngine;

public static class VersionHelper
{
	[MenuItem("Build/Update Version")]
	public static void UpdateVersion()
	{
		int major = 0;
		int minor = 0;
		int build = 0;

		string versionText = PlayerSettings.bundleVersion;
		if (!string.IsNullOrEmpty(versionText))
		{
			string[] parts = versionText.Trim().Split('.');

			if (parts.Length > 0) int.TryParse(parts[0], out major);
			if (parts.Length > 1) int.TryParse(parts[1], out minor);
			if (parts.Length > 2) int.TryParse(parts[2], out build);
		}

		build++;

		PlayerSettings.bundleVersion = major + "." + minor + "." + build; ;
		PlayerSettings.Android.bundleVersionCode = major * 100000 + minor * 1000 + build;
		PlayerSettings.macOS.buildNumber = PlayerSettings.bundleVersion;
		PlayerSettings.iOS.buildNumber = PlayerSettings.bundleVersion;

		AssetDatabase.SaveAssets();

		Debug.Log("Version updated to " + Application.version);
	}

	[MenuItem("Build/Build...")]
	public static void Build()
	{
		EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
	}
}
