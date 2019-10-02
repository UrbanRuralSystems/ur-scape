// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEditor.Callbacks;
using UnityEditor;
using System.IO;
using UnityEngine.Events;
using System;

public static class BuildExtras
{
	private const string UseTextureDefine = "USE_TEXTURE";
	private static readonly string[] ExtraFiles = new string[]
	{
		"LICENSE",
		"LICENSE-3RD-PARTY"
	};

	[InitializeOnLoadMethod]
	private static void Initialize()
	{
		BuildPlayerWindow.RegisterBuildPlayerHandler(BuildPlayer);
	}

	[MenuItem("Build/Increase Version")]
	public static void IncreaseVersion()
	{
		GetVersion(out int major, out int minor, out int build);

		build++;

		SetVersion(major, minor, build);
	}

	public static void GetVersion(out int major, out int minor, out int build)
	{
		major = 0;
		minor = 0;
		build = 0;

		string versionText = PlayerSettings.bundleVersion;
		if (!string.IsNullOrEmpty(versionText))
		{
			string[] parts = versionText.Trim().Split('.');

			if (parts.Length > 0) int.TryParse(parts[0], out major);
			if (parts.Length > 1) int.TryParse(parts[1], out minor);
			if (parts.Length > 2) int.TryParse(parts[2], out build);
		}
	}

	public static void SetVersion(int major, int minor, int build)
	{
		PlayerSettings.bundleVersion = major + "." + minor + "." + build;
		PlayerSettings.Android.bundleVersionCode = major * 100000 + minor * 1000 + build;
		PlayerSettings.macOS.buildNumber = PlayerSettings.bundleVersion;
		PlayerSettings.iOS.buildNumber = PlayerSettings.bundleVersion;

		AssetDatabase.SaveAssets();

		Debug.Log("Version updated to " + Application.version);
	}

	private static void BuildPlayer(BuildPlayerOptions options)
	{
		BuildTarget buildTarget = EditorUserBuildSettings.activeBuildTarget;

		BuildExtrasWindow.ShowWindow(buildTarget, () => {

			// Add "USE_TEXTURE" define symbol for WebGL builds
			if (buildTarget == BuildTarget.WebGL)
			{
				var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL);
				if (!defines.Contains(UseTextureDefine))
				{
					PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, defines + ";" + UseTextureDefine);
					AssetDatabase.SaveAssets();
				}
			}

			BuildPlayerWindow.DefaultBuildMethods.BuildPlayer(options);
		});
	}

	[PostProcessBuild(1)]
	public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
	{
		switch (target)
		{
			case BuildTarget.iOS:
				if (File.Exists("ios_post.sh"))
				{
					Command("ios_post.sh " + pathToBuiltProject);
				}
				break;
			case BuildTarget.WebGL:
				var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL);
				if (defines.Contains(UseTextureDefine))
				{
					int index = 0;
					if (defines == UseTextureDefine)
						PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, "");
					else if ((index = defines.IndexOf(UseTextureDefine)) >= 0)
						PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, defines.Remove(index, UseTextureDefine.Length));

					AssetDatabase.SaveAssets();
				}
				break;
		}

		if (!Directory.Exists(pathToBuiltProject))
			pathToBuiltProject = Path.GetDirectoryName(pathToBuiltProject);

		CopyExtraFiles(pathToBuiltProject);
	}

	private static void Command(string cmd)
	{
		var processInfo = new System.Diagnostics.ProcessStartInfo("/bin/bash", cmd)
		{
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true
		};

		var process = System.Diagnostics.Process.Start(processInfo);
		process.ErrorDataReceived += (_, e) => Debug.LogError(e.Data);
		process.OutputDataReceived += (_, e) => Debug.Log(e.Data);
		process.Exited += (_, e) => Debug.LogError(e.ToString());

		string line;
		while ((line = process.StandardOutput.ReadLine()) != null)
			Debug.Log(line);

		while ((line = process.StandardError.ReadLine()) != null)
			Debug.LogError(line);

		process.WaitForExit();
		process.Close();
	}

	private static void CopyExtraFiles(string path)
	{
		foreach (var file in ExtraFiles)
		{
			if (File.Exists(file))
				File.Copy(file, Path.Combine(path, file), true);
		}
	}
}

public class BuildExtrasWindow : EditorWindow
{
    private const string WindowName = "Build Settings for ur-scape";
    private const string ConfigKey = "BuildSettings_Config";

	private UnityAction callback;

    private BuildConfig buildConfig;
    private PlatformConfig[] platformConfigs;
    private int selected = -1;
	private int buildTargetBitMask = 0 ;

	private int major = 0;
	private int minor = 0;
	private int build = 0;

	public static void ShowWindow(BuildTarget target, UnityAction callback)
	{
		// Get existing open window or if none, make a new one:
		var wnd = GetWindow<BuildExtrasWindow>(WindowName);
		wnd.callback = callback;
		wnd.Init(target);
		wnd.Show();
	}

	public void Init(BuildTarget buildTarget)
	{
		var configName = EditorPrefs.GetString(ConfigKey, "Standalone");

		var buildConfigs = GetAllInstances<BuildConfig>();
		if (buildConfigs.Length == 1 && buildConfigs[0] != null)
		{
			buildConfig = buildConfigs[0];
			configName = buildConfig.platform.name;
		}

		int buildTargetId = (int)buildTarget;
		int index = 1;
		var type = typeof(BuildTarget);
		var targets = (int[])Enum.GetValues(type);
		Array.Sort(targets);
		foreach (int target in targets)
		{
			if (buildTargetId == target)
			{
				buildTargetBitMask = index;
				break;
			}
			index <<= 1;
		}

		platformConfigs = GetAllInstances<PlatformConfig>();
		for (int i = 0; i < platformConfigs.Length; i++)
		{
			if (configName.Equals(platformConfigs[i].name) && ((int)platformConfigs[i].targets & buildTargetBitMask) != 0)
				selected = i;
		}

		BuildExtras.GetVersion(out major, out minor, out build);
	}

	void OnGUI()
    {
		EditorGUILayout.Space();

		GUILayout.BeginVertical("Box");
		GUILayout.Label("Version:", EditorStyles.boldLabel);

		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		GUILayout.Label("Major");
		GUILayout.Label("Minor");
		GUILayout.Label("Build");
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		major = EditorGUILayout.IntField(major);
		minor = EditorGUILayout.IntField(minor);
		build = EditorGUILayout.IntField(build);

		GUILayout.EndHorizontal();
		if (GUILayout.Button("Increase", GUILayout.Width(120)))
		{
			BuildExtras.IncreaseVersion();
			BuildExtras.GetVersion(out major, out minor, out build);
		}

		GUILayout.EndVertical();

		EditorGUILayout.Space();

        GUILayout.BeginVertical("Box");
        GUILayout.Label("Build configuration:", EditorStyles.boldLabel);

		GUILayout.BeginHorizontal();
		GUILayout.Space(10);
		GUILayout.BeginVertical();
		for (int i = 0; i < platformConfigs.Length; i++)
		{
			var config = platformConfigs[i];
			bool disabled = ((int)config.targets & buildTargetBitMask) == 0;
			if (disabled)
				GUI.enabled = false;
			if (GUILayout.Toggle(i == selected, " " + config.name, EditorStyles.radioButton))
			{
				if (i != selected)
				{
					selected = i;
					buildConfig.platform = platformConfigs[selected];
					EditorUtility.SetDirty(buildConfig);
				}
			}
			if (disabled)
				GUI.enabled = true;
		}
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();

		GUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUI.BeginDisabledGroup(selected == -1);
        if (GUILayout.Button("Continue", GUILayout.Width(120)))
        {
            EditorPrefs.SetString(ConfigKey, platformConfigs[selected].name);
            AssetDatabase.SaveAssets();

			BuildExtras.GetVersion(out int oldMajor, out int oldMinor, out int oldBuild);
			if (oldBuild != build || oldMinor != minor || oldMajor != major)
			{
				BuildExtras.SetVersion(major, minor, build);
			}

			Close();
			callback?.Invoke();
			return;
        }
        EditorGUI.EndDisabledGroup();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(16);
    }

    public static T[] GetAllInstances<T>() where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
        T[] a = new T[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            a[i] = AssetDatabase.LoadAssetAtPath<T>(path);
        }

        return a;
    }

}
