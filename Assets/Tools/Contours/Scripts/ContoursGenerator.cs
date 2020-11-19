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

using UnityEngine;

public abstract class ContoursGenerator
{
	protected ContoursMapLayer contoursMapLayer;

	public bool excludeCellsWithNoData;

	public ContoursGenerator(ContoursMapLayer contoursMapLayer)
	{
		this.contoursMapLayer = contoursMapLayer;
	}

	public abstract void InitializeValues(int layersCount, Vector2[] boundary);
	public abstract void CalculateValues();
	public abstract void Release();
}

public class ContoursGenerator_CPU : ContoursGenerator
{
	public ContoursGenerator_CPU(ContoursMapLayer contoursMapLayer) : base(contoursMapLayer)
	{
	}

	public override void InitializeValues(int layersCount, Vector2[] boundary)
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
					int pY = grid.countX * (int)(offsetY + y * scaleY);
					int contourIndex = y * contourGrid.countX + startX;
					for (int x = startX; x < endX; x++, contourIndex++)
					{
						int pX = (int)(offsetX + x * scaleX);
						int patchIndex = pY + pX;

						int value = (int)grid.values[patchIndex];
						byte mask = grid.valuesMask == null ? (byte)1 : grid.valuesMask[patchIndex];
						if (excludeCellsWithNoData)
						{
							value = mask & grid.categoryFilter.IsSetAsInt(value);
							contourGrid.values[contourIndex] += value;
						}
						else
						{
							value = mask == 1? (int)contourGrid.values[contourIndex] * grid.categoryFilter.IsSetAsInt(value) : 1;
							contourGrid.values[contourIndex] *= value;
						}
					}
				}
			}
			else
			{
				for (int y = startY; y < endY; y++)
				{
					int pY = grid.countX * (int)(offsetY + y * scaleY);
					int contourIndex = y * contourGrid.countX + startX;
					for (int x = startX; x < endX; x++, contourIndex++)
					{
						int pX = (int)(offsetX + x * scaleX);
						int patchIndex = pY + pX;

						float value = grid.values[patchIndex];
						byte mask = grid.valuesMask == null ? (byte)1 : grid.valuesMask[patchIndex];
						if (excludeCellsWithNoData)
						{
							value = (value >= grid.minFilter && value <= grid.maxFilter) ? 1 : 0;
							contourGrid.values[contourIndex] += mask == 1? value : 0;
						}
						else
						{
							value = (value >= grid.minFilter && value <= grid.maxFilter) ? contourGrid.values[contourIndex] : 0;
							contourGrid.values[contourIndex] *= mask == 1? value : 1;
						}
					}
				}
			}
		}

		contoursMapLayer.SubmitGridValues();
	}

	public override void Release() { }
}

#if !USE_TEXTURE
public class ContoursGenerator_GPU : ContoursGenerator
{
	private readonly int Reset_KID;
	private readonly int ClipViewArea_Include_KID;
	private readonly int ClipViewArea_Exclude_KID;
	private readonly ComputeShader compute;
	private readonly ComputeBuffer categoryFilterBuffer;

	public ContoursGenerator_GPU(ContoursMapLayer contoursMapLayer) : base(contoursMapLayer)
	{
		compute = contoursMapLayer.compute;
		Reset_KID = compute.FindKernel("CSReset");
		ClipViewArea_Include_KID = compute.FindKernel("CSClipViewArea_Include");
		ClipViewArea_Exclude_KID = compute.FindKernel("CSClipViewArea_Exclude");
		categoryFilterBuffer = new ComputeBuffer(CategoryFilter.MaxCategories, sizeof(uint), ComputeBufferType.Default);
	}

	public override void InitializeValues(int layersCount, Vector2[] boundary)
	{
		var contourGrid = contoursMapLayer.Grid;
		int count = contourGrid.countX * contourGrid.countY;
		if (contoursMapLayer.ValuesBuffer == null || contoursMapLayer.ValuesBuffer.count != count)
		{
			contoursMapLayer.CreateBuffer();
		}

		int kernelID = boundary == null ? Reset_KID : (excludeCellsWithNoData ? ClipViewArea_Exclude_KID : ClipViewArea_Include_KID);

		int initialValue = excludeCellsWithNoData ? 1 - layersCount : 1;

		// Get kernel thread count
		compute.GetKernelThreadGroupSizes(kernelID, out uint threadsX, out uint threadsY, out uint threadsZ);

		// Calculate threads & groups
		uint threads = threadsX * threadsY * threadsZ;
		int groups = (int)((count + threads - 1) / threads);

		// Assign shader variables
		compute.SetBuffer(kernelID, "contourValues", contoursMapLayer.ValuesBuffer);
		compute.SetInt("contourValueCount", count);
		compute.SetInt("initialValue", initialValue);

		if (boundary != null)
		{
			// Calculate scale and offset
			var metersNW = GeoCalculator.LonLatToMeters(contourGrid.west, contourGrid.north);
			var metersSE = GeoCalculator.LonLatToMeters(contourGrid.east, contourGrid.south);
			double scaleX = (metersSE.x - metersNW.x) / contourGrid.countX;
			double scaleY = (contourGrid.south - contourGrid.north) / contourGrid.countY;
			double offsetX = metersNW.x + 0.5 * scaleX;
			double offsetY = contourGrid.north + 0.5 * scaleY;

			compute.SetInt("contourCountX", contourGrid.countX);
			compute.SetVector("offset", new Vector2((float)offsetX, (float)offsetY));
			compute.SetVector("scale", new Vector2((float)scaleX, (float)scaleY));
			for (int i = 0; i < boundary.Length; i++)
			{
				compute.SetVector("pt" + i, boundary[i]);
			}
		}

		compute.Dispatch(kernelID, groups, 1, 1);
		contoursMapLayer.SetGpuChangedValues();
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
		for (int i = 0; i < grids.Count; i++)
		{
			var grid = grids[i];

			GridMapLayer mapLayer;
			if (grid.patch is GridPatch)
			{
				mapLayer = grid.patch.GetMapLayer() as GridMapLayer;
			}
			else
			{
				mapLayer = (grid.patch as MultiGridPatch).GetMapLayer(grid);
			}

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
			string kernelMask = mapLayer.MaskBuffer != null ? "_Masked" : "_Unmasked";
			int kernelID = compute.FindKernel(kernelName + kernelMask + kernelSufix);
			compute.GetKernelThreadGroupSizes(kernelID, out uint threadsX, out uint threadsY, out uint threadsZ);
			compute.SetBuffer(kernelID, "categoryFilter", categoryFilterBuffer);

			// Calculate threads & groups
			uint threads = threadsX * threadsY * threadsZ;
			int groups = (int)((count + threads - 1) / threads);

			compute.SetBuffer(kernelID, "contourValues", contoursMapLayer.ValuesBuffer);
			compute.SetBuffer(kernelID, "patchValues", mapLayer.ValuesBuffer);
			if (mapLayer.MaskBuffer != null)
				compute.SetBuffer(kernelID, "patchMask", mapLayer.MaskBuffer);

			compute.SetInt("contourValueCount", count);
			compute.SetInt("croppedContourCountX", countX);
			compute.SetInt("gridCountX", grid.countX);
			categoryFilterBuffer.SetData(grid.categoryFilter.bits);
			compute.SetVector("minmax", new Vector2(grid.minFilter, grid.maxFilter));
			compute.SetVector("offset", new Vector2((float)offsetX, (float)offsetY));
			compute.SetVector("scale", new Vector2((float)scaleX, (float)scaleY));
			compute.SetInt("startX", startX);
			compute.SetInt("startY", startY);

			compute.Dispatch(kernelID, groups, 1, 1);
		}
		contoursMapLayer.SetGpuChangedValues();
	}

	public override void Release()
	{
		if (categoryFilterBuffer != null)
		{
			categoryFilterBuffer.Release();
		}
	}
}
#endif
