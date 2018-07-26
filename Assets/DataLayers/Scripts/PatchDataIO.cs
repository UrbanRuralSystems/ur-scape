// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//			David Neudecker  (neudecker@arch.ethz.ch)

using ExtensionMethods;
using System.Collections;
using System.Collections.Generic;
using System.IO;

// Callback executed after a patch csv/bin has been parsed
public delegate void PatchDataLoadedCallback<D>(D data) where D : PatchData;

// Callback executed after loading metadata
public delegate void LoadMetadataCallback(Metadata metadata);

// Delegate for loading patch data (csv/bin file)
public delegate IEnumerator LoadPatchData<P, D>(string filename, PatchDataLoadedCallback<D> callback) where P : Patch where D : PatchData;

// Delegate for getting a delegate to load a patch
public delegate LoadPatchData<P, D> GetPatchLoader<P, D>(PatchDataFormat format) where P : Patch where D : PatchData;


public static class PatchDataIO
{
#if UNITY_WEBGL
    public static System.IO.BinaryReader brHeaders = null;

	public static IEnumerator StartReadingHeaders()
	{
		return FileRequest.GetStream(Paths.DataWebDBHeaders, (stream) => brHeaders = new BinaryReader(stream));
	}

	public static void FinishReadingHeaders()
	{
		if (brHeaders != null)
		{
			brHeaders.Close();
			brHeaders = null;
		}
	}
#endif

	public static void WriteBinBoundsHeader(BinaryWriter bw, PatchData patch)
	{
		bw.Write(patch.west);
		bw.Write(patch.east);
		bw.Write(patch.north);
		bw.Write(patch.south);
	}

	public static PatchData ReadBinBoundsHeader(BinaryReader br, PatchData patch)
	{
		patch.west = br.ReadDouble();
		patch.east = br.ReadDouble();
		patch.north = br.ReadDouble();
		patch.south = br.ReadDouble();

		return patch;
	}

	public static void WriteBinMetadata(BinaryWriter bw, Metadata metadata)
	{
		// Write Metadata (if available)
		bw.Write(metadata != null);
		if (metadata != null)
		{
			bw.Write(metadata.name);
			bw.Write(metadata.date);
			bw.Write(metadata.source);
			bw.Write(metadata.accuracy);
			bw.Write(metadata.mean);
			bw.Write(metadata.stdDeviation);
		}
	}

	public static Metadata ReadBinMetadata(BinaryReader br)
	{
		// Read Metadata (if available)
		if (!br.ReadBoolean())
			return null;

		Metadata metadata = new Metadata();
		metadata.name = br.ReadString();
		metadata.date = br.ReadString();
		metadata.source = br.ReadString();
		metadata.accuracy = br.ReadString();
		metadata.mean = br.ReadNullableSingle();
		metadata.stdDeviation = br.ReadNullableSingle();

		return metadata;
	}

	public static Metadata ReadCsvMetadata(StreamReader sr, string[] csvTokens, ref string line)
	{
		var metadata = new Metadata();

		string[] cells;

		while ((line = sr.ReadLine()) != null)
		{
			cells = line.Split(',');

			if (cells[0].EqualsIgnoreCase("Name"))
			{
				metadata.name = cells[1];
			}
			else if (cells[0].EqualsIgnoreCase("Date"))
			{
				metadata.date = cells[1];
			}
			else if (cells[0].EqualsIgnoreCase("Source"))
			{
				metadata.source = cells[1];
			}
			else if (cells[0].EqualsIgnoreCase("Accuracy"))
			{
				metadata.accuracy = cells[1];
			}
			else if (cells[0].EqualsIgnoreCase("Mean"))
			{
				float value;
				if (float.TryParse(cells[1], out value))
					metadata.mean = value;
			}
			else if (cells[0].EqualsIgnoreCase("Standard Deviation"))
			{
				float value;
				if (float.TryParse(cells[1], out value))
					metadata.stdDeviation = value;
			}
			else if (cells[0].EqualsIgnoreCase("Units"))	//- Remove this after a while: this was added to avoid Units in Metadata
			{
				// DO NOTHING
			}
			else if (cells[0].IsIn(csvTokens))
			{
				break;
			}
		}

		return metadata;
	}
    
}
