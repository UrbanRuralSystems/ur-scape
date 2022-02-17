// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//			Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System;
using UnityEngine;
using Catfood.Shapefile;
using System.IO;

public class ShapefileInterpreter : Interpreter
{
	private static readonly DataFormat dataFormat = new DataFormat("Shapefile", "shp");
	public override DataFormat GetDataFormat() => dataFormat;

	public override BasicInfo GetBasicInfo(string filename)
	{
		BasicInfo info = new BasicInfo();
		try
		{
			var shapefile = new Shapefile(filename);
			info.bounds = shapefile.BoundingBox;
			info.isRaster = false;

			//TestPrintShapefile(shapefile);
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

		using (var shapefile = new Shapefile(filename))
		{
			if (shapefile == null)
				return false;

			// TODO: Rasterize shapefile
		}

		string rasterFile = Path.ChangeExtension(filename, "tif");

		GeoTiffInterpreter geoTiffInterpreter = new GeoTiffInterpreter();
		geoTiffInterpreter.Read(rasterFile, progress, out data);

		return true;
	}

	public override bool Write(string filename, Patch patch)
	{
		return false;
	}

	private void TestPrintShapefile(Shapefile shapefile)
    {
		Debug.Log($"Type: {shapefile.Type}");
		Debug.Log($"Extents: {shapefile.BoundingBox.west}, {shapefile.BoundingBox.east}, {shapefile.BoundingBox.north}, {shapefile.BoundingBox.south}");
		Debug.Log($"Shapes count: {shapefile.Shapes.Count}");

		for (int i = 0; i < shapefile.Features.Count; ++i)
		{
			for (int j = 0; j < shapefile.Fields.Count; ++j)
			{
				var attribute = shapefile.Features[i].Attributes[j];
				Debug.Log($"{shapefile.FieldNames[j]}");
				Debug.Log($"{attribute.Field.FieldType}: {attribute.Value}");
			}
		}
	}
}
