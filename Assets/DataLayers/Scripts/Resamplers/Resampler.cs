// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public abstract class Resampler
{
	public abstract bool IntegralFactorOnly { get; }

	public bool Convert(GridData grid, double degPerCellX, double degPerCellY)
	{
		grid.CalculateResolution(degPerCellX, degPerCellY, out int countX, out int countY);
		return Convert(grid, countX, countY);
	}

	public bool Convert(GridData grid, int countX, int countY)
	{
		if (countX == grid.countX && countY == grid.countY)
		{
			Debug.LogWarning("Trying to resample raster with same resolution");
			return false;
		}
		if (countX > grid.countX || countY > grid.countY)
		{
			Debug.LogWarning("Trying to increase raster resolution. Disaggregation not allowed");
			return false;
		}
		if (countX < 1 || countY < 1)
		{
			Debug.LogWarning("Trying to resample raster to invalid resolution (zero or negative)");
			return false;
		}

		if (IntegralFactorOnly && (grid.countX % countX != 0 || grid.countY % countY != 0))
		{
			Debug.LogWarning("Can't resample with non integral factor");
			return false;
		}

		Resample(grid, countX, countY);

		return true;
	}

	protected abstract void Resample(GridData grid, int countX, int countY);
}


public class NearestNeighbourResampler : Resampler
{
	public override bool IntegralFactorOnly { get => false; }

	protected override void Resample(GridData grid, int countX, int countY)
	{
		float[] inputValues = grid.values;
		byte[] inputMask = grid.valuesMask;
		int inputCountX = grid.countX;
		int inputCountY = grid.countY;

		grid.countX = countX;
		grid.countY = countY;
		grid.InitGridValues(inputMask != null);

		double scaleX = (double)inputCountX / countX;
		double scaleY = (double)inputCountY / countY;

		double offsetX = 0.5 * scaleX;
		double offsetY = 0.5 * scaleY;

		int outputIndex = 0;
		if (inputMask == null)
		{
			for (int y = 0; y < countY; y++)
			{
				int pY = inputCountX * (int)(offsetY + y * scaleY);
				for (int x = 0; x < countX; x++, outputIndex++)
				{
					int inputIndex = pY + (int)(offsetX + x * scaleX);
					grid.values[outputIndex] = inputValues[inputIndex];
				}
			}
		}
		else
		{
			for (int y = 0; y < countY; y++)
			{
				int pY = inputCountX * (int)(offsetY + y * scaleY);
				for (int x = 0; x < countX; x++, outputIndex++)
				{
					int inputIndex = pY + (int)(offsetX + x * scaleX);
					grid.values[outputIndex] = inputValues[inputIndex];
					grid.valuesMask[outputIndex] = inputMask[inputIndex];
				}
			}
		}
	}
}

public class MaxResampler : Resampler
{
	public override bool IntegralFactorOnly { get => true; }

	protected override void Resample(GridData grid, int countX, int countY)
	{
		float[] inputValues = grid.values;
		byte[] inputMask = grid.valuesMask;
		int inputCountX = grid.countX;
		int inputCountY = grid.countY;

		grid.countX = countX;
		grid.countY = countY;
		grid.InitGridValues(inputMask != null);

		float[] outputValues = grid.values;

		// Initialize values
		outputValues.Fill(float.MinValue);

		double scaleX = (double)countX / inputCountX;
		double scaleY = (double)countY / inputCountY;

		double offsetX = 0.5 * scaleX;
		double offsetY = 0.5 * scaleY;

		int inputIndex = 0;
		if (inputMask == null)
		{
			for (int y = 0; y < inputCountY; y++)
			{
				int pY = countX * (int)(offsetY + y * scaleY);
				for (int x = 0; x < inputCountX; x++, inputIndex++)
				{
					int outputIndex = pY + (int)(offsetX + x * scaleX);
					outputValues[outputIndex] = Mathf.Max(outputValues[outputIndex], inputValues[inputIndex]);
				}
			}

			// Leave the grid without mask
			grid.valuesMask = null;
		}
		else
		{
			byte[] outputMask = grid.valuesMask;

			for (int y = 0; y < inputCountY; y++)
			{
				int pY = countX * (int)(offsetY + y * scaleY);
				for (int x = 0; x < inputCountX; x++, inputIndex++)
				{
					int outputIndex = pY + (int)(offsetX + x * scaleX);
					if (inputMask[inputIndex] == 1)
					{
						outputValues[outputIndex] = Mathf.Max(outputValues[outputIndex], inputValues[inputIndex]);
						outputMask[outputIndex] = 1;
					}
				}
			}
		}
	}
}
