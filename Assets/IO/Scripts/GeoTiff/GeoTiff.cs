// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;
using BitMiracle.LibTiff.Classic;
using System;

public struct Tiepoint
{
	public Vector3d rasterPoint;
	public Vector3d modelPoint;

	public Tiepoint(Vector3d rp, Vector3d mp)
	{
		rasterPoint = rp;
		modelPoint = mp;
	}
}

public partial class GeoTiff : IDisposable
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public int BitsPerSample { get; private set; } = 0;
    public Type DataType { get; private set; } = null;
    private Vector3d pixelScale = Vector3d.zero;
	public AreaBounds bounds;
	public AreaBounds Bounds { get => bounds; }

	// GeoTIFF Configuration
	public ModelType ModelType { get; private set; }
	public RasterType RasterType { get; private set; } = RasterType.PixelIsArea;
	public string Citation { get; private set; }

	// Geographic Coordinate System Parameters
	public GeographicCoordinateSystem GeographicCoordinateSystem { get; private set; }
	public string GeographicCitation { get; private set; }
	public GeodeticDatum GeodeticDatum { get; private set; }
	public PrimeMeridian PrimeMeridian { get; private set; }
	public LinearUnit LinearUnit { get; private set; }
	public double GeogLinearUnitSize { get; private set; }
	public AngularUnit AngularUnit { get; private set; }
	public double AngularUnitSize { get; private set; }
	public Ellipsoid Ellipsoid { get; private set; }
	public double SemiMajorAxis { get; private set; }
	public double SemiMinorAxis { get; private set; }
	public double InvFlattening { get; private set; }
	public AngularUnit AzimuthUnit { get; private set; }
	public double PrimeMeridianLongitude { get; private set; }

	// Projected Coordinate System Parameters
	public ProjectedCoordinateSystem ProjectedCoordinateSystem { get; private set; }
	public string ProjectedCitation { get; private set; }
	public double ProjLinearUnitSize { get; private set; }

	private List<Tiepoint> tiepoints = null;
    private Matrix4x4d modelTransformation = Matrix4x4d.identity;

    public double NoDataValue { get; private set; } = -1d;

	internal Tiff Tiff { get; private set; }

	public GeoTiff(string filename)
	{
		Tiff = Tiff.Open(filename, "r");
		if (Tiff == null)
			throw new Exception("Couldn't open " + filename);

		// LogMetadata();
		ReadMetadata();
		//ExtractRasterValues();
	}

	~GeoTiff()
	{
		Dispose(false);
	}

	//
	// Inheritance Methods
	//

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}


	//
	// Public Methods
	//

	public double GetDegreesPerPixel()
	{
		switch (ModelType)
		{
			case ModelType.Geographic:
			case ModelType.Geocentric:
				return pixelScale.x;
			case ModelType.Projected:
				// ProjLinearUnitSize
				if (LinearUnit == LinearUnit.Meter)
					return pixelScale.x * GeoCalculator.Meters2Deg;
				else
					Debug.LogError("LinearUnit + " + LinearUnit + " not supported");
				break;
		}
		return 0;
	}

	//
	// Private Methods
	//

	private bool disposed = false;
	protected virtual void Dispose(bool disposing)
	{
		if (disposed)
			return;

		// Free Managed Resources
		if (disposing)
		{
			if (Tiff != null)
			{
				Tiff.Dispose();
				Tiff = null;
			}
		}

		// Free Unmanaged Resources
		// ...

		disposed = true;

		// Call the base class implementation
		// base.Dispose(disposing);
	}

	private bool TryGetField(TiffTag tag, ref FieldValue[] fieldValues)
	{
        fieldValues = Tiff.GetField(tag);
		return (fieldValues != null);
	}

    private int GetFieldInt(TiffTag tag)
    {
        return Tiff.GetField(tag)[0].ToInt();
    }

    private void ReadMetadata()
	{
		ReadGeoKeyDirectory();

		// Image Dimensions
		Width = GetFieldInt(TiffTag.IMAGEWIDTH);
        Height = GetFieldInt(TiffTag.IMAGELENGTH);
        BitsPerSample = GetFieldInt(TiffTag.BITSPERSAMPLE);
        // SamplesPerPixel = GetFieldInt(TiffTag.SAMPLESPERPIXEL);
        ReadDataType();
        ReadNoDataValue();

		ReadModelPixelScale();
        ReadModelTiepoints();
        ReadModelTransformation();

		// Compute east, west, north, south
		Vector4d northWest = modelTransformation * new Vector4d(0, 0, 0, 1);
		Vector4d southEast = modelTransformation * new Vector4d(Width, Height, 0, 1);
		bounds = new AreaBounds(northWest.x, southEast.x, northWest.y, southEast.y);

		if (ModelType == ModelType.Projected)
		{
			if (LinearUnit == LinearUnit.Meter)
			{
				if (ProjectedCoordinateSystem >= ProjectedCoordinateSystem.WGS84_UTM_zone_1N &&
					ProjectedCoordinateSystem <= ProjectedCoordinateSystem.WGS84_UTM_zone_60N)
				{
					int zone = ProjectedCoordinateSystem - ProjectedCoordinateSystem.WGS84_UTM_zone_1N;
					ProjectedToGeographic(zone, true);
				}
				else if (ProjectedCoordinateSystem >= ProjectedCoordinateSystem.WGS84_UTM_zone_1S &&
						 ProjectedCoordinateSystem <= ProjectedCoordinateSystem.WGS84_UTM_zone_60S)
				{
					int zone = ProjectedCoordinateSystem - ProjectedCoordinateSystem.WGS84_UTM_zone_1S;
					ProjectedToGeographic(zone, false);
				}
			}
			else
			{
				Debug.LogError("LinearUnit '" + LinearUnit + "' not supported");
			}
		}
	}

	private void ProjectedToGeographic(int zone, bool north)
	{
		double f = 1.0 / InvFlattening;

		var ne = GeoCalculator.MetersUtmToLonLat(bounds.east, bounds.north, zone, north, f);
		var nw = GeoCalculator.MetersUtmToLonLat(bounds.west, bounds.north, zone, north, f);
		var se = GeoCalculator.MetersUtmToLonLat(bounds.east, bounds.south, zone, north, f);
		var sw = GeoCalculator.MetersUtmToLonLat(bounds.west, bounds.south, zone, north, f);

		bounds.north = Math.Max(ne.Latitude, nw.Latitude);
		bounds.east = Math.Max(se.Longitude, ne.Longitude);
		bounds.west = Math.Min(nw.Longitude, sw.Longitude);
		bounds.south = Math.Min(sw.Latitude, se.Latitude);
	}

	private void ReadDataType()
    {
        FieldValue[] field = Tiff.GetField(TiffTag.SAMPLEFORMAT);
        if (field != null)
        {
            SampleFormat format = (SampleFormat)field[0].ToInt();
            switch (format)
            {
                case SampleFormat.IEEEFP:
                    DataType = BitsPerSample == 32 ? typeof(float) : typeof(double);
                    break;
                case SampleFormat.INT:
                    DataType = BitsPerSample <= 8 ? typeof(sbyte) : BitsPerSample <= 16 ? typeof(short) : typeof(long);
                    break;
                case SampleFormat.UINT:
                    DataType = BitsPerSample <= 8 ? typeof(byte) : BitsPerSample <= 16 ? typeof(ushort) : typeof(ulong);
                    break;
            }
        }
    }

    private void ReadModelPixelScale()
	{
        pixelScale = ReadModelPixelScale(Tiff);
    }

	private static Vector3d ReadModelPixelScale(Tiff tiff)
	{
		// This field should return an array of 2 elements: the first should be '3', and the second a vector3
		var field = tiff.GetField(TiffTag.GEOTIFF_MODELPIXELSCALETAG);
		if (field == null ||
			field.Length != 2 ||
			field[0].ToInt() != 3)
			return Vector3d.zero;

		// ModelPixelScale indicates how many degrees/inches a pixel represents
		byte[] modelPixelScale = field[1].GetBytes();
		double sX = BitConverter.ToDouble(modelPixelScale, 0);
		double sY = BitConverter.ToDouble(modelPixelScale, 8);
		double sZ = BitConverter.ToDouble(modelPixelScale, 16);
		return new Vector3d(sX, sY, sZ);
	}

	private void ReadModelTiepoints()
	{
        // This field should return an array of 2 elements:
        //   the first is the number of values (multiple of 6)
        //   the second is the array of values (pairs of vector3)
        var field = Tiff.GetField(TiffTag.GEOTIFF_MODELTIEPOINTTAG);
        if (field == null ||
            field.Length != 2 ||
            (field[0].ToInt() % 6) != 0)
            return;

        byte[] modelTiepoints = field[1].GetBytes();
        int numOfTiepointPairs = modelTiepoints.Length / (6 * sizeof(double));
		tiepoints = new List<Tiepoint>(numOfTiepointPairs);

        for (int n = 0; n < numOfTiepointPairs; ++n)
        {
            double I = BitConverter.ToDouble(modelTiepoints, 0);
            double J = BitConverter.ToDouble(modelTiepoints, 8);
            double K = BitConverter.ToDouble(modelTiepoints, 16);
            double X = BitConverter.ToDouble(modelTiepoints, 24);
            double Y = BitConverter.ToDouble(modelTiepoints, 32);
            double Z = BitConverter.ToDouble(modelTiepoints, 40);

            Vector3d rasterPoint = new Vector3d(I, J, K);
            Vector3d modelPoint = new Vector3d(X, Y, Z);

			tiepoints.Add(new Tiepoint(rasterPoint, modelPoint));
        }
	}

	private void ReadModelTransformation()
	{
		if (pixelScale == Vector3d.zero || tiepoints == null || tiepoints.Count == 0)
		{
			FieldValue[] field = Tiff.GetField(TiffTag.GEOTIFF_MODELTRANSFORMATIONTAG);
			if (field == null)
				return;

			byte[] modelTransformationBytes = field[1].GetBytes();

			int sizeOfDouble = sizeof(double);
			for (int c = 0; c < 4; ++c)
			{
				double x = BitConverter.ToDouble(modelTransformationBytes, c * sizeOfDouble);
				double y = BitConverter.ToDouble(modelTransformationBytes, (4 + c) * sizeOfDouble);
				double z = BitConverter.ToDouble(modelTransformationBytes, (8 + c) * sizeOfDouble);
				double w = BitConverter.ToDouble(modelTransformationBytes, (12 + c) * sizeOfDouble);
				modelTransformation.SetColumn(c, new Vector4d(x, y, z, w));
			}
		}
		else
		{
			var modelPoint = tiepoints[0].modelPoint;
			var rasterPoint = tiepoints[0].rasterPoint;

			if (RasterType == RasterType.PixelIsPoint)
			{
				rasterPoint.x += 0.5;
				rasterPoint.y += 0.5;
			}

			modelTransformation.SetRow(0, pixelScale.x, 0, 0, modelPoint.x - rasterPoint.x * pixelScale.x);
			modelTransformation.SetRow(1, 0,-pixelScale.y, 0, modelPoint.y + rasterPoint.y * pixelScale.y);
			modelTransformation.SetRow(2, 0, 0, pixelScale.z, modelPoint.z - rasterPoint.z * pixelScale.z);
			modelTransformation.SetRow(3, 0, 0, 0, 1);
		}
	}

    public float[] ReadRasterValues()
	{
        int width = Width;
        int height = Height;
        int bufferSize = width * height;
		int bytesPerSample = BitsPerSample / 8;
        int scanlineSize = width * bytesPerSample;

        var values = new float[bufferSize];
        byte[] buffer = new byte[scanlineSize];

		int offset;
		int index = 0;
        for (int r = 0; r < height; r++)
        {
			offset = 0;
			Tiff.ReadScanline(buffer, r);
	        for (int c = 0; c < width; c++)
	        {
				values[index++] = BitConverter.ToSingle(buffer, offset);
				offset += bytesPerSample;
			}
        }

		return values;
	}

	private void LogMetadata()
	{
		/*var fields = tiff.GetFields();
		foreach (var field in fields)
		{
			if (tiff.fieldSet(field.Bit))
			{
				var values = tiff.GetTagMethods().GetField(tiff, field.Tag);
				if (values != null)
				{
					string s = field.Name;
					if (values.Length == 1)
					{
						s += ": ";
						var value = values[0];
						Type type = value.Value.GetType();
						s += value.ToString() + " (" + type.Name + ") | ";
					}
					else
					{
						s += "{ ";
						foreach (var v in values)
						{
							Type type = v.Value.GetType();
							s += v.ToString() + " (" + type.Name + "), ";
						}
						s += "}";
					}
					Debug.Log(s);
				}
			}
		}*/

		short numberOfDirectories = Tiff.NumberOfDirectories();
		for (short d = 0; d < numberOfDirectories; ++d)
		{
			Tiff.SetDirectory(d);
			for (ushort t = ushort.MinValue; t < ushort.MaxValue; ++t)
			{
				TiffTag tag = (TiffTag)t;
				FieldValue[] values = Tiff.GetField(tag);
				if (values != null)
				{
					int count = values.Length;
					string msg = tag.ToString();
					if (count == 1)
					{
						var value = values[0];
						msg += " (" + value.Value.GetType().Name + "): ";
						Type type = value.Value.GetType();
						if (type.IsArray)
						{
							msg += "...";
						}
						else
						{
							msg += value.ToString();
						}
					}
					else
					{
						msg += ": { ";
						for (int j = 0; j < count; j++)
						{
							var value = values[j];
							Type type = value.Value.GetType();
							if (type.IsArray)
							{
								msg += value.ToString() + ", ";
							}
							else
							{
								msg += value.ToString() + " (" + type.Name + "), ";
							}
						}
						msg += "}";
					}
					Debug.Log(msg);
				}
			}
		}
	}

}
