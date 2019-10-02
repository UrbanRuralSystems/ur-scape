// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

public class ProgressInfo
{
	public float value = 0;
	private float scale = 1;
	private ProgressInfo parent = null;
	private ProgressInfo child = null;
	public ProgressInfo Push(float scale = 1)
	{
		if (child == null)
			child = new ProgressInfo();
		child.parent = this;
		child.scale = scale;
		return child;
	}
	public void Pop()
	{
		if (child != null)
		{
			value += child.scale;
			child = null;
		}
	}
	public ProgressInfo End()
	{
		if (parent != null)
		{
			parent.value += scale;
			parent.child = null;
		}
		return parent;
	}
	public float Total()
	{
		var item = this;
		while (item.parent != null)
			item = item.parent;

		float scale = 1;
		float total = 0;
		do
		{
			scale *= item.scale;
			total += item.value * scale;
			item = item.child;
		}
		while (item != null);
		return total;
	}
}