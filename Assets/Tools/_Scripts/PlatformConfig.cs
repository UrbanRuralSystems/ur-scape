// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEditor;
using UnityEngine;

public enum ToolState
{
	Enabled,
	Disabled,
	Hidden
}

[CreateAssetMenu(menuName = "URS/Platform Config")]
public class PlatformConfig : ScriptableObject
{
	[System.Serializable]
	public class PlatformToolConfig
	{
		public ToolConfig config;
		public ToolState state = ToolState.Enabled;
	}

#if UNITY_EDITOR
	[EnumFlags]
	public BuildTarget targets;
#endif

	public PlatformToolConfig[] tools;

}
