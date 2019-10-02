// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;
using UnityEngine;

public class GeoTiffInterpreter : Interpreter
{
	private static readonly DataFormat dataFormat = new DataFormat("GeoTIFF", "tiff", "tif");
	public override DataFormat GetDataFormat() => dataFormat;

	public override BasicInfo GetBasicInfo(string filename)
	{
		BasicInfo info = new BasicInfo();
		try
		{
			var geoTiff = new GeoTiff(filename);
			info.bounds = geoTiff.Bounds;
			info.isRaster = true;
			info.width = geoTiff.Width;
			info.height = geoTiff.Height;
			info.degreesPerPixel = geoTiff.GetDegreesPerPixel();
		}
		catch (Exception e)
		{
			Debug.LogException(e);
			return null;
		}
		return info;
	}

	private delegate float ValueConverter(byte[] buffer, int offset);
	private static float ConvertFloat(byte[] buffer, int offset) => BitConverter.ToSingle(buffer, offset);
	private static float ConvertDouble(byte[] buffer, int offset) => (float)BitConverter.ToDouble(buffer, offset);
	private static float ConvertInt8(byte[] buffer, int offset) => (sbyte)buffer[offset];
	private static float ConvertUInt8(byte[] buffer, int offset) => buffer[offset];
	private static float ConvertInt16(byte[] buffer, int offset) => BitConverter.ToInt16(buffer, offset);
	private static float ConvertUInt16(byte[] buffer, int offset) => BitConverter.ToUInt16(buffer, offset);
	private static float ConvertInt32(byte[] buffer, int offset) => BitConverter.ToInt32(buffer, offset);
	private static float ConvertUInt32(byte[] buffer, int offset) => BitConverter.ToUInt32(buffer, offset);
	private static float ConvertInt64(byte[] buffer, int offset) => BitConverter.ToInt64(buffer, offset);
	private static float ConvertUInt64(byte[] buffer, int offset) => BitConverter.ToUInt64(buffer, offset);

	private static readonly Dictionary<Type, ValueConverter> ConverterMap = new Dictionary<Type, ValueConverter>()
	{
		[typeof(float)] = ConvertFloat,
		[typeof(double)] = ConvertDouble,
		[typeof(sbyte)] = ConvertInt8,
		[typeof(byte)] = ConvertUInt8,
		[typeof(short)] = ConvertInt16,
		[typeof(ushort)] = ConvertUInt16,
		[typeof(int)] = ConvertInt32,
		[typeof(uint)] = ConvertUInt32,
		[typeof(long)] = ConvertInt64,
		[typeof(ulong)] = ConvertUInt64,
	};

	public override bool Read(string filename, ProgressInfo progress, out PatchData data)
	{
		data = null;

		using (var geoTiff = new GeoTiff(filename))
		{
			if (geoTiff == null)
				return false;

			var bouds = geoTiff.Bounds;

			GridData grid = new GridData
			{
				north = bouds.north,
				east = bouds.east,
				west = bouds.west,
				south = bouds.south,
			};

			grid.countX = geoTiff.Width;
			grid.countY = geoTiff.Height;
			grid.InitGridValues(false);

			// Read raster values
			int width = grid.countX;
			int height = grid.countY;
			int bufferSize = width * height;
			int bytesPerSample = geoTiff.BitsPerSample / 8;

			var dataType = geoTiff.DataType;
			if (dataType == null)
			{
				switch (geoTiff.BitsPerSample)
				{
					case 8:
						dataType = typeof(byte);
						break;
					case 16:
						dataType = typeof(UInt16);
						break;
					case 32:
						dataType = typeof(UInt32);
						break;
					case 64:
						dataType = typeof(UInt64);
						break;
				}
			}
			ValueConverter convert = ConverterMap[dataType];

			if (geoTiff.Tiff.IsTiled())
			{
				int tileCount = geoTiff.Tiff.NumberOfTiles();
				int bytesPerTileRow = geoTiff.Tiff.TileRowSize();

				int tileWidth = 0, tileHeight = 0;
				tileWidth = bytesPerTileRow / bytesPerSample;
				tileHeight = bytesPerTileRow / bytesPerSample;

				int tilesPerRow = Mathf.CeilToInt((float)width / tileWidth);

				int bytesPerTile = geoTiff.Tiff.TileSize();
				byte[] buffer = new byte[bytesPerTile];

				int offset, index = 0;
				float progressStep = 1f / tileCount;
				for (int tile = 0; tile < tileCount; tile++)
				{
					geoTiff.Tiff.ReadEncodedTile(tile, buffer, 0, bytesPerTile);

					int tileX = tile % tilesPerRow;
					int tileY = tile / tilesPerRow;
					int xOffset = tileX * tileWidth;
					int yOffset = tileY * tileHeight;
					index = yOffset * width + xOffset;

					offset = 0;
					int w = Math.Min(width, xOffset + tileWidth) - xOffset;
					int h = Math.Min(height, yOffset + tileHeight) - yOffset;
					for (int r = 0; r < h; r++)
					{
						for (int c = 0; c < w; c++)
						{
							grid.values[index++] = convert(buffer, offset);
							offset += bytesPerSample;
						}
						index += width - w;
						offset += (tileWidth - w) * bytesPerSample;
					}

					progress.value += progressStep;
				}
			}
			else
			{
				int scanlineSize = width * bytesPerSample;
				byte[] buffer = new byte[scanlineSize];

				int offset, index = 0;
				float progressStep = 1f / grid.countY;
				for (int r = 0; r < height; r++)
				{
					offset = 0;
					geoTiff.Tiff.ReadScanline(buffer, r);
					for (int c = 0; c < width; c++)
					{
						grid.values[index++] = convert(buffer, offset);
						offset += bytesPerSample;
					}

					progress.value += progressStep;
				}
			}

			progress.value = 1;

			grid.AddMaskValue((float)geoTiff.NoDataValue);

			var citation = geoTiff.Citation;
			if (!string.IsNullOrWhiteSpace(citation))
				grid.AddMetadata("Citation", citation);

			data = grid;
		}

		return true;
	}

	public override bool Write(string filename, Patch patch)
	{
		return false;
	}

}
