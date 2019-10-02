// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//			Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public static class SurveyConfig
{
    public delegate void OnConfigLoadedCallbackDic(WordCloudDatabase database);

    public static IEnumerator Load(string filename, OnConfigLoadedCallbackDic callback)
    {
        yield return FileRequest.GetText(filename, (stream) => callback(Parse(stream, filename)));
    }

	private static WordCloudDatabase Parse(StreamReader sr, string filename)
	{
		WordCloudDatabase database2 = new WordCloudDatabase();

		int LatColumn = -1;
		int LongColumn = -1;
		int range = 5;
		string marker = "*";
		System.Random rnd = new System.Random(1);

		string line = sr.ReadLine();
		MatchCollection matches = CsvHelper.regex.Matches(line);

		int headerCount = matches.Count;
		WordCloudTopic[] topics = new WordCloudTopic[headerCount];
		for (int i = 0; i < headerCount; ++i)
		{
			string headerCell = matches[i].Groups[2].Value.Trim();
			bool ignore = headerCell.StartsWith(marker);

			if (ignore)
				headerCell = headerCell.Substring(1);

			// Check for column position of Latitude and Longtitude
			if (headerCell.EqualsIgnoreCase("latitude")
				|| headerCell.EqualsIgnoreCase("lat")
				|| headerCell.EqualsIgnoreCase("USULAN_LAT"))
			{
				LatColumn = i;
			}
			else if (headerCell.EqualsIgnoreCase("longitude")
				|| headerCell.EqualsIgnoreCase("long")
				|| headerCell.EqualsIgnoreCase("lng")
				|| headerCell.EqualsIgnoreCase("USULAN_LONG"))
			{
				LongColumn = i;
			}
			else if (!ignore)
			{
				var topic = new WordCloudTopic();
				topic.name = headerCell;

				topics[i] = topic;
				database2.topics.Add(topic);
			}
		}

		if (LatColumn == -1 || LongColumn == -1)
		{
			Debug.LogError("File " + filename + " doesn't have longitude/latitude headers");
			return null;
		}

		while ((line = sr.ReadLine()) != null)
		{
			matches = CsvHelper.regex.Matches(line);

			while (matches.Count < headerCount)
			{
				string extraLine = sr.ReadLine();

				if (extraLine == null)
					break;

				line += extraLine;
				matches = CsvHelper.regex.Matches(line);
			}

			if (string.IsNullOrEmpty(matches[0].Groups[2].Value))
				continue;

			double latitude = 0;
			double longtitude = 0;
			if (!double.TryParse(matches[LatColumn].Groups[2].Value, out latitude) ||
				!double.TryParse(matches[LongColumn].Groups[2].Value, out longtitude))
			{
				continue;
			}

			double randomLat = rnd.Next(-range, range) * 0.0001;
			double randomLong = rnd.Next(-range, range) * 0.0001;

			int count = matches.Count;
			for (int i = 0; i < count; ++i)
			{
				var topic = topics[i];
				if (topic == null)
					continue;

				// split if multiple answers in survey separated by ',' or by new line
				var cell = matches[i].Groups[2].Value;
				var item = cell.Split(new string[] { "\n", "\r\n", ",", "#" }, System.StringSplitOptions.RemoveEmptyEntries);
				for (int j = 0; j < item.Length; ++j)
				{
					var text = item[j].Trim();
					if (!string.IsNullOrEmpty(text))
					{
						TopicItem topicItem = new TopicItem();

						// Randomize position to protect privacy and avoid same position
						topicItem.coord.Latitude = latitude + randomLat;
						topicItem.coord.Longitude = longtitude + randomLong;

						topicItem.stringLabel = text;
						topicItem.size = 1;
						topic.topicItems.Add(topicItem);
					}
				}
			}
		}

		// Assing id based on string similarity
		Dictionary<string, int> idCheck = new Dictionary<string, int>();
		foreach (var topic in database2.topics)
        {
            int id = 0;
            foreach (var dataItemA in topic.topicItems)
            {
                string idHelp = dataItemA.stringLabel;
            
                if (idCheck.ContainsKey(idHelp))
                {
                    dataItemA.stringId = idCheck[idHelp];
                }
                else
                {
                    dataItemA.stringId = ++id;
                    idCheck.Add(idHelp, dataItemA.stringId);
                }
            }
        }

        return database2;
    }

}
