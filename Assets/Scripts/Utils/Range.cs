// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public class MinMaxRangeAttribute : PropertyAttribute
{
	public float min, max;

	public MinMaxRangeAttribute(float min, float max)
	{
		this.min = min;
		this.max = max;
	}
}
