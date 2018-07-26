// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using ExtensionMethods;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class GridDataIO
{
    private static readonly LoadPatchData<GridPatch, GridData>[] headerLoader =
    {
        LoadCsv,
        LoadBinHeader
    };

    public static LoadPatchData<GridPatch, GridData> GetPatchHeaderLoader(PatchDataFormat format)
    {
        return headerLoader[(int)format];
    }


    public static IEnumerator LoadCsv(string filename, PatchDataLoadedCallback<GridData> callback)
    {
		yield return FileRequest.GetText(filename, (sr) => callback(ParseCsv(sr, filename)));
	}

    enum ParamId
	{
        Metadata,
        NameToValue,
        Categories,
		Coloring,
        West,
        North,
        East,
        South,
        CountX,
        CountY,
		Units,
        Values,
    }

    private class Parameter
    {
		public ParamId id;
		public string label;
        public bool isRequired;
        public bool isSection;
		public bool hasData;

        public Parameter(ParamId id, string label, bool isRequired, bool isSection)
        {
			this.id = id;
            this.label = label;
            this.isRequired = isRequired;
            this.isSection = isSection;
			hasData = false;
		}
    }

    static readonly Parameter[] Parameters = {
					// Id					Label			Required	Section
        new Parameter(ParamId.Metadata,		"METADATA",		false,		true),
        new Parameter(ParamId.NameToValue,  "NAMETOVALUE",  false,      true),
        new Parameter(ParamId.Categories,	"CATEGORIES",	false,		true),
		new Parameter(ParamId.Coloring,		"Coloring",		false,		false),
		new Parameter(ParamId.West,			"West",			true,       false),
		new Parameter(ParamId.North,		"North",        true,       false),
		new Parameter(ParamId.East,			"East",			true,       false),
		new Parameter(ParamId.South,		"South",		true,       false),
		new Parameter(ParamId.CountX,		"Count X",		true,		false),
        new Parameter(ParamId.CountY,		"Count Y",		true,		false),
		new Parameter(ParamId.Units,		"Units",		false,      false),
		new Parameter(ParamId.Values,		"VALUE",		true,		false),
	};

	static readonly string[] CsvTokens = ExtractTokens(Parameters);
	private static string[] ExtractTokens(Parameter[] parameters)
	{
		string[] tokens = new string[parameters.Length];
		for (int i = 0; i < parameters.Length; i++)
			tokens[i] = parameters[i].label;
		return tokens;
	}

	private static Parameter CheckParameter(string cellA, string cellB)
    {
        for (int i = 0; i < Parameters.Length; i++)
        {
            if (cellA.Equals(Parameters[i].label, StringComparison.CurrentCultureIgnoreCase))
            {
				var t = Parameters[i];
				t.hasData = !t.isSection || cellB.EqualsIgnoreCase("TRUE");
				return t;
            }
        }
        return null;
    }

    private static GridData ParseCsv(StreamReader sr, string filename)
    {
		Parameter parameter = null;
		bool[] found = new bool[Parameters.Length];

		string line = null;
		string[] cells = null;
		bool skipLineRead = false;

		GridData grid = new GridData();
		List<int> nameToValues = null;

		while (true)
		{
			if (!skipLineRead)
				line = sr.ReadLine();
			else
				skipLineRead = false;

			if (line == null)
				break;

			cells = line.Split(',');
			parameter = CheckParameter(cells[0], cells[1]);

			if (parameter == null)
				continue;

			found[(int)parameter.id] = true;

			switch (parameter.id)
			{
				case ParamId.Metadata:
					if (parameter.hasData)
					{
						grid.metadata = PatchDataIO.ReadCsvMetadata(sr, CsvTokens, ref line);
						skipLineRead = line != null;
					}
					break;
                case ParamId.NameToValue:
                    if (parameter.hasData)
                    {
                        nameToValues = ReadCsvNameToValues(sr, CsvTokens, ref line);
                        skipLineRead = line != null;
                    }
                    break;
                case ParamId.Categories:
					if (parameter.hasData)
					{
						grid.categories = ReadCsvCategories(sr, CsvTokens, ref line);
						if (grid.categories != null && grid.categories[0].name.EqualsIgnoreCase("None"))
						{
							grid.categoryMask &= ~1u;
						}
						skipLineRead = line != null;
					}
					break;
				case ParamId.Coloring:
					try
					{
						grid.coloring = (GridData.Coloring)Enum.Parse(typeof(GridData.Coloring), cells[1], true);
					}
					catch (Exception)
					{
						grid.coloring = GridData.Coloring.Single;
					}
					break;
				case ParamId.West:
					grid.west = double.Parse(cells[1]);
					break;
				case ParamId.North:
					grid.north = double.Parse(cells[1]);
					break;
				case ParamId.East:
					grid.east = double.Parse(cells[1]);
					break;
				case ParamId.South:
					grid.south = double.Parse(cells[1]);
					break;
				case ParamId.CountX:
					grid.countX = int.Parse(cells[1]);
					break;
				case ParamId.CountY:
					grid.countY = int.Parse(cells[1]);
					break;
				case ParamId.Units:
					grid.units = cells[1];
					break;
				case ParamId.Values:
					ReadValues(sr, grid, nameToValues, filename);
					break;
				default:
					skipLineRead = false;
					break;
			}
        }

#if UNITY_EDITOR
		foreach (var p in Parameters)
		{
			if (p.isRequired && !found[(int)p.id])
			{
				Debug.LogError("Didn't find " + p.label + " in " + filename);
				return null;
			}
		}
#endif

		grid.UpdateDistribution(false);

		return grid;
	}

	public static Category[] ReadCsvCategories(StreamReader sr, string[] csvTokens, ref string line)
	{
		List<Category> categories = new List<Category>();

		// Read each data row at a time
		while ((line = sr.ReadLine()) != null)
		{
			string[] cells = line.Split(',');

			if (cells[0].IsIn(csvTokens))
			{
				break;
			}

			Category categoryDescription = new Category();
			categoryDescription.name = cells[0];
			categoryDescription.value = (int)float.Parse(cells[1]);

			categories.Add(categoryDescription);
		}

		return categories.ToArray();
	}

	private static void ReadValues(StreamReader sr, GridData grid, List<int> valuesMap, string filename)
	{
		int count = grid.countX * grid.countY;

		List<float> values = new List<float>(count);
		List<bool> masks = new List<bool>(count);
		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		string line;
		string[] cells;
		float value;
		bool mask;
       
#if UNITY_EDITOR
		int valueCounter = 0;
#endif

        if (valuesMap == null)
        {
            // Read each data row at a time with value
            while ((line = sr.ReadLine()) != null)
            {
                cells = line.Split(',');
                value = float.Parse(cells[0]);
                mask = int.Parse(cells[1]) == 1;
                values.Add(value);
                masks.Add(mask);

                if (mask)
                {
                    minValue = Mathf.Min(minValue, value);
                    maxValue = Mathf.Max(maxValue, value);
                }

#if UNITY_EDITOR
                valueCounter++;
#endif
            }
        }
        else
        {
			// Read each data row at a time and replace original value with value set for name from NAMETOVALUE
            while ((line = sr.ReadLine()) != null)
            {
                cells = line.Split(',');
                int valueAsId = int.Parse(cells[0]);
                mask = int.Parse(cells[1]) == 1;
				value = mask ? valuesMap[valueAsId - 1] : -1;
                values.Add(value);
                masks.Add(mask);

                if (mask)
                {
                    minValue = Mathf.Min(minValue, value);
                    maxValue = Mathf.Max(maxValue, value);
                }

#if UNITY_EDITOR
                valueCounter++;
#endif
            }
        }

#if UNITY_EDITOR
		if (valueCounter != count)
		{
			Debug.LogWarning(filename + " has " + valueCounter + " values instead of the expected " + count);
		}
#endif

		grid.minValue = minValue;
		grid.maxValue = maxValue;
        grid.values = values.ToArray();
		grid.valuesMask = masks.ToArray();
	}

	public static IEnumerator LoadBinHeader(string filename, PatchDataLoadedCallback<GridData> callback)
    {
#if UNITY_WEBGL
        callback(ParseBinHeader(PatchDataIO.brHeaders));
        yield break;
#else
        yield return FileRequest.GetBinary(filename, (br) => callback(ParseBinHeader(br)));
#endif
    }

    private static GridData ParseBinHeader(BinaryReader br)
    {
        return ParseBinHeader(br, new GridData());
    }

    public static IEnumerator LoadBin(this GridData grid, string filename, PatchDataLoadedCallback<GridData> callback)
    {
        yield return FileRequest.GetBinary(filename, (br) => callback(ParseBin(br, grid)));
    }

    private static GridData ParseBinHeader(BinaryReader br, GridData grid)
    {
		// Read header
		PatchDataIO.ReadBinBoundsHeader(br, grid);

		// Read categories count
		int count = br.ReadInt32();
		if (count > 0)
		{
			grid.categories = new Category[count];
		}

		return grid;
    }

	private static void WriteBinCategories(BinaryWriter bw, Category[] categories)
	{
		if (categories != null)
		{
			foreach (var cat in categories)
			{
				bw.Write(cat.name);
				bw.Write(cat.value);
			}
		}
	}

	private static int ReadCategoriesCount(BinaryReader br)
	{
		return br.ReadInt32();
	}

	private static void ReadBinCategories(BinaryReader br, GridData grid)
	{
		int count = grid.categories.Length;
		for (int i = 0; i < count; i++)
		{
			Category cat = new Category();
			cat.name = br.ReadString();
			cat.value = br.ReadInt32();
			grid.categories[i] = cat;
		}
	}

	private static GridData ParseBin(BinaryReader br, GridData grid)
    {
        // Read header
        ParseBinHeader(br, grid);

		grid.minValue = br.ReadSingle();
		grid.maxValue = br.ReadSingle();
		grid.countX = br.ReadInt32();
        grid.countY = br.ReadInt32();
		grid.units = br.ReadString();
		grid.coloring = (GridData.Coloring)br.ReadByte();

		// Read Metadata (if available)
		grid.metadata = PatchDataIO.ReadBinMetadata(br);

		// Read Categories (if available)
		if (grid.categories != null)
		{
			ReadBinCategories(br, grid);
			if (grid.categories[0].name.EqualsIgnoreCase("None"))
				grid.categoryMask &= ~1u;
		}

		// Allocate grid memory
		grid.InitGridValues();

		byte[] bytes = null;

		// Read values
		bytes = ReadArray(br, bytes, grid.values, 4);

		// Read value masks
		bytes = ReadArray(br, bytes, grid.valuesMask);

		// Read distribution values
		var count = br.ReadByte();
		int[] distributionValues = new int[count];
		bytes = ReadArray(br, bytes, distributionValues, 4);

		// Read max distribution value
		int maxDistributionValue = br.ReadInt32();
		grid.SetDistribution(distributionValues, maxDistributionValue);

        return grid;
    }

	public static void SaveBin(this GridData grid, string filename)
    {
		using (var bw = new BinaryWriter(File.Open(filename, FileMode.Create)))
        {
			PatchDataIO.WriteBinBoundsHeader(bw, grid);

			// Write Categories count
			bw.Write(grid.categories == null ? 0 : grid.categories.Length);

			bw.Write(grid.minValue);
			bw.Write(grid.maxValue);
			bw.Write(grid.countX);
			bw.Write(grid.countY);
			bw.Write(grid.units);
			bw.Write((byte)grid.coloring);

			// Write Metadata (if available)
			PatchDataIO.WriteBinMetadata(bw, grid.metadata);

			// Write Categories (if available)
			WriteBinCategories(bw, grid.categories);

			// Write values
			byte[] byteArray = new byte[grid.values.Length * 4];
            Buffer.BlockCopy(grid.values, 0, byteArray, 0, byteArray.Length);
            bw.Write(byteArray);

			// Write mask
			byteArray = new byte[grid.valuesMask.Length];
			Buffer.BlockCopy(grid.valuesMask, 0, byteArray, 0, byteArray.Length);
			bw.Write(byteArray);

			// Write distribution values
			var distribution = grid.DistributionValues;
			bw.Write((byte)distribution.Length);
			byteArray = new byte[distribution.Length * 4];
            Buffer.BlockCopy(distribution, 0, byteArray, 0, byteArray.Length);
            bw.Write(byteArray);

			// Write max distribution value
			bw.Write(grid.MaxDistributionValue);
        }
    }

	private static byte[] ReadArray(BinaryReader br, byte[] bytes, Array arr, int elementBytes = 1)
	{
		int readSize = arr.Length * elementBytes;

		if (bytes == null || bytes.Length < readSize)
		{
			bytes = new byte[readSize];
		}

		if (br.Read(bytes, 0, readSize) != readSize)
		{
			Debug.LogError("Couldn't read all values");
		}
		Buffer.BlockCopy(bytes, 0, arr, 0, readSize);

		return bytes;
	}

    public static List<int> ReadCsvNameToValues(StreamReader sr, string[] csvTokens, ref string line)
    {
        List<int> namesToValues = new List<int>();

		// Read each data row at a time
		while ((line = sr.ReadLine()) != null)
		{
			string[] cells = line.Split(',');

			if (cells[0].IsIn(csvTokens))
				break;

			namesToValues.Add(int.Parse(cells[1]));
        }

        return namesToValues;
    }
}

