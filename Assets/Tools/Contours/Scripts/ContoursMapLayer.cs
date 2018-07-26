// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
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

	private int selectedContour = 0;
	private ContoursGenerator generator;


	//
	// Inheritance Methods
	//

	public override void Init(MapController map, GridData grid)
    {
        base.Init(map, grid);

        SetSelectedContour(selectedContour);

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
	}

	protected override void OnDestroy()
    {
        base.OnDestroy();

        map.OnPostLevelChange -= OnPostLevelChange;
    }

	public override void Show(bool show)
	{
		base.Show(show);

		if (show)
			map.OnPostLevelChange += OnPostLevelChange;
		else
			map.OnPostLevelChange -= OnPostLevelChange;
	}

	public override void UpdateContent()
	{
		base.UpdateContent();
		if (autoAdjustLineThickness)
		{
			float feather = Mathf.Clamp(0.002f * grid.countX / transform.localScale.x, 0.03f, 2f);
			material.SetFloat("LineFeather", feather);
		}
	}

	//
	// Event Methods
	//

	private void OnPostLevelChange(int level)
    {
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
		layersNeedUpdate = true;
	}

	public void Refresh()
	{
		if (grids.Count > 0 && !UpdateBounds())
		{
			Debug.LogError("Invalid contour area!");
			return;
		}

		UpdateData();
	}

	public void CreateBuffer()
	{
		CreateValuesBuffer();
	}

	public void ExcludeCellsWithNoData(bool exclude)
	{
		generator.excludeCellsWithNoData = exclude;
	}

    public void SetSelectedContour(int index)
    {
        selectedContour = index;
        if (selectedContour > 1)
        {
            material.EnableKeyword("BLINK");
        }
        else
        {
            material.DisableKeyword("BLINK");
        }
        material.SetInt("SelectedValue", selectedContour);
    }

    public int GetContoursCount(bool selected)
    {
        int sum = 0;
        if (grid.values != null)
        {
            int count = grid.values.Length;
            int value = selected ? selectedContour : 1;
            for (int i = 0; i < count; ++i)
            {
				if (grid.values[i] == value)
                    ++sum;
            }
        }
        return sum;
    }

	public float GetContoursSquareMeters()
    {
		if (grid.countX == 0)
			return 0;

		double x, y;
		GeoCalculator.GetDistanceInMeters(grid.west, grid.south, grid.east, grid.north, out x, out y);
		double xSize = x / grid.countX;
		double ySize = y / grid.countY;
		return (float)(xSize * ySize);
    }

    public float GetGridContouredData(GridData gridData, bool selected)
    {
		var contourGrid = grid;

		double contoursDegreesPerCellX = (contourGrid.east - contourGrid.west) / contourGrid.countX;
		double contoursDegreesPerCellY = (contourGrid.south - contourGrid.north) / contourGrid.countY;
		double contoursCellsPerDegreeX = 1.0 / contoursDegreesPerCellX;
		double contoursCellsPerDegreeY = 1.0 / contoursDegreesPerCellY;

		var cellsPerDegreeX = gridData.countX / (gridData.east - gridData.west);
		var cellsPerDegreeY = gridData.countY / (gridData.south - gridData.north);

		double scaleX = cellsPerDegreeX * contoursDegreesPerCellX;
		double scaleY = cellsPerDegreeY * contoursDegreesPerCellY;

		double offsetX = (contourGrid.west - gridData.west) * cellsPerDegreeX + 0.5 * scaleX;
		double offsetY = (contourGrid.north - gridData.north) * cellsPerDegreeY + 0.5 * scaleY;

		int startX = (int)((gridData.west - contourGrid.west) * contoursCellsPerDegreeX + 0.5);
		int startY = (int)((gridData.north - contourGrid.north) * contoursCellsPerDegreeY + 0.5);
		int endX = (int)((gridData.east - contourGrid.west) * contoursCellsPerDegreeX + 0.5);
		int endY = (int)((gridData.south - contourGrid.north) * contoursCellsPerDegreeY + 0.5);

		float sum = 0;
		int value = selected ? selectedContour : 1;
		for (int y = startY; y < endY; y++)
		{
			int contourIndex = y * contourGrid.countX + startX;
			for (int x = startX; x < endX; x++, contourIndex++)
			{
                if (contourGrid.values[contourIndex] == value)
				{
					int pX = (int)(offsetX + x * scaleX);
					int pY = (int)(offsetY + y * scaleY);
					int patchIndex = pY * gridData.countX + pX;
					sum += gridData.values[patchIndex];
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

		// Update the material
		UpdateResolution();

		if (grid.west != west || grid.east != east || grid.north != north || grid.south != south)
		{
			grid.ChangeBounds(west, east, north, south);
		}

		return true;
	}

	private void UpdateData()
	{
		bool hasGrids = grids.Count > 0;

		if (hasGrids)
		{
			if (layersNeedUpdate)
				UpdateLayerCount();

			generator.InitializeValues(layers.Count);
			generator.CalculateValues();
		}
		else
		{
			grid.values = null;
			grid.countX = 0;
			grid.countY = 0;
		}

		if (hasGrids ^ GetComponent<MeshRenderer>().enabled)
		{
			GetComponent<MeshRenderer>().enabled = hasGrids;
		}
	}
	
	private void UpdateLayerCount()
	{
		layers.Clear();
		foreach (var grid in grids)
		{
			if (!layers.Contains(grid.patch.dataLayer))
				layers.Add(grid.patch.dataLayer);
		}
		layersNeedUpdate = false;
	}
}
