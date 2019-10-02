// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using UnityEngine;

public class HgtInterpreter : Interpreter
{
	private static readonly DataFormat dataFormat = new DataFormat("SRTM HGT", "hgt");
	public override DataFormat GetDataFormat() => dataFormat;

	private const string SuggestedLayerName = "Topography";
	private const string SuggestedUnits = "Meters"/*Translatable*/;

	public override BasicInfo GetBasicInfo(string filename)
	{
		BasicInfo info = new BasicInfo();
		try
		{
			var format = HgtFormat.Get(filename);
			info.bounds = Hgt.GetBounds(filename);
			info.isRaster = true;
			// Substract 1 to width/height to avoid overlaps
			info.width = info.height = format.RowsAndColumns - 1;
			info.degreesPerPixel = format.GetDegreesPerPixel();
			info.suggestedLayerName = SuggestedLayerName;
			info.suggestedUnits = SuggestedUnits;
		}
		catch (Exception e)
		{
			Debug.LogException(e);
			return null;
		}
		return info;
	}

	public override bool Read(string filename, ProgressInfo progress, out PatchData data)
	{
		data = null;

		using (var hgt = Hgt.Open(filename))
		{
			if (hgt == null)
				return false;

			var bounds = hgt.Bounds;

			GridData grid = new GridData
			{
				north = bounds.north,
				east = bounds.east,
				west = bounds.west,
				south = bounds.south,
			};

			// Substract 1 to width/height to avoid overlaps
			grid.countX = hgt.Width - 1;
			grid.countY = hgt.Height - 1;
			grid.InitGridValues(false);

			// Read raster values
			var buffer = hgt.Buffer;
			int pos = 0;
			int index = 0;
			float invCountY = 1f / grid.countY;
			for (int y = 0; y < grid.countY; y++)
			{
				for (int x = 0; x < grid.countX; x++)
				{
					short value = (short)((buffer[pos++] << 8) | buffer[pos++]);
					grid.values[index++] = value;
				}
				// Advance another 2 positions to skip the last column
				pos += 2;

				progress.value += invCountY;
			}

			progress.value = 1;

			grid.AddMaskValue(Hgt.DataVoid);
			grid.units = SuggestedUnits;

			data = grid;
		}

		return true;
	}

	public override bool Write(string filename, Patch patch)
	{
		return false;
	}
}
