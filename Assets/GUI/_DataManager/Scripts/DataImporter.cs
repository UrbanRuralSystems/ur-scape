// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//
// This MonoBehaviour will be added to a GameObject via code and
// it is not intended to be added to a GameObject by a user
// in the Unity editor.

using System.Collections;
using System.IO;
using System.Threading;
using UnityEngine;

[AddComponentMenu("")] // Hide this class from the "Add Components" list
public class DataImporter : MonoBehaviour
{
	public void Import(ImportInfo info)
	{
		StartCoroutine(_Import(info));
	}

	private IEnumerator _Import(ImportInfo info)
	{
		var componentManager = ComponentManager.Instance;
		var dialogManager = componentManager.Get<ModalDialogManager>();
		var dataManager = componentManager.Get<DataManager>();
		var dataLayers = componentManager.Get<DataLayers>();

		bool addedGroupOrLayer = false;

		// Create group if necessary (also UI panel)
		DataLayerGroupPanel groupPanel;
		var group = info.group;
		if (group == null)
		{
			group = dataManager.AddLayerGroup(info.newGroupName);
			groupPanel = dataLayers.AddLayerGroup(group.name);
			addedGroupOrLayer = true;
		}
		else
		{
			groupPanel = dataLayers.GetLayerGroup(group.name);
		}

		// Create layer if necessary (also UI panel)
		var layer = info.layer;
		if (layer == null)
		{
			var layerName = info.newLayerName;
			layer = group.GetLayer(layerName);
			if (layer == null)
			{
				layer = new DataLayer(dataManager, layerName, info.newLayerColor, group);
				dataLayers.AddLayer(layer, groupPanel);
				addedGroupOrLayer = true;
			}
		}

		if (layer.Group != group)
		{
			Debug.LogError("Selected group doesn't match selected layer's group");
			yield break;
		}

		if (addedGroupOrLayer)
		{
			dataManager.UpdateLayerConfig();
		}

		var filename = info.inputFilename;
		var resX = info.resolution.ToDegrees();
		var resY = resX;

		Resampler resampler = null;
		if (info.needsResampling)
		{
			//+ TODO: create appropriate resampler 
			resampler = new NearestNeighbourResampler();
		}

		// Create output directory
		Directory.CreateDirectory(Path.GetDirectoryName(info.outputFilename));

		bool patchAlreadyExists = File.Exists(info.outputFilename);

		// Show progress bar
		var progressDialog = dialogManager.NewProgressDialog();
		progressDialog.SetMessage(Translator.Get("Importing") + " " + Path.GetFileName(filename));
		progressDialog.SetProgress(0);

		var threadInfo = new ImportThreadInfo
		{
			inputFilename = filename,
			outputFilename = info.outputFilename,
			resX = resX,
			resY = resY,
			units = info.units,
			metadata = info.metadata,
			categories = info.categories,	//?
			resampler = resampler,
			running = true
		};

		threadInfo.thread = new Thread(() => ImportThread(threadInfo))
		{
			Name = "ImportDataThread"
		};
		threadInfo.thread.Start();

		// Import data
		do
		{
			yield return null;
			progressDialog.SetProgress(threadInfo.progress.Total());
		}
		while (threadInfo.running);

		yield return null;

		if (patchAlreadyExists)
		{
			dataManager.ReloadPatches(layer, info.site.Name, info.level, info.year);
		}
		else
		{
			yield return layer.CreatePatch(info.outputFilename);
		}

		yield return null;

		// Update Site Browser
		var siteBrowser = componentManager.Get<SiteBrowser>();
		if (info.site != null && info.site == siteBrowser.ActiveSite)
		{
			siteBrowser.UpdateMinMaxLevels();
		}
		else
		{
			// If it's the first site, it will change to the new site automatically
			siteBrowser.RebuildList();
		}

		yield return null;

		// Update the Data Layers list
		componentManager.Get<DataLayers>().UpdateLayers();

		yield return null;

		// Hide the progress
		progressDialog.Close();

		// Send the event that the import process has finished
		var site = info.site;
		if (site == null)
			dataManager.TryGetSite(info.newSiteName, out site);
		info.OnFinishImport.Invoke(site, layer);

		// Destroy this (the importer) game object
		Destroy(gameObject);
	}

	private static void ImportThread(ImportThreadInfo info)
	{
		var interpreter = Interpreter.Get(info.inputFilename);
		if (interpreter != null)
		{
			if (interpreter.Read(info.inputFilename, info.progress.Push(0.8f), out PatchData data))
			{
				info.progress.Pop();

				data.metadata = info.metadata;

				if (data is GridData grid)
				{
					if (info.resampler != null)
						info.resampler.Convert(grid, info.resX, info.resY);

					info.progress.value = 0.9f;

					grid.UpdateMinMaxValues();
					grid.UpdateDistribution();
					grid.categories = info.categories;	//?

					info.progress.value = 0.95f;

					grid.SaveBin(info.outputFilename);
				}
			}
		}

		info.progress.value = 1f;
		info.running = false;
		info.thread = null;
	}
}
