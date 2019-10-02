// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos (joos@arch.ethz.ch)
//
// References:
//   6.3.2.3 Ellipsoid Codes:  http://geotiff.maptools.org/spec/geotiff6.html#6.3.2.3

public enum Ellipsoid
{
	Undefined = 0,

	Airy_1830 = 7001,
	Airy_Modified_1849 = 7002,
	Australian_National_Spheroid = 7003,
	Bessel_1841 = 7004,
	Bessel_Modified = 7005,
	Bessel_Namibia = 7006,
	Clarke_1858 = 7007,
	Clarke_1866 = 7008,
	Clarke_1866_Michigan = 7009,
	Clarke_1880_Benoit = 7010,
	Clarke_1880_IGN = 7011,
	Clarke_1880_RGS = 7012,
	Clarke_1880_Arc = 7013,
	Clarke_1880_SGA_1922 = 7014,
	Everest_1830_1937_Adjustment = 7015,
	Everest_1830_1967_Definition = 7016,
	Everest_1830_1975_Definition = 7017,
	Everest_1830_Modified = 7018,
	GRS_1980 = 7019,
	Helmert_1906 = 7020,
	Indonesian_National_Spheroid = 7021,
	International_1924 = 7022,
	International_1967 = 7023,
	Krassowsky_1940 = 7024,
	NWL_9D = 7025,
	NWL_10D = 7026,
	Plessis_1817 = 7027,
	Struve_1860 = 7028,
	War_Office = 7029,
	WGS_84 = 7030,
	GEM_10C = 7031,
	OSU86F = 7032,
	OSU91A = 7033,
	Clarke_1880 = 7034,
	Sphere = 7035,

	UserDefined = 32767,
}
