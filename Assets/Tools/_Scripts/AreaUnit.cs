// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using UnityEngine;

[Serializable]
public class AreaUnit
{
	public string name;
	public string symbol;
	[Tooltip("Factor to convert from Sq. Meters to this unit")]
	public double factor;
}