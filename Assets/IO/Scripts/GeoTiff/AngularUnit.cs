// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos (joos@arch.ethz.ch)
//
// References:
//   6.3.1.4 Angular Units Codes: http://geotiff.maptools.org/spec/geotiff6.html#6.3.1.4

public enum AngularUnit : ushort
{
	Undefined = 0,

	Radian = 9101,
	Degree = 9102,
	Arc_Minute = 9103,
	Arc_Second = 9104,
	Grad = 9105,
	Gon = 9106,
	DMS = 9107,
	DMS_Hemisphere = 9108,

	UserDefined = 32767
}
