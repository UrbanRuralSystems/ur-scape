// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;

public class ContourUtils
{
	public static double GetContoursSquareMeters(GridData grid, bool selected = false, int selectedContour = 0)
	{
		if (grid.countX == 0 || grid.values == null)
			return 0;

		double scaleX = (grid.east - grid.west) / grid.countX;
		double scaleY = (grid.south - grid.north) / grid.countY;

		//double metersY1 = GeoCalculator.LatitudeToMeters(grid.north);
		double metersY1 = Math.Log(Math.Tan((90d + grid.north) * GeoCalculator.Deg2HalfRad)) * GeoCalculator.Rad2Meters;
		double dx = scaleX * GeoCalculator.Deg2Meters;

		double sqm = 0;
		if (selected)
		{
			if (selectedContour == 0)
				return 0;

			int i = 0;
			for (int y = 1; y <= grid.countY; ++y)
			{
				//double metersY2 = GeoCalculator.LatitudeToMeters(grid.north + y * scaleY);
				double metersY2 = Math.Log(Math.Tan((90d + (grid.north + y * scaleY)) * GeoCalculator.Deg2HalfRad)) * GeoCalculator.Rad2Meters;
				double cellSqm = (metersY1 - metersY2) * dx;

				for (int x = 0; x < grid.countX; ++x, ++i)
				{
					if (grid.values[i] == selectedContour)
					{
						sqm += cellSqm;
					}
				}
				metersY1 = metersY2;
			}
		}
		else
		{
			int i = 0;
			for (int y = 1; y <= grid.countY; ++y)
			{
				//double metersY2 = GeoCalculator.LatitudeToMeters(grid.north + y * scaleY);
				double metersY2 = Math.Log(Math.Tan((90d + (grid.north + y * scaleY)) * GeoCalculator.Deg2HalfRad)) * GeoCalculator.Rad2Meters;
				double cellSqm = (metersY1 - metersY2) * dx;

				for (int x = 0; x < grid.countX; ++x, ++i)
				{
					if (grid.values[i] > 0)
					{
						sqm += cellSqm;
					}
				}
				metersY1 = metersY2;
			}
		}
		return sqm;
	}

	public static double SumContouredValues(GridData contourGrid, GridData otherGrid, out int cellCount, bool selected = false, int selectedContour = 0)
	{
		cellCount = 0;

		if (selected && selectedContour == 0)
			return 0;

		double sum = 0;
		int count = otherGrid.values.Length;

		// Shortcut for grids that have the same resolution and boundary as the contours
		if (contourGrid.countX == otherGrid.countX &&
			contourGrid.countY == otherGrid.countY &&
			contourGrid.north == otherGrid.north &&
			contourGrid.east == otherGrid.east &&
			contourGrid.south == otherGrid.south &&
			contourGrid.west == otherGrid.west)
		{
			if (selected)
			{
				if (otherGrid.valuesMask == null)
				{
					for (int i = 0; i < count; ++i)
					{
						if (contourGrid.values[i] != selectedContour) continue;
						sum += otherGrid.values[i];
						cellCount++;
					}
				}
				else
				{
					for (int i = 0; i < count; ++i)
					{
						if (contourGrid.values[i] != selectedContour) continue;
						sum += otherGrid.values[i] * otherGrid.valuesMask[i];
						cellCount++;
					}
				}
			}
			else
			{
				if (otherGrid.valuesMask == null)
				{
					for (int i = 0; i < count; ++i)
					{
						if (contourGrid.values[i] <= 0) continue;
						sum += otherGrid.values[i];
						cellCount++;
					}
				}
				else
				{
					for (int i = 0; i < count; ++i)
					{
						if (contourGrid.values[i] <= 0) continue;
						sum += otherGrid.values[i] * otherGrid.valuesMask[i];
						cellCount++;
					}
				}
			}
		}
		else
		{
			double contoursCellsPerDegreeX = contourGrid.countX / (contourGrid.east - contourGrid.west);
			double contoursCellsPerDegreeY = contourGrid.countY / (contourGrid.south - contourGrid.north);

			var degreesPerCellX = (otherGrid.east - otherGrid.west) / otherGrid.countX;
			var degreesPerCellY = (otherGrid.south - otherGrid.north) / otherGrid.countY;

			double scaleX = contoursCellsPerDegreeX * degreesPerCellX;
			double scaleY = contoursCellsPerDegreeY * degreesPerCellY;

			double offsetX = (otherGrid.west - contourGrid.west) * contoursCellsPerDegreeX + 0.5 * scaleX;
			double offsetY = (otherGrid.north - contourGrid.north) * contoursCellsPerDegreeY + 0.5 * scaleY;

			if (selected)
			{
				for (int i = 0; i < count; ++i)
				{
					int y = i / otherGrid.countX;
					int x = i - y * otherGrid.countX;
					int cX = (int)(offsetX + x * scaleX);
					int cY = (int)(offsetY + y * scaleY);
					int contourIndex = cY * contourGrid.countX + cX;
					if (contourGrid.values[contourIndex] == selectedContour)
					{
						if (otherGrid.valuesMask == null)
							sum += otherGrid.values[i];
						else
							sum += otherGrid.values[i] * otherGrid.valuesMask[i];
						cellCount++;
					}
				}
			}
			else
			{
				for (int i = 0; i < count; ++i)
				{
					int y = i / otherGrid.countX;
					int x = i - y * otherGrid.countX;
					int cX = (int)(offsetX + x * scaleX);
					int cY = (int)(offsetY + y * scaleY);
					int contourIndex = cY * contourGrid.countX + cX;
					if (contourGrid.values[contourIndex] > 0)
					{
						if (otherGrid.valuesMask == null)
							sum += otherGrid.values[i];
						else
							sum += otherGrid.values[i] * otherGrid.valuesMask[i];
						cellCount++;
					}
				}
			}
		}

		return sum;
	}

}
