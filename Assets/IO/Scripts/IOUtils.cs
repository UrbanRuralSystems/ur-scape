// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class IOUtils
{
	public static void SafeMove(string from, string to)
	{
		try
		{
			File.Move(from, to);
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}

	public static void SafeDelete(IEnumerable<string> files)
	{
		foreach (var file in files)
		{
			try
			{
				File.Delete(file);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}
	}

	public static bool IsDirectoryEmpty(string path)
	{
		IEnumerable<string> items = Directory.EnumerateFileSystemEntries(path);
		using (IEnumerator<string> en = items.GetEnumerator())
		{
			return !en.MoveNext();
		}
	}

	public static void DeleteDirectoryIfEmpty(string path)
	{
		try
		{
			if (Directory.Exists(path) && IsDirectoryEmpty(path))
			{
				Directory.Delete(path);
			}
		}
		catch (Exception e)
		{
			Debug.LogException(e);
		}
	}
}
