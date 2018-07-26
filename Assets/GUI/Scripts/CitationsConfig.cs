// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//          Michael Joos  (joos@arch.ethz.ch)

using ExtensionMethods;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class Citations
{
    public Dictionary<string, string> map = new Dictionary<string, string>();
    public HashSet<string> mandatory = new HashSet<string>();
}

public class CitationsConfig
{
    public delegate void OnConfigLoadedCallbackDic(Citations Citations);

	public static IEnumerator Load(string filename, OnConfigLoadedCallbackDic callback)
    {
        yield return FileRequest.GetText(filename, (sr) => callback(Parse(sr)));
    }

    private static Citations Parse(StreamReader sr)
    {
        Citations citations = new Citations();

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
				string key = layerName + "|" + source;

				if (!citations.map.ContainsKey(key))
                {
					string mandatory = matches[2].Groups[2].Value;
					string citation = matches[3].Groups[2].Value;

					citations.map.Add(key, citation);
					if (mandatory.EqualsIgnoreCase("true"))
						citations.mandatory.Add(key);
                }
            }
        }

        return citations;
	}
}
