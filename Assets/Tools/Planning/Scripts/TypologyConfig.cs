// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//          Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using System.Collections.Generic;
using System.IO;

public static class TypologyConfig
{
	private const int FirstAttributeColumn = 7;

    public delegate void OnConfigLoadedCallbackDic(List<Typology> Typologies);

	public static IEnumerator Load(string filename, OnConfigLoadedCallbackDic callback)
    {
        yield return FileRequest.GetText(filename, (sr) => callback(Parse(sr)));
    }

    private static List<Typology> Parse(StreamReader sr)
    {
        List<Typology> typologies = new List<Typology>();

        // Parse header
        string line = sr.ReadLine();
		var matches = CsvHelper.regex.Matches(line);
        string[] names = new string[matches.Count];

		Dictionary<string, TypologyInfo> infos = new Dictionary<string, TypologyInfo>();

		// Separate name and unit
		for (int i = 0; i < matches.Count; ++i)
        {
            string[] toSplit = matches[i].Groups[2].Value.Split('|');
            names[i] = toSplit[0].Trim();

            if (i >= FirstAttributeColumn && !infos.ContainsKey(names[i]))
			{
                if (names[i][0].Equals('_'))
                    continue;

				if (toSplit.Length > 1)
				{
					var units = toSplit[1].Trim();
					infos.Add(names[i], new TypologyInfo {
						units = (units == "/") ? "" : units,
						isRatio = units == "/"//"%"
					});
				}
				else
					infos.Add(names[i], new TypologyInfo { units = "N/A" });
			}
		}

        while ((line = sr.ReadLine()) != null)
        {
			matches = CsvHelper.regex.Matches(line);
			string typologyName = matches[0].Groups[2].Value;

			if (string.IsNullOrEmpty(typologyName))
                continue;

			Typology typology = new Typology
			{
				name = typologyName,
				description = matches[2].Groups[2].Value.Trim(),
				author = matches[3].Groups[2].Value.Trim(),
				color = ColorExtensions.FromRGB(int.Parse(matches[4].Groups[2].Value), int.Parse(matches[5].Groups[2].Value), int.Parse(matches[6].Groups[2].Value)),
				values = new Dictionary<string, float>()
			};

			string sites = matches[1].Groups[2].Value;
			if (!string.IsNullOrWhiteSpace(sites))
				typology.sites = new HashSet<string>(sites.Trim().Split(','));

			// Add a value for each column (data layer)
			for (int i = FirstAttributeColumn; i < matches.Count; ++i)
            {
				if (float.TryParse(matches[i].Groups[2].Value, out float value))
				{
					if (names[i][0].Equals('_'))
                        continue;

					typology.values.Add(names[i], value);
				}
			}
			typologies.Add(typology);
        }

		Typology.info = infos;

		return typologies;
    }
}
