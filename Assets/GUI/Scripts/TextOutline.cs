// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextOutline : Shadow
{
	[Range(0, 10)]
	public float width = 1;

	public override void ModifyMesh(VertexHelper vh)
	{
		if (!IsActive())
			return;

		var verts = new List<UIVertex>();

		vh.GetUIVertexStream(verts);

		var neededCpacity = verts.Count * 5;
		if (verts.Capacity < neededCpacity)
			verts.Capacity = neededCpacity;

		var w15 = width * 1.5f;
		var start = 0;
		var end = verts.Count;
		ApplyShadowZeroAlloc(verts, effectColor, start, end, width, width);
		start = end; end = verts.Count;
		ApplyShadowZeroAlloc(verts, effectColor, start, end, width, -width);
		start = end; end = verts.Count;
		ApplyShadowZeroAlloc(verts, effectColor, start, end, -width, width);
		start = end; end = verts.Count;
		ApplyShadowZeroAlloc(verts, effectColor, start, end, -width, -width);
		start = end; end = verts.Count;
		ApplyShadowZeroAlloc(verts, effectColor, start, end, 0, w15);
		start = end; end = verts.Count;
		ApplyShadowZeroAlloc(verts, effectColor, start, end, w15, 0);
		start = end; end = verts.Count;
		ApplyShadowZeroAlloc(verts, effectColor, start, end, -w15, 0);
		start = end; end = verts.Count;
		ApplyShadowZeroAlloc(verts, effectColor, start, end, 0, -w15);

		vh.Clear();
		vh.AddUIVertexTriangleStream(verts);
	}
}
