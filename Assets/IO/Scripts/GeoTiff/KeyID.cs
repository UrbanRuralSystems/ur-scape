// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos (joos@arch.ethz.ch)
//
// References:
//   6.2 Key ID:  http://geotiff.maptools.org/spec/geotiff6.html#6.2
//   GeoKeys details: http://geotiff.maptools.org/spec/geotiff2.7.html

public enum KeyID : ushort
{
	//
	// GeoTIFF Configuration GeoKeys
	//

	GTModelTypeGeoKey = 1024,
	GTRasterTypeGeoKey = 1025,
	GTCitationGeoKey = 1026,

	//
	// Geographic Coordinate System (CS) Parameter GeoKeys
	// 

	GeographicTypeGeoKey = 2048,
	GeogCitationGeoKey = 2049,
	GeogGeodeticDatumGeoKey = 2050,
	GeogPrimeMeridianGeoKey = 2051,
	GeogLinearUnitsGeoKey = 2052,
	GeogLinearUnitSizeGeoKey = 2053,
	GeogAngularUnitsGeoKey = 2054,
	GeogAngularUnitSizeGeoKey = 2055,
	GeogEllipsoidGeoKey = 2056,
	GeogSemiMajorAxisGeoKey = 2057,
	GeogSemiMinorAxisGeoKey = 2058,
	GeogInvFlatteningGeoKey = 2059,
	GeogAzimuthUnitsGeoKey = 2060,
	GeogPrimeMeridianLongGeoKey = 2061,

	//
	// Projected Coordinate System (CS) Parameter GeoKeys
	//

	ProjectedCSTypeGeoKey = 3072,
	PCSCitationGeoKey = 3073,

	//
	// Projection Definition GeoKeys
	//

	ProjectionGeoKey = 3074,
	ProjCoordTransGeoKey = 3075,
	ProjLinearUnitsGeoKey = 3076,
	ProjLinearUnitSizeGeoKey = 3077,
	ProjStdParallel1GeoKey = 3078,
	ProjStdParallel2GeoKey = 3079,
	ProjNatOriginLongGeoKey = 3080,
	ProjNatOriginLatGeoKey = 3081,
	ProjFalseEastingGeoKey = 3082,
	ProjFalseNorthingGeoKey = 3083,
	ProjFalseOriginLongGeoKey = 3084,
	ProjFalseOriginLatGeoKey = 3085,
	ProjFalseOriginEastingGeoKey = 3086,
	ProjFalseOriginNorthingGeoKey = 3087,
	ProjCenterLongGeoKey = 3088,
	ProjCenterLatGeoKey = 3089,
	ProjCenterEastingGeoKey = 3090,
	ProjCenterNorthingGeoKey = 3091,
	ProjScaleAtNatOriginGeoKey = 3092,
	ProjScaleAtCenterGeoKey = 3093,
	ProjAzimuthAngleGeoKey = 3094,
	ProjStraightVertPoleLongGeoKey = 3095,

	//
	// Vertical CS Parameter Keys
	//

	VerticalCSTypeGeoKey = 4096,
	VerticalCitationGeoKey = 4097,
	VerticalDatumGeoKey = 4098,
	VerticalUnitsGeoKey = 4099,

}
