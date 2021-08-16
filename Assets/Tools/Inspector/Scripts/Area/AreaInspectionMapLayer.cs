// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System;
using System.Collections.Generic;
using UnityEngine;

public class AreaInspectionMapLayer : GridMapLayer
{
    public readonly List<GridData> grids = new List<GridData>();
	public readonly Dictionary<GridData, List<float>> inspectedGridsData = new Dictionary<GridData, List<float>>();
    public bool useFilters = false;
	public int numOfSamplesX = 10;
	public int numOfSamplesY = 10;

    private List<Coordinate> coords;
    private List<Vector3> points;
    private Coordinate minPt;
    private Coordinate maxPt;

    public Coordinate MinPt { get { return minPt; } }
    public Coordinate MaxPt { get { return maxPt; } }
    public List<Vector3> Points { get { return points; } }

    //
    // Public Methods
    //

    public void Init(List<Coordinate> coords)
    {
        Refresh(coords);
    }

    public void Add(GridData otherGrid)
    {
        // Ignore Network patches
        if (otherGrid.patch != null && otherGrid.patch is GraphPatch)
            return;

        otherGrid.OnFilterChange += OnOtherGridFilterChange;

		if (!grids.Contains(otherGrid))
		{
			grids.Add(otherGrid);
		}
		else
		{
			int index = grids.FindIndex(a => a == otherGrid);
			grids[index] = otherGrid;
		}
		AddInspectedGridData(otherGrid);
	}

	public void Remove(GridData otherGrid)
    {
        otherGrid.OnFilterChange -= OnOtherGridFilterChange;

        grids.Remove(otherGrid);
		inspectedGridsData.Remove(otherGrid);
	}

	public void Clear()
    {
        foreach (var g in grids)
        {
            g.OnFilterChange -= OnOtherGridFilterChange;
        }
        grids.Clear();
		inspectedGridsData.Clear();
	}

    public void Refresh(List<Coordinate> coords)
    {
        if (grids.Count > 0 && !UpdateBounds())
        {
            Debug.LogError("Invalid area!");
            return;
        }

        this.points = ConvertToVec3List(coords);
        AssignMinMaxCoord(coords);
        UpdateData();
    }

    public void UpdateData()
    {
        bool hasGrids = grids.Count > 0;

        if (hasGrids)
        {
            InitializeValues();
			CalculateValues();
        }
        else
        {
            grid.values = null;
            grid.countX = 0;
            grid.countY = 0;
		}
	}

    public bool IsPointInPoly(Coordinate p, List<Vector3> poly)
    {
        var pos = map.GetUnitsFromCoordinates(p);

        bool c = false;
        int count = poly.Count;
        int j = count - 1;
        for (int i = 0; i < count; j = i++)
        {
            c ^= poly[i].z > pos.y ^ poly[j].z > pos.y && pos.x < (poly[j].x - poly[i].x) * (pos.y - poly[i].z) / (poly[j].z - poly[i].z) + poly[i].x;
        }
        return c;
    }

    public double ComputeTotalArea()
    {
        // Cache grid transformations
        var gridCoordsToCell = new Distance(grid.countX / (grid.east - grid.west), grid.countY / (grid.south - grid.north));
        var gridCellToCoords = new Distance((grid.east - grid.west) / grid.countX, (grid.south - grid.north) / grid.countY);

        double scaleX = (grid.east - grid.west) / grid.countX;
        double scaleY = (grid.south - grid.north) / grid.countY;

        double metersY1 = Math.Log(Math.Tan((90d + grid.north) * GeoCalculator.Deg2HalfRad)) * GeoCalculator.Rad2Meters;
		double dx = scaleX * GeoCalculator.Deg2Meters;

        double sqm = 0.0;

        Coordinate coords = new Coordinate();
        for (int y = 1; y <= grid.countY; ++y)
        {
            coords.Latitude = (y + 0.5) * gridCellToCoords.y + grid.north;

            double metersY2 = Math.Log(Math.Tan((90d + (grid.north + y * scaleY)) * GeoCalculator.Deg2HalfRad)) * GeoCalculator.Rad2Meters;
            double cellSqm = (metersY1 - metersY2) * dx;

            for (int x = 0; x < grid.countX; ++x)
            {
                coords.Longitude = (x + 0.5) * gridCellToCoords.x + grid.west;

                if (IsPointInPoly(coords, points))
                {
                    sqm += cellSqm;
                }
            }
            metersY1 = metersY2;
        }
        return sqm;
	}
    
    //
    // Event Methods
    //

    private void OnOtherGridFilterChange(GridData grid)
    {
        UpdateData();
    }

    //
    // Private Methods
    //

	private void AddInspectedGridData(GridData otherGrid)
	{
		if(!inspectedGridsData.ContainsKey(otherGrid))
			inspectedGridsData.Add(otherGrid, new List<float>());
	}

	private void InitializeValues()
    {
        int length = grid.countX * grid.countY;
        if (grid.values == null || grid.values.Length != length)
        {
            grid.values = new float[length];
		}

        // Set all values to 0
        for (int i = 0; i < length; ++i)
        {
            grid.values[i] = 0;
        }
	}

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

    private void CalculateValues()
    {
		double thisDegreesPerCellX = (grid.east - grid.west) / grid.countX;
		double thisDegreesPerCellY = (grid.south - grid.north) / grid.countY;
		double thisCellsPerDegreeX = 1.0 / thisDegreesPerCellX;
		double thisCellsPerDegreeY = 1.0 / thisDegreesPerCellY;

        // Cache grid transformations
        var gridCoordsToCell = new Distance(grid.countX / (grid.east - grid.west), grid.countY / (grid.south - grid.north));
        var gridCellToCoords = new Distance((grid.east - grid.west) / grid.countX, (grid.south - grid.north) / grid.countY);

		for (int i = 0; i < grids.Count; i++)
		{
			var g = grids[i];
			inspectedGridsData[g].Clear();

			var patchCellsPerDegreeX = g.countX / (g.east - g.west);
			var patchCellsPerDegreeY = g.countY / (g.south - g.north);

			double scaleX = patchCellsPerDegreeX * thisDegreesPerCellX;
			double scaleY = patchCellsPerDegreeY * thisDegreesPerCellY;

			double offsetX = (grid.west - g.west) * patchCellsPerDegreeX + 0.5 * scaleX;
			double offsetY = (grid.north - g.north) * patchCellsPerDegreeY + 0.5 * scaleY;

            double minLon = (minPt.Longitude > g.west) ? minPt.Longitude : g.west;
			double minLat = (minPt.Latitude < g.north) ? minPt.Latitude : g.north;
			double maxLon = (maxPt.Longitude < g.east) ? maxPt.Longitude : g.east;
			double maxLat = (maxPt.Latitude > g.south) ? maxPt.Latitude : g.south;

            int startX = (int)((minLon - grid.west) * thisCellsPerDegreeX + 0.5);
			int startY = (int)((minLat - grid.north) * thisCellsPerDegreeY + 0.5);
			int endX = (int)((maxLon - grid.west) * thisCellsPerDegreeX + 0.5);
			int endY = (int)((maxLat - grid.north) * thisCellsPerDegreeY + 0.5);

            int incrementX = (int)((endX - startX) / numOfSamplesX);
            int incrementY = (int)((endY - startY) / numOfSamplesY);

			if (g.IsCategorized)
			{
                if (!useFilters)
					continue;

				if (g.values != null)
                {
                    Coordinate coords = new Coordinate();
                    for (int y = startY; y < endY; y += incrementY)
                    {
			            coords.Latitude = (y + 0.5) * gridCellToCoords.y + grid.north;

                        int pY = grid.countX * (int)(offsetY + y * scaleY);
                        int thisIndex = y * grid.countX + startX;
                        for (int x = startX; x < endX; x += incrementX, thisIndex += incrementX)
                        {
                            coords.Longitude = (x + 0.5) * gridCellToCoords.x + grid.west;

                            if (IsPointInPoly(coords, points))
                            {
                                int pX = (int)(offsetX + x * scaleX);
                                int patchIndex = pY + pX;

                                if (patchIndex >= 0 && patchIndex < grid.values.Length)
                                {
                                    int value = (int)g.values[patchIndex];
                                    byte mask = grid.valuesMask == null ? (byte)1 : grid.valuesMask[patchIndex];
                                    float valToAdd = mask == 1 ? g.categoryFilter.IsSetAsInt(value) : 0;
                                    grid.values[thisIndex] = valToAdd;
                                    inspectedGridsData[g].Add(valToAdd);
                                }
                            }
                        }
                    }
                }
			}
			else
			{
				float gridMin = g.minValue;
                if (useFilters)
                {
                    if (g.values != null)
                    {
                        Coordinate coords = new Coordinate();
                        for (int y = startY; y < endY; y += incrementY)
                        {
			                coords.Latitude = (y + 0.5) * gridCellToCoords.y + grid.north;
                            
                            int pY = grid.countX * (int)(offsetY + y * scaleY);
                            int thisIndex = y * grid.countX + startX;
                            for (int x = startX; x < endX; x += incrementX, thisIndex += incrementX)
                            {
                                coords.Longitude = (x + 0.5) * gridCellToCoords.x + grid.west;
                                
                                if (IsPointInPoly(coords, points))
                                {
                                    int pX = (int)(offsetX + x * scaleX);
                                    int patchIndex = pY + pX;

                                    if (patchIndex >= 0 && patchIndex < grid.values.Length)
                                    {
                                        int value = (int)g.values[patchIndex];
                                        byte mask = grid.valuesMask == null ? (byte)1 : grid.valuesMask[patchIndex];
                                        if (mask == 1 && value >= g.minFilter && value <= g.maxFilter)
                                        {
                                            float valToAdd = (value - gridMin);
                                            grid.values[thisIndex] = valToAdd;
                                            inspectedGridsData[g].Add(valToAdd);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
				{
					if (g.values != null)
					{
                        Coordinate coords = new Coordinate();
						for (int y = startY; y < endY; y += incrementY)
                        {
			                coords.Latitude = (y + 0.5) * gridCellToCoords.y + grid.north;
                            
                            int pY = grid.countX * (int)(offsetY + y * scaleY);
                            int thisIndex = y * grid.countX + startX;
                            for (int x = startX; x < endX; x += incrementX, thisIndex += incrementX)
                            {
                                coords.Longitude = (x + 0.5) * gridCellToCoords.x + grid.west;
                                
                                if (IsPointInPoly(coords, points))
                                {
                                    int pX = (int)(offsetX + x * scaleX);
                                    int patchIndex = pY + pX;

                                    if (patchIndex >= 0 && patchIndex < grid.values.Length)
                                    {
                                        byte mask = grid.valuesMask == null ? (byte)1 : grid.valuesMask[patchIndex];
                                        if (mask == 1)
                                        {
                                            float valToAdd = (g.values[patchIndex] - gridMin);
                                            grid.values[thisIndex] = valToAdd;
                                            inspectedGridsData[g].Add(valToAdd);
                                        }
                                    }
                                }
                            }
                        }
					}
				}
			}
		}

		float min = float.MaxValue;
		float max = float.MinValue;
		int gridCount = grid.countX * grid.countY;
		for (int i = 0; i < gridCount; ++i)
		{
			float value = grid.values[i];
			min = Mathf.Min(min, value);
			max = Mathf.Max(max, value);
		}

		grid.maxValue = max;
		grid.minValue = min;

		grid.ValuesChanged();
	}

    private void AssignMinMaxCoord(List<Coordinate> coords)
    {
        minPt = maxPt = coords[0];
        int count = coords.Count;
        for (int i = 1; i < count; ++i)
        {
            if (coords[i].Longitude < minPt.Longitude)
                minPt.Longitude = coords[i].Longitude;

            if (coords[i].Latitude > minPt.Latitude)
                minPt.Latitude = coords[i].Latitude;

            if (coords[i].Longitude > maxPt.Longitude)
                maxPt.Longitude = coords[i].Longitude;

            if (coords[i].Latitude < maxPt.Latitude)
                maxPt.Latitude = coords[i].Latitude;
        }
    }

    private List<Vector3> ConvertToVec3List(List<Coordinate> coords)
    {
        List<Vector3> points = new List<Vector3>();
        foreach (var coord in coords)
        {
            var coordToWorldPos = map.GetUnitsFromCoordinates(coord);
		    Vector3 worldPos = new Vector3(coordToWorldPos.x, coordToWorldPos.z, coordToWorldPos.y);

            points.Add(worldPos);
        }

        return points;
    }
}