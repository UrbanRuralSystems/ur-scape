// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using ExtensionMethods;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public static class FileRequest
{
    public delegate void FileRequestCallback(Stream stream);
    public delegate void TextFileRequestCallback(StreamReader reader);
	public delegate void BinaryFileRequestCallback(BinaryReader reader);
	public delegate IEnumerator BinaryFileRequestCallbackCO(BinaryReader reader);

    private delegate void OnRequestCompleteDelegate(byte[] bytes);

#if UNITY_WEBGL && UNITY_EDITOR
    private static readonly WaitForSeconds WebGLDelay = new WaitForSeconds(0.5f);
#endif


    //
    // Public Methods
    //

    public static IEnumerator GetStream(string filename, FileRequestCallback callback, UnityAction errCallback = null)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        yield return GetFromURL(Web.GetUrl(filename), null, callback, errCallback);
#else
#if UNITY_WEBGL
        yield return WebGLDelay;
#endif
        GetFromFile(filename, callback, errCallback);
        yield break;
#endif
    }

    public static IEnumerator GetText(string filename, TextFileRequestCallback callback, UnityAction errCallback = null)
    {
		Stream stream = null;
        yield return GetStream(filename, (s) => stream = s, errCallback);

		if (stream != null)
		{
			using (var sr = new StreamReader(stream))
			{
				callback(sr);
			}
		}
	}

	public static IEnumerator GetBinary(string filename, BinaryFileRequestCallback callback, UnityAction errCallback = null)
	{
		yield return GetStream(filename, (s) => { using (var br = new BinaryReader(s)) { callback(br); } }, errCallback);
	}

	public static IEnumerator GetBinary(string filename, BinaryFileRequestCallbackCO callback, UnityAction errCallback = null)
    {
		Stream stream = null;
		yield return GetStream(filename, (s) => stream = s, errCallback);
		if (stream != null)
		{
			using (var br = new BinaryReader(stream))
			{
				yield return callback(br);
			}
		}
	}


	//
	// Private Methods
	//

	private static void GetFromFile(string filename, FileRequestCallback callback, UnityAction errCallback = null)
    {
        if (!File.Exists(filename))
        {
            Debug.LogError(filename + " does not exist!");
            if (errCallback != null)
                errCallback();
            return;
        }

        callback(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
    }

    private static IEnumerator GetFromURL(string url, string saveAs, FileRequestCallback callback, UnityAction errCallback = null)
    {
        byte[] data = null;
        yield return GetFromURL(url, d => data = d);

        if (data == null)
        {
            Debug.LogError("Could not download " + url);
            if (errCallback != null)
                errCallback();
            yield break;
        }

        if (!string.IsNullOrEmpty(saveAs))
        {
            File.WriteAllBytes(saveAs, data);
        }

        callback(new MemoryStream(data));
    }

    private static IEnumerator GetFromURL(string url, OnRequestCompleteDelegate callback)
    {
        url = url.Replace(" ", "%20");
        WWW www = new WWW(url);

        do
        {
            yield return IEnumeratorExtensions.AvoidRunThru;
        } while (!www.isDone);

        if (!string.IsNullOrEmpty(www.error))
        {
            Debug.LogError(www.error);
            yield break;
        }
        else
        {
            if (www.text.Contains("404 Not Found"))
            {
                Debug.LogError(www.text);
            }
            else
            {
                callback(www.bytes);
            }
        }
    }
	
}
