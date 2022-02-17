// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

// Notes:
// Google Earth is in a geographic coordinate system with the WGS84 datum. (EPSG: 4326)
// Google Maps is in a projected coordinate system that is based on the WGS84 datum. (EPSG 3857)
// The OSM database is in decimal degrees & datum of WGS84. (EPSG: 4326)
// OSM tiles and the Web Map Service are in the projected coordinate system that is based on the WGS84 datum. (EPSG 3857)
// Tiles from Google Maps and OSM will be in Sperical Mercator (EPSG 3857 or srid: 900913)
//
// EPSG:4326 (aka WGS84) treats the earth as an ellipsoid, using lat/long decimal degree values (-180 to 180 and -85 to 85)
// EPSG:3785 (unofficially EPSG:900913), uses a spherical (rather than ellipsoidal) model of the Earth.
// EPSG:3857 (aka Web Mercator, Google Web Mercator, Spherical Mercator, WGS84 Web Mercator or WGS84 Pseudo-Mercator)
//           treats the earth as a sphere, uses x/y axis coordinate values (-20037508.34 to 20037508.34) with meters as unit.

using System;

public static class GeoCalculator
{
    private const int TileSize = MapTile.Size;   // pixels

    // Equatorial radius(m)          6378137    
    // OSM radius(m)                 6372798         
    // Volumetric mean radius(m)     6371008
    public const double EarthRadius = 6378137.0;                            // meters
    public const double EarthDiameter = EarthRadius * 2.0;					// meters

    private const double invTileSize = 1.0 / TileSize;
    public const double EarthCircumference = Math.PI * EarthDiameter;		// meters (~ 40075 km)
    public const double InvEarthCircumference = 1.0 / EarthCircumference;
	public const double HalfEarthCircumference = Math.PI * EarthRadius;		// meters
	public const double InitialResolution = EarthCircumference / TileSize;	// meters/pixel at zoom 0
    public const double Deg2Meters = HalfEarthCircumference / 180.0;		// meters per degree (~ 111 km)
    public const double Meters2Deg = 180.0 / HalfEarthCircumference;		// degrees per meter
	public const double Rad2Meters = EarthRadius;							// meters per radian
	public const double Meters2Rad = 1.0 / EarthRadius;						// radians per meter

	public const double Deg2HalfRad = Math.PI / 360.0;
    public const double Rad2Deg = 180.0 / Math.PI;
	public const double Deg2Rad = Math.PI / 180.0;
	public const double HalfPI = Math.PI / 2.0;
	public const double invPI = 1.0 / Math.PI;
	public const double inv360 = 1.0 / 360.0;

    // Min/Max latitude limit in Mercator projection
    public const double MinLatitude = -85.051128779806592377796715521925;
    public const double MaxLatitude = 85.051128779806592377796715521925;

    // Max longitude limit in Mercator projection
    public const double MinLongitude = -180;
    public const double MaxLongitude = 180;

    public static readonly double[] MetersPerPixel;
    public static readonly double[] PixelsPerMeter;

    static GeoCalculator()
    {
        MetersPerPixel = new double[21];
        PixelsPerMeter = new double[21];
        for (int i = 0; i <= 20; i++)
        {
            double pow = Math.Pow(2, i);
            MetersPerPixel[i] = InitialResolution / pow;
            PixelsPerMeter[i] = pow / InitialResolution;
        }
    }

    public static double PixelsToMetes(int pixels, float canvasScaleFactor, float zoom)
    {
        return pixels * canvasScaleFactor * InitialResolution / Math.Pow(2, zoom);
    }

    public static double MetersToPixels(float meters, float canvasScaleFactor, float zoom)
    {
        return meters * Math.Pow(2, zoom) / (canvasScaleFactor * InitialResolution);
    }

    // Converts given lat/lon (WGS84) to X/Y in Spherical Mercator (EPSG:3785)
    public static Distance LonLatToMeters(double lon, double lat)
    {
        return new Distance(
            lon * Deg2Meters,
            Math.Log(Math.Tan((90d + lat) * Deg2HalfRad)) * Rad2Meters);         // Rad2Deg * Deg2Meters => Rad2Meters
    }

	public static double LatitudeToMeters(double lat)
	{
		return Math.Log(Math.Tan((90d + lat) * Deg2HalfRad)) * Rad2Meters;
	}

	public static double LatitudeToNormalizedMercator(double lat)
    {
        return Math.Log(Math.Tan((90d + lat) * Deg2HalfRad)) * invPI;        // [0, 90] -> [0, 1]
    }

    // Converts X/Y point from Spherical Mercator (EPSG:3785) to lat/lon (WGS84)
    public static Coordinate MetersToLonLat(Distance m)
    {
        return MetersToLonLat(m.x, m.y);
    }
    public static Coordinate MetersToLonLat(double x, double y)
    {
        double lon = x * Meters2Deg;
        double lat = (2 * Math.Atan(Math.Exp(y * Meters2Rad)) - HalfPI) * Rad2Deg;  // Meters2Deg * Deg2Rad => Meters2Rad
        return new Coordinate(lon, lat);
    }

	private const double k0 = 0.9996;
	private const double DefaultF = 0.00335281066474748;
	public static Coordinate MetersUtmToLonLat(double x, double y, int zone, bool north, double f = DefaultF)
	{
		double falseEasting = 500e3;
		double falseNorthing = north ? 0 : 10000e3;

		double e = Math.Sqrt(f * (2 - f));
		double n = f / (2 - f);
		double n2 = n * n;
		double n3 = n2 * n;
		double n4 = n2 * n2;
		double n6 = n4 * n2;
		double A = EarthRadius / (1 + n) * (1 + n2 / 4 + n4 / 64 + n6 / 256);
		double invk0A = 1.0 / (k0 * A);
		const double iterations = 3;
		double[] β = {
						0,
						0.5 * n - 2.0 * n2 / 3.0 + 37.0 * n3 / 96.0,
						n2 / 48.0 + n3 / 15.0,
						17.0 * n3 / 480.0,
					};

		double[] d = {
						0,
						2 * n - 2.0 * n2 / 3.0 - 2.0 * n3,
						7 * n2 / 3.0 - 8 * n3 / 5.0,
						56 * n3 / 15.0,
					};

		x -= falseEasting;
		y -= falseNorthing;

		double ξ = y * invk0A;
		double twoξ = 2 * ξ;
		double η = x * invk0A;
		double twoη = 2 * η;

		double ξʹ = ξ;
		double ηʹ = η;
		for (int j = 1; j <= iterations; j++)
		{
			ξʹ -= β[j] * Math.Sin(twoξ * j) * Math.Cosh(twoη * j);
			ηʹ -= β[j] * Math.Cos(twoξ * j) * Math.Sinh(twoη * j);
		}

		double lon = Math.Atan2(Math.Sinh(ηʹ), Math.Cos(ξʹ)) * Rad2Deg;
		lon += zone * 6 - 180 + 3;

		double X = Math.Asin(Math.Sin(ξʹ) / Math.Cosh(ηʹ));
		double twoX = 2 * X;
		double lat = X;
		for (int j = 1; j <= iterations; j++)
			lat += d[j] * Math.Sin(twoX * j);
		lat *= Rad2Deg;

		return new Coordinate(lon, lat);
	}

	private static double ATanh(double x)
	{
		return (Math.Log(1 + x) - Math.Log(1 - x)) * 0.5;
	}

	// Converts absolute pixel coordinates for a given zoom level to Spherical Mercator (EPSG:3785)
	public static Distance AbsolutePixelsToMeters(Distance p, int zoom)
    {
        return AbsolutePixelsToMeters(p.x, p.y, zoom);
    }
    public static Distance AbsolutePixelsToMeters(double x, double y, int zoom)
    {
        double mpp = MetersPerPixel[zoom];
        return new Distance(x * mpp - HalfEarthCircumference, y * mpp - HalfEarthCircumference);
    }

    public static Distance RelativePixelsToMeters(Distance p, int zoom)
    {
        return RelativePixelsToMeters(p.x, p.y, zoom);
    }
    public static Distance RelativePixelsToMeters(double x, double y, int zoom)
    {
        double mpp = MetersPerPixel[zoom];
        return new Distance(x * mpp, y * mpp);
    }

    // Converts X/Y point from Spherical Mercator (EPSG:3785) to pyramid pixel coordinates for a given zoom level
    public static Distance AbsoluteMetersToPixels(Distance m, int zoom)
    {
        return AbsoluteMetersToPixels(m.x, m.y, zoom);
    }
    public static Distance AbsoluteMetersToPixels(double x, double y, int zoom)
    {
        double ppm = PixelsPerMeter[zoom];
        return new Distance((x + HalfEarthCircumference) * ppm, (y + HalfEarthCircumference) * ppm);
    }

    // Converts X/Y distance from Spherical Mercator (EPSG:3785) to pyramid pixel coordinates for a given zoom level
    public static Distance RelativeMetersToPixels(Distance m, int zoom)
    {
        return RelativeMetersToPixels(m.x, m.y, zoom);
    }
    public static Distance RelativeMetersToPixels(double x, double y, int zoom)
    {
        double ppm = PixelsPerMeter[zoom];
        return new Distance(x * ppm, y * ppm);
    }


    //
    // Optimized shortcut methods
    //
    
    // Convert WGS84 coordinates (lon/lat) to OSM tile coordinates
    public static MapTileId AbsoluteCoordinateToTile(Coordinate coord, int zoomLevel)
    {
        return AbsoluteCoordinateToTile(coord.Longitude, coord.Latitude, zoomLevel);
    }
    public static MapTileId AbsoluteCoordinateToTile(double lon, double lat, int zoomLevel)
    {
        double twoToThePowerOfZoom = 1 << zoomLevel;
        double normalizedLat = LatitudeToNormalizedMercator(lat);

        return new MapTileId(
            (int)((lon + 180.0) * twoToThePowerOfZoom * inv360),
            (int)((1.0 - normalizedLat) * twoToThePowerOfZoom * 0.5),
            zoomLevel);
    }

    // Convert OSM tile coordinates to WGS84 coordinates (lon/lat) (upper left corner of the tile).
    public static Coordinate AbsoluteTileToCoordinate(MapTileId tileId)
    {
        return AbsoluteTileToCoordinate(tileId.X, tileId.Y, tileId.Z);
    }
    public static Coordinate AbsoluteTileToCoordinate(int x, int y, int zoomLevel)
    {
        double invTwoToThePowerOfZoom = 1.0 / Math.Pow(2.0, zoomLevel);
        double n = Math.PI - 2.0 * Math.PI * y * invTwoToThePowerOfZoom;
        
        return new Coordinate(
            x * invTwoToThePowerOfZoom * 360.0 - 180.0,
            Rad2Deg * Math.Atan(Math.Sinh(n)));
    }

    // Convert OSM tile coordinates to meters (upper left corner of the tile).
    public static Distance AbsoluteTileToMeters(MapTileId tileId)
    {
        return AbsoluteTileToMeters(tileId.X, tileId.Y, tileId.Z);
    }
    public static Distance AbsoluteTileToMeters(int x, int y, int zoom)
    {
        var res = 2.0 / Math.Pow(2.0, zoom);
        return new Distance(
            (x * res - 1.0) * HalfEarthCircumference,
            (1.0 - y * res) * HalfEarthCircumference);
    }

	public static void GetDistanceInMeters(double lon1, double lat1, double lon2, double lat2, out double x, out double y)
	{
		var metersX1 = lon1 * Deg2Meters;
		var metersY1 = Math.Log(Math.Tan((90d + lat1) * Deg2HalfRad)) * Rad2Meters;

		var metersX2 = lon2 * Deg2Meters;
		var metersY2 = Math.Log(Math.Tan((90d + lat2) * Deg2HalfRad)) * Rad2Meters;

		x = Math.Abs(metersX1 - metersX2);
		y = Math.Abs(metersY1 - metersY2);
	}

	public static double GetDistanceInMeters(double lon1, double lat1, double lon2, double lat2)
	{
		var metersX1 = lon1 * Deg2Meters;
		var metersY1 = Math.Log(Math.Tan((90d + lat1) * Deg2HalfRad)) * Rad2Meters;

		var metersX2 = lon2 * Deg2Meters;
		var metersY2 = Math.Log(Math.Tan((90d + lat2) * Deg2HalfRad)) * Rad2Meters;

		var x = metersX1 - metersX2;
		var y = metersY1 - metersY2;
		return Math.Pow(x * x + y * y, 0.5);
	}

}
