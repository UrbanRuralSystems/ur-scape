// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos (joos@arch.ethz.ch)
//
// References:
//   6.3.2.4 Prime Meridian Codes:  http://geotiff.maptools.org/spec/geotiff6.html#6.3.2.4

public enum PrimeMeridian
{
	Undefined = 0,

	Greenwich = 8901,
	Lisbon = 8902,
	Paris = 8903,
	Bogota = 8904,
	Madrid = 8905,
	Rome = 8906,
	Bern = 8907,
	Jakarta = 8908,
	Ferro = 8909,
	Brussels = 8910,
	Stockholm = 8911,

	UserDefined = 32767,
}
