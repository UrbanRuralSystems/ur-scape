// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using System.IO;
using UnityEngine;

namespace ExtensionMethods
{
	public static class StringExtensions
	{
		public static bool EqualsIgnoreCase(this string a, string b)
		{
			return a.Equals(b, System.StringComparison.CurrentCultureIgnoreCase);
		}

		public static bool IsIn(this string a, string[] array)
		{
			foreach (string b in array)
			{
				if (a.Equals(b, System.StringComparison.CurrentCultureIgnoreCase))
					return true;
			}
			return false;
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
            var current = e.Current as IEnumerator;
            if (current != null)
            {
                return Iterate(current);
            }
            return false;
        }

        private static bool Continue(IEnumerator e)
        {
            var current = e.Current as IEnumerator;
            if (current != null && Continue(current))
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

                var current = e.Current as IEnumerator;
                if (current != null && Iterate(current))
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
}
