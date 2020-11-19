// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public static class StringExtensions
{
	public static bool EqualsIgnoreCase(this string a, string b)
	{
		return a.Equals(b, StringComparison.CurrentCultureIgnoreCase);
	}

	public static bool StartsWithIgnoreCase(this string filename, string start)
	{
		if (filename.StartsWith(start, StringComparison.CurrentCultureIgnoreCase))
		{
			if (!filename.StartsWith(start))
			{
				Debug.LogWarning(filename + " has a different case than layer name: " + start);
			}
			return true;
		}
		return false;
	}

	public static bool IsIn(this string a, string[] array)
	{
		foreach (string b in array)
		{
			if (a.Equals(b, StringComparison.CurrentCultureIgnoreCase))
				return true;
		}
		return false;
	}

	public static string SplitCamelCase(this string str)
	{
		return Regex.Replace(str, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
		//return Regex.Replace(str, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled).Trim();
	}

	public static string FirstLetterToUpper(this string str)
	{
		if (str.Length > 1)
			return char.ToUpper(str[0]) + str.Substring(1);

		return str.ToUpper();
	}
}

public static class ColorExtensions
{
    public static Color FromRGB(int r, int g, int b)
    {
        return new Color(
            Mathf.Clamp(r, 0, 255) / 255f,
            Mathf.Clamp(g, 0, 255) / 255f,
            Mathf.Clamp(b, 0, 255) / 255f);
    }

    public static Color FromRGB(int r, int g, int b, int a)
    {
        return new Color(
            Mathf.Clamp(r, 0, 255) / 255f,
            Mathf.Clamp(g, 0, 255) / 255f,
            Mathf.Clamp(b, 0, 255) / 255f,
            Mathf.Clamp(a, 0, 255) / 255f);
    }

	public static Color Parse(string value)
	{
		value = value.TrimStart('#');

		if (value.Length != 6 && value.Length != 8)
			return Color.clear;

		var style = System.Globalization.NumberStyles.HexNumber;
		var provider = System.Globalization.CultureInfo.InvariantCulture;

		if (!int.TryParse(value.Substring(0, 2), style, provider, out int r))
			return Color.clear;
		if (!int.TryParse(value.Substring(2, 2), style, provider, out int g))
			return Color.clear;
		if (!int.TryParse(value.Substring(4, 2), style, provider, out int b))
			return Color.clear;

		int a = 255;
		if (value.Length == 8 && !int.TryParse(value.Substring(6, 2), style, provider, out a))
			return Color.clear;

		return new Color(r / 255f, g / 255f, b / 255f, a / 255f);
	}

	public static float GetLuma(Color color)
	{
		return (0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b);
	}
}

public class IteratorSignal { }

public static class IEnumeratorExtensions
{
    public static readonly IteratorSignal AvoidRunThru;

    public static bool RunThru(this IEnumerator e)
    {
        return Continue(e);
    }

    public static bool RunThruChildren(this IEnumerator e)
    {
		if (e.Current is IEnumerator current)
		{
			return Iterate(current);
		}
		return false;
    }

    private static bool Continue(IEnumerator e)
    {
		if (e.Current is IEnumerator current && Continue(current))
		{
			return true;
		}

		return Iterate(e);
    }

    private static bool Iterate(IEnumerator e)
    {
        while (e.MoveNext())
        {
            if (e.Current == AvoidRunThru)
                return true;

			if (e.Current is IEnumerator current && Iterate(current))
			{
				return true;
			}
		}
        return false;
    }
}

public static class BinaryWriterEntensions
{
	public static void Write(this BinaryWriter bw, float? nf)
	{
		bw.Write(nf.HasValue);
		if (nf.HasValue)
			bw.Write(nf.Value);
	}
}

public static class BinaryReaderEntensions
{
	public static float? ReadNullableSingle(this BinaryReader br)
	{
		if (br.ReadBoolean())
			return br.ReadSingle();
		return null;
	}
}

public static class ListExtensions
{
	public static int MoveForward<T>(this IList<T> list, T element)
	{
		int index = list.IndexOf(element);
		if (index > 0)
		{
			Swap(list, index, --index);
		}
		return index;
	}

	public static int MoveBack<T>(this IList<T> list, T element)
	{
		int index = list.IndexOf(element);
		if (index < list.Count - 1)
		{
			Swap(list, index, ++index);
		}
		return index;
	}

	public static void Swap<T>(this IList<T> list, int indexA, int indexB)
	{
		T tmp = list[indexA];
		list[indexA] = list[indexB];
		list[indexB] = tmp;
	}
}

public static class DoubleExtensions
{
	public static void ToDMS(this double degrees, out double d, out double m, out double s)
	{
		var absDegrees = Math.Abs(degrees);
		d = (int)degrees;
		m = (int)((degrees - d) * 60);
		s = (absDegrees - (int)absDegrees - m / 60d) * 3600d;
	}

    public static double DMStoDegrees(double d, double m, double s)
    {
        return d + m/60d + s/3600d;
    }

    public static string ToDMS(this double degrees)
    {
		degrees.ToDMS(out double d, out double m, out double s);
		return d + "° " + m + "' " + s.ToString("0.00") + "\"";
    }

    public static string ToDMS(this double degrees, string positive, string negative)
	{
		var side = degrees < 0 ? negative : positive;

		var absDegrees = Math.Abs(degrees);
		var d = (int)absDegrees;
		var m = (int)((absDegrees - d) * 60);
		var s = (absDegrees - d - m / 60d) * 3600d;
		return d + "° " + m + "' " + s.ToString("0.00") + "\" " + side;
	}

    public static double ToMetricDistance(this double value, out string unit)
    {
        if (value >= 1000)
        {
            unit = "km";
            return value * 0.001;
        }
        unit = "m";
        return value;
    }

    public static string MetersToString(this double meters)
    {
		double distance = meters.ToMetricDistance(out string distanceUnits);
		return distance + " " + distanceUnits;
    }

    public static double Round(this double value)
    {
        double rounded;
        if (value > 5)
        {
            int steps = 0;
            do
            {
                value *= 0.1;
                steps++;
            }
            while (value > 5);
            rounded = Math.Round(value);
            rounded *= Math.Pow(10, steps);
        }
        else
        {
            int steps = 0;
            do
            {
                value *= 10;
                steps++;
            }
            while (value < 0.5);
            rounded = Math.Round(value);
            rounded *= Math.Pow(0.1, steps);
        }
        return rounded;
    }

	public static bool Similar(this double a, double b, double percent = 0.0001)
	{
		return Math.Abs(a - b) <= Math.Abs(a * percent);
	}
	public static bool SimilarOrLargerThan(this double a, double b, double percent = 0.0001)
	{
		return a + a * percent >= b;
	}
	public static bool SimilarOrSmallerThan(this double a, double b, double percent = 0.0001)
	{
		return a - a * percent <= b;
	}
}

public static class ArrayExtensions
{
	public static void Fill<T>(this T[] array, T value)
	{
		int count = array.Length;
		for (int i = 0; i < count; i++)
		{
			array[i] = value;
		}
	}

	public static T[] Concat<T>(this T[] a, T[] b)
	{
		var c = new T[a.Length + b.Length];
		a.CopyTo(c, 0);
		b.CopyTo(c, a.Length);
		return c;
	}
}

public static class HashSetExtensions
{
	public static bool AddOnce<T>(this HashSet<T> hs, T item)
	{
		if (!hs.Contains(item))
		{
			hs.Add(item);
			return true;
		}
		return false;
	}
}

public static class Texture2DExtensions
{
	public static void FlipVertically(this Texture2D t)
	{
		var originalPixels = t.GetPixels();

		Color[] newPixels = new Color[originalPixels.Length];

		int width = t.width;
		int rows = t.height;

		for (int x = 0; x < width; x++)
		{
			for (int y = 0; y < rows; y++)
			{
				newPixels[x + y * width] = originalPixels[x + (rows - y - 1) * width];
			}
		}

		t.SetPixels(newPixels);
		t.Apply();
	}
}

public static class RectTransformExtensions
{
	public static bool IsMouseInside(this RectTransform rt)
	{
		return IsPositionInside(rt, Input.mousePosition);
	}

	public static bool IsPositionInside(this RectTransform rt, Vector3 pos)
	{
		return rt.rect.Contains(rt.InverseTransformPoint(pos));
	}
}

public static class DateTimeExtensions
{
	private static readonly DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

	public static DateTime FromUnixTime(long unixtime)
	{
		return dtDateTime.AddMilliseconds(unixtime).ToLocalTime();
	}

}
