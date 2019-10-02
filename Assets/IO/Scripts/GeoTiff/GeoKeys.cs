// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos (joos@arch.ethz.ch)
//
// References:
//   Key Code Summary: http://geotiff.maptools.org/spec/geotiff6.html#6.3
//   GeoTIFF "Key" Structure: http://geotiff.maptools.org/spec/geotiff2.4.html
//   GeoKeys details: http://geotiff.maptools.org/spec/geotiff2.7.html

using BitMiracle.LibTiff.Classic;
using System;
using UnityEngine;

public partial class GeoTiff
{
	private class GeoKeyInfo
	{
		public double[] doubleParams;
		public string asciiParams;

		public ushort TIFFTagLocation;
		public ushort Count;
		public ushort ValueOffset;

		public ushort GetValue()
		{
			if (TIFFTagLocation == 0)
			{
				return ValueOffset;
			}
			return 0;
		}

		public double GetDouble()
		{
			if (TIFFTagLocation == ExtraTiffTag.GeoDoubleParamsTag && Count == 1)
			{
				return doubleParams[ValueOffset];
			}
			return 0;
		}

		public string GetString()
		{
			if (TIFFTagLocation == ExtraTiffTag.GeoAsciiParamsTag)
			{
				return asciiParams.Substring(ValueOffset, Count - 1);
			}
			return string.Empty;
		}

	}

	private void ReadGdalMetadata()
	{
		FieldValue[] field = Tiff.GetField((TiffTag)ExtraTiffTag.GDAL_METADATA);
		if (field != null && field.Length == 2)
		{
			//string xml = field[1].ToString();
		}
	}

	private void ReadNoDataValue()
	{
		FieldValue[] field = Tiff.GetField((TiffTag)ExtraTiffTag.GDAL_NODATA);
		if (field != null && field.Length == 2)
		{
			NoDataValue = double.Parse(field[1].ToString(), System.Globalization.CultureInfo.InvariantCulture);
		}
	}

	private void ReadGeoKeyDirectory()
	{
		FieldValue[] field = Tiff.GetField((TiffTag)ExtraTiffTag.GeoKeyDirectoryTag);
		if (field != null && field.Length == 2 && field[0].ToInt() >= 4)
		{
			int count = field[0].ToInt();
			var bytes = field[1].ToByteArray();
			ushort[] shorts = bytes.ConvertToArray(count, sizeof(ushort), BitConverter.ToUInt16);

			ushort KeyDirectoryVersion = shorts[0];
			ushort KeyRevision = shorts[1];
			ushort MinorRevision = shorts[2];
			ushort NumberOfKeys = shorts[3];

			if (NumberOfKeys > 0)
			{
				var info = new GeoKeyInfo
				{
					doubleParams = ReadGeoDoubleParamsTag(),
					asciiParams = ReadGeoAsciiParamsTag()
				};

				for (ushort i = 0; i < NumberOfKeys; i++)
				{
					ushort keyOffset = (ushort)(4 + i * 4);

					var keyID = (KeyID)shorts[keyOffset];
					info.TIFFTagLocation = shorts[keyOffset + 1];
					info.Count = shorts[keyOffset + 2];
					info.ValueOffset = shorts[keyOffset + 3];

					switch (keyID)
					{
						//
						// GeoTIFF Configuration Keys
						//
						case KeyID.GTModelTypeGeoKey:
							ModelType = (ModelType)info.GetValue();
							break;
						case KeyID.GTRasterTypeGeoKey:
							RasterType = (RasterType)info.GetValue();
							break;
						case KeyID.GTCitationGeoKey:
							Citation = info.GetString();
							break;

						//
						// Geographic Coordinate System Parameter Keys
						//
						case KeyID.GeographicTypeGeoKey:
							GeographicCoordinateSystem = (GeographicCoordinateSystem)info.GetValue();
							break;
						case KeyID.GeogCitationGeoKey:
							GeographicCitation = info.GetString();
							break;
						case KeyID.GeogGeodeticDatumGeoKey:
							GeodeticDatum = (GeodeticDatum)info.GetValue();
							break;
						case KeyID.GeogPrimeMeridianGeoKey:
							PrimeMeridian = (PrimeMeridian)info.GetValue();
							break;
						case KeyID.GeogLinearUnitsGeoKey:
							LinearUnit = (LinearUnit)info.GetValue();
							break;
						case KeyID.GeogLinearUnitSizeGeoKey:
							GeogLinearUnitSize = info.GetDouble();
							break;
						case KeyID.GeogAngularUnitsGeoKey:
							AngularUnit = (AngularUnit)info.GetValue();
							break;
						case KeyID.GeogAngularUnitSizeGeoKey:
							AngularUnitSize = info.GetDouble();
							break;
						case KeyID.GeogEllipsoidGeoKey:
							Ellipsoid = (Ellipsoid)info.GetValue();
							break;
						case KeyID.GeogSemiMajorAxisGeoKey:
							SemiMajorAxis = info.GetDouble();
							break;
						case KeyID.GeogSemiMinorAxisGeoKey:
							SemiMinorAxis = info.GetDouble();
							break;
						case KeyID.GeogInvFlatteningGeoKey:
							InvFlattening = info.GetDouble();
							break;
						case KeyID.GeogAzimuthUnitsGeoKey:
							AzimuthUnit = (AngularUnit)info.GetValue();
							break;
						case KeyID.GeogPrimeMeridianLongGeoKey:
							PrimeMeridianLongitude = info.GetDouble();
							break;

						//
						// Projected Coordinate System Parameter Keys
						//
						case KeyID.ProjectedCSTypeGeoKey:
							ProjectedCoordinateSystem = (ProjectedCoordinateSystem)info.GetValue();
							break;
						case KeyID.PCSCitationGeoKey:
							Citation = info.GetString();
							break;
						case KeyID.ProjLinearUnitsGeoKey:
							LinearUnit = (LinearUnit)info.GetValue();
							break;
						case KeyID.ProjLinearUnitSizeGeoKey:
							ProjLinearUnitSize = info.GetDouble();
							break;

							
						default:
							Debug.LogWarning("Ignored GeoKey: " + keyID);
							break;
					}
				}
			}
		}
	}

	private double[] ReadGeoDoubleParamsTag()
	{
		double[] doubles = null;
		FieldValue[] field = Tiff.GetField((TiffTag)ExtraTiffTag.GeoDoubleParamsTag);
		if (field != null && field.Length == 2)
		{
			int count = field[0].ToInt();
			var bytes = field[1].ToByteArray();
			doubles = bytes.ConvertToArray(count, sizeof(double), BitConverter.ToDouble);
		}
		return doubles;
	}

	private string ReadGeoAsciiParamsTag()
	{
		string asciiParams = null;
		FieldValue[] field = Tiff.GetField((TiffTag)ExtraTiffTag.GeoAsciiParamsTag);
		if (field != null && field.Length == 2)
		{
			var bytes = field[1].ToByteArray();
			asciiParams = System.Text.Encoding.Default.GetString(bytes);
		}
		return asciiParams;
	}

}

public static class GeoTiffExtensions
{
	public delegate T ConvertFunc<T>(byte[] bytes, int offset);
	public static T[] ConvertToArray<T>(this byte[] bytes, int count, int stride, ConvertFunc<T> func)
	{
		T[] values = new T[count];
		int offset = 0;
		for (int i = 0; i < count; i++)
		{
			values[i] = func(bytes, offset);
			offset += stride;
		}
		return values;
	}

}
