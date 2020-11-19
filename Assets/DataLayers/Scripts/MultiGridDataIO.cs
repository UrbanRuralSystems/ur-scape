// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class MultiGridDataIO
{
	public static readonly string FileSufix = "multi";

	private static readonly LoadPatchData<MultiGridPatch, MultiGridData>[] headerLoader =
    {
        LoadCsv,
        LoadBinHeader
    };

    public static LoadPatchData<MultiGridPatch, MultiGridData> GetPatchHeaderLoader(PatchDataFormat format)
    {
        return headerLoader[(int)format];
    }


    public static IEnumerator LoadCsv(string filename, PatchDataLoadedCallback<MultiGridData> callback)
    {
		yield return FileRequest.GetText(filename, (sr) => PatchDataIO.ParseAsync(sr, filename, ParseCsv, callback));
	}

	private static void ReadValues(StreamReader sr, MultiGridData multigrid, string filename)
	{
		int categoriesCount = multigrid.categories == null? 0 : multigrid.categories.Length;
		if (categoriesCount == 0)
			return;

		List<float>[] data = new List<float>[categoriesCount];
		for (int i = 0; i < categoriesCount; i++)
		{
			data[i] = new List<float>();
		}

		string line;
		string[] cells;
		float value;
		var cultureInfo = CultureInfo.InvariantCulture;

		// Read each data row at a time with value
		while ((line = sr.ReadLine()) != null)
		{
			cells = line.Split(',');
			for (int i = 0; i < categoriesCount; i++)
			{
				value = float.Parse(cells[i], cultureInfo);
				data[i].Add(value);
				var g = multigrid.categories[i].grid;
				g.minValue = Mathf.Min(g.minValue, value);
				g.maxValue = Mathf.Max(g.maxValue, value);
			}
		}

		for (int i = 0; i < categoriesCount; i++)
		{
			multigrid.categories[i].grid.values = data[i].ToArray();
		}
	}

	public static IEnumerator LoadBinHeader(string filename, PatchDataLoadedCallback<MultiGridData> callback)
	{
#if UNITY_WEBGL
        callback(ParseBinHeader(PatchDataIO.brHeaders, filename));
        yield break;
#else
		yield return FileRequest.GetBinary(filename, (br) => callback(ParseBinHeader(br, filename)));
#endif
	}

	public static MultiGridData ParseBinHeader(BinaryReader br, string filename)
	{
		return ParseBinHeader(br, filename, new MultiGridData());
	}

	public static IEnumerator LoadBin(this MultiGridData mulyigrid, string filename, PatchDataLoadedCallback<MultiGridData> callback)
	{
		yield return FileRequest.GetBinary(filename, (br) => callback(ParseBin(br, filename, mulyigrid)));
	}

	private static void ParseCsv(ParseTaskData data)
	{
		GridDataIO.Parameter parameter = null;
		bool[] found = new bool[GridDataIO.Parameters.Length];

		string line = null;
		string[] cells = null;
		bool skipLineRead = false;

        GridData grid = new GridData();

        MultiGridData multigrid = null;

		while (true)
		{
			if (!skipLineRead)
				line = data.sr.ReadLine();
			else
				skipLineRead = false;

			if (line == null)
				break;

			cells = line.Split(',');
			parameter = GridDataIO.CheckParameter(cells[0], cells[1], GridDataIO.Parameters, out bool hasData);

            if (parameter == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("File " + data.filename + " has unrecognized header parameter " + cells[0] + ". Should it go to the metadata?");
#endif
                if (grid.metadata != null)
                {
                    grid.metadata.Add(cells[0], cells[1]);
                }
                continue;
            }

#if UNITY_EDITOR
            if (found[(int)parameter.id])
            {
                Debug.LogWarning("File " + data.filename + " has duplicate metadata entry: " + parameter.label);
            }
#endif
            found[(int)parameter.id] = true;

			switch (parameter.id)
			{
				case GridDataIO.ParamId.Metadata:
					if (hasData)
					{
						grid.metadata = PatchDataIO.ReadCsvMetadata(data.sr, GridDataIO.CsvTokens, ref line);
						skipLineRead = line != null;
					}
					break;
                //case GridDataIO.ParamId.NameToValue:
                //    if (hasData)
                //    {
				//		nameToValues = GridDataIO.ReadCsvNameToValues(sr, CsvTokens, ref line);
                //        skipLineRead = line != null;
                //    }
                //    break;
                case GridDataIO.ParamId.Categories:
					if (hasData)
					{
						grid.categories = GridDataIO.ReadCsvCategories(data.sr, data.filename, GridDataIO.CsvTokens, ref line);
						skipLineRead = line != null;
					}
					break;
                case GridDataIO.ParamId.Coloring:
                case GridDataIO.ParamId.Colouring:
					try
					{
						grid.coloring = (GridData.Coloring)Enum.Parse(typeof(GridData.Coloring), cells[1], true);
					}
					catch (Exception)
					{
						grid.coloring = GridData.Coloring.Single;
					}
					break;
				case GridDataIO.ParamId.West:
					grid.west = double.Parse(cells[1], CultureInfo.InvariantCulture);
					break;
				case GridDataIO.ParamId.North:
					grid.north = double.Parse(cells[1], CultureInfo.InvariantCulture);
					if (grid.north > GeoCalculator.MaxLatitude)
						Debug.LogWarning("File " + data.filename + " has north above " + GeoCalculator.MaxLatitude + ": " + grid.north);
					break;
				case GridDataIO.ParamId.East:
					grid.east = double.Parse(cells[1], CultureInfo.InvariantCulture);
					break;
				case GridDataIO.ParamId.South:
					grid.south = double.Parse(cells[1], CultureInfo.InvariantCulture);
					if (grid.south < GeoCalculator.MinLatitude)
						Debug.LogWarning("File " + data.filename + " has south below " + GeoCalculator.MinLatitude + ": " + grid.south);
					break;
				case GridDataIO.ParamId.CountX:
					grid.countX = int.Parse(cells[1]);
					break;
				case GridDataIO.ParamId.CountY:
					grid.countY = int.Parse(cells[1]);
					break;
				case GridDataIO.ParamId.Units:
					grid.units = cells[1];
					break;
				case GridDataIO.ParamId.Values:
					multigrid = new MultiGridData(grid);
					ReadValues(data.sr, multigrid, data.filename);
					break;
				default:
#if UNITY_EDITOR
                    Debug.Log("File " + data.filename + " will ignore row: " + line);
#endif
                    skipLineRead = false;
					break;
			}
		}

#if UNITY_EDITOR
		foreach (var p in GridDataIO.Parameters)
		{
			if (p.isRequired && !found[(int)p.id])
			{
				Debug.LogError("Didn't find " + p.label + " in " + data.filename);
			}
		}
#endif

		data.patch = multigrid;
	}

	private static MultiGridData ParseBin(BinaryReader br, string filename, MultiGridData multigrid)
    {
		ParseBinHeader(br, filename, multigrid);
		ParseBinProperties(br, multigrid);
		ParseBinGrids(br, filename, multigrid);
		return multigrid;
    }

	public static MultiGridData ParseBinHeader(BinaryReader br, string filename, MultiGridData multigrid)
	{
		// Read header
		PatchDataIO.SkipBinVersion(br);
		PatchDataIO.ReadBinBoundsHeader(br, filename, multigrid);

		return multigrid;
	}

	public static void ParseBinProperties(BinaryReader br, MultiGridData multigrid)
	{
		// Read Metadata (if available)
		multigrid.metadata = PatchDataIO.ReadBinMetadata(br);

		// Read coloring
		multigrid.coloring = (GridData.Coloring)br.ReadByte();

		// Read categories (without values)
		int gridCount = br.ReadInt32();
		if (gridCount > 0)
		{
			multigrid.categories = new GridCategory[gridCount];
			for (int i = 0; i < gridCount; i++)
			{
				multigrid.categories[i] = new GridCategory(br.ReadString(), null, Color.white);
			}
		}
	}

	public static void ParseBinGrids(BinaryReader br, string filename, MultiGridData multigrid)
	{
		if (multigrid.categories != null)
		{
			int count = multigrid.categories.Length;
			for (int i = 0; i < count; i++)
			{
				var grid = new GridData(multigrid);
				GridDataIO.ParseBinProperties(br, filename, grid);
				GridDataIO.ParseBinValues(br, grid);
				grid.patch = multigrid.patch;

				multigrid.categories[i].grid = grid;
			}
		}
	}

	public static void SaveBin(this MultiGridData multigrid, string filename)
    {
		using (var bw = new BinaryWriter(File.Open(filename, FileMode.Create)))
        {
			PatchDataIO.WriteBinVersion(bw);
			PatchDataIO.WriteBinBoundsHeader(bw, multigrid);

			// Write Metadata (if available)
			PatchDataIO.WriteBinMetadata(bw, multigrid.metadata);

			bw.Write((byte)multigrid.coloring);

			int categoriesCount = multigrid.categories == null ? 0 : multigrid.categories.Length;
			bw.Write(categoriesCount);

			if (multigrid.categories != null)
			{
				// Write categories (without values)
				foreach (var c in multigrid.categories)
				{
					bw.Write(c.name);
				}

				// Write Grids
				foreach (var c in multigrid.categories)
				{
					GridDataIO.WriteBinProperties(bw, c.grid);
					GridDataIO.WriteBinValues(bw, c.grid);
				}
			}
        }
    }

}

