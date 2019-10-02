// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//
// Notes on SRTM HGT file format:  (more here https://wiki.openstreetmap.org/wiki/SRTM)
// SRTM HGT stands for Shuttle Radar Topography Mission Height
// SRTM HGT are data elevation models (DEM) and are distributed in two levels:
//    - SRTM-1: data sampled at 1 arc-second intervals in latitude and longitude
//    - SRTM-3: data sampled at 3 arc-second intervals
// File names refer to the latitude and longitude of the lower left corner of the tile. These
//   coordinates refer to the geometric center of the lower left pixel (~90 meters in extent for SRTM-3 data)
// Every pixel is an integer written as 2 signed bytes (16 bit big-endian)
// Heights are in meters referenced to the WGS84 geoid
// Data voids are assigned the value 32768
// SRTM-1 files contain 3601 rows and 3601 columns
// SRTM-3 files contain 1201 rows and 1201 columns
// The rows at the North and South edges and the columns at the East and West edges of each file overlap and
//   are identical to the edge rows and columns in the adjacent height file.
// HGT file naming convection is "abbcddd.hgt"
//   a: can be N, n, S or s
//   b: 2 digit latitude
//   c: can be E, e, W or w
//   d: 3 digit longitude

using System;
using System.IO;
using UnityEngine;

public class HgtFormat
{
	public readonly string Name;
	public readonly int SecondsPerPixel;
	public readonly double PixelsPerSecond;
	public readonly int RowsAndColumns;
	public readonly int TotalBytes;

	public static readonly HgtFormat Unknown = new HgtFormat("Unknown");
	public static readonly HgtFormat[] Formats =
	{
		//+ Fix the extra row/column that overlaps with the adjacent tile
		new HgtFormat("SRTM-1", 1, 3601),	// 1 arc second per pixel (~30m along the equator)
		new HgtFormat("SRTM-3", 3, 1201),	// 3 arc seconds per pixel (~90m along the equator)
		Unknown,
	};

	public bool IsUnknown { get { return this == Unknown; } }

	private HgtFormat(string name)
	{
		Name = name;
	}

	public HgtFormat(string name, int seconds, int size)
	{
		Name = name;
		SecondsPerPixel = seconds;
		PixelsPerSecond = 1d / SecondsPerPixel;
		RowsAndColumns = size;
		TotalBytes = RowsAndColumns * RowsAndColumns * 2;
	}

	public int GetBufferPos(int x, int y)
	{
		return (y * RowsAndColumns + x) * 2;
	}

	public static HgtFormat Get(string filename)
	{
		return Get(new FileInfo(filename).Length);
	}

	public static HgtFormat Get(Stream stream)
	{
		return Get(stream.Length);
	}

	private static HgtFormat Get(long size)
	{
		foreach (var format in Formats)
		{
			if (size == format.TotalBytes)
			{
				return format;
			}
		}
		return Unknown;
	}

	public static double GetDegreesPerPixel(string filename)
	{
		return Get(filename).GetDegreesPerPixel();
	}

	public double GetDegreesPerPixel()
	{
		return SecondsPerPixel / 3600d;
	}
}

public class Hgt : IDisposable
{
	public const short DataVoid = -32768;

	public AreaBounds bounds = new AreaBounds(0, 0, 0, 0);
	public AreaBounds Bounds { get => bounds; }
	public int Width { get { return Format.RowsAndColumns; } }
	public int Height { get { return Format.RowsAndColumns; } }

	public byte[] Buffer { get; private set; } = null;

	public HgtFormat Format { get; private set; }
	private Stream fileStream;
	private readonly byte[] twoBytes = new byte[2];

	public static Hgt Open(string filename)
	{
		Hgt hgt = new Hgt();
		hgt.Open(filename, true);
		return hgt;
	}

	public void Open(string path, int lonDec, int latDec, bool readAll = true)
	{
		string filename = Path.Combine(path, (latDec > 0 ? 'N' : 'S') + Math.Abs(latDec).ToString("02d") + (lonDec > 0 ? 'E' : 'W') + Math.Abs(lonDec).ToString("03d") + ".hgt");
		Open(filename, readAll);
	}

	public void Open(string filename, bool readAll = true)
	{
		if (!GetFileLonLat(filename))
		{
			Debug.LogError("Failed to open " + filename + ": Wrong file name");
			return;
		}

		try
		{
			fileStream = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);

			Format = HgtFormat.Get(fileStream);
			if (Format.IsUnknown)
			{
				fileStream = null;
				Debug.LogError("Failed to open " + filename + ": invalid file size (" + Buffer.Length + ")");
				return;
			}
		}
		catch (Exception e)
		{
			Debug.LogError("Failed to open " + filename + ": " + e.Message);
			return;
		}

		if (readAll || !fileStream.CanSeek)
		{
			Buffer = new byte[Format.TotalBytes];
			int bytesRead = fileStream.Read(Buffer, 0, Buffer.Length);
			if (bytesRead != Buffer.Length)
			{
				Debug.LogError("Didn't read correct buffer size. Expected: " + Buffer.Length + ", Read: " + bytesRead);
			}
		}
	}

	//
	// Inheritance Methods
	//

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	private bool disposed = false;
	private void Dispose(bool disposing)
	{
		if (disposed)
			return;

		// Free Managed Resources
		if (disposing)
		{
			if (fileStream != null)
			{
				fileStream.Dispose();
				fileStream = null;
			}
		}

		// Free Unmanaged Resources
		// ...

		disposed = true;

		// Call the base class implementation
		// base.Dispose(disposing);
	}

	//
	// Public Methods
	//

	//public double GetDegreesPerPixel()
	//{
	//	return Format.GetDegreesPerPixel();
	//}

	public static AreaBounds GetBounds(string filename)
	{
		AreaBounds bounds = new AreaBounds(0, 0, 0, 0);

		if (filename.Length < 7)
			return bounds;

		string name = Path.GetFileNameWithoutExtension(filename);
		if (name.StartsWithIgnoreCase("N"))
		{
			if (!int.TryParse(name.Substring(1, 2), out int lat))
				return bounds;
			bounds.south = lat;
		}
		else if (name.StartsWithIgnoreCase("S"))
		{
			if (!int.TryParse(name.Substring(1, 2), out int lat))
				return bounds;
			bounds.south = -lat;
		}
		else
		{
			return bounds;
		}

		name = name.Substring(3);
		if (name.StartsWithIgnoreCase("W"))
		{
			if (!int.TryParse(name.Substring(1, 3), out int lon))
				return bounds;
			bounds.west = -lon;
		}
		else if (name.StartsWithIgnoreCase("E"))
		{
			if (!int.TryParse(name.Substring(1, 3), out int lon))
				return bounds;
			bounds.west = lon;
		}
		else
		{
			return bounds;
		}

		bounds.north = bounds.south + 1;
		bounds.east = bounds.west + 1;

		return bounds;
	}

	private bool GetFileLonLat(string filename)
	{
		bounds = GetBounds(filename);
		return !bounds.IsEmpty;
	}

	public short Get(int x, int y)
	{
		int pos = Format.GetBufferPos(x, y);

		if (Buffer != null)
		{
			twoBytes[0] = Buffer[pos];
			twoBytes[1] = Buffer[pos + 1];
		}
		else
		{
			// DataVoid (-32768 in big-endian)
			twoBytes[0] = 0x80;
			twoBytes[1] = 0x00;

			if (fileStream.CanSeek)
			{
				fileStream.Seek(pos, SeekOrigin.Begin);
				fileStream.Read(twoBytes, 0, 2);
			}
		}

		return (short)((twoBytes[0] << 8) | twoBytes[1]);
	}

	public bool Get(int x, int y, out short height)
	{
		height = Get(x, y);
		if (height == DataVoid)
		{
			Debug.LogError("Void pixel found on (" + x + "," + y + ")");
			return false;
		}
		return true;
	}

	// Returns interpolated height between four neighbour points
	public double GetInterpolated(double lon, double lat)
	{
		int lonDec = (int)Math.Floor(lon);
		int latDec = (int)Math.Floor(lat);

		double secondsLon = (lon - lonDec) * 3600;
		double secondsLat = (lat - latDec) * 3600;

		// X coresponds to x/y values,
		// Everything easter/norther (< S) is rounded to X.
		//
		//  y   ^
		//  3   |       |   S
		//      +-------+-------
		//  0   |   X   |
		//      +-------+-------->
		// (sec)    0        3   x  (lon)

		// Both values are 0-1199 (1200 reserved for interpolating)
		int x = (int)(secondsLon * Format.PixelsPerSecond);
		int y = (int)(secondsLat * Format.PixelsPerSecond);

		// Get Norther and Easter points
		// h0------------h1
		// |             |
		// |--dx-- o     |
		// |       |     |
		// |      dy     |
		// |       |     |
		// h2------------h3   
		short[] height = new short[4];
		height[0] = Get(x, y + 1);
		height[1] = Get(x + 1, y + 1);
		height[2] = Get(x, y);
		height[3] = Get(x + 1, y);

		double dy = (secondsLat % Format.SecondsPerPixel) * Format.PixelsPerSecond;
		double dx = (secondsLon % Format.SecondsPerPixel) * Format.PixelsPerSecond;

		// Bilinear interpolation
		return height[0] * dy * (1 - dx) +
				height[1] * dy * dx +
				height[2] * (1 - dy) * (1 - dx) +
				height[3] * (1 - dy) * dx;
	}

}
