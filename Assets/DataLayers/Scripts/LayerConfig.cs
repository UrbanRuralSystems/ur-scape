// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;


public static class LayerConfig
{
	public static readonly string Filename = "layers.csv";

	public delegate void OnConfigLoadedCallback(List<LayerGroup> groups);

	public static IEnumerator Load(OnConfigLoadedCallback callback)
	{
		return Load(Paths.Data + Filename, callback);
	}

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

		var groupNames = new Dictionary<string, LayerGroup>(StringComparer.CurrentCultureIgnoreCase);
		var layerNames = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);

		var dataManager = ComponentManager.Instance.Get<DataManager>();

		// Read one line at a time
		while ((line = sr.ReadLine()) != null)
        {
            string[] cells = line.Split(',');
            if (!string.IsNullOrEmpty(cells[0]))
            {
				string name = cells[1].Trim();

				if (cells[0].Equals("Layer"))
				{
					if (layerNames.Contains(name))
					{
						Debug.LogError(name + " layer already exists. Looks like a duplicate line in " + Filename);
					}
					else
					{
						new DataLayer(dataManager, name, ReadColorCells(cells, Color.gray), group);
						layerNames.Add(name);
					}
				}
				else if (cells[0].Equals("Group"))
                {
					if (groups.Count > 0 || groupNames.Count > 0 || group.layers.Count > 0)
						groups.Add(group);

					if (groupNames.ContainsKey(name))
					{
						Debug.LogError(name + " group already exists. Looks like a duplicate line in " + Filename);
						group = groupNames[name];
					}
					else
					{
						group = new LayerGroup(name);
						groupNames.Add(name, group);
					}
				}
				else
				{
					Debug.LogWarning("Layer type " + cells[0] + " is not supported");
					continue;
				}
			}
		}

		if (groups.Count > 0 || groupNames.Count > 0 || group.layers.Count > 0)
			groups.Add(group);

        return groups;
    }

    private static Color ReadColorCells(string[] cells, Color _default)
    {
        if (int.TryParse(cells[2], out int r) &&
            int.TryParse(cells[3], out int g) &&
            int.TryParse(cells[4], out int b))
        {
            return ColorExtensions.FromRGB(r, g, b);
        }

        return _default;
    }

#if !UNITY_WEBGL
	public static void Save(List<LayerGroup> groups)
	{
		Save(Paths.Data + Filename, groups);
	}

	public static void Save(string filename, List<LayerGroup> groups)
	{
		using (var sw = new StreamWriter(filename, false, Encoding.UTF8))
		{
			sw.WriteLine("Type,Name,Color (red),Color (green),Color (blue)");
			foreach (var group in groups)
			{
				sw.WriteLine("Group," + group.name + ",,,");
				foreach (var layer in group.layers)
				{
					Color32 c = layer.Color;
					sw.WriteLine("Layer," + layer.Name + "," + c.r + "," + c.g + "," + c.b);
				}
				sw.WriteLine(",,,,");
			}
		}
	}
#endif
}
