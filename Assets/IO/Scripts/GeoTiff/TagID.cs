// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos (joos@arch.ethz.ch)
//
// References:
//   6.1 Tag ID:  http://geotiff.maptools.org/spec/geotiff6.html#6.1
//   Private GeoTiff Tags:  https://www.awaresystems.be/imaging/tiff/tifftags/private.html
//
// These tags extend the already existing TiffTag enum in LibTiff
//

public static class ExtraTiffTag
{
	public const ushort GeoKeyDirectoryTag = 34735;         // (SPOT)
	public const ushort GeoDoubleParamsTag = 34736;         // (SPOT)
	public const ushort GeoAsciiParamsTag = 34737;          // (SPOT)
	public const ushort GDAL_METADATA = 42112;              // (GDAL)
	public const ushort GDAL_NODATA = 42113;				// (GDAL)
}
