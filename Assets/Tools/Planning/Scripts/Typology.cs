// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;

public class TypologyInfo
{
	public string units;
	public bool isRatio;
}

public class Typology
{
    public string name;
    public Color color;
	public HashSet<string> sites = new HashSet<string>();
	public Dictionary<string, float> values = new Dictionary<string, float>();
    public string description;
    public string author;

	public static Dictionary<string, TypologyInfo> info = new Dictionary<string, TypologyInfo>();

	public void Clear()
    {
        values.Clear();
	}
}
