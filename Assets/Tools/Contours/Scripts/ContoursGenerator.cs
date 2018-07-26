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

using UnityEngine;

public abstract class ContoursGenerator
{
	protected ContoursMapLayer contoursMapLayer;

	public bool excludeCellsWithNoData;

	public ContoursGenerator(ContoursMapLayer contoursMapLayer)
	{
		this.contoursMapLayer = contoursMapLayer;
	}

	public abstract void InitializeValues(int layersCount);
	public abstract void CalculateValues();
}

public class ContoursGenerator_CPU : ContoursGenerator
{
	public ContoursGenerator_CPU(ContoursMapLayer contoursMapLayer) : base(contoursMapLayer)
	{
	}

	public override void InitializeValues(int layersCount)
	{
		var grid = contoursMapLayer.Grid;
		int count = grid.countX * grid.countY;

		if (grid.values == null || grid.values.Length != count)
		{
			grid.values = new float[count];
		}

		// Set all values to 1 - layersCount
		int initialValue = excludeCellsWithNoData? 1 - layersCount : -1;
		for (int i = 0; i < count; ++i)
		{
			grid.values[i] = initialValue;
		}
	}

	public override void CalculateValues()
	{
		var contourGrid = contoursMapLayer.Grid;
		var grids = contoursMapLayer.grids;

		double contoursDegreesPerCellX = (contourGrid.east - contourGrid.west) / contourGrid.countX;
		double contoursDegreesPerCellY = (contourGrid.south - contourGrid.north) / contourGrid.countY;
		double contoursCellsPerDegreeX = 1.0 / contoursDegreesPerCellX;
		double contoursCellsPerDegreeY = 1.0 / contoursDegreesPerCellY;
		for (int i = 0; i < grids.Count; i++)
		{
			var grid = grids[i];

			var cellsPerDegreeX = grid.countX / (grid.east - grid.west);
			var cellsPerDegreeY = grid.countY / (grid.south - grid.north);

			double scaleX = cellsPerDegreeX * contoursDegreesPerCellX;
			double scaleY = cellsPerDegreeY * contoursDegreesPerCellY;

			double offsetX = (contourGrid.west - grid.west) * cellsPerDegreeX + 0.5 * scaleX;
			double offsetY = (contourGrid.north - grid.north) * cellsPerDegreeY + 0.5 * scaleY;

			int startX = (int)((grid.west - contourGrid.west) * contoursCellsPerDegreeX + 0.5);
			int startY = (int)((grid.north - contourGrid.north) * contoursCellsPerDegreeY + 0.5);
			int endX = (int)((grid.east - contourGrid.west) * contoursCellsPerDegreeX + 0.5);
			int endY = (int)((grid.south - contourGrid.north) * contoursCellsPerDegreeY + 0.5);

			int count = grid.values.Length;
			if (grid.IsCategorized)
			{
				for (int y = startY; y < endY; y++)
				{
					int contourIndex = y * contourGrid.countX + startX;
					for (int x = startX; x < endX; x++, contourIndex++)
					{
						int pX = (int)(offsetX + x * scaleX);
						int pY = (int)(offsetY + y * scaleY);
						int patchIndex = pY * grid.countX + pX;

						int value = (int)grid.values[patchIndex];
						if (excludeCellsWithNoData)
						{
							value = grid.valuesMask[patchIndex] ? (int)((grid.categoryMask >> value) & 1) : 0;
							contourGrid.values[contourIndex] += value;
						}
						else
						{
							value = grid.valuesMask[patchIndex] ? (int)contourGrid.values[contourIndex] * (int)((grid.categoryMask >> value) & 1) : 1;
							contourGrid.values[contourIndex] *= value;
						}
					}
				}
			}
			else
			{
				for (int y = startY; y < endY; y++)
				{
					int contourIndex = y * contourGrid.countX + startX;
					for (int x = startX; x < endX; x++, contourIndex++)
					{
						int pX = (int)(offsetX + x * scaleX);
						int pY = (int)(offsetY + y * scaleY);
						int patchIndex = pY * grid.countX + pX;

						float value = grid.values[patchIndex];
						if (excludeCellsWithNoData)
						{
							value = (value >= grid.minFilter && value <= grid.maxFilter) ? 1 : 0;
							contourGrid.values[contourIndex] += grid.valuesMask[patchIndex] ? value : 0;
						}
						else
						{
							value = (value >= grid.minFilter && value <= grid.maxFilter) ? contourGrid.values[contourIndex] : 0;
							contourGrid.values[contourIndex] *= grid.valuesMask[patchIndex] ? value : 1;
						}
					}
				}
			}
		}

		contoursMapLayer.SubmitGridValues();
	}
}

#if !USE_TEXTURE
public class ContoursGenerator_GPU : ContoursGenerator
{
	private readonly int ResetKID;
	private readonly ComputeShader compute;

	public ContoursGenerator_GPU(ContoursMapLayer contoursMapLayer) : base(contoursMapLayer)
	{
		compute = contoursMapLayer.compute;
		ResetKID = compute.FindKernel("CSReset");
	}

	public override void InitializeValues(int layersCount)
	{
		var contourGrid = contoursMapLayer.Grid;
		int count = contourGrid.countX * contourGrid.countY;
		if (contoursMapLayer.ValuesBuffer == null || contoursMapLayer.ValuesBuffer.count != count)
		{
			contoursMapLayer.CreateBuffer();
		}

		int initialValue = excludeCellsWithNoData ? 1 - layersCount : -1;

		// Get kernel thread count
		uint threadsX, threadsY, threadsZ;
		compute.GetKernelThreadGroupSizes(ResetKID, out threadsX, out threadsY, out threadsZ);

		// Calculate threads & groups
		uint threads = threadsX * threadsY * threadsZ;
		int groups = (int)((count + threads - 1) / threads);

		// Assign shader variables
		compute.SetBuffer(ResetKID, "contourValues", contoursMapLayer.ValuesBuffer);
		compute.SetInt("contourValueCount", count);
		compute.SetInt("initialValue", initialValue);

		compute.Dispatch(ResetKID, groups, 1, 1);
	}

	public override void CalculateValues()
	{
		var contourGrid = contoursMapLayer.Grid;
		var grids = contoursMapLayer.grids;

		double contoursDegreesPerCellX = (contourGrid.east - contourGrid.west) / contourGrid.countX;
		double contoursDegreesPerCellY = (contourGrid.south - contourGrid.north) / contourGrid.countY;
		double contoursCellsPerDegreeX = 1.0 / contoursDegreesPerCellX;
		double contoursCellsPerDegreeY = 1.0 / contoursDegreesPerCellY;

		compute.SetInt("contourCountX", contourGrid.countX);

		string kernelSufix = excludeCellsWithNoData ? "_Exclude" : "_Include";

		// Calculate total number of threads and buffer size
		uint threadsX, threadsY, threadsZ;

		for (int i = 0; i < grids.Count; i++)
		{
			var grid = grids[i];
			var patchMapLayer = grid.patch.GetMapLayer() as GridMapLayer;

			var cellsPerDegreeX = grid.countX / (grid.east - grid.west);
			var cellsPerDegreeY = grid.countY / (grid.south - grid.north);

			double scaleX = cellsPerDegreeX * contoursDegreesPerCellX;
			double scaleY = cellsPerDegreeY * contoursDegreesPerCellY;

			double offsetX = (contourGrid.west - grid.west) * cellsPerDegreeX + 0.5 * scaleX;
			double offsetY = (contourGrid.north - grid.north) * cellsPerDegreeY + 0.5 * scaleY;

			int startX = (int)((grid.west - contourGrid.west) * contoursCellsPerDegreeX + 0.5);
			int startY = (int)((grid.north - contourGrid.north) * contoursCellsPerDegreeY + 0.5);
			int endX = (int)((grid.east - contourGrid.west) * contoursCellsPerDegreeX + 0.5);
			int endY = (int)((grid.south - contourGrid.north) * contoursCellsPerDegreeY + 0.5);

			int countX = endX - startX;
			int countY = endY - startY;
			int count = countX * countY;

			string kernelName = grid.IsCategorized ? "CSCategorized" : "CSContinuous";
			string kernelMask = patchMapLayer.MaskBuffer != null? "_Masked" : "_Unmasked";
			int kernelID = compute.FindKernel(kernelName + kernelMask + kernelSufix);
			compute.GetKernelThreadGroupSizes(kernelID, out threadsX, out threadsY, out threadsZ);

			// Calculate threads & groups
			uint threads = threadsX * threadsY * threadsZ;
			int groups = (int)((count + threads - 1) / threads);

			compute.SetBuffer(kernelID, "contourValues", contoursMapLayer.ValuesBuffer);
			compute.SetBuffer(kernelID, "patchValues", patchMapLayer.ValuesBuffer);
			if (patchMapLayer.MaskBuffer != null)
				compute.SetBuffer(kernelID, "patchMask", patchMapLayer.MaskBuffer);

			compute.SetInt("contourValueCount", count);
			compute.SetInt("croppedContourCountX", countX);
			compute.SetInt("gridCountX", grid.countX);
			compute.SetInt("categoryMask", (int)grid.categoryMask);
			compute.SetVector("minmax", new Vector2(grid.minFilter, grid.maxFilter));
			compute.SetVector("offset", new Vector2((float)offsetX, (float)offsetY));
			compute.SetVector("scale", new Vector2((float)scaleX, (float)scaleY));
			compute.SetInt("startX", startX);
			compute.SetInt("startY", startY);

			compute.Dispatch(kernelID, groups, 1, 1);
		}
	}
}
#endif
