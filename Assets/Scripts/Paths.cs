// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.IO;

public static class Paths
{
	public static readonly string Data;
	public static readonly string Cache;
	public static readonly string Sites;
	public static readonly string Backgrounds;

#if UNITY_WEBGL
	public static readonly string DataWebDB;
	public static readonly string DataWebDBHeaders;
	public static readonly string DataWebDBPatches;
#endif

	static Paths()
	{
#if UNITY_ANDROID
		Data = "/sdcard/urscape" + Path.DirectorySeparatorChar;
#elif UNITY_IOS && !UNITY_EDITOR
		Data = UnityEngine.Application.streamingAssetsPath + Path.DirectorySeparatorChar;
#else
		Data = "Data" + Path.DirectorySeparatorChar;
#endif

#if UNITY_IOS
		Cache = UnityEngine.Application.persistentDataPath + Path.DirectorySeparatorChar;
#else
		Cache = Data;
#endif

		Sites = Data + "Sites" + Path.DirectorySeparatorChar;
		Backgrounds = Cache + "Backgrounds" + Path.DirectorySeparatorChar;

#if UNITY_WEBGL
	    DataWebDB = Data + "WebDB" + Path.DirectorySeparatorChar;
	    DataWebDBHeaders = DataWebDB + "headers";
	    DataWebDBPatches = DataWebDB + "patches";
#endif

	}
}
