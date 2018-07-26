// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using ExtensionMethods;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public static class LayerConfig
{
    public delegate void OnConfigLoadedCallback(List<LayerGroup> groups);

    public static IEnumerator Load(string filename, OnConfigLoadedCallback callback)
    {
        yield return FileRequest.GetText(filename, (sr) => callback(Parse(sr)));
    }

    private static List<LayerGroup> Parse(StreamReader sr)
    {
        List<LayerGroup> groups = new List<LayerGroup>();

        // Create a default group in case layers are created without a group
        LayerGroup group = new LayerGroup("No Name");

        // Read/skip header
        string line = sr.ReadLine();

        int id = 0;

        // Read one line at a time
        Color groupColor = Color.gray;
        while ((line = sr.ReadLine()) != null)
        {
            string[] cells = line.Split(',');
            if (!string.IsNullOrEmpty(cells[0]))
            {
                if (cells[0].Equals("Group"))
                {
                    if (group.layers.Count > 0)
                    {
                        groups.Add(group);
                    }

                    group = new LayerGroup(cells[1].Trim());
                    groupColor = ReadColorCells(cells, Color.gray);
                }
				else if (cells[0].Equals("Layer"))
				{
                    string name = cells[1].Trim();
					DataLayer layer = new DataLayer(name, ReadColorCells(cells, groupColor), id++);
                    group.layers.Add(layer);
                }
				else
				{
					Debug.LogWarning("Layer type " + cells[0] + " is not supported");
					continue;
				}
			}
		}

        if (group.layers.Count > 0)
        {
            groups.Add(group);
        }

        return groups;
    }

    private static Color ReadColorCells(string[] cells, Color _default)
    {
        int r, g, b;
        if (int.TryParse(cells[2], out r) &&
            int.TryParse(cells[3], out g) &&
            int.TryParse(cells[4], out b))
        {
            return ColorExtensions.FromRGB(r, g, b);
        }

        return _default;
    }
}
