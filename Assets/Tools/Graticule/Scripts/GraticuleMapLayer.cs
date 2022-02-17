// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class GraticuleMapLayer : MapLayer
{
	protected Material material;
	private Texture2D projection;
	private AreaBounds bounds;

	private float intervalX = 1000000;
	private float intervalY = 1000000;
	private bool degrees = false;
	private bool locked = false;

	private const int ProjectionResolution = 101;
	private const double HalfPI = Math.PI * 0.5;
	private const double Rad2Deg = 180.0 / Math.PI;

	public float IntervalX { get { return intervalX; } }
	public float IntervalY { get { return intervalY; } }

	//
	// Unity Methods
	//

	protected virtual void OnEnable()
	{
		material = GetComponent<MeshRenderer>().material;
		if (projection == null)
			CreateProjectionBuffer(ProjectionResolution);
	}

	protected void OnDestroy()
	{
		ReleaseProjectionBuffer();
	}


	//
	// Inheritance Methods
	//

	public override void Init(MapController map)
	{
		base.Init(map);

		var viewBounds = map.ViewBounds;
		transform.localPosition = new Vector3(0, 0, -0.0012f);
		transform.localScale = new Vector3(viewBounds.width, viewBounds.height, 1);

		SetInterval(intervalX, intervalY, degrees);
		SetLocked(locked);
		UpdateContent();
	}

	public override void UpdateContent()
	{
		bounds = map.MapCoordBounds;

		if (degrees)
		{
			UpdateProjectionValues();
		}
		else
		{
			// Convert Lon/Lat to meters
			var meters = GeoCalculator.LonLatToMeters(bounds.east, bounds.north);
			bounds.east = meters.x;
			bounds.north = meters.y;
			meters = GeoCalculator.LonLatToMeters(bounds.west, bounds.south);
			bounds.west = meters.x;
			bounds.south = meters.y;
		}

		var invBoundsX = 1.0 / (bounds.east - bounds.west);
		var invBoundsY = 1.0 / (bounds.north - bounds.south);
		var interval = new Vector4((float)(intervalX * invBoundsX), (float)(intervalY * invBoundsY), 0, 0);
		var offsetX = (float)(((0.5f * (bounds.east + bounds.west)) % intervalX) * invBoundsX);
		var offsetY = (float)(((0.5f * (bounds.north + bounds.south)) % intervalY) * invBoundsY);
		var offset = new Vector4(offsetX, offsetY, 0, 0);

		if (degrees)
			offset.y = -offset.y;

		material.SetVector("Interval", interval);
		material.SetVector("Offset", offset);
	}


	//
	// Public Methods
	//

	public void SetInterval(float x, float y, bool degrees)
	{
		intervalX = x;
		intervalY = y;

		this.degrees = degrees;
		if (degrees)
			material.EnableKeyword("USE_DEGREES");
		else
			material.DisableKeyword("USE_DEGREES");
	}

	public void SetLocked(bool locked)
	{
		this.locked = locked;
		if (locked)
			material.EnableKeyword("LOCKED");
		else
			material.DisableKeyword("LOCKED");
	}

	public void SetColor(Color color)
	{
		material.SetColor("Tint", color);
	}


	//
	// Private/Protected Methods
	//

	private void UpdateProjectionValues()
	{
		double min = GeoCalculator.LatitudeToNormalizedMercator(bounds.south);
		double max = GeoCalculator.LatitudeToNormalizedMercator(bounds.north);
		double invLatRange = 1.0 / (bounds.north - bounds.south);

		float[] lats = new float[ProjectionResolution];
		double projLatInterval = (max - min) / (ProjectionResolution - 1);
		for (int i = 0; i < ProjectionResolution; i++)
		{
			double projLat = min + i * projLatInterval;
			double lat = (2 * Math.Atan(Math.Exp(projLat * Math.PI)) - HalfPI) * Rad2Deg;
			lats[i] = Mathf.Clamp01((float)(1 - (lat - bounds.south) * invLatRange));
		}
		byte[] latBytes = new byte[lats.Length * 4];
		Buffer.BlockCopy(lats, 0, latBytes, 0, latBytes.Length);
		projection.LoadRawTextureData(latBytes);
		projection.Apply();
	}

	private void CreateProjectionBuffer(int count)
	{
		ReleaseProjectionBuffer();

		projection = new Texture2D(count, 1, TextureFormat.RFloat, false);
		projection.wrapMode = TextureWrapMode.Clamp;
		projection.filterMode = FilterMode.Bilinear;
		material.SetTexture("Projection", projection);
		material.SetInt("CountY", count - 1);
	}

	private void ReleaseProjectionBuffer()
	{
		if (projection != null)
		{
			Destroy(projection);
			projection = null;
		}
	}

}
