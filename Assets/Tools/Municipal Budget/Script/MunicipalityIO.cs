// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//			Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public static class MunicipalityIO
{
	public static readonly GridDataIO.Parameter[] Parameters = {
													// Id			Label			Required	Section
		new GridDataIO.Parameter(GridDataIO.ParamId.Metadata,       "METADATA",     false,      true),
		new GridDataIO.Parameter(GridDataIO.ParamId.Categories,     "CATEGORIES",   true,       true),
		new GridDataIO.Parameter(GridDataIO.ParamId.West,           "West",         true,       false),
		new GridDataIO.Parameter(GridDataIO.ParamId.North,          "North",        true,       false),
		new GridDataIO.Parameter(GridDataIO.ParamId.East,           "East",         true,       false),
		new GridDataIO.Parameter(GridDataIO.ParamId.South,          "South",        true,       false),
		new GridDataIO.Parameter(GridDataIO.ParamId.CountX,         "Count X",      true,       false),
		new GridDataIO.Parameter(GridDataIO.ParamId.CountY,         "Count Y",      true,       false),
		new GridDataIO.Parameter(GridDataIO.ParamId.Values,         "VALUE",        true,       false),
	};
	public static readonly string[] CsvTokens = GridDataIO.ExtractTokens(Parameters);

	public static IEnumerator Load(string filename, UnityAction<MunicipalityData> callback, UnityAction errCallback)
	{
		yield return FileRequest.GetText(filename, (sr) => callback(Parse(sr)), errCallback);
	}

	private static MunicipalityData Parse(StreamReader sr)
	{
		double west = 0;
		double east = 0;
		double north = 0;
		double south = 0;
		int countX = 0;
		int countY = 0;
		List<int> ids = new List<int>();
		Dictionary<int, string> idToName = new Dictionary<int, string>();

		var found = new Dictionary<int, bool>();
		string line = null;
		string[] cells = null;
		bool skipLineRead = false;

		while (true)
		{
			if (!skipLineRead)
				line = sr.ReadLine();
			else
				skipLineRead = false;

			if (line == null)
				break;

			cells = line.Split(',');
			var parameter = GridDataIO.CheckParameter(cells[0], cells[1], Parameters, out bool hasData);

			if (parameter == null)
			{
#if UNITY_EDITOR
				Debug.LogWarning("Municipal budget file has unrecognized header parameter " + cells[0] + ". Should it go to the metadata?");
#endif
				continue;
			}

#if UNITY_EDITOR
			if (found.TryGetValue((int)parameter.id, out bool f) && f)
			{
				Debug.LogWarning("Municipal budget file has duplicate metadata entry: " + parameter.label);
			}
#endif
			found[(int)parameter.id] = true;

			switch (parameter.id)
			{
				case GridDataIO.ParamId.Metadata:
					if (hasData)
					{
						PatchDataIO.ReadCsvMetadata(sr, CsvTokens, ref line);
						skipLineRead = line != null;
					}
					break;
				case GridDataIO.ParamId.Categories:
					if (hasData)
					{
						idToName = ReadIdToName(sr, CsvTokens, ref line);
						skipLineRead = line != null;
					}
					break;
				case GridDataIO.ParamId.West:
					west = double.Parse(cells[1]);
					if (west < GeoCalculator.MinLongitude)
						Debug.LogWarning("Municipal budget file has west below " + GeoCalculator.MinLongitude + ": " + west);
					break;
				case GridDataIO.ParamId.North:
					north = double.Parse(cells[1]);
					if (north > GeoCalculator.MaxLatitude)
						Debug.LogWarning("Municipal budget file has north above " + GeoCalculator.MaxLatitude + ": " + north);
					break;
				case GridDataIO.ParamId.East:
					east = double.Parse(cells[1]);
					if (east > GeoCalculator.MaxLongitude)
						Debug.LogWarning("Municipal budget file has east above " + GeoCalculator.MaxLongitude + ": " + east);
					break;
				case GridDataIO.ParamId.South:
					south = double.Parse(cells[1]);
					if (south < GeoCalculator.MinLatitude)
						Debug.LogWarning("Municipal budget file has south below " + GeoCalculator.MinLatitude + ": " + south);
					break;
				case GridDataIO.ParamId.CountX:
					countX = int.Parse(cells[1]);
					break;
				case GridDataIO.ParamId.CountY:
					countY = int.Parse(cells[1]);
					break;
				case GridDataIO.ParamId.Values:
					ids = ReadValues(sr);
					break;
				default:
#if UNITY_EDITOR
					Debug.Log("Municipal budget file will ignore row: " + line);
#endif
					skipLineRead = false;
					break;
			}
		}

#if UNITY_EDITOR
		foreach (var p in Parameters)
		{
			if (p.isRequired && !(found.TryGetValue((int)p.id, out bool f) && f))
			{
				Debug.LogError("Didn't find '" + p.label + "' property in municipal budget file");
			}
		}
#endif

		int totalCount = countX * countY;
		if (ids.Count > totalCount)
		{
			Debug.Log("Municipal budget file has too many values. Expected: " + totalCount + ". Read: " + ids.Count);
			ids.RemoveRange(totalCount, ids.Count - totalCount);
		}
		else if (ids.Count < totalCount)
		{
			Debug.Log("Municipal budget file has insufficient values. Expected: " + totalCount + ". Read: " + ids.Count);
			ids.AddRange(System.Linq.Enumerable.Repeat(-1, totalCount - ids.Count));
		}

		return new MunicipalityData(ids.ToArray(), idToName, north, east, south, west, countX, countY);
	}

	public static Dictionary<int, string> ReadIdToName(StreamReader sr, string[] csvTokens, ref string line)
	{
		Dictionary<int, string> idToName = new Dictionary<int, string>();
		while ((line = sr.ReadLine()) != null)
		{
			string[] cells = line.Split(',');

			if (cells[0].IsIn(csvTokens))
				break;
			idToName.Add((int)float.Parse(cells[1]), cells[0]);
		}

		return idToName;
	}

	private static List<int> ReadValues(StreamReader sr)
	{
		List<int> ids = new List<int>();
		string line;
		string[] cells;

		// Read each data row at a time with value
		while ((line = sr.ReadLine()) != null)
		{
			cells = line.Split(','); // This line is here for backwards compatibility. Remove aftwer a while
			if (int.TryParse(cells[0], out int id))
				ids.Add(id);
		}
		return ids;
	}

}
