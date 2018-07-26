// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

#if UNITY_EDITOR
#define SAFETY_CHECK
#endif

using System.Collections;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ExtensionMethods;

public enum PatchDataFormat
{
    CSV,
    BIN
}

// Callback executed after a patch has been loaded
public delegate void PatchLoadedCallback(Patch patch);

// Callback executed after a patch has been created
public delegate void PatchCreatedCallback(Patch patch);

// Delegate for creating a Patch
public delegate P CreatePatch<P, D>(DataLayer dataLayer, string site, int level, int year, D data, string filename) where P : Patch where D : PatchData;

public abstract class Patch
{
    public const string CSV_EXTENSION = "csv";
    public const string BIN_EXTENSION = "bin";

    public readonly DataLayer dataLayer;
	public SiteRecord siteRecord;
	public readonly string name;
    public readonly int level;
    public readonly int year;
    private string filename;
	public string Filename { get { return filename; } protected set { filename = value; } }

	protected MapLayer mapLayer;

    public abstract PatchData Data { get; }


    public Patch(DataLayer dataLayer, string name, int level, int year, string filename)
    {
		this.dataLayer = dataLayer;
		this.name = name;
        this.level = level;
        this.year = year;
        this.filename = filename;
    }

    public void SetMapLayer(MapLayer mapLayer)
    {
        this.mapLayer = mapLayer;
    }

    public MapLayer GetMapLayer()
    {
        return mapLayer;
    }

    public bool IsVisible()
    {
        return mapLayer != null;
    }

    public static IEnumerator Create<P, D>(DataLayer dataLayer, string file, CreatePatch<P, D> create, GetPatchLoader<P, D> getLoader, PatchCreatedCallback callback)
        where P : Patch
        where D: PatchData
    {
		// Assume we're getting a bin file
		PatchDataFormat format = PatchDataFormat.BIN;

#if !UNITY_WEBGL
		string binFile = file;

		// Check which format it is (Web only reads binary files)
		if (file.EndsWith(CSV_EXTENSION))
		{
			binFile = Path.ChangeExtension(file, BIN_EXTENSION);
			format = PatchDataFormat.CSV;
		}
#endif

		// Load the patch file (this only reads the (.bin) header, or the whole .csv)
		D data = null;
		var load = getLoader(format);
		yield return load(file, (g) => data = g);

        if (data == null)
		{
#if !UNITY_WEBGL
			// Take a break (wait for next frame) since loading CSVs is quite slow
			if (format != PatchDataFormat.BIN)
				yield return IEnumeratorExtensions.AvoidRunThru;
#endif
			yield break;
		}

		int level, patch, year;
        string site, type, subtype;
        SplitFileName(file, out level, out site, out patch, out type, out year, out subtype);

        // Create the patch based on the file name
        P newPatch = create(dataLayer, site, level, year, data, file);

#if !UNITY_WEBGL
        // Create binary file if data was loaded from different format
        if (format != PatchDataFormat.BIN)
        {
			newPatch.Save(binFile);
			newPatch.UnloadData();
		}
#endif

		callback(newPatch);

#if !UNITY_WEBGL
		// Take a break (wait for next frame) since loading CSVs is quite slow
		if (format != PatchDataFormat.BIN)
			yield return IEnumeratorExtensions.AvoidRunThru;
#endif
	}

	public abstract IEnumerator LoadData(PatchLoadedCallback callback);
    public abstract void UnloadData();
    public abstract void Save(string filename, PatchDataFormat format = PatchDataFormat.BIN);


	//
	// Static helpers
	//

	public static string SplitFileName(string file, out int level, out string site, out int patch, out string type, out int year, out string subtype)
    {
        string[] parts = Path.GetFileNameWithoutExtension(file).Split('_');
#if SAFETY_CHECK
        if (parts.Length < 5)
        {
            Debug.LogError("Found file with wrong name: " + file);
            level = -1;
            site = "";
            patch = 0;
            year = 0;
            type = "";
            subtype = "";
            return null;
        }
#endif
        level = parts[1][0] - 'A';
        site = parts[2];
        int index = site.IndexOf('@');
        if (index > 0)
        {
            patch = int.Parse(site.Substring(index + 1));
            site = site.Substring(0, index);
        }
        else
        {
            patch = 0;
        }

        if (parts[3].Length == 2)
        {
            year = int.Parse(parts[3]);
        }
        else
        {
            string[] dateTemp = Split(parts[3], 2).ToArray();
            year = int.Parse(dateTemp[0]);
            //int month = int.Parse(dateTemp[1]);
            //date = new DateTime(year > 50 ? 1900 + year : 2000 + year, month, 1);
        }
        year = year > 50 ? 1900 + year : 2000 + year;

        type = parts[4];
        subtype = (parts.Length == 6) ? parts[5] : null;

        return parts[0];
    }

    public static string GetFileNameType(string file)
    {
        string[] parts = Path.GetFileNameWithoutExtension(file).Split('_');
#if SAFETY_CHECK
        if (parts.Length < 5)
        {
            Debug.LogError("Found file with wrong name: " + file);
            return null;
        }
#endif
        return parts[4];
    }

    static IEnumerable<string> Split(string str, int chunkSize)
    {
        return System.Linq.Enumerable.Range(0, str.Length / chunkSize)
            .Select(i => str.Substring(i * chunkSize, chunkSize));
    }

}
