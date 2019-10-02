// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos (joos@arch.ethz.ch)
//
// References:
//   6.3.1.2 Raster Type Codes: http://geotiff.maptools.org/spec/geotiff6.html#6.3.1.2

public enum RasterType : ushort
{
	Undefined = 0,

	PixelIsArea = 1,
	PixelIsPoint = 2,

	UserDefined = 32767
}
