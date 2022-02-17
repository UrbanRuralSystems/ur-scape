// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public class BoundaryMapLayer : PatchMapLayer
{
	private Material material;

	private void OnEnable()
	{
		material = GetComponent<MeshRenderer>().material;
	}

	public void SetColor(Color color)
	{
		material.SetColor("Tint", color);
	}
	
}
