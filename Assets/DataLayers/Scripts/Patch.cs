// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
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
using System;

public enum PatchDataFormat
{
    CSV,
    BIN
}

// Callback executed after a patch request has finished
public delegate void PatchLoadRequestCallback(Patch patch, bool isInView);

// Callback executed after a patch has been loaded
public delegate void PatchLoadedCallback(Patch patch);

// Delegate for creating a Patch
public delegate P CreatePatch<P, D>(DataLayer dataLayer, int level, int year, D data, string filename) where P : Patch where D : PatchData;

public abstract class Patch
{
    public const string CSV_EXTENSION = "csv";
    public const string BIN_EXTENSION = "bin";

    public int Level { get; }
	public int Year { get; private set; }
	public string Filename { get; private set; }

	// Link to parent SiteRecord
	public SiteRecord SiteRecord { get; private set; }
	// Link to grand-grand-grand-parent DataLayer
	public DataLayer DataLayer { get; }

    public abstract PatchData Data { get; }

	protected MapLayer mapLayer;


	public Patch(DataLayer dataLayer, int level, int year, string filename)
    {
		DataLayer = dataLayer;
        Level = level;
		Year = year;
        Filename = filename;
    }

	public void SetSiteRecord(SiteRecord siteRecord)
	{
		SiteRecord = siteRecord;
	}

	public void ChangeSiteName(string newSiteName, string siteDir)
	{
		UpdateFilenameSite(newSiteName, siteDir);
	}

	public void ChangeYear(int year)
	{
		Year = year;
		UpdateFilenameYear(year);
	}

	public void RenameFile(string newFilename)
	{
#if !UNITY_WEBGL
		IOUtils.SafeMove(Filename, newFilename);
		if (Filename.EndsWith(BIN_EXTENSION))
		{
			var oldBinFile = Path.ChangeExtension(Filename, CSV_EXTENSION);
			var newBinFile = Path.ChangeExtension(newFilename, CSV_EXTENSION);
			IOUtils.SafeMove(oldBinFile, newBinFile);
		}
#endif
		SetFilename(newFilename);
	}

	public string GetSiteName()
	{
		return SiteRecord.layerSite.Site.Name;
	}

	public virtual void SetMapLayer(MapLayer mapLayer)
    {
        this.mapLayer = mapLayer;
    }

	public virtual MapLayer GetMapLayer()
    {
        return mapLayer;
    }

    public virtual bool IsVisible()
    {
        return mapLayer != null;
    }

    public static IEnumerator Create<P, D>(DataLayer dataLayer, string file, CreatePatch<P, D> create, GetPatchLoader<P, D> getLoader, Action<Patch, string> callback)
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

        SplitFileName(file, out int level, out string site, out int patch, out int year, out int month, out int day, out string type);

        // Create the patch based on the file name
        P newPatch = create(dataLayer, level, year, data, file);

#if !UNITY_WEBGL
        // Create binary file if data was loaded from different format
        if (format != PatchDataFormat.BIN)
        {
			newPatch.Save(binFile);
			newPatch.UnloadData();
		}
#endif

		callback(newPatch, site);

#if !UNITY_WEBGL
		// Take a break (wait for next frame) since loading CSVs is quite slow
		if (format != PatchDataFormat.BIN)
			yield return IEnumeratorExtensions.AvoidRunThru;
#endif
	}

	public abstract IEnumerator LoadData(PatchLoadedCallback callback);
    public abstract void UnloadData();
    public abstract void Save(string filename, PatchDataFormat format = PatchDataFormat.BIN);

	protected void SetFilename(string filename)
	{
		Filename = filename;
	}

	private void UpdateFilenameSite(string newSiteName, string siteDir)
	{
		if (Filename == null)
			return;

		var fi = new FileInfo(Filename);

		string layerName = SplitFileName(fi.Name, out int level, out string siteName, out int patch, out int year, out int month, out int day, out string type);
		string newFilename = GetFileName(layerName, level, newSiteName, patch, year, month, day, type);

		RenameFile(Path.Combine(siteDir, newFilename + fi.Extension));
	}

	private void UpdateFilenameYear(int newYear)
	{
		if (Filename == null)
			return;

		var fi = new FileInfo(Filename);

		string layerName = SplitFileName(fi.Name, out int level, out string siteName, out int patch, out int year, out int month, out int day, out string type);
		string newFilename = GetFileName(layerName, level, siteName, patch, newYear, month, day, type);

		RenameFile(Path.Combine(fi.DirectoryName, newFilename + fi.Extension));
	}


	//
	// Static helpers
	//

	public static string GetFileName(string layer, int level, string site, int patch, int year, string type)
	{
		return GetFileName(layer, level, site, patch, year, 0, 0, type);
	}

	public static string GetFileName(string layer, int level, string site, int patch, int year, int month, string type)
	{
		return GetFileName(layer, level, site, patch, year, month, 0, type);
	}

	public static string GetFileName(string layer, int level, string site, int patch, int year, int month, int day, string type)
	{
		string date = year.ToString("D4");
		if (month > 0)
		{
			date += month.ToString("D2");
			if (day > 0)
				date += day.ToString("D2");
		}
		return layer + "_" + (char)('A' + level) + "_" + site + "@" + patch + "_" + date + "_" + type;
	}

	public static string SplitFileName(string file, out int level, out string site, out int patch, out int year, out int month, out int day, out string type)
    {
        string[] parts = Path.GetFileNameWithoutExtension(file).Split('_');

		year = 0;
		month = 0;
		day = 0;

#if SAFETY_CHECK
		if (parts.Length < 5)
        {
            Debug.LogError("Found file with wrong name: " + file);
            level = -1;
            site = "";
            patch = 0;
			type = "";
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

		try
		{
			var dateLength = parts[3].Length;
			if (dateLength == 2)
			{
				year = int.Parse(parts[3]);
				year = year > 50 ? 1900 + year : 2000 + year;
			}
			else if (dateLength == 4)
			{
				year = int.Parse(parts[3]);
			}
			else if (dateLength == 6)
			{
				year = int.Parse(parts[3].Substring(0, 4));
				month = int.Parse(parts[3].Substring(4, 2));
			}
			else if (dateLength == 8)
			{
				year = int.Parse(parts[3].Substring(0, 4));
				month = int.Parse(parts[3].Substring(4, 2));
				day = int.Parse(parts[3].Substring(6, 2));
			}
			else
			{
				Debug.LogError("Invalid year found in filename: " + file);
			}
		}
		catch (Exception)
		{
			Debug.LogError("Invalid year found in filename: " + file);
		}

		type = parts[4];

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

	public static string GetFileNameLayer(string file)
	{
		string[] parts = Path.GetFileNameWithoutExtension(file).Split('_');

#if SAFETY_CHECK
		if (parts.Length < 5)
		{
			Debug.LogError("Found file with wrong name: " + file);
			return null;
		}
#endif

		return parts[0];
	}

	public static string GetFileNameSite(string file)
	{
		string[] parts = Path.GetFileNameWithoutExtension(file).Split('_');

#if SAFETY_CHECK
		if (parts.Length < 5)
		{
			Debug.LogError("Found file with wrong name: " + file);
			return null;
		}
#endif

		var site = parts[2];
		int index = site.IndexOf('@');
		if (index > 0)
			site = site.Substring(0, index);

		return site;
	}

	static IEnumerable<string> Split(string str, int chunkSize)
    {
        return System.Linq.Enumerable.Range(0, str.Length / chunkSize)
            .Select(i => str.Substring(i * chunkSize, chunkSize));
    }

}
