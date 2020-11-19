// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

#if UNITY_EDITOR
#define SAFETY_CHECK
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class ContoursMapLayer : GridMapLayer
{
	public ComputeShader compute;
	public bool autoAdjustLineThickness = true;

	public readonly List<GridData> grids = new List<GridData>();
	private readonly HashSet<DataLayer> layers = new HashSet<DataLayer>();
	private bool layersNeedUpdate;

	public int SelectedContour { get; private set; }

	private ContoursGenerator generator;
	private MapCamera mapCamera;
	private bool cropWithViewArea = true;
	public bool CropWithViewArea { get { return cropWithViewArea; } }
	private readonly Vector2[] boundaryPoints = new Vector2[4] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
	private Coroutine delayedUpdate;

	public bool NeedsUpdate { get { return layersNeedUpdate; } }


	//
	// Inheritance Methods
	//

	public override void Init(MapController map, GridData grid)
    {
        base.Init(map, grid);

        SetSelectedContour(SelectedContour);

#if !USE_TEXTURE
		if (SystemInfo.supportsComputeShaders && compute != null)
		{
			generator = new ContoursGenerator_GPU(this);
		}
		else
#endif
		{
			generator = new ContoursGenerator_CPU(this);
		}

		mapCamera = ComponentManager.Instance.Get<MapCamera>();
	}

	protected override void OnDestroy()
    {
        base.OnDestroy();

		map.OnPreLevelChange -= OnPreLevelChange;
		map.OnPostLevelChange -= OnPostLevelChange;
		map.OnMapUpdate -= OnMapUpdate;

		if (generator != null)
		{
			generator.Release();
			generator = null;
		}
	}

	public override void Show(bool show)
	{
		base.Show(show);

		if (show)
		{
			map.OnPreLevelChange += OnPreLevelChange;
			map.OnPostLevelChange += OnPostLevelChange;
			if (cropWithViewArea)
			{
				map.OnMapUpdate += OnMapUpdate;
				UpdateBoundaryPoints();
			}
		}
		else
		{
			map.OnPreLevelChange -= OnPreLevelChange;
			map.OnPostLevelChange -= OnPostLevelChange;
			map.OnMapUpdate -= OnMapUpdate;
		}
	}

	public void SetCropWithViewArea(bool crop)
	{
		if (cropWithViewArea != crop)
		{
			cropWithViewArea = crop;
			if (crop)
			{
				map.OnMapUpdate += OnMapUpdate;
				UpdateBoundaryPoints();
			}
			else
			{
				map.OnMapUpdate -= OnMapUpdate;
			}
		}
	}

	public override void UpdateContent()
	{
		base.UpdateContent();
		if (autoAdjustLineThickness)
		{
			AdjustLineThickness();
		}
	}

	//
	// Event Methods
	//

	private bool avoidRefreshDuringLevelChange = false;
	private void OnPreLevelChange(int level)
	{
		avoidRefreshDuringLevelChange = true;
		StartDelayedUpdateData();
	}

	private void OnPostLevelChange(int level)
    {
		avoidRefreshDuringLevelChange = false;
    }

	private void OnMapUpdate()
	{
		UpdateBoundaryPoints();
		Refresh();
	}

	private void OnOtherGridChange(GridData otherGrid)
	{
		UpdateData();
	}

	private void OnOtherGridFilterChange(GridData grid)
	{
		UpdateData();
	}


	//
	// Public Methods
	//

	public void Add(GridData otherGrid)
    {
		// Ignore Network patches
		if (otherGrid.patch != null && otherGrid.patch is GraphPatch)
			return;

		otherGrid.OnGridChange += OnOtherGridChange;
		otherGrid.OnValuesChange += OnOtherGridChange;
		otherGrid.OnFilterChange += OnOtherGridFilterChange;
        
        grids.Add(otherGrid);
		layersNeedUpdate = true;
	}

    public void Remove(GridData otherGrid)
    {
        otherGrid.OnGridChange -= OnOtherGridChange;
		otherGrid.OnValuesChange -= OnOtherGridChange;
		otherGrid.OnFilterChange -= OnOtherGridFilterChange;

        grids.Remove(otherGrid);

		if (grids.Count == 0)
			layers.Clear();
		else
			layersNeedUpdate = true;
	}

	public void Clear()
    {
        foreach (var g in grids)
        {
            g.OnGridChange -= OnOtherGridChange;
			g.OnValuesChange -= OnOtherGridChange;
			g.OnFilterChange -= OnOtherGridFilterChange;
        }
        grids.Clear();
		layers.Clear();
	}

	public void Refresh()
	{
		if (avoidRefreshDuringLevelChange)
			return;

		if (grids.Count > 0 && !UpdateBounds())
		{
			Debug.LogError("Invalid contour area!");
			return;
		}

		UpdateData();

		if (autoAdjustLineThickness)
		{
			AdjustLineThickness();
		}
	}

	public void CreateBuffer()
	{
#if SAFETY_CHECK
		var count = grid.countX * grid.countY;
		if (grid.values != null && grid.values.Length == count)
			Debug.LogWarning("CreateBuffer: creating new buffer with same size!");
#if USE_TEXTURE
		if (valuesBuffer != null && grid.countX == valuesBuffer.width && grid.countY == valuesBuffer.height)
#else
        if (valuesBuffer != null && valuesBuffer.count == count)
#endif
			Debug.LogWarning("CreateBuffer: creating new buffer with same size!");
#endif
		grid.InitGridValues(false);
		CreateValuesBuffer();
	}

	public void ExcludeCellsWithNoData(bool exclude)
	{
		generator.excludeCellsWithNoData = exclude;
	}

	public void DeselectContour()
	{
		SetSelectedContour(0);
	}

    public void SetSelectedContour(int index)
    {
		SelectedContour = index;
        if (SelectedContour > 1)
        {
            material.EnableKeyword("BLINK");
        }
        else
        {
            material.DisableKeyword("BLINK");
        }
        material.SetInt("SelectedValue", SelectedContour);
    }

    public int GetContoursCount(bool selected)
    {
        int sum = 0;
        if (grid.values != null)
        {
            int count = grid.values.Length;
			if (selected)
			{
				if (SelectedContour == 0)
					return 0;

				for (int i = 0; i < count; ++i)
				{
					if (grid.values[i] == SelectedContour)
						++sum;
				}
			}
			else
			{
				for (int i = 0; i < count; ++i)
				{
					if (grid.values[i] > 0)
						++sum;
				}
			}
		}
        return sum;
    }


	//
	// Private Methods
	//

	private bool UpdateBounds()
	{
		double dotsPerDegreeX = 0;
		double dotsPerDegreeY = 0;

		double west = double.MaxValue;
		double east = double.MinValue;
		double north = double.MinValue;
		double south = double.MaxValue;

		foreach (var g in grids)
		{
			dotsPerDegreeX = Math.Max(g.countX / (g.east - g.west), dotsPerDegreeX);
			dotsPerDegreeY = Math.Max(g.countY / (g.north - g.south), dotsPerDegreeY);
			west = Math.Min(west, g.west);
			east = Math.Max(east, g.east);
			north = Math.Max(north, g.north);
			south = Math.Min(south, g.south);
		}

		if (east <= west || north <= south)
			return false;

		// Calculate grid resolution
		grid.countX = (int)Math.Round((east - west) * dotsPerDegreeX);
		grid.countY = (int)Math.Round((north - south) * dotsPerDegreeY);

		var count = grid.countX * grid.countY;
		if (grid.values != null && grid.values.Length != count)
		{
			grid.values = null;
		}

#if USE_TEXTURE
		if (valuesBuffer != null && (grid.countX != valuesBuffer.width || grid.countY != valuesBuffer.height))
#else
        if (valuesBuffer != null && valuesBuffer.count != count)
#endif
		{
			ReleaseValuesBuffer();
		}

		// Don't update the material here. Material is updated later in the _UpdateData method

		if (grid.west != west || grid.east != east || grid.north != north || grid.south != south)
		{
			grid.ChangeBounds(west, east, north, south);
		}

		return true;
	}

	private void UpdateData()
	{
		if (delayedUpdate == null)
			_UpdateData();
		else
			lastDelayedFrame = Time.frameCount + 1;
	}

	private void StartDelayedUpdateData()
	{
		if (delayedUpdate != null)
			return;

		ShowRenderer(false);
		delayedUpdate = StartCoroutine(DelayedUpdateData());
	}

	private int lastDelayedFrame = 0;
	private IEnumerator DelayedUpdateData()
	{
		do
		{
			yield return null;
		}
		while (Time.frameCount <= lastDelayedFrame);
		delayedUpdate = null;

		Refresh();
	}

	private void _UpdateData()
	{
		bool hasGrids = grids.Count > 0;
		if (hasGrids)
		{
			if (layersNeedUpdate)
			{
				UpdateLayerCount();

				// Update the material
				UpdateResolution();
			}

			generator.InitializeValues(layers.Count, cropWithViewArea ? boundaryPoints : null);
			generator.CalculateValues();

			grid.OnValuesChange -= base.OnGridValuesChange;
			grid.ValuesChanged();
			grid.OnValuesChange += base.OnGridValuesChange;

		}
		else
		{
			layersNeedUpdate = false;
			grid.values = null;
			grid.countX = 0;
			grid.countY = 0;
			ReleaseValuesBuffer();
		}

		ShowRenderer(hasGrids);
	}

	private void ShowRenderer(bool show)
	{
		if (show ^ GetComponent<MeshRenderer>().enabled)
		{
			GetComponent<MeshRenderer>().enabled = show;
		}
	}
	
	private void UpdateLayerCount()
	{
		layers.Clear();
		foreach (var grid in grids)
		{
			if (!layers.Contains(grid.patch.DataLayer))
				layers.Add(grid.patch.DataLayer);
		}
		layersNeedUpdate = false;
	}

	private void AdjustLineThickness()
	{
		float feather = Mathf.Clamp(0.002f * grid.countX / transform.localScale.x, 0.01f, 2f);
		material.SetFloat("LineFeather", feather);
	}

	private void UpdateBoundaryPoints()
	{
		var currentMeters = map.MapCenterInMeters;
		var unitsToMeters = map.UnitsToMeters;
		var pts = mapCamera.BoundaryPoints;
		for (int i = 0; i < pts.Length; i++)
		{
			boundaryPoints[i].x = (float)(currentMeters.x + pts[i].x * unitsToMeters);
			boundaryPoints[i].y = (float)(currentMeters.y + pts[i].y * unitsToMeters);
		}
	}
}
