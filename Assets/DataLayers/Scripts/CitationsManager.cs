// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class Citation
{
	public readonly string text;
	public readonly bool isMandatory;

	public Citation(string text, bool isMandatory)
	{
		this.text = text;
		this.isMandatory = isMandatory;
	}
}

public class CitationsManager : UrsComponent
{
	public string citationsFile = "citations.csv";

	private readonly Dictionary<string, Citation> citations = new Dictionary<string, Citation>();


	//
	// Unity Methods
	//

	private void Start()
	{
		StartCoroutine(Load(Paths.Data + citationsFile));
	}


	//
	// Public Methods
	//

	public bool TryGet(Patch patch, out Citation citation)
	{
		if (patch.Data is GridData gridData && gridData.metadata != null)
		{
			if (gridData.metadata.TryGetValue("MandatoryCitation", out string mandatoryCitationStr))
			{
				citation = new Citation(mandatoryCitationStr, true);
				return true;
			}
			else if (gridData.metadata.TryGetValue("Citation", out string citationStr))
			{
				citation = new Citation(citationStr, false);
				return true;
			}
			else if (gridData.metadata.TryGetValue("Source", out string source))
				return citations.TryGetValue(GenerateKey(patch.DataLayer.Name, source), out citation);
		}

		citation = null;
		return false;
	}


	//
	// Private Methods
	//

	private IEnumerator Load(string filename)
	{
		yield return FileRequest.GetText(filename, (sr) => Parse(sr));
	}

	private void Parse(StreamReader sr)
	{
		// Skip header
		string line = sr.ReadLine();

		while ((line = sr.ReadLine()) != null)
		{
			MatchCollection matches = CsvHelper.regex.Matches(line);

			string layerName = matches[0].Groups[2].Value;
			if (string.IsNullOrEmpty(layerName))
				continue;

			string source = matches[1].Groups[2].Value;
			if (!string.IsNullOrEmpty(source))
			{
				string key = GenerateKey(layerName, source);

				if (!citations.ContainsKey(key))
				{
					string mandatory = matches[2].Groups[2].Value;
					string citation = matches[3].Groups[2].Value;

					citations.Add(key, new Citation(citation, mandatory.EqualsIgnoreCase("true")));
				}
			}
		}
	}

	private static string GenerateKey(string layerName, string source)
	{
		return layerName + "|" + source;
	}
}
