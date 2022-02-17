// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
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

public static class PointDataIO
{
	public const string FileSufix = "point";
	public const int MaxWriteSize = 100000000;  // 100 MB

	private static readonly LoadPatchData<PointPatch, PointData>[] headerLoader =
    {
        LoadCsv,
        LoadBinHeader
	};

    public static LoadPatchData<PointPatch, PointData> GetPatchHeaderLoader(PatchDataFormat format)
    {
        return headerLoader[(int)format];
    }


    public static IEnumerator LoadCsv(string filename, PatchDataLoadedCallback<PointData> callback)
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
        Count,
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
		new Parameter(ParamId.Count,		"Count",		true,		false),
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

		PointData pointData = new PointData();

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
				if (pointData.metadata != null)
				{
					pointData.metadata.Add(cells[0], cells[1]);
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
						pointData.metadata = PatchDataIO.ReadCsvMetadata(data.sr, CsvTokens, ref line);
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
						pointData.categories = ReadCsvCategories(data.sr, data.filename, CsvTokens, ref line);
						AssignCustomColors(pointData, ref colors);
						skipLineRead = line != null;
					}
					break;
                case ParamId.Coloring:
                case ParamId.Colouring:
					try
					{
						pointData.coloring = (PointData.Coloring)Enum.Parse(typeof(PointData.Coloring), cells[1], true);
						AssignCustomColors(pointData, ref colors);
					}
					catch (Exception)
					{
						pointData.coloring = PointData.Coloring.Single;
					}
					break;
				case ParamId.Colors:
				case ParamId.Colours:
					if (hasData)
					{
						colors = ReadCsvColors(data.sr, CsvTokens, ref line);
						AssignCustomColors(pointData, ref colors);
						skipLineRead = line != null;
					}
					break;
				case ParamId.West:
					pointData.west = double.Parse(cells[1], CultureInfo.InvariantCulture);
					//if (pointData.west < GeoCalculator.MinLongitude)
					//    Debug.LogWarning("File " + data.filename + " has west below " + GeoCalculator.MinLongitude + ": " + pointData.west);
					break;
				case ParamId.North:
					pointData.north = double.Parse(cells[1], CultureInfo.InvariantCulture);
                    if (pointData.north > GeoCalculator.MaxLatitude)
                        Debug.LogWarning("File " + data.filename + " has north above " + GeoCalculator.MaxLatitude + ": " + pointData.north);
                    break;
				case ParamId.East:
					pointData.east = double.Parse(cells[1], CultureInfo.InvariantCulture);
					//if (pointData.east > GeoCalculator.MaxLongitude)
					//    Debug.LogWarning("File " + data.filename + " has east above " + GeoCalculator.MaxLongitude + ": " + pointData.east);
					break;
				case ParamId.South:
					pointData.south = double.Parse(cells[1], CultureInfo.InvariantCulture);
                    if (pointData.south < GeoCalculator.MinLatitude)
                        Debug.LogWarning("File " + data.filename + " has south below " + GeoCalculator.MinLatitude + ": " + pointData.south);
                    break;
				case ParamId.Count:
					pointData.count = int.Parse(cells[1]);
					break;
				case ParamId.Units:
					pointData.units = cells[1];
					break;
				case ParamId.Values:
					ReadValues(data.sr, data.filename, pointData, nameToValues);
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

		if (pointData.IsCategorized)
		{
			pointData.RemapCategories(data.filename, false);
		}
		else
		{
			pointData.UpdateDistribution();
		}

		data.patch = pointData;
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

	private static void ReadValues(StreamReader sr, string filename, PointData pointData, List<float> valuesMap)
	{
		double[] lons = new double[pointData.count];
		double[] lats = new double[pointData.count];
		float[] values = new float[pointData.count];
		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		string line;
		string[] cells;
		float value;
		int index = 0;
		var cultureInfo = CultureInfo.InvariantCulture;

		if (valuesMap == null)
        {
            // Read each data row at a time with value
            while ((line = sr.ReadLine()) != null)
            {
                cells = line.Split(',');

				value = float.Parse(cells[0], cultureInfo);
                values[index] = value;

                minValue = Mathf.Min(minValue, value);
                maxValue = Mathf.Max(maxValue, value);

				lons[index] = double.Parse(cells[1], cultureInfo);
				lats[index] = double.Parse(cells[2], cultureInfo);

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
				value = valuesMap[valueAsId - 1];
                values[index] = value;

                minValue = Mathf.Min(minValue, value);
                maxValue = Mathf.Max(maxValue, value);

				lons[index] = double.Parse(cells[1], cultureInfo);
				lats[index] = double.Parse(cells[2], cultureInfo);

				index++;
            }
        }

		if (index != pointData.count)
		{
			Debug.LogWarning(filename + " has " + index + " values instead of the expected " + pointData.count);
		}

        pointData.minValue = minValue;
		pointData.maxValue = maxValue;
        pointData.lons = lons;
		pointData.lats = lats;
		pointData.values = values;
	}

	private static IEnumerator LoadBinHeader(string filename, PatchDataLoadedCallback<PointData> callback)
    {
#if UNITY_WEBGL
        callback(ParseBinHeader(PatchDataIO.brHeaders, filename));
        yield break;
#else
        yield return FileRequest.GetBinary(filename, (br) => callback(ParseBinHeader(br, filename)));
#endif
    }

	private static PointData ParseBinHeader(BinaryReader br, string filename)
    {
        return ParseBinHeader(br, filename, new PointData());
    }

	public static IEnumerator LoadBin(this PointData pointData, string filename, PatchDataLoadedCallback<PointData> callback)
    {
        yield return FileRequest.GetBinary(filename, (br) => callback(ParseBin(br, filename, pointData)));
    }

	public static void SaveBin(this PointData pointData, string filename)
	{
		using (var bw = new BinaryWriter(File.Open(filename, FileMode.Create)))
		{
			WriteBinHeader(bw, pointData);
			WriteBinProperties(bw, pointData);
			WriteBinValues(bw, pointData);
		}
	}

	private static void WriteBinHeader(BinaryWriter bw, PointData pointData)
	{
		PatchDataIO.WriteBinVersion(bw);
		PatchDataIO.WriteBinBoundsHeader(bw, pointData);

		// Write Categories count
		bw.Write(pointData.categories == null ? 0 : pointData.categories.Length);
	}

	public static void WriteBinProperties(BinaryWriter bw, PointData pointData)
	{
		bw.Write(pointData.minValue);
		bw.Write(pointData.maxValue);
		bw.Write(pointData.count);
		bw.Write(pointData.units);
		bw.Write((byte)pointData.coloring);

		// Write Metadata (if available)
		PatchDataIO.WriteBinMetadata(bw, pointData.metadata);

		// Write Categories (if available)
		WriteBinCategories(bw, pointData.categories, pointData.coloring == PointData.Coloring.Custom);
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

	public static void WriteBinValues(BinaryWriter bw, PointData pointData)
	{
		// Write values
		WriteBinValues(bw, pointData.lons, pointData.lats, pointData.values);

		// Write distribution values
		var distribution = pointData.DistributionValues;
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
			bw.Write(pointData.MaxDistributionValue);
		}
	}

	private static void WriteBinValues(BinaryWriter bw, double[] lons, double[] lats, float[] values)
	{
		int lonsBytes = lons.Length * 8;
		int valueBytes = values.Length * 4;
		int totalBytes = lonsBytes * 2 + valueBytes;
		if (totalBytes > MaxWriteSize)
		{
			byte[] byteArray = new byte[MaxWriteSize];
			WriteBinValues(bw, lons, 8, byteArray);
			WriteBinValues(bw, lats, 8, byteArray);
			WriteBinValues(bw, values, 4, byteArray);
		}
		else
		{
			int size = totalBytes;
			byte[] byteArray = new byte[size];
			Buffer.BlockCopy(lons, 0, byteArray, 0, lonsBytes);
			Buffer.BlockCopy(lats, 0, byteArray, lonsBytes, lonsBytes);
			Buffer.BlockCopy(values, 0, byteArray, 2 * lonsBytes, valueBytes);
			bw.Write(byteArray);
		}
	}

	private static void WriteBinValues<T>(BinaryWriter bw, T[] items, int stride, byte[] byteArray)
	{
		int iterationElements = Math.Min(items.Length, MaxWriteSize / stride);      // MaxWriteSize = 100 Mb
		int iterationSize = iterationElements * stride;
		int sizeBytes = items.Length * stride;
		int offset = 0;
		do
		{
			Buffer.BlockCopy(items, offset, byteArray, 0, iterationSize);
			bw.Write(byteArray, 0, iterationSize);
			sizeBytes -= iterationSize;
			offset += iterationElements;
		}
		while (sizeBytes >= iterationSize);

		if (sizeBytes > 0)
		{
			Buffer.BlockCopy(items, offset, byteArray, 0, sizeBytes);
			bw.Write(byteArray, 0, sizeBytes);
		}
	}

	private static PointData ParseBin(BinaryReader br, string filename, PointData pointData)
	{
		// Read header
		ParseBinHeader(br, filename, pointData);
		ParseBinProperties(br, filename, pointData);
		ParseBinValues(br, pointData);
		return pointData;
	}

	public static PointData ParseBinHeader(BinaryReader br, string filename, PointData pointData)
	{
		// Read header
		PatchDataIO.SkipBinVersion(br);
		PatchDataIO.ReadBinBoundsHeader(br, filename, pointData);

		// Read categories count
		int count = br.ReadInt32();
		if (count > 0)
		{
			pointData.categories = new IntCategory[count];
		}

		return pointData;
	}

	public static void ParseBinProperties(BinaryReader br, string filename, PointData pointData)
	{
		pointData.minValue = br.ReadSingle();
		pointData.maxValue = br.ReadSingle();
		pointData.count = br.ReadInt32();
		pointData.units = br.ReadString();
		pointData.coloring = (PointData.Coloring)br.ReadByte();

		// Read Metadata (if available)
		pointData.metadata = PatchDataIO.ReadBinMetadata(br);

		// Read Categories (if available)
		if (pointData.categories != null)
		{
			ReadBinCategories(br, pointData, pointData.coloring == PointData.Coloring.Custom);

			int categoriesCount = pointData.categories.Length;
			if (categoriesCount > CategoryFilter.MaxCategories)
				Debug.LogWarning(filename + " has more than " + CategoryFilter.MaxCategories + " categories.");
		}
	}

	private static void ReadBinCategories(BinaryReader br, PointData pointData, bool withColor)
	{
		int count = pointData.categories.Length;
		for (int i = 0; i < count; ++i)
		{
			pointData.categories[i] = new IntCategory
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
				var cat = pointData.categories[i];
				cat.color.r = br.ReadByte() / 255f;
				cat.color.g = br.ReadByte() / 255f;
				cat.color.b = br.ReadByte() / 255f;
				cat.color.a = 1;
			}
		}
	}

	public static void ParseBinValues(BinaryReader br, PointData pointData)
	{
		// Allocate point data memory
		pointData.InitPointData();

		byte[] bytes = null;

		// Read values
		PatchDataIO.ReadArray(br, ref bytes, pointData.lons, 8);
		PatchDataIO.ReadArray(br, ref bytes, pointData.lats, 8);
		PatchDataIO.ReadArray(br, ref bytes, pointData.values, 4);

		// Read distribution values
		var count = br.ReadByte();
		if (count > 0)
		{
			int[] distributionValues = new int[count];
			PatchDataIO.ReadArray(br, ref bytes, distributionValues, 4);

			// Read max distribution value
			int maxDistributionValue = br.ReadInt32();
			pointData.SetDistribution(distributionValues, maxDistributionValue);
		}
	}

	private static readonly PointData tempPointData = new PointData();
	public static void UpdateBin(string fileIn, string fileOut, string units, PointData.Coloring coloring, List<MetadataPair> metadata, IntCategory[] categories)
	{
		using (var br = new BinaryReader(File.Open(fileIn, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
		{
			using (var bw = new BinaryWriter(File.Open(fileOut, FileMode.Create)))
			{
				// Read original
				ParseBinHeader(br, fileIn, tempPointData);
				ParseBinProperties(br, fileIn, tempPointData);

				int readSize = (int)(br.BaseStream.Length - br.BaseStream.Position);
				var bytes = new byte[readSize];
				int read = 0;
				if ((read = br.Read(bytes, 0, readSize)) != readSize)
				{
					Debug.LogError("Couldn't read all values. Expected (" + readSize + "), Read(" + read + ")");
					return;
				}

				// Update properties
				tempPointData.units = units;
				tempPointData.coloring = coloring;
				tempPointData.metadata = metadata;
				tempPointData.categories = categories;

				// Write 
				WriteBinHeader(bw, tempPointData);
				WriteBinProperties(bw, tempPointData);
				bw.Write(bytes);
			}
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

	private static void AssignCustomColors(PointData pointData, ref Color[] colors)
	{
		if (colors != null && pointData.categories != null && colors.Length == pointData.categories.Length && pointData.coloring == PointData.Coloring.Custom)
		{
			int count = pointData.categories.Length;
			for (int i = 0; i < count; ++i)
			{
				pointData.categories[i].color = colors[i];
			}
			colors = null;
		}
	}
}

