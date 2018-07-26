// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

//#define USE_UnityWebRequest

using System.Collections;
using System.IO;
using UnityEngine;

public class TileProvider : ResourceProvider<TileRequest>
{
    private MonoBehaviour behaviour;
    private TextureCache cache;

    public TileProvider(MonoBehaviour behaviour, TextureCache cache)
    {
        this.behaviour = behaviour;
        this.cache = cache;
    }

    public void Run(TileRequest request, ProviderCallback<TileRequest> callback)
    {
        // Try finding the texture in the cache
        Texture texture;
        if (cache.TryRemove(request.id, out texture))
        {
            request.texture = texture;
            request.State = RequestState.Succeeded;
            callback(request);
            return;
        }

#if UNITY_STANDALONE || UNITY_IOS
        // If not, try loading it on disk
        if (File.Exists(request.file))
        {
            ReadFromDisk(request);
            callback(request);
            return;
        }
#endif

        // Otherwise, get it from the url
        behaviour.StartCoroutine(GetFromURL(request, callback));
    }

    private void ReadFromDisk(TileRequest request)
    {
        var data = File.ReadAllBytes(request.file);

        if (!request.IsCanceled)
        {
            request.SetData(data);
            request.State = RequestState.Succeeded;
        }
    }

    private void AddToDisk(TileRequest request, byte[] data)
    {
#if UNITY_STANDALONE || UNITY_IOS
        if (!string.IsNullOrEmpty(request.file))
        {
			var dirName = Path.GetDirectoryName(request.file);
#if UNITY_STANDALONE_OSX
			dirName = dirName.Replace('\\', Path.DirectorySeparatorChar);
#endif
			Directory.CreateDirectory(dirName);
            File.WriteAllBytes(request.file, data);
        }
#endif
    }

#if USE_UnityWebRequest
    private IEnumerator GetFromURL(TileRequest request, ProviderCallback<TileRequest> callback)
    {
        var www = UnityEngine.Networking.UnityWebRequest.Get(request.url);
        yield return www.Send();

        if (www.isError)
        {
            request.Error = www.error;
            request.State = RequestState.Failed;
        }
        else
        {
            if (www.responseCode == 200)    // 200 = HttpStatusCode.OK
            {
                request.SetData(www.downloadHandler.data);
                request.State = RequestState.Succeeded;

                // Add it to cache and disk
                AddToDisk(request, www.downloadHandler.data);
            }
            else
            {
                request.Error = "Response code: " + www.responseCode;
                request.State = RequestState.Failed;
            }
        }
        callback(request);
    }
#else
    private IEnumerator GetFromURL(TileRequest request, ProviderCallback<TileRequest> callback)
    {
        WWW www = new WWW(request.url);
        do
        {
            yield return null;
            if (request.IsCanceled)
            {
                www.Dispose();
                callback(request);
                yield break;
            }
        } while (!www.isDone);
        

        if (!string.IsNullOrEmpty(www.error))
        {
            request.Error = www.error;
            request.State = RequestState.Failed;
        }
        else
        {
            if (!www.text.Contains("404 Not Found"))
            {
                request.SetData(www.bytes);
                request.State = RequestState.Succeeded;

                // Add it to cache and disk
                AddToDisk(request, www.bytes);
            }
            else
            {
                request.Error = www.text;
                request.State = RequestState.Failed;
            }
        }
        callback(request);
    }
#endif
}
