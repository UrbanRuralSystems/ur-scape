// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;

public class MetadataPair : IEquatable<MetadataPair>
{
	public string Key;
	public string Value;

	public override bool Equals(object obj)
	{
		return Equals(obj as MetadataPair);
	}

	public bool Equals(MetadataPair other)
	{
		return other != null && Key == other.Key && Value == other.Value;
	}

	public override int GetHashCode()
	{
		var hashCode = 206514262;
		hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Key);
		hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Value);
		return hashCode;
	}

	public static bool operator ==(MetadataPair pair1, MetadataPair pair2)
	{
		return EqualityComparer<MetadataPair>.Default.Equals(pair1, pair2);
	}

	public static bool operator !=(MetadataPair pair1, MetadataPair pair2)
	{
		return !(pair1 == pair2);
	}
}

public static class MetadataPairExtensions
{
	public static void Add(this List<MetadataPair> list, string key, string value)
	{
		list.Add(new MetadataPair { Key = key, Value = value });
	}

	public static void Remove(this List<MetadataPair> list, string key)
	{
		list.RemoveAll((pair) => pair.Key == key);
	}

	public static bool TryGetValue(this List<MetadataPair> list, string key, out string value)
	{
		foreach (var pair in list)
		{
			if (pair.Key == key)
			{
				value = pair.Value;
				return true;
			}
		}
		value = null;
		return false;
	}

	public static string Get(this List<MetadataPair> list, string key)
	{
		foreach (var pair in list)
		{
			if (pair.Key == key)
				return pair.Value;
		}
		return null;
	}
}
