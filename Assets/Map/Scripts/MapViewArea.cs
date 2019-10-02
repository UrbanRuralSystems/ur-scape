// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public class MapViewArea : UrsComponent
{
	public delegate void OnMapViewAreaChangeDelegate();
    public event OnMapViewAreaChangeDelegate OnMapViewAreaChange;


	public RectTransform RectTransform { get; private set; }
	public Rect Rect { get { return RectTransform.rect; } }

	protected override void Awake()
	{
		base.Awake();
		RectTransform = GetComponent<RectTransform>();
	}

	private void OnRectTransformDimensionsChange()
    {
		if (!isActiveAndEnabled)
			return;

		OnMapViewAreaChange?.Invoke();
	}

	public bool IsMouseInside()
	{
		return Contains(Input.mousePosition);
	}

	public bool Contains(Vector3 pos)
	{
		return RectTransform.rect.Contains(WorldToLocal(pos));
	}

	public Vector3 WorldToLocal(Vector3 pos)
	{
		return RectTransform.InverseTransformPoint(pos);
	}

	public Vector3 LocalToWorld(Vector3 pos)
	{
		return RectTransform.TransformPoint(pos);
	}

	public Vector3 WorldCenter()
	{
		var center = LocalToWorld(RectTransform.rect.center);
		center.x -= Screen.width * 0.5f;
		center.y -= Screen.height * 0.5f;
		return center;
	}
}
