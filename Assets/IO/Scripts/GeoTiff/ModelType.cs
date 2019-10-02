// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos (joos@arch.ethz.ch)
//
// References:
//   6.3.1.1 Model Type Codes: http://geotiff.maptools.org/spec/geotiff6.html#6.3.1.1

public enum ModelType : ushort
{
	Undefined = 0,

	Projected = 1,  // Projection Coordinate System
	Geographic = 2,    // Geographic latitude-longitude System
	Geocentric = 3,    // Geocentric (X,Y,Z) Coordinate System

	UserDefined = 32767
}
