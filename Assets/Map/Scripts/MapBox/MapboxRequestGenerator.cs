// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: generates Mapbox tile requests according to their API
//
// Source: https://www.mapbox.com/api-documentation
//
// Style URL
//   mapbox://styles/{user}/{style}
//   mapbox://styles/mapbox/traffic-night-v2
//
// Retrieve raster tiles from styles
//   GET  /styles/v1/{username}/{style_id}/tiles/[tileSize/]{z}/{x}/{y}[@2x]
//
// Request a 512x512 pixel tile:
//   https://api.mapbox.com/styles/v1/mapbox/traffic-night-v2/tiles/0/0/0?access_token=token
//
// Request a 256x256 pixel tile:
//   https://api.mapbox.com/styles/v1/mapbox/traffic-night-v2/tiles/256/0/0/0?access_token=token
//

using System;
using System.IO;
using UnityEngine;

public class MapboxRequestGenerator
{
    private const string BaseURL = "https://api.mapbox.com/styles/v1/";

    private readonly string tokenSufix;
    private readonly string urlPrefix;
    private readonly string cachePath;

    public MapboxRequestGenerator(string token, string styleUrl, string cachePath)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("MapboxRequestGenerator: empty token!");
        }

        tokenSufix = "?access_token=" + token;
		urlPrefix = CreateUrlPrefix(styleUrl) + "/";
        this.cachePath = cachePath;
    }

    private static MapTileId tId = new MapTileId();
    public TileRequest CreateTileRequest(long id, RequestCallback callback)
    {
        tId.Set(id);
        string url = urlPrefix + tId.ToURL() + tokenSufix;
        string file = cachePath + tId.Z + Path.DirectorySeparatorChar + tId.X + "_" + tId.Y + ".tile";

        return new TileRequest(id, file, url, callback);
    }

    private static string CreateUrlPrefix(string url)
    {
        if (!url.StartsWith("mapbox://", StringComparison.Ordinal))
            return url;

        string[] split = url.Split('/');
        var user = split[3];
        var style = split[4];

        return BaseURL + user + "/" + style + "/tiles";
    }
}
