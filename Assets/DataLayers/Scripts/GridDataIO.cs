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

public static class GridDataIO
{
	public const string FileSufix = "grid";
	public const int MaxWriteSize = 100000000;  // 100 MB

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
		yield return FileRequest.GetText(filename, (sr) => PatchDataIO.ParseAsync(sr, filename, ParseCsv, callback));
	}

	public enum ParamId
	{
        Metadata,
        NameToValue,
        Categories,
        Coloring,
        Colouring,
		Colors,
		Colours,
		West,
        North,
        East,
        South,
        CountX,
        CountY,
		Units,
        Values,
    }

    public class Parameter
    {
		public ParamId id;
		public string label;
        public bool isRequired;
        public bool isSection;

        public Parameter(ParamId id, string label, bool isRequired, bool isSection)
        {
			this.id = id;
            this.label = label;
            this.isRequired = isRequired;
            this.isSection = isSection;
		}
    }

    public static readonly Parameter[] Parameters = {
					// Id					Label			Required	Section
        new Parameter(ParamId.Metadata,		"METADATA",		false,		true),
        new Parameter(ParamId.NameToValue,	"NAMETOVALUE",	false,      true),
        new Parameter(ParamId.Categories,	"CATEGORIES",	false,		true),
        new Parameter(ParamId.Coloring,		"Coloring",		false,      false),
        new Parameter(ParamId.Colouring,	"Colouring",	false,		false),
		new Parameter(ParamId.Colors,		"Colors",		false,      true),
		new Parameter(ParamId.Colours,		"Colours",		false,      true),
		new Parameter(ParamId.West,			"West",			true,       false),
		new Parameter(ParamId.North,		"North",        true,       false),
		new Parameter(ParamId.East,			"East",			true,       false),
		new Parameter(ParamId.South,		"South",		true,       false),
		new Parameter(ParamId.CountX,		"Count X",		true,		false),
        new Parameter(ParamId.CountY,		"Count Y",		true,		false),
		new Parameter(ParamId.Units,		"Units",		false,      false),
		new Parameter(ParamId.Values,		"VALUE",		true,		false),
	};

	public static readonly string[] CsvTokens = ExtractTokens(Parameters);
	public static string[] ExtractTokens(Parameter[] parameters)
	{
		string[] tokens = new string[parameters.Length];
		for (int i = 0; i < parameters.Length; i++)
			tokens[i] = parameters[i].label;
		return tokens;
	}

	public static Parameter CheckParameter(string cellA, string cellB, Parameter[] parameters, out bool hasData)
    {
        for (int i = 0; i < parameters.Length; i++)
        {
            if (cellA.Equals(parameters[i].label, StringComparison.CurrentCultureIgnoreCase))
            {
				var t = parameters[i];
				hasData = !t.isSection || cellB.EqualsIgnoreCase("TRUE");
				return t;
            }
        }
		hasData = false;
		return null;
    }

	private static void ParseCsv(ParseTaskData data)
	{
		Parameter parameter = null;
		bool[] found = new bool[Parameters.Length];

		string line = null;
		string[] cells = null;
		bool skipLineRead = false;

		GridData grid = new GridData();

		List<float> nameToValues = null;
		Color[] colors = null;
		
		while (true)
		{
			if (!skipLineRead)
				line = data.sr.ReadLine();
			else
				skipLineRead = false;

			if (line == null)
				break;

			cells = line.Split(',');
			parameter = CheckParameter(cells[0], cells[1], Parameters, out bool hasData);

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
				case ParamId.Metadata:
					if (hasData)
					{
						grid.metadata = PatchDataIO.ReadCsvMetadata(data.sr, CsvTokens, ref line);
						skipLineRead = line != null;
					}
					break;
                case ParamId.NameToValue:
                    if (hasData)
                    {
                        nameToValues = ReadCsvNameToValues(data.sr, CsvTokens, ref line);
                        skipLineRead = line != null;
                    }
                    break;
                case ParamId.Categories:
					if (hasData)
					{
						grid.categories = ReadCsvCategories(data.sr, data.filename, CsvTokens, ref line);
						AssignCustomColors(grid, ref colors);
						skipLineRead = line != null;
					}
					break;
                case ParamId.Coloring:
                case ParamId.Colouring:
					try
					{
						grid.coloring = (GridData.Coloring)Enum.Parse(typeof(GridData.Coloring), cells[1], true);
						AssignCustomColors(grid, ref colors);
					}
					catch (Exception)
					{
						grid.coloring = GridData.Coloring.Single;
					}
					break;
				case ParamId.Colors:
				case ParamId.Colours:
					if (hasData)
					{
						colors = ReadCsvColors(data.sr, CsvTokens, ref line);
						AssignCustomColors(grid, ref colors);
						skipLineRead = line != null;
					}
					break;
				case ParamId.West:
					grid.west = double.Parse(cells[1], CultureInfo.InvariantCulture);
                    //if (grid.west < GeoCalculator.MinLongitude)
                    //    Debug.LogWarning("File " + data.filename + " has west below " + GeoCalculator.MinLongitude + ": " + grid.west);
                    break;
				case ParamId.North:
					grid.north = double.Parse(cells[1], CultureInfo.InvariantCulture);
                    if (grid.north > GeoCalculator.MaxLatitude)
                        Debug.LogWarning("File " + data.filename + " has north above " + GeoCalculator.MaxLatitude + ": " + grid.north);
                    break;
				case ParamId.East:
					grid.east = double.Parse(cells[1], CultureInfo.InvariantCulture);
                    //if (grid.east > GeoCalculator.MaxLongitude)
                    //    Debug.LogWarning("File " + data.filename + " has east above " + GeoCalculator.MaxLongitude + ": " + grid.east);
                    break;
				case ParamId.South:
					grid.south = double.Parse(cells[1], CultureInfo.InvariantCulture);
                    if (grid.south < GeoCalculator.MinLatitude)
                        Debug.LogWarning("File " + data.filename + " has south below " + GeoCalculator.MinLatitude + ": " + grid.south);
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
					ReadValues(data.sr, data.filename, grid, nameToValues);
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
		foreach (var p in Parameters)
		{
			if (p.isRequired && !found[(int)p.id])
			{
				Debug.LogError("Didn't find '" + p.label + "' property in " + data.filename);
			}
		}
#endif

		if (grid.IsCategorized)
		{
			grid.RemapCategories(data.filename, false);
		}
		else
		{
			grid.UpdateDistribution();
		}

		data.patch = grid;
	}

	public static IntCategory[] ReadCsvCategories(StreamReader sr, string filename, string[] csvTokens, ref string line)
	{
		List<IntCategory> categories = new List<IntCategory>();
		var cultureInfo = CultureInfo.InvariantCulture;

		while ((line = sr.ReadLine()) != null)
		{
			string[] cells = line.Split(',');

			if (cells[0].IsIn(csvTokens))
				break;

			categories.Add(new IntCategory
			{
				name = cells[0],
				value = (int)float.Parse(cells[1], cultureInfo)
			});
		}

		if (categories.Count > CategoryFilter.MaxCategories)
			Debug.LogWarning(filename + " has more than " + CategoryFilter.MaxCategories + " categories.");

		return categories.ToArray();
	}

	private static Color[] ReadCsvColors(StreamReader sr, string[] csvTokens, ref string line)
	{
		List<Color> colors = new List<Color>();
		while ((line = sr.ReadLine()) != null)
		{
			string[] cells = line.Split(',');

			if (cells[0].IsIn(csvTokens))
				break;

			colors.Add(ColorExtensions.Parse(cells[1]));
		}

		return colors.ToArray();
	}

	private static void ReadValues(StreamReader sr, string filename, GridData grid, List<float> valuesMap)
	{
		int count = grid.countX * grid.countY;

		float[] values = new float[count];
        byte[] masks = GridData.CreateMaskBuffer(count); // masks array size needs to be multiple of 4
		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		string line;
		string[] cells;
		float value;
		byte mask;
		bool hasMask = false;
		int index = 0;
		var cultureInfo = CultureInfo.InvariantCulture;

		if (valuesMap == null)
        {
            // Read each data row at a time with value
            while ((line = sr.ReadLine()) != null)
            {
                cells = line.Split(',');
                value = float.Parse(cells[0], cultureInfo);
                mask = byte.Parse(cells[1]);
                values[index] = value;
                masks[index] = mask;

                if (mask == 1)
                {
                    minValue = Mathf.Min(minValue, value);
                    maxValue = Mathf.Max(maxValue, value);
                }
				else
				{
					hasMask = true;
				}

                index++;
            }
        }
        else
        {
			// Read each data row at a time and replace original value with value set for name from NAMETOVALUE
            while ((line = sr.ReadLine()) != null)
            {
                cells = line.Split(',');
                int valueAsId = int.Parse(cells[0]);
                mask = byte.Parse(cells[1]);
				value = mask == 1 ? valuesMap[valueAsId - 1] : -1;
                values[index] = value;
                masks[index] = mask;

                if (mask == 1)
                {
                    minValue = Mathf.Min(minValue, value);
                    maxValue = Mathf.Max(maxValue, value);
                }
				else
				{
					hasMask = true;
				}

				index++;
            }
        }

		if (index != count)
		{
			Debug.LogWarning(filename + " has " + index + " values instead of the expected " + count);
		}

        grid.minValue = minValue;
		grid.maxValue = maxValue;
        grid.values = values;

		// Only copy mask if at least one value is 0
		if (hasMask)
		{
			grid.valuesMask = masks;
		}
	}

	private static IEnumerator LoadBinHeader(string filename, PatchDataLoadedCallback<GridData> callback)
    {
#if UNITY_WEBGL
        callback(ParseBinHeader(PatchDataIO.brHeaders, filename));
        yield break;
#else
        yield return FileRequest.GetBinary(filename, (br) => callback(ParseBinHeader(br, filename)));
#endif
    }

	private static GridData ParseBinHeader(BinaryReader br, string filename)
    {
        return ParseBinHeader(br, filename, new GridData());
    }

	public static IEnumerator LoadBin(this GridData grid, string filename, PatchDataLoadedCallback<GridData> callback)
    {
        yield return FileRequest.GetBinary(filename, (br) => callback(ParseBin(br, filename, grid)));
    }

	public static void SaveBin(this GridData grid, string filename)
	{
		using (var bw = new BinaryWriter(File.Open(filename, FileMode.Create)))
		{
			WriteBinHeader(bw, grid);
			WriteBinProperties(bw, grid);
			WriteBinValues(bw, grid);
		}
	}

	private static void WriteBinHeader(BinaryWriter bw, GridData grid)
	{
		PatchDataIO.WriteBinVersion(bw);
		PatchDataIO.WriteBinBoundsHeader(bw, grid);

		// Write Categories count
		bw.Write(grid.categories == null ? 0 : grid.categories.Length);
	}

	public static void WriteBinProperties(BinaryWriter bw, GridData grid)
	{
		bw.Write(grid.minValue);
		bw.Write(grid.maxValue);
		bw.Write(grid.countX);
		bw.Write(grid.countY);
		bw.Write(grid.units);
		bw.Write((byte)grid.coloring);

		// Write Metadata (if available)
		PatchDataIO.WriteBinMetadata(bw, grid.metadata);

		// Write Categories (if available)
		WriteBinCategories(bw, grid.categories, grid.coloring == GridData.Coloring.Custom);
	}

	private static void WriteBinCategories(BinaryWriter bw, IntCategory[] categories, bool withColor)
	{
		if (categories != null)
		{
			foreach (var cat in categories)
			{
				bw.Write(cat.name);
				bw.Write(cat.value);
			}
			if (withColor)
			{
				foreach (var cat in categories)
				{
					bw.Write((byte)(cat.color.r * 255));
					bw.Write((byte)(cat.color.g * 255));
					bw.Write((byte)(cat.color.b * 255));
				}
			}
		}
	}

	public static void WriteBinValues(BinaryWriter bw, GridData grid)
	{
		// Write values
		WriteBinValues(bw, grid.values);

		// Write mask
		if (grid.valuesMask == null || grid.valuesMask.Length == 0)
		{
			bw.Write(false);
		}
		else
		{
			bw.Write(true);
			bw.Write(grid.valuesMask);
		}

		// Write distribution values
		var distribution = grid.DistributionValues;
		if (distribution == null || distribution.Length == 0)
		{
			bw.Write((byte)0);
		}
		else
		{
			bw.Write((byte)distribution.Length);
			var byteArray = new byte[distribution.Length * 4];
			Buffer.BlockCopy(distribution, 0, byteArray, 0, byteArray.Length);
			bw.Write(byteArray);

			// Write max distribution value
			bw.Write(grid.MaxDistributionValue);
		}
	}

	private static void WriteBinValues(BinaryWriter bw, float[] values)
	{
		int totalBytes = values.Length * 4;
		if (totalBytes > MaxWriteSize)
		{
			int iterationSize = Math.Min(MaxWriteSize, totalBytes);    // 100 Mb
			int iterationElements = iterationSize >> 2;
			byte[] byteArray = new byte[iterationSize];
			int offset = 0;
			do
			{
				Buffer.BlockCopy(values, offset, byteArray, 0, iterationSize);
				bw.Write(byteArray);
				totalBytes -= iterationSize;
				offset += iterationElements;
			}
			while (totalBytes >= iterationSize);

			if (totalBytes > 0)
			{
				Buffer.BlockCopy(values, offset, byteArray, 0, totalBytes);
				bw.Write(byteArray, 0, totalBytes);
			}
		}
		else
		{
			int size = totalBytes;
			byte[] byteArray = new byte[size];
			Buffer.BlockCopy(values, 0, byteArray, 0, size);
			bw.Write(byteArray, 0, size);
		}
	}

	private static GridData ParseBin(BinaryReader br, string filename, GridData grid)
	{
		// Read header
		ParseBinHeader(br, filename, grid);
		ParseBinProperties(br, filename, grid);
		ParseBinValues(br, grid);
		return grid;
	}

	public static GridData ParseBinHeader(BinaryReader br, string filename, GridData grid)
	{
		// Read header
		PatchDataIO.SkipBinVersion(br);
		PatchDataIO.ReadBinBoundsHeader(br, filename, grid);

		// Read categories count
		int count = br.ReadInt32();
		if (count > 0)
		{
			grid.categories = new IntCategory[count];
		}

		return grid;
	}

	public static void ParseBinProperties(BinaryReader br, string filename, GridData grid)
	{
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
			ReadBinCategories(br, grid, grid.coloring == GridData.Coloring.Custom);

			int categoriesCount = grid.categories.Length;
			if (categoriesCount > CategoryFilter.MaxCategories)
				Debug.LogWarning(filename + " has more than " + CategoryFilter.MaxCategories + " categories.");
		}
	}

	private static void ReadBinCategories(BinaryReader br, GridData grid, bool withColor)
	{
		int count = grid.categories.Length;
		for (int i = 0; i < count; ++i)
		{
			grid.categories[i] = new IntCategory
			{
				name = br.ReadString(),
				value = br.ReadInt32()
			};
		}

		// Read custom category colors
		if (withColor)
		{
			for (int i = 0; i < count; ++i)
			{
				var cat = grid.categories[i];
				cat.color.r = br.ReadByte() / 255f;
				cat.color.g = br.ReadByte() / 255f;
				cat.color.b = br.ReadByte() / 255f;
				cat.color.a = 1;
			}
		}
	}

	public static void ParseBinValues(BinaryReader br, GridData grid)
	{
		// Allocate grid memory
		grid.InitGridValues(false);

		byte[] bytes = null;

		// Read values
		ReadArray(br, ref bytes, grid.values, 4);

		// Read if it has values mask
		var withMask = br.ReadBoolean();
		if (withMask)
		{
			grid.CreateMaskBuffer();

			// Read values mask
			ReadArray(br, ref bytes, grid.valuesMask);
		}

		// Read distribution values
		var count = br.ReadByte();
		if (count > 0)
		{
			int[] distributionValues = new int[count];
			ReadArray(br, ref bytes, distributionValues, 4);

			// Read max distribution value
			int maxDistributionValue = br.ReadInt32();
			grid.SetDistribution(distributionValues, maxDistributionValue);
		}
	}

	private static readonly GridData updateGrid = new GridData();
	public static void UpdateBin(string fileIn, string fileOut, string units, GridData.Coloring coloring, List<MetadataPair> metadata, IntCategory[] categories)
	{
		using (var br = new BinaryReader(File.Open(fileIn, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
		{
			using (var bw = new BinaryWriter(File.Open(fileOut, FileMode.Create)))
			{
				// Read original
				ParseBinHeader(br, fileIn, updateGrid);
				ParseBinProperties(br, fileIn, updateGrid);

				int readSize = (int)(br.BaseStream.Length - br.BaseStream.Position);
				var bytes = new byte[readSize];
				int read = 0;
				if ((read = br.Read(bytes, 0, readSize)) != readSize)
				{
					Debug.LogError("Couldn't read all values. Expected (" + readSize + "), Read(" + read + ")");
					return;
				}

				// Update properties
				updateGrid.units = units;
				updateGrid.coloring = coloring;
				updateGrid.metadata = metadata;
				updateGrid.categories = categories;

				// Write 
				WriteBinHeader(bw, updateGrid);
				WriteBinProperties(bw, updateGrid);
				bw.Write(bytes);
			}
		}
	}

	private static void ReadArray(BinaryReader br, ref byte[] bytes, Array arr, int elementBytes = 1)
	{
		int readSize = arr.Length * elementBytes;
		if (readSize > 0)
		{
			if (bytes == null || bytes.Length < readSize)
			{
				bytes = new byte[readSize];
			}

			int read = 0;
			if ((read = br.Read(bytes, 0, readSize)) != readSize)
			{
				Debug.LogError("Couldn't read all values. Expected (" + arr.Length + "), Read(" + (read / elementBytes) + ")");
				readSize = read;
			}
			Buffer.BlockCopy(bytes, 0, arr, 0, readSize);
		}
	}

	private static List<float> ReadCsvNameToValues(StreamReader sr, string[] csvTokens, ref string line)
    {
        List<float> namesToValues = new List<float>();
		var cultureInfo = CultureInfo.InvariantCulture;

		// Read each data row at a time
		while ((line = sr.ReadLine()) != null)
		{
			string[] cells = line.Split(',');

			if (cells[0].IsIn(csvTokens))
				break;

			namesToValues.Add(float.Parse(cells[1], cultureInfo));
        }

        return namesToValues;
    }

	private static void AssignCustomColors(GridData grid, ref Color[] colors)
	{
		if (colors != null && grid.categories != null && colors.Length == grid.categories.Length && grid.coloring == GridData.Coloring.Custom)
		{
			int count = grid.categories.Length;
			for (int i = 0; i < count; ++i)
			{
				grid.categories[i].color = colors[i];
			}
			colors = null;
		}
	}
}

