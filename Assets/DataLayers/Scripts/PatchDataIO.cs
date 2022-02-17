// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//			David Neudecker  (neudecker@arch.ethz.ch)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

// Callback executed after a patch csv/bin has been parsed
public delegate void PatchDataLoadedCallback<D>(D data) where D : PatchData;

// Callback executed after loading metadata
public delegate void LoadMetadataCallback(List<MetadataPair> metadata);

// Delegate for loading patch data (csv/bin file)
public delegate IEnumerator LoadPatchData<P, D>(string filename, PatchDataLoadedCallback<D> callback) where P : Patch where D : PatchData;

// Delegate for getting a delegate to load a patch
public delegate LoadPatchData<P, D> GetPatchLoader<P, D>(PatchDataFormat format) where P : Patch where D : PatchData;

// Callback executed after a patch csv/bin has been parsed
public delegate void ParseTask(ParseTaskData data);


public class ParseTaskData
{
	public StreamReader sr;
	public string filename;
	public ParseTask task;
	public PatchData patch;

	public void Parse()
	{
		if (task != null)
			task(this);
	}

	public void Clear()
	{
		sr = null;
		filename = null;
		task = null;
		patch = null;
	}
}


/* Binary File Format

Meaning			Offset	Type		Example (Hex)				Example Value
BIN_TOKEN		0000	UInt32		0D F0 0D 60					(0x600DF00D)
BIN_VERSION		0004	UInt32		0D 00 00 00					(13)
West			0008	Double		00 00 00 00 00 80 66 C0		(-180)
East			0010	Double		00 00 00 00 00 80 66 40		(180)
North			0018	Double		00 00 00 00 00 00 42 40		(36)
South			0020	Double		00 00 00 00 00 00 42 C0		(-36)
CategoryCount	0028	Int32		00 00 00 00					(0)
Min Value		002C	Float		00 00 00 00					(0)
Max Value		0030	Float		00 88 18 46					(9762)
Count X			0034	Int32		60 03 00 00					(864)
Count Y			0038	Int32		AD 00 00 00					(173)
Units			003C	String		00 00 00 00					("")
Coloring		----	UInt8		00							(0)

---Metadata---
Count					Int32		00 00 00 01					(1)
{
	Key					String		...
	Value				String		...
}
---Metadata---

---Categories---
{
	Name				String		...
	Value				Int32		00 00 00 00					(0)
}
---Categories---

---Values---
{
	Value				Float		00 00 00 00					(0)
}
---Values---

---Mask---
HasMask					Bool		00 00 00 01					(true)
{
	Value				UInt8		00 00 00 00					(0)	
}
---Mask---

---Distribution---
Size					UInt8		01							(1)
{
	Value				UInt32		00 00 00 00					(0)
}
---Distribution---

*/

public static class PatchDataIO
{
	public static readonly uint BIN_TOKEN = 0x600DF00D;
	public static readonly uint BIN_VERSION = 13;


#if UNITY_WEBGL
    public static BinaryReader brHeaders = null;

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

	public static bool CheckBinVersion(string filename)
	{
		using (var br = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
		{
			return CheckBinVersion(br);
		}
	}

	public static bool CheckBinVersion(BinaryReader br)
	{
		return br.ReadUInt32() == BIN_TOKEN && br.ReadUInt32() == BIN_VERSION;
	}

	public static void SkipBinVersion(BinaryReader br)
	{
		br.ReadUInt32();
		br.ReadUInt32();
	}

	public static void WriteBinVersion(BinaryWriter bw)
	{
		bw.Write(BIN_TOKEN);
		bw.Write(BIN_VERSION);
	}

	public static void WriteBinBoundsHeader(BinaryWriter bw, PatchData patch)
	{
		bw.Write(patch.west);
		bw.Write(patch.east);
		bw.Write(patch.north);
		bw.Write(patch.south);
	}

	public static PatchData ReadBinBoundsHeader(BinaryReader br, string filename, PatchData patch)
	{
		patch.west = br.ReadDouble();
		patch.east = br.ReadDouble();
		patch.north = br.ReadDouble();
		patch.south = br.ReadDouble();

        if (/*patch.west < GeoCalculator.MinLongitude ||
            patch.east > GeoCalculator.MaxLongitude ||*/
            patch.north > GeoCalculator.MaxLatitude ||
            patch.south < GeoCalculator.MinLatitude)
            UnityEngine.Debug.LogWarning("File " + filename + " has bounds beyond limits (W,E,N,S): " + patch.west + ", " + patch.east + ", " + patch.north + ", " + patch.south);

        return patch;
	}

	public static void WriteBinMetadata(BinaryWriter bw, List<MetadataPair> metadata)
	{
		// Write Metadata (if available)
		if (metadata == null || metadata.Count == 0)
		{
			bw.Write(0);
			return;
		}

		bw.Write(metadata.Count);
		foreach (var row in metadata)
		{
			bw.Write(row.Key);
			bw.Write(row.Value);
		}
	}

	public static List<MetadataPair> ReadBinMetadata(BinaryReader br)
	{
		// Read Metadata (if available)
		int count = br.ReadInt32();
		if (count == 0)
			return null;

		var metadata = new List<MetadataPair>();
		for (int i = 0; i < count; i++)
		{
			var key = br.ReadString();
			var value = br.ReadString();
			metadata.Add(key, value);
		}
		
		return metadata;
	}

	public static List<MetadataPair> ReadCsvMetadata(StreamReader sr, string[] csvTokens, ref string line)
	{
		var metadata = new List<MetadataPair>();

		while ((line = sr.ReadLine()) != null)
		{
			var matches = CsvHelper.regex.Matches(line);
			var key = matches[0].Groups[2].Value;
			if (key.IsIn(csvTokens))
			{
				break;
			}

			metadata.Add(key, matches[1].Groups[2].Value);
		}

		if (metadata.Count == 0)
			return null;

		return metadata;
	}

	public static void ReadArray(BinaryReader br, ref byte[] bytes, Array arr, int elementBytes = 1)
	{
		int readSize = arr.Length * elementBytes;
		if (readSize > 0)
		{
			if (bytes == null || bytes.Length < readSize)
			{
				bytes = new byte[readSize];
			}

			int read;
			if ((read = br.Read(bytes, 0, readSize)) != readSize)
			{
				UnityEngine.Debug.LogError("Couldn't read all values. Expected (" + arr.Length + "), Read(" + (read / elementBytes) + ")");
				readSize = read;
			}
			Buffer.BlockCopy(bytes, 0, arr, 0, readSize);
		}
	}


	private static readonly object _lock = new object();
	private static bool blockParser = true;
	private static Thread parseThread = null;
	private static volatile ParseTaskData taskData = new ParseTaskData();

	public static IEnumerator ParseAsync<T>(StreamReader sr, string filename, ParseTask task, PatchDataLoadedCallback<T> callback) where T : PatchData
	{
		if (parseThread == null)
		{
			taskData = new ParseTaskData();

			parseThread = new Thread(ParseThread)
			{
				Name = "ParseThread",
				Priority = ThreadPriority.Highest,
			};
			parseThread.Start();
		}

		lock (_lock)
		{
			taskData.sr = sr;
			taskData.filename = filename;
			taskData.task = task;

			blockParser = false;
			Monitor.Pulse(_lock);
		}

		while (!blockParser)
		{
			yield return IEnumeratorExtensions.AvoidRunThru;
		}

		callback(taskData.patch as T);

		// Clear task data
		taskData.Clear();
	}

	public static void StopParsingThread()
	{
        if (parseThread != null)
        {
            lock (_lock)
            {
                taskData = null;
                blockParser = false;
                Monitor.Pulse(_lock);
            }
        }
    }

    private static void ParseThread()
	{
		while (true)
		{
			lock (_lock)
			{
				while (blockParser) Monitor.Wait(_lock);
				if (taskData == null)
					break;

				try
				{
					taskData.Parse();
				}
				catch (Exception e)
				{
					UnityEngine.Debug.LogError("Exception thrown file parsing " + taskData.filename);
					UnityEngine.Debug.LogException(e);
				}

				blockParser = true;
			}
		}
		parseThread = null;
	}

}
