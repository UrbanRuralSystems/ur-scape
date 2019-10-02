// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)


using System;
using UnityEngine;

public class ShapefileInterpreter : Interpreter
{
	private static readonly DataFormat dataFormat = new DataFormat("Shapefile", "shp");
	public override DataFormat GetDataFormat() => dataFormat;

	public override BasicInfo GetBasicInfo(string filename)
	{
		BasicInfo info = new BasicInfo();
		try
		{
			//var geoTiff = new GeoTiff(filename);
			//info.bounds = geoTiff.Bounds;
			info.isRaster = false;
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
		return false;
	}

	public override bool Write(string filename, Patch patch)
	{
		return false;
	}

}
