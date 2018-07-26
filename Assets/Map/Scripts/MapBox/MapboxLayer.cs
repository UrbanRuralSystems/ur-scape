// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

public class MapboxLayer : TileMapLayer
{
    private MapboxRequestGenerator requestGenerator;


    //
    // Inheritance Methods
    //


    public void Init(MapController map, int layerId, TextureCache cache, MapboxRequestGenerator requestGenerator)
    {
        base.Init(map, layerId, cache);
        this.requestGenerator = requestGenerator;
    }

    protected override TileRequest CreateTileRequest(long id, RequestCallback callback)
    {
        return requestGenerator.CreateTileRequest(id, callback);
    }

}
