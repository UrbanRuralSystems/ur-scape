// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

#if UNITY_WEBGL
using System.Runtime.InteropServices;
using UnityEngine;

public class Web
{
    private static class WebHelper
    {
        [DllImport("__Internal")]
        private static extern string GetWebUrl();

        public static readonly string url = GetWebUrl();
    }

#if UNITY_EDITOR
    public static void Log(string msg) { Debug.Log(msg); }
    public static void LogWarning(string msg) { Debug.LogWarning(msg); }
    public static void LogError(string msg) { Debug.LogError(msg); }
#else
    [DllImport("__Internal")]
    public static extern void Log(string msg);
    [DllImport("__Internal")]
    public static extern void LogWarning(string msg);
    [DllImport("__Internal")]
    public static extern void LogError(string msg);
#endif

    public static string GetUrl(string path)
    {
#if UNITY_EDITOR
        return path;
#else
        return (WebHelper.url + path).Replace('\\', '/');
#endif
    }
}

#endif