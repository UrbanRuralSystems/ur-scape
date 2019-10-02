// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;
using System.IO;

public class Site
{
	public static readonly AreaBounds MaxBounds = new AreaBounds(float.MaxValue, float.MinValue, float.MinValue, float.MaxValue);

	public string Name { get; private set; }
	private AreaBounds bounds;
	public AreaBounds Bounds { get => bounds; }
	public readonly List<DataLayer> layers;

	public Site(string name) : this(name, new List<DataLayer>())
	{
	}

	public Site(string name, List<DataLayer> layers)
	{
		Name = name;
		this.layers = layers;
		RecalculateBounds();
	}

	public void ChangeName(string newName)
	{
		if (newName == Name)
			return;

		Name = newName;

		var siteDir = CreateDirectory();

		foreach (var layer in layers)
		{
			foreach (var level in layer.levels)
			{
				foreach (var layerSite in level.layerSites)
				{
					if (layerSite.Site == this)
					{
						foreach (var record in layerSite.records)
						{
							foreach (var patch in record.Value.patches)
							{
								patch.ChangeSiteName(newName, siteDir);
							}
						}
						break;
					}
				}
			}
		}
	}

	public bool HasDataLayer(DataLayer layer)
	{
		return layers.Contains(layer);
	}

	public void AddLayer(DataLayer layer, bool updateBounds = true)
	{
		if (layer != null && !layers.Contains(layer))
		{
			layers.Add(layer);

			if (updateBounds)
				AddToBounds(layer);
		}
	}

	public void RemoveLayer(DataLayer layer, bool updateBounds = true)
	{
		if (layers.Remove(layer))
		{
			if (updateBounds)
				RecalculateBounds();
		}
	}

	public void PatchWasAdded(Patch patch)
	{
		AddToBounds(patch.Data);
		AddLayer(patch.DataLayer, false);
	}

	public void RecalculateBounds()
	{
		bounds = MaxBounds;
		foreach (var layer in layers)
		{
			AddToBounds(layer);
		}
	}

	public string GetDirectory()
	{
		return Path.Combine(Paths.Sites, Name);
	}

#if !UNITY_WEBGL
	public List<string> GetFiles(List<string> directories)
	{
		var files = new List<string>();

		foreach (var layer in layers)
		{
			files.AddRange(layer.GetPatchFiles(directories, Name));
		}

		return files;
	}
#endif

	public void MoveLayerToSite(DataLayer layer, Site site)
	{
		RemoveLayer(layer);

		// Create the site directory (just in case it's a new site)
		site.CreateDirectory();

		// Update all necessary layerSites
		foreach (var level in layer.levels)
		{
			foreach (var layerSite in level.layerSites)
			{
				if (layerSite.Site == this)
				{
					layerSite.ChangeSite(site);
					break;
				}
			}
		}

		// Add layer to site AFTER updating the layerSites and their patches
		site.AddLayer(layer);
	}


	//
	// Private Methods
	//

	private void AddToBounds(DataLayer layer)
	{
		foreach (var level in layer.levels)
		{
			foreach (var layerSite in level.layerSites)
			{
				if (layerSite.Site == this)
				{
					foreach (var record in layerSite.records)
					{
						foreach (var patch in record.Value.patches)
						{
							AddToBounds(patch.Data);
						}
					}
				}
			}
		}
	}

	private void AddToBounds(PatchData data)
	{
		bounds.east = Math.Max(bounds.east, data.east);
		bounds.west = Math.Min(bounds.west, data.west);
		bounds.north = Math.Max(bounds.north, data.north);
		bounds.south = Math.Min(bounds.south, data.south);
	}

	private string CreateDirectory()
	{
		var dir = GetDirectory();
#if !UNITY_WEBGL
		Directory.CreateDirectory(dir);
#endif
		return dir;
	}

}
