// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class SnapshotMapLayer : GridMapLayer
{
	public bool autoAdjustLineThickness = true;

	public override void UpdateContent()
	{
		base.UpdateContent();
		if (autoAdjustLineThickness)
		{
			float feather = Mathf.Clamp(0.002f * grid.countX / transform.localScale.x, 0.03f, 2f);
			material.SetFloat("LineFeather", feather);
		}
	}
}
