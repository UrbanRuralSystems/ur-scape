// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

[CreateAssetMenu(menuName = "URS/Tool Config")]
public class ToolConfig : ScriptableObject
{
	public string label;
	public Sprite icon;
	public GameObject panelPrefab;
	public bool enabled = true;
}
