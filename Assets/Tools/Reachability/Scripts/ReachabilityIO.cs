// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//          Michael Joos  (joos@arch.ethz.ch)
//			Muhammad Salihin Bin Zaol-kefli

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;

public static class ReachabilityIO
{
    public static IEnumerator Load(string filename, UnityAction<List<MobilityMode>> callback, UnityAction errCallback)
    {
		yield return FileRequest.GetText(filename, (sr) => callback(Parse(sr)), errCallback);
	}

    private static List<MobilityMode> Parse(StreamReader sr)
    {
        // Parse header
        string line = sr.ReadLine();
        string[] header = line.Split(',');

		if (header == null || header.Length != ClassificationValue.Count)
			return null;

		List<MobilityMode> modes = new List<MobilityMode>();
        while ((line = sr.ReadLine()) != null)
        {
            string[] cells = line.Split(',');
            if (string.IsNullOrEmpty(cells[0]))
                continue;

			if (cells.Length != ClassificationValue.Count)
				return null;

			MobilityMode mode = new MobilityMode();
			mode.name = cells[0].Trim();

			for (int i = ClassificationValue.Count - 1; i > 0; --i)
            {
				float.TryParse(cells[i], out float value);
				mode.speeds[ClassificationValue.Count - i] = value * ReachabilityTool.kmPerHourToMetersPerMin;   //convert from km/h to m/min
			}
			modes.Add(mode);
        }

		return modes;
	}

	public static void Save(List<MobilityMode> modes, string filename)
	{
#if UNITY_STANDALONE
		string path = Path.GetDirectoryName(filename);
		Directory.CreateDirectory(path);

		using (var sw = new StreamWriter(File.Open(filename, FileMode.Create), System.Text.Encoding.UTF8))
		{
			sw.WriteLine("Mode,Highway,Highway Link,Primary,Secondary,Other");
			foreach (var mode in modes)
			{
				sw.Write(mode.name);
				for (int i = mode.speeds.Length - 1; i > 0; --i)
				{
					sw.Write("," + mode.speeds[i]);
				}
				sw.WriteLine();
			}
		}
#endif
	}
}
