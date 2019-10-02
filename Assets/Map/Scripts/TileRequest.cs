// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public class TileRequest : ResourceRequest
{
    public readonly long id;
    public readonly string url;
    public Texture texture;

    public TileRequest(long id, string cachedFile, string url, RequestCallback callback) : base(cachedFile, callback)
    {
        this.id = id;
        this.url = url;
    }

    public void SetData(byte[] data)
    {
#if UNITY_WEBGL
        var t2D = new Texture2D(MapTile.Size, MapTile.Size, TextureFormat.DXT1, false);
#elif UNITY_ANDROID
        var t2D = new Texture2D(MapTile.Size, MapTile.Size, TextureFormat.ETC2_RGB, false);
#elif UNITY_IOS
        var t2D = new Texture2D(MapTile.Size, MapTile.Size, TextureFormat.PVRTC_RGB4, false);
#else
        var t2D = new Texture2D(MapTile.Size, MapTile.Size, TextureFormat.RGB24, false);
#endif
        t2D.LoadImage(data);

        texture = t2D;
    }
   
}
