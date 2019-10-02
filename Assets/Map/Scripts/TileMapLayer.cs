// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: class representing a tile layer

#if UNITY_EDITOR
#define SAFETY_CHECK
#endif

using System.Collections.Generic;
using UnityEngine;

public abstract class TileMapLayer : MapLayer
{
    private const int InitialTileCount = 220;
    private const int MaxZoomLevelDifference = 2;

    protected TextureCache textureCache;
    public TileRequestHandler requestHandler;

    protected readonly Dictionary<long, MapTile> tilesMap = new Dictionary<long, MapTile>();
    protected readonly List<MapTile> tilesList = new List<MapTile>();
    protected readonly Dictionary<long, MapTile> oldTilesMap = new Dictionary<long, MapTile>();
    protected readonly List<MapTile> oldTilesList = new List<MapTile>();

    protected Stack<MapTile> unusedTiles = new Stack<MapTile>();
    protected HashSet<long> requestedTiles = new HashSet<long>();

    public int TotalTiles { get { return tilesMap.Count + unusedTiles.Count; } }
    public int UsedTiles { get { return tilesMap.Count; } }

    private int layerId;


    //
    // Inheritance Methods
    //

    protected void Init(MapController map, int layerId, TextureCache textureCache)
    {
        base.Init(map);

        this.layerId = layerId;
        this.textureCache = textureCache;
        this.requestHandler = map.tileRequestHandler;

        if (TotalTiles == 0)
        {
            CreateTileCache();
        }
        else
        {
            CancelAllRequests();

            // Recover old tiles that match the new layerId
            int lastTileIndex = tilesList.Count - 1;
            int lastOldTileIndex = oldTilesList.Count - 1;
            for (int i = lastOldTileIndex; i >= 0; i--)
            {
                if (oldTilesList[i].Id.Layer == layerId)
                {
                    var tile = oldTilesList[i];
                    var tid = tile.Id.ToId();

                    tilesMap.Add(tid, tile);
                    tilesList.Add(tile);

                    oldTilesMap.Remove(tid);
                    oldTilesList[i] = oldTilesList[lastOldTileIndex];
                    oldTilesList.RemoveAt(lastOldTileIndex--);
                }
            }

            // Deprecate tiles that don't match the new layerId
            int last = tilesList.Count - 1;
            for (int i = lastTileIndex; i >= 0; i--)
            {
                if (tilesList[i].Id.Layer != layerId)
                {
                    var tile = tilesList[i];
                    var tid = tile.Id.ToId();

                    oldTilesMap.Add(tid, tile);
                    oldTilesList.Add(tile);

                    tilesMap.Remove(tid);
                    tilesList[i] = tilesList[last];
                    tilesList.RemoveAt(last--);
                }
            }
        }
    }

    public override void UpdateContent()
    {
        var bounds = map.MapTileBounds;

        // Request layer tiles
        RequestTiles(bounds.West, bounds.East, bounds.North, bounds.South, map.ZoomLevel);

        // Update current tiles
        UpdateTiles();

        // Clean up tiles
        CleanUpTiles();
    }


    //
    // Private Methods
    //

    private void CreateTileCache()
    {
        for (int i = 0; i < InitialTileCount; i++)
        {
            var tile = MapTile.Create(null, transform);
            tile.name = "unused";
            unusedTiles.Push(tile);
        }
    }

    protected MapTile AddTile(long id)
    {
        var tileId = new MapTileId(id);

        MapTile tile;
        if (unusedTiles.Count > 0)
        {
            // Grab tile from unused list
            tile = unusedTiles.Pop();

            tile.Init(tileId);
        }
        else
        {
            // Debug.LogWarning("Creating new tile. Total tiles: " + (tiles.Count + 1));

            // Create new tile
            tile = MapTile.Create(tileId, transform);
        }

        tile.name = tileId.ToString();

        // Add it to the list
        tilesMap.Add(id, tile);
        tilesList.Add(tile);

        return tile;
    }

    private void RemoveTile(Dictionary<long, MapTile> map, List<MapTile> list, int index)
    {
        MapTile tile = list[index];
        long id = tile.Id.ToId();

        // Remove tile from list
        map.Remove(id);
        int last = list.Count - 1;
        list[index] = list[last];
        list.RemoveAt(last);

        // Update the texture cache
        textureCache.Add(id, tile.GetTexture());

        tile.Hide();

        // Put it back to the unused tiles stack
        unusedTiles.Push(tile);
        tile.name = "unused";
    }

    protected void UpdateTiles()
    {
        MapTileId anchorId = map.Anchor;
        float anchorScale = map.TilesToUnits;
        Vector2 anchorOffset = map.AnchorOffsetInUnits;

        for (int i = tilesList.Count - 1; i >= 0; i--)
        {
            MapTile tile = tilesList[i];
            float tileScale = Mathf.Pow(2, map.zoom - tile.ZoomLevel);
            tile.UpdateTile(anchorId, anchorScale, anchorOffset, tileScale);
        }
    }

    protected void RequestTiles(int west, int east, int north, int south, int zoomLevel)
    {
		RequestTilesSpiralOut(west, east, north, south, zoomLevel);
	}

    protected void CleanUpTiles()
    {
        int mapZoomLevel = map.ZoomLevel;
        MapTileBounds mapBounds = map.MapTileBounds;

        CleanUpTiles(mapZoomLevel, mapBounds);
        CleanUpOldTiles(mapZoomLevel, mapBounds);

        // Remove any requests that are not for this zoom level and bounds
        CancelRequests(mapZoomLevel, mapBounds);
    }

    private void CleanUpTiles(int mapZoomLevel, MapTileBounds mapBounds)
    {
        // Remove tiles that are 3 levels higher than current zoom level
        int mapZoomLevelThreshold = mapZoomLevel - MaxZoomLevelDifference;

        for (int i = tilesList.Count - 1; i >= 0; i--)
        {
            var tileId = tilesList[i].Id;

            // Calculate the tile bounds in the map's zoom level
            float toMapZoom = Mathf.Pow(2, mapZoomLevel - tileId.Z);
            int tileWest = (int)(tileId.X * toMapZoom);
            int tileEast = (int)((tileId.X + 1) * toMapZoom);
            int tileNorth = (int)(tileId.Y * toMapZoom);
            int tileSouth = (int)((tileId.Y + 1) * toMapZoom);

            bool inFrustum = tileEast > mapBounds.West && tileWest < mapBounds.East &&
                             tileSouth > mapBounds.North && tileNorth < mapBounds.South;

            if (!inFrustum || tileId.Z < mapZoomLevelThreshold)
            {
                RemoveTile(tilesMap, tilesList, i);
            }
            else if (tileId.Z != mapZoomLevel)
            {
                if ((tileId.Z > mapZoomLevel && IsCoveredByLargerTile(mapZoomLevel, tileId.X, tileId.Y, tileId.Z)) ||
                    (tileId.Z < mapZoomLevel && IsCoveredBySmallerTiles(mapZoomLevel, tileId.X, tileId.Y, tileId.Z)))
                {
                    RemoveTile(tilesMap, tilesList, i);
                }
            }
        }
    }

    private void CleanUpOldTiles(int mapZoomLevel, MapTileBounds mapBounds)
    {
        // Remove tiles that are 3 levels higher than current zoom level
        int mapZoomLevelThreshold = mapZoomLevel - MaxZoomLevelDifference;

        MapTileId anchorId = map.Anchor;
        float anchorScale = map.TilesToUnits;
        Vector2 anchorOffset = map.AnchorOffsetInUnits;

        for (int i = oldTilesList.Count - 1; i >= 0; i--)
        {
            var tileId = oldTilesList[i].Id;
            if (tilesMap.ContainsKey(MapTileId.ToId(tileId.X, tileId.Y, tileId.Z, layerId)))
            {
                RemoveTile(oldTilesMap, oldTilesList, i);
            }
            else
            {
                // Calculate the tile bounds in the map's zoom level
                float toMapZoom = Mathf.Pow(2, mapZoomLevel - tileId.Z);
                int tileWest = (int)(tileId.X * toMapZoom);
                int tileEast = (int)((tileId.X + 1) * toMapZoom);
                int tileNorth = (int)(tileId.Y * toMapZoom);
                int tileSouth = (int)((tileId.Y + 1) * toMapZoom);

                bool inFrustum = tileEast > mapBounds.West && tileWest < mapBounds.East &&
                                 tileSouth > mapBounds.North && tileNorth < mapBounds.South;

                if (!inFrustum || tileId.Z < mapZoomLevelThreshold)
                {
                    RemoveTile(oldTilesMap, oldTilesList, i);
                }
                else if (tileId.Z != mapZoomLevel)
                {
                    if ((tileId.Z > mapZoomLevel && IsCoveredByLargerTile(mapZoomLevel, tileId.X, tileId.Y, tileId.Z)) ||
                        (tileId.Z < mapZoomLevel && IsCoveredBySmallerTiles(mapZoomLevel, tileId.X, tileId.Y, tileId.Z)))
                    {
                        RemoveTile(oldTilesMap, oldTilesList, i);
                    }
                    else
                    {
                        MapTile tile = oldTilesList[i];
                        tile.UpdateTile(anchorId, anchorScale, anchorOffset, Mathf.Pow(2, map.zoom - tile.ZoomLevel), 0.0001f);
                    }
                }
                else
                {
                    MapTile tile = oldTilesList[i];
                    tile.UpdateTile(anchorId, anchorScale, anchorOffset, Mathf.Pow(2, map.zoom - tile.ZoomLevel), 0.0001f);
                }
            }
        }
    }
    
    // Check if a tile is covered by other tiles with a smaller zoom 
    private bool IsCoveredByLargerTile(int zoomLevel, int tileX, int tileY, int tileZ)
    {
        tileZ--;
        tileX /= 2;
        tileY /= 2;

        return tilesMap.ContainsKey(MapTileId.ToId(tileX, tileY, zoomLevel, layerId)) || (zoomLevel < tileZ && IsCoveredByLargerTile(zoomLevel, tileX, tileY, tileZ));
    }

    // Check if a tile is covered by other tiles with a larger zoom
    private bool IsCoveredBySmallerTiles(int zoomLevel, int tileX, int tileY, int tileZ)
    {
        tileZ++;
        tileX *= 2;
        tileY *= 2;

        return IsCoveredBySmallerTile(zoomLevel, tileX, tileY, tileZ)
            && IsCoveredBySmallerTile(zoomLevel, tileX + 1, tileY, tileZ)
            && IsCoveredBySmallerTile(zoomLevel, tileX, tileY + 1, tileZ)
            && IsCoveredBySmallerTile(zoomLevel, tileX + 1, tileY + 1, tileZ);
    }
    private bool IsCoveredBySmallerTile(int zoomLevel, int tileX, int tileY, int tileZ)
    {
        var id = MapTileId.ToId(tileX, tileY, tileZ, layerId);
        return tilesMap.ContainsKey(id) ||
                (zoomLevel == tileZ && !requestedTiles.Contains(id)) ||
                (zoomLevel > tileZ && IsCoveredBySmallerTiles(zoomLevel, tileX, tileY, tileZ));
    }

    private void CancelRequests(int mapZoom, MapTileBounds bounds)
    {
        var node = requestHandler.PendingRequests.First;
        var tId = new MapTileId();
        while (node != null)
        {
            var next = node.Next;
            var request = node.Value;
            tId.Set(request.id);
            if (tId.Z != mapZoom || tId.X < bounds.West || tId.X >= bounds.East || tId.Y < bounds.North || tId.Y >= bounds.South)
            {
                request.Cancel();
                requestHandler.RemovePending(node);
                requestedTiles.Remove(request.id);
            }
            node = next;
        }
    }

    private void CancelAllRequests()
    {
        requestHandler.CancelAll();
        requestedTiles.Clear();
    }

    private void RequestTilesBasic(int west, int east, int north, int south, int zoomLevel)
    {
        for (int y = north; y < south; ++y)
        {
            for (int x = west; x < east; ++x)
            {
                RequestTile(x, y, zoomLevel, layerId);
            }
        }
    }

    private void RequestTilesSpiralIn(int west, int east, int north, int south, int zoomLevel)
    {
        // Request tiles in a spiral (from outside to inside)

        int x = west;
        int y = north;
        while (true)
        {
            if (x >= east) break;
            do
            {
                RequestTile(x, y, zoomLevel, layerId);
            } while (++x < east);
            x--;

            north++;
            y++;
            if (y >= south) break;
            do
            {
                RequestTile(x, y, zoomLevel, layerId);
            } while (++y < south);
            y--;

            east--;
            x--;
            if (x < west) break;
            do
            {
                RequestTile(x, y, zoomLevel, layerId);
            } while (--x >= west);
            x++;

            south--;
            y--;
            if (y < north) break;
            do
            {
                RequestTile(x, y, zoomLevel, layerId);
            } while (--y >= north);
            y++;

            west++;
            x++;
        }
    }

    private void RequestTilesSpiralOut(int west, int east, int north, int south, int zoomLevel)
    {
        // Request tiles in a spiral (from inside to outside)

        int w = east - west;
        int h = south - north;
        int c, x, y, count;
        if (h > w)
        {
            count = (1 + w) / 2;
            int wm2 = w % 2;
            c = wm2 * 2;
            x = (west + east - 1) / 2;
            y = (wm2 == 0) ? north + count : south - count;
            int hw = w / 2;
            west += (w - 1) / 2;
            north += count;
            east -= hw;
            south -= hw;
        }
        else
        {
            count = (1 + h) / 2;
            int hm2 = h % 2;
            c = hm2 * 2 + 1;
            y = (south + north) / 2;
            x = (hm2 == 0) ? west + count - 1 : east - count;
            int hh = h / 2;
            west += count - 1;
            north += hh;
            east -= hh;
            south -= count - 1;
        }
        for (int i = 0; i < count; i++)
        {
            switch (c)
            {
                case 0: // move down
                    do
                    {
                        RequestTile(x, y, zoomLevel, layerId);
                    } while (++y < south);
                    south++;
                    goto case 1;
                case 1: // move right
                    do
                    {
                        RequestTile(x, y, zoomLevel, layerId);
                    } while (++x < east);
                    east++;
                    goto case 2;
                case 2: // move up
                    do
                    {
                        RequestTile(x, y, zoomLevel, layerId);
                    } while (--y >= north);
                    north--;
                    goto case 3;
                case 3: // move left
                    do
                    {
                        RequestTile(x, y, zoomLevel, layerId);
                    } while (--x >= west);
                    west--;
                    break;
            }
            c = 0;
        }
    }

    protected void RequestTile(int x, int y, int zoomLevel, int layerId)
    {
        long tileId = MapTileId.CreateWrappedId(x, y, zoomLevel, layerId);

        // Do not request tiles that already exist (or are being requested)
        if (!tilesMap.ContainsKey(tileId) && !requestedTiles.Contains(tileId))
        {
            requestedTiles.Add(tileId);

            // Request the texture for it
            requestHandler.Add(CreateTileRequest(tileId, RequestFinished));
        }
    }

    protected abstract TileRequest CreateTileRequest(long id, RequestCallback callback);

    private void RequestFinished(ResourceRequest r)
    {
        TileRequest request = r as TileRequest;
        var tileId = request.id;

        if (request.State != RequestState.Canceled)
        {
            requestedTiles.Remove(tileId);

            if (request.State == RequestState.Succeeded)
#if SAFETY_CHECK
            {
                if (tilesMap.ContainsKey(tileId))
                {
                    Debug.LogError("Tile '" + tileId + "' already exists!");
                }
                else
#endif
                {
                    MapTile tile = AddTile(tileId);

                    float tileScale = Mathf.Pow(2, map.zoom - tile.ZoomLevel);
                    tile.SetTexture(request.texture);
                    tile.UpdateTile(map.Anchor, map.TilesToUnits, map.AnchorOffsetInUnits, tileScale);
                    tile.Show();
                }
#if SAFETY_CHECK
            }
            else 
            {
				Debug.LogError("Requesting tile '" + tileId + "' finished with state: " + request.State);
				if (!string.IsNullOrEmpty(request.Error))
                {
                    Debug.LogError("Requesting error: " + request.Error);
                }
            }
#endif
        }

        if (requestedTiles.Count == 0)
        {
            CleanUpTiles();
        }
    }

}