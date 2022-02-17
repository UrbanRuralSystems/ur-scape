// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class MapController : UrsComponent
{
    public const float MinZoomLevel = 0;
    public const float MaxZoomLevel = 20;

	[Header("Settings")]
	[Tooltip("When enabled, the longitude/latitude/zoom settings will be ignored")]
	public bool startWithCenteredWorld = true;

	[Header("Location")]
    [SerializeField]
    private double longitude;
    [SerializeField]
    private double latitude;

    [Header("Zoom")]
	public float mapScale = 1f;
	[Range(MinZoomLevel, MaxZoomLevel)]
    public float minZoomLevel = 0f;
    [Range(MinZoomLevel, MaxZoomLevel)]
    public float maxZoomLevel = 20f;
    [Range(MinZoomLevel, MaxZoomLevel)]
    public float zoom = 10f;

    [Header("Levels")]
    public DataLevels dataLevels;

    [Header("Border Buffer")]
    [Range(0f, 3f)]
    public float horizontalBuffer = 0.5f;
    [Range(0f, 3f)]
    public float verticalBuffer = 0.5f;

    [Header("Layer Controllers")]
    public MapLayerController[] controllerPrefabs;

    [Header("Helpers")]
    public TileRequestHandler tileRequestHandler;
    public PatchRequestHandler patchRequestHandler;
    public TextureCache textureCache;

    [Header("Debug")]
    public bool showDebugDrawings = false;
    public bool showViewBounds = true;
    public bool showTileGrid = true;
    public bool showWorldBounds = false;

    public delegate void OnZoomChangeDelegate(float zoom);
    public event OnZoomChangeDelegate OnZoomChange;

    public delegate void OnLevelChangeDelegate(int level);
	public event OnLevelChangeDelegate OnPreLevelChange;
	public event OnLevelChangeDelegate OnLevelChange;
    public event OnLevelChangeDelegate OnPostLevelChange;

    public delegate void OnBoundsChangeDelegate();
    public event OnBoundsChangeDelegate OnBoundsChange;

    public delegate void OnMapUpdateDelegate();
    public event OnMapUpdateDelegate OnMapUpdate;

    public double Longitude { get { return longitude; } }
    public double Latitude { get { return latitude; } }

    private float tilesToUnits;
    public float TilesToUnits { get { return tilesToUnits; } }

    private float unitsToTiles;

    private float pixelsToUnits;    // Multiply by this variable to convert pixels to Unity units
    private float unitsToPixels;    // Multiply by this variable to convert Unity units to pixels

    public float PixelsToUnits { get { return pixelsToUnits; } }

    private float unitsToMeters;
    public float UnitsToMeters { get { return unitsToMeters; } }

    private float metersToUnits;
    public float MetersToUnits { get { return metersToUnits; } }

    private int zoomLevel = 0;
    public int ZoomLevel { get { return zoomLevel; } }

    private int currentLevel = 0;
    public int CurrentLevel { get { return currentLevel; } }
	private int minLevel;
	public int MinLevel { get { return minLevel; } }
	private int maxLevel;
	public int MaxLevel { get { return maxLevel; } }

	private Distance currentMeters;     // Is the current lon/lat measured in meters
    public Distance MapCenterInMeters { get { return currentMeters; } }

    private Rect viewBounds = new Rect(0, 0, 0, 0);   // Measured in units
    public Rect ViewBounds { get => viewBounds; }

    private AreaBounds mapCoordBounds = new AreaBounds(0,0,0,0); // Measured in degrees (Lon/Lat)
    public AreaBounds MapCoordBounds { get => mapCoordBounds; }

    private readonly MapTileBounds mapTileBounds = new MapTileBounds(0, 0, 0, 0);
    public MapTileBounds MapTileBounds { get => mapTileBounds; }

    private MapTileId anchor = new MapTileId();
    public MapTileId Anchor { get => anchor; }

    private Vector2 anchorOffsetInUnits = Vector2.zero;
    public Vector2 AnchorOffsetInUnits { get => anchorOffsetInUnits; }

    private bool needsUpdate = false;
    private List<MapLayerController> layerControllers = new List<MapLayerController>();

	private static readonly AreaBounds WorldBounds = new AreaBounds(GeoCalculator.MinLongitude, GeoCalculator.MaxLongitude, GeoCalculator.MaxLatitude, GeoCalculator.MinLatitude);
	private static readonly Distance WorldExtentMeters = GeoCalculator.LonLatToMeters(WorldBounds.east, WorldBounds.north) - GeoCalculator.LonLatToMeters(WorldBounds.west, WorldBounds.south);
	private static readonly double InvLogTwo = 1.0 / Math.Log(2);


	//
	// Unity Methods
	//

	protected override void Awake()
    {
        if (Application.isPlaying)
        {
            base.Awake();

			minLevel = 0;
			maxLevel = dataLevels.levels.Length - 1;

			foreach (var prefab in controllerPrefabs)
            {
                GetOrCreateMapLayerController(prefab);
            }
        }
    }

    private void OnEnable()
	{
		if (startWithCenteredWorld)
		{
			SetCenter(0, 0);
			SetZoom(0);
			StartCoroutine(WaitForUI());
		}
		else
		{
			SetCenter(longitude, latitude);
			SetZoom(zoom);
		}

		// Ignore the first update
		needsUpdate = false;
	}

	private void LateUpdate()
    {
        if (needsUpdate)
        {
            needsUpdate = false;

            UpdateMap();
        }
    }

    private void OnValidate()
    {
        if (dataLevels != null && dataLevels.levels.Length > 0)
        {
            minZoomLevel = Mathf.Clamp(minZoomLevel, Math.Max(dataLevels.levels[0].minZoom, MinZoomLevel), MaxZoomLevel);
            maxZoomLevel = Mathf.Clamp(maxZoomLevel, MinZoomLevel, Math.Min(dataLevels.levels[dataLevels.levels.Length - 1].maxZoom, MaxZoomLevel));
        }
        if (!Application.isPlaying)
        {
            SetCenter(longitude, latitude);
            SetZoom(zoom);
        }
    }

    private void OnDrawGizmos()
    {
        if (showDebugDrawings)
        {
            if (showViewBounds)
            {
                DebugDrawViewBounds();
            }

            if (showTileGrid)
            {
                DebugDrawTiles();
            }
        }
    }


    //
    // Inheritance Methods
    //

    public override bool HasBookmarkData()
    {
        return true;
    }

    public override void SaveToBookmark(BinaryWriter bw, string bookmarkPath)
    {
        bw.Write(longitude);
        bw.Write(latitude);

        bw.Write(zoom);
        bw.Write(zoomLevel);

        bw.Write(tilesToUnits);
        bw.Write(unitsToTiles);

        bw.Write(pixelsToUnits);
        bw.Write(unitsToPixels);

        bw.Write(unitsToMeters);
        bw.Write(metersToUnits);

        bw.Write(currentLevel);

        bw.Write(currentMeters.x);
        bw.Write(currentMeters.y);

        bw.Write(viewBounds.x);
        bw.Write(viewBounds.y);
        bw.Write(viewBounds.width);
        bw.Write(viewBounds.height);

        bw.Write(mapCoordBounds.west);
        bw.Write(mapCoordBounds.east);
        bw.Write(mapCoordBounds.north);
        bw.Write(mapCoordBounds.south);

        bw.Write(mapTileBounds.West);
        bw.Write(mapTileBounds.East);
        bw.Write(mapTileBounds.North);
        bw.Write(mapTileBounds.South);

        bw.Write(anchor.ToId());

        bw.Write(anchorOffsetInUnits.x);
        bw.Write(anchorOffsetInUnits.y);
    }

    public override void LoadFromBookmark(BinaryReader br, string bookmarkPath)
    {
        longitude = br.ReadDouble();
        latitude = br.ReadDouble();

        zoom = br.ReadSingle();
        zoomLevel = br.ReadInt32();

        tilesToUnits = br.ReadSingle();
        unitsToTiles = br.ReadSingle();

        pixelsToUnits = br.ReadSingle();
        unitsToPixels = br.ReadSingle();

        unitsToMeters = br.ReadSingle();
        metersToUnits = br.ReadSingle();

        currentLevel = br.ReadInt32();

        currentMeters.x = br.ReadDouble();
        currentMeters.y = br.ReadDouble();

        viewBounds.x = br.ReadSingle();
        viewBounds.y = br.ReadSingle();
        viewBounds.width = br.ReadSingle();
        viewBounds.height = br.ReadSingle();

        mapCoordBounds.west = br.ReadDouble();
        mapCoordBounds.east = br.ReadDouble();
        mapCoordBounds.north = br.ReadDouble();
        mapCoordBounds.south = br.ReadDouble();

        mapTileBounds.West = br.ReadInt32();
        mapTileBounds.East = br.ReadInt32();
        mapTileBounds.North = br.ReadInt32();
        mapTileBounds.South = br.ReadInt32();

        anchor.Set(br.ReadInt64());

        anchorOffsetInUnits.x = br.ReadSingle();
        anchorOffsetInUnits.y = br.ReadSingle();

        SetCenter(longitude, latitude);
        SetZoom(zoom);
    }

    //
    // Public Methods
    //

    public void AddLayerController(MapLayerController layerController)
    {
        layerController.transform.SetParent(transform, false);
        layerControllers.Add(layerController);
    }

    public void RemoveLayerController(MapLayerController layerController)
    {
        layerControllers.Remove(layerController);
    }

    public T GetLayerController<T>() where T : MapLayerController
    {
        foreach (var controller in layerControllers)
        {
            if (controller is T)
            {
                return controller as T;
            }
        }
        return null;
    }

    public T GetOrCreateLayerController<T>(T prefab) where T : MapLayerController
    {
        return GetOrCreateMapLayerController(prefab) as T;
    }

    private MapLayerController GetOrCreateMapLayerController(MapLayerController prefab)
    {
        Type t = prefab.GetType();
        foreach (var controller in layerControllers)
        {
            if (t.IsInstanceOfType(controller))
            {
                return controller;
            }
        }

        // Create Layer Controller
        var newController = Instantiate(prefab);
        newController.name = prefab.name;
        newController.Init(this);

        return newController;
    }

    public void MoveInUnits(float x, float y)
    {
        var newLonLat = GetCoordinatesFromUnits(x, y);
        SetCenter(newLonLat.Longitude, newLonLat.Latitude);
    }

	public void SetViewBounds(float n, float s, float e, float w, bool adjustZoom)
    {
        // Measured in units. Center is the origin
        viewBounds.yMax = n;
        viewBounds.yMin = s;
        viewBounds.xMax = e;
        viewBounds.xMin = w;

		if (adjustZoom)
			ZoomToBounds(mapCoordBounds);
		else
			UpdateMapBounds();

        RequestMapUpdate();
    }

    public void ChangeZoom(float change)
    {
        SetZoom(zoom + change);
    }

    public void ChangeZoom(float change, float offsetX, float offsetY)
    {
        int previousLevel = currentLevel;

        UpdateCenter(GetCoordinatesFromUnits(offsetX, offsetY));
        UpdateZoom(zoom + change);
        UpdateCenter(GetCoordinatesFromUnits(-offsetX, -offsetY));

        UpdateAnchor();
        RequestMapUpdate();

        if (OnZoomChange != null)
            OnZoomChange(zoom);

        HandleLevelChange(previousLevel);
    }

    public void SetZoom(float newZoom)
    {
        int previousLevel = currentLevel;

        UpdateZoom(newZoom);

        UpdateAnchor();
        RequestMapUpdate();

        if (OnZoomChange != null)
            OnZoomChange(zoom);

        HandleLevelChange(previousLevel);
    }

	public void ZoomToBounds(AreaBounds bounds)
	{
		var max = GeoCalculator.LonLatToMeters(bounds.east, bounds.north);
		var min = GeoCalculator.LonLatToMeters(bounds.west, bounds.south);
		var center = GeoCalculator.MetersToLonLat((min.x + max.x) * 0.5, (min.y + max.y) * 0.5);
		var meters = max - min;

		UpdateCenter(center.Longitude, center.Latitude);

		CalculateZoomForExtent(WorldExtentMeters, out double minZoomX, out double minZoomY);
		CalculateZoomForExtent(meters, out double zoomX, out double zoomY);

		float minZoom = (float)Math.Max(minZoomX, minZoomY);
		minZoom = 0.1f * Mathf.Ceil(minZoom * 10);

		float zoom = (float)Math.Min(zoomX, zoomY);
		zoom = 0.1f * Mathf.Floor(zoom * 10);

		SetZoom(Math.Max(minZoom, zoom));
	}

	private void CalculateZoomForExtent(Distance meters, out double zoomX, out double zoomY)
	{
		var canvas = GameObject.FindWithTag("Canvas").GetComponent<Canvas>();
		var rect = ComponentManager.Instance.Get<MapViewArea>().Rect;

		zoomX = Math.Log(rect.width * canvas.scaleFactor * GeoCalculator.InitialResolution / (meters.x * mapScale)) * InvLogTwo;
		zoomY = Math.Log(rect.height * canvas.scaleFactor * GeoCalculator.InitialResolution / (meters.y * mapScale)) * InvLogTwo;
	}

	private void HandleLevelChange(int previousLevel)
    {
        if (previousLevel != currentLevel)
        {
			if (OnPreLevelChange != null)
				OnPreLevelChange(currentLevel);
			if (OnLevelChange != null)
                OnLevelChange(currentLevel);
            if (OnPostLevelChange != null)
                OnPostLevelChange(currentLevel);
        }
    }

    public void RequestMapUpdate()
    {
        // A map update is requested in the following cases:
        // - Location change (due to camera panning)
        // - Viewing bounds change (due to camera orbiting)
        // - Zoom change
        // - Change of map background
#if UNITY_EDITOR
        if (Application.isPlaying)
#endif
        {
            needsUpdate = true;
        }
    }

    public void SetCenter(double lon, double lat)
    {
        // Update lon/lat location
        UpdateCenter(lon, lat);

        UpdateAnchor();
        RequestMapUpdate();
    }

    public Coordinate GetCoordinatesFromUnits(float x, float y)
    {
        return GeoCalculator.MetersToLonLat(currentMeters.x + x * unitsToMeters, currentMeters.y + y * unitsToMeters);
    }

    public Distance GetMetersFromUnits(float x, float y)
    {
        return new Distance(currentMeters.x + x * unitsToMeters, currentMeters.y + y * unitsToMeters);
    }

    public Vector3 GetUnitsFromCoordinates(Coordinate coords)
    {
        var distance = GeoCalculator.LonLatToMeters(coords.Longitude, coords.Latitude) - currentMeters;
        return new Vector3((float)distance.x * metersToUnits, (float)distance.y * metersToUnits, 0);
    }

	public Vector3 GetUnitsFromMeters(Distance meters)
	{
		return new Vector3((float)(meters.x - currentMeters.x) * metersToUnits, 0, (float)(meters.y - currentMeters.y) * metersToUnits);
	}


	public void SetMinMaxLevels(int min, int max, bool update)
	{
		if (minLevel != min || maxLevel != max)
		{
			minLevel = min;
			maxLevel = max;

			if (update)
			{
				var previousLevel = currentLevel;
				currentLevel = Mathf.Clamp(dataLevels.GetDataLevelIndex(zoom), minLevel, maxLevel);
				HandleLevelChange(previousLevel);
			}
		}
	}


	//
	// Private Methods
	//

	private IEnumerator WaitForUI()
	{
		// Wait 2 frames for UI to be created
		yield return null;
		yield return null;

		ZoomToBounds(WorldBounds);
	}

	private void UpdateCenter(Coordinate coords)
    {
        UpdateCenter(coords.Longitude, coords.Latitude);
    }

    private void UpdateCenter(double lon, double lat)
    {
        // Wrap coordinate
        while (lon < GeoCalculator.MinLongitude)
        {
            lon += 2 * GeoCalculator.MaxLongitude;
        }
        while (lon > GeoCalculator.MaxLongitude)
        {
            lon += 2 * GeoCalculator.MinLongitude;
        }
        while (lat < GeoCalculator.MinLatitude)
        {
            lat += 2 * GeoCalculator.MaxLatitude;
        }
        while (lat > GeoCalculator.MaxLatitude)
        {
            lat += 2 * GeoCalculator.MinLatitude;
        }

        // Update lon/lat location
        longitude = lon;
        latitude = lat;
        currentMeters = GeoCalculator.LonLatToMeters(longitude, latitude);
    }

    private void UpdateZoom(float newZoom)
    {
        zoom = Mathf.Clamp(newZoom, minZoomLevel, maxZoomLevel);
        zoomLevel = Mathf.CeilToInt(zoom);
        currentLevel = Mathf.Clamp(dataLevels.GetDataLevelIndex(zoom), minLevel, maxLevel);

		tilesToUnits = Mathf.Pow(2, zoom - zoomLevel);
        unitsToTiles = 1f / tilesToUnits;
        pixelsToUnits = tilesToUnits * MapTile.InvSize;
        unitsToPixels = unitsToTiles * MapTile.Size;
        unitsToMeters = (float)(GeoCalculator.EarthCircumference * Math.Pow(2, -zoom));
        metersToUnits = 1f / unitsToMeters;
    }

    private void UpdateAnchor()
    {
        // Get the tile Id in the center of the screen (at lon/lat)
        anchor = GeoCalculator.AbsoluteCoordinateToTile(longitude, latitude, zoomLevel);

        // Get the upper left corner (in meters) of the anchor tile
        var anchorMeters = GeoCalculator.AbsoluteTileToMeters(anchor.X, anchor.Y, anchor.Z);

        // Calculate the offset (in units) between the requested lon/lat and the anchor
        anchorOffsetInUnits = GeoCalculator.RelativeMetersToPixels(anchorMeters - currentMeters, zoomLevel).ToVector2() * pixelsToUnits;

        // Also need to update the map bounds
        UpdateMapBounds();
    }

    private void UpdateMapBounds()
    {
        // Convert view bounds (in Unity units) to tile ids
        int northId = anchor.Y - Mathf.CeilToInt((viewBounds.yMax - anchorOffsetInUnits.y) * unitsToTiles + verticalBuffer);
        int southId = anchor.Y - Mathf.FloorToInt((viewBounds.yMin - anchorOffsetInUnits.y) * unitsToTiles - verticalBuffer);
        int westId = anchor.X + Mathf.FloorToInt((viewBounds.xMin - anchorOffsetInUnits.x) * unitsToTiles - horizontalBuffer);
        int eastId = anchor.X + Mathf.CeilToInt((viewBounds.xMax - anchorOffsetInUnits.x) * unitsToTiles + horizontalBuffer);
        mapTileBounds.Set(northId, southId, westId, eastId);

        // Update coordinate bounds (in degrees)
        var offsetMeters = GeoCalculator.RelativePixelsToMeters(viewBounds.xMax * unitsToPixels, viewBounds.yMax * unitsToPixels, zoomLevel);
        var LonLat = GeoCalculator.MetersToLonLat(currentMeters + offsetMeters);
        mapCoordBounds.east = LonLat.Longitude;
        mapCoordBounds.north = LonLat.Latitude;
        offsetMeters = GeoCalculator.RelativePixelsToMeters(viewBounds.xMin * unitsToPixels, viewBounds.yMin * unitsToPixels, zoomLevel);
        LonLat = GeoCalculator.MetersToLonLat(currentMeters + offsetMeters);
        mapCoordBounds.west = LonLat.Longitude;
        mapCoordBounds.south = LonLat.Latitude;

        if (OnBoundsChange != null)
            OnBoundsChange();
    }

    private void UpdateMap()
    {
        if (OnMapUpdate != null)
            OnMapUpdate();

        foreach (var layerController in layerControllers)
        {
            layerController.UpdateLayers();
        }
    }

    private void DebugDrawViewBounds()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(viewBounds.xMin, 0, viewBounds.yMax), new Vector3(viewBounds.xMax, 0, viewBounds.yMax));
        Gizmos.DrawLine(new Vector3(viewBounds.xMin, 0, viewBounds.yMax), new Vector3(viewBounds.xMin, 0, viewBounds.yMin));
        Gizmos.DrawLine(new Vector3(viewBounds.xMax, 0, viewBounds.yMin), new Vector3(viewBounds.xMax, 0, viewBounds.yMax));
        Gizmos.DrawLine(new Vector3(viewBounds.xMax, 0, viewBounds.yMin), new Vector3(viewBounds.xMin, 0, viewBounds.yMin));
    }

    private void DebugDrawTiles()
    {
        var currentMtx = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        float tileSize = tilesToUnits;
        Vector3 right = new Vector3(tileSize, 0);
        Vector3 bottom = new Vector3(0, -tileSize);
        Vector3 bottomRight = new Vector3(tileSize, -tileSize);

        int horizontalCount = mapTileBounds.HorizontalCount;
        int verticalCount = mapTileBounds.VerticalCount;
        Vector3 p = new Vector3();

        // Draw the view-able tiles
        Gizmos.color = Color.green;
        int north = anchor.Y - mapTileBounds.North;
        int west = mapTileBounds.West - anchor.X;
        p.y = north * tileSize + anchorOffsetInUnits.y;
        for (int y = 0; y < verticalCount; y++)
        {
            p.x = west * tileSize + anchorOffsetInUnits.x;
            for (int x = 0; x < horizontalCount; x++)
            {
                Vector3 pos1 = p + right;
                Vector3 pos2 = p + bottom;
                Vector3 pos3 = p + bottomRight;
                Gizmos.DrawLine(p, pos1);
                Gizmos.DrawLine(p, pos2);
                Gizmos.DrawLine(pos3, pos1);
                Gizmos.DrawLine(pos3, pos2);
                p.x += tileSize;
            }
            p.y -= tileSize;
        }

        // Draw the world bounds
        if (showWorldBounds)
        {
            int count = (1 << zoomLevel);
            float size = count * tileSize;
            right = new Vector3(size, 0);
            bottom = new Vector3(0, -size);
            bottomRight = new Vector3(size, -size);

            Gizmos.color = Color.magenta;
            p.y = anchor.Y * tileSize + anchorOffsetInUnits.y;
            p.x = -anchor.X * tileSize + anchorOffsetInUnits.x;
            Vector3 pos1 = p + right;
            Vector3 pos2 = p + bottom;
            Vector3 pos3 = p + bottomRight;
            Gizmos.DrawLine(p, pos1);
            Gizmos.DrawLine(p, pos2);
            Gizmos.DrawLine(pos3, pos1);
            Gizmos.DrawLine(pos3, pos2);
        }

        Gizmos.matrix = currentMtx;
    }

}
