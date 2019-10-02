// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEditor;

public class GeoCalculatorWindow : EditorWindow
{
    private const string WindowName = "Geo Calculator";


    private double all2mX = 0;
    private double all2mY = 0;

    private float m2llX = 0;
    private float m2llY = 0;

    private double ap2mX = 0;
    private double ap2mY = 0;
    private int ap2mZ = 0;

    private double rp2mX = 0;
    private double rp2mY = 0;
    private int rp2mZ = 0;

    private float am2pX = 0;
    private float am2pY = 0;
    private int am2pZ = 0;

    private float rm2pX = 0;
    private float rm2pY = 0;
    private int rm2pZ = 0;

    private double all2tX = 0;
    private double all2tY = 0;
    private int all2tZ = 0;

    private int at2llX = 0;
    private int at2llY = 0;
    private int at2llZ = 0;

    [MenuItem("Window/" + WindowName)]
    static void ShowWindow()
    {
        // Get existing open window or if none, make a new one:
        GetWindow<GeoCalculatorWindow>(WindowName).Show();
    }


    void OnGUI()
    {
        GUILayout.Label("Absolute longitude/latitude to meters", EditorStyles.boldLabel);
        ShowLonLatFields(ref all2mX, ref all2mY);
        ShowResultMeters(GeoCalculator.LonLatToMeters(all2mX, all2mY));

        EditorGUILayout.Space();

        GUILayout.Label("Absolute meters to longitude/latitude", EditorStyles.boldLabel);
        m2llX = EditorGUILayout.FloatField("Meters X", m2llX);
        m2llY = EditorGUILayout.FloatField("Meters Y", m2llY);
        ShowResultLonLat(GeoCalculator.MetersToLonLat(m2llX, m2llX));

        EditorGUILayout.Space();

        GUILayout.Label("Absolute pixels to meters", EditorStyles.boldLabel);
        ShowPixelFields(ref ap2mX, ref ap2mY, ref ap2mZ);
        ShowResultMeters(GeoCalculator.AbsolutePixelsToMeters(ap2mX, ap2mY, ap2mZ));

        EditorGUILayout.Space();

        GUILayout.Label("Relative pixels to meters", EditorStyles.boldLabel);
        ShowPixelFields(ref rp2mX, ref rp2mY, ref rp2mZ);
        ShowResultMeters(GeoCalculator.RelativePixelsToMeters(rp2mX, rp2mY, rp2mZ));

        EditorGUILayout.Space();

        GUILayout.Label("Absolute meters to pixels", EditorStyles.boldLabel);
        ShowMeterFields(ref am2pX, ref am2pY, ref am2pZ);
        ShowResultPixels(GeoCalculator.AbsoluteMetersToPixels(am2pX, am2pY, am2pZ));

        EditorGUILayout.Space();

        GUILayout.Label("Relative meters to pixels", EditorStyles.boldLabel);
        ShowMeterFields(ref rm2pX, ref rm2pY, ref rm2pZ);
        ShowResultPixels(GeoCalculator.RelativeMetersToPixels(rm2pX, rm2pY, rm2pZ));

        EditorGUILayout.Space();

        GUILayout.Label("Absolute longitude/latitude to tile", EditorStyles.boldLabel);
        ShowLonLatFields(ref all2tX, ref all2tY, ref all2tZ);
        ShowResultTile(GeoCalculator.AbsoluteCoordinateToTile(all2tX, all2tY, all2tZ));

        EditorGUILayout.Space();

        GUILayout.Label("Absolute tile to longitude/latitude", EditorStyles.boldLabel);
        ShowTileFields(ref at2llX, ref at2llY, ref at2llZ);
        ShowResultLonLat(GeoCalculator.AbsoluteTileToCoordinate(at2llX, at2llY, at2llZ));

    }

    private void ShowLonLatFields(ref double longitude, ref double latitude)
    {
        longitude = EditorGUILayout.DoubleField("Longitude", longitude);
        latitude = EditorGUILayout.DoubleField("Latitude", latitude);
    }
    private void ShowLonLatFields(ref double longitude, ref double latitude, ref int zoom)
    {
        ShowLonLatFields(ref longitude, ref latitude);
        zoom = EditorGUILayout.IntField("Zoom Level", zoom);
    }

    private void ShowMeterFields(ref float x, ref float y, ref int z)
    {
        x = EditorGUILayout.FloatField("Meters X", x);
        y = EditorGUILayout.FloatField("Meters Y", y);
        z = EditorGUILayout.IntField("Zoom Level", z);
    }

    private void ShowPixelFields(ref double x, ref double y, ref int z)
    {
        x = EditorGUILayout.DoubleField("Pixels X", x);
        y = EditorGUILayout.DoubleField("Pixels Y", y);
        z = EditorGUILayout.IntField("Zoom Level", z);
    }

    private void ShowTileFields(ref int x, ref int y, ref int z)
    {
        x = EditorGUILayout.IntField("X", x);
        y = EditorGUILayout.IntField("Y", y);
        z = EditorGUILayout.IntField("Z", z);
    }

    private void ShowResultMeters(Distance meters)
    {
        EditorGUILayout.LabelField("Meters X", meters.x.ToString());
        EditorGUILayout.LabelField("Meters Y", meters.y.ToString());
    }

    private void ShowResultLonLat(Coordinate lonlat)
    {
        EditorGUILayout.LabelField("Longitude", lonlat.Longitude.ToString());
        EditorGUILayout.LabelField("Latitude", lonlat.Latitude.ToString());
    }

    private void ShowResultPixels(Distance pixels)
    {
        EditorGUILayout.LabelField("Pixels X", pixels.x.ToString());
        EditorGUILayout.LabelField("Pixels Y", pixels.y.ToString());
    }

    private void ShowResultTile(MapTileId tileId)
    {
        EditorGUILayout.LabelField("Tile ID", tileId.ToString());
    }

    private void ShowResultTiles(Vector2 tiles)
    {
        EditorGUILayout.LabelField("Tiles X", tiles.x.ToString());
        EditorGUILayout.LabelField("Tiles Y", tiles.y.ToString());
    }

}
