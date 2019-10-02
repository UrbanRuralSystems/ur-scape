// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CitationsPanel : MonoBehaviour
{
	[Header("Prefabs")]
	public InputField citationPrefab;

	private CitationsManager citationsManager;
	private Dictionary<Patch, Citation> patches = new Dictionary<Patch, Citation>();
	private Dictionary<Citation, CitationInfo> citations = new Dictionary<Citation, CitationInfo>();

	private class CitationInfo
	{
		public int count = 1;
		public readonly GameObject go;
		public CitationInfo(GameObject go)
		{
			this.go = go;
		}
	}

	//
	// Unity Methods
	//

	private void Start()
	{
		ComponentManager.Instance.OnRegistrationFinished += OnRegistrationFinished;
	}

	private void OnDestroy()
	{
		if (ComponentManager.Instance.Has<DataLayers>())
		{
			var dataLayers = ComponentManager.Instance.Get<DataLayers>();
			dataLayers.OnLayerVisibilityChange -= OnDataLayerVisibilityChange;
		}
	}

	//
	// Event Methods
	//

	private void OnRegistrationFinished()
	{
		var componentManager = ComponentManager.Instance;

		citationsManager = componentManager.Get<CitationsManager>();

		var dataLayers = componentManager.Get<DataLayers>();
		dataLayers.OnLayerVisibilityChange += OnDataLayerVisibilityChange;
	}

	private void OnDataLayerVisibilityChange(DataLayer layer, bool visible)
	{
		if (visible)
		{
			layer.OnPatchVisibilityChange += OnPatchVisibilityChange;
		}
		else
		{
			layer.OnPatchVisibilityChange -= OnPatchVisibilityChange;
		}
	}

	private void OnPatchVisibilityChange(DataLayer dataLayer, Patch patch, bool visible)
	{
		if (visible)
		{
			if (citationsManager.TryGet(patch, out Citation citation) && citation.isMandatory)
			{
				patches.Add(patch, citation);

				if (citations.TryGetValue(citation, out CitationInfo info))
				{
					info.count++;
				}
				else
				{
					var uiCitation = Instantiate(citationPrefab);
					uiCitation.text = "<b>" + patch.DataLayer.Name + ":</b>  " + citation.text;
					uiCitation.transform.SetParent(transform, false);

					citations.Add(citation, new CitationInfo(uiCitation.gameObject));
				}
			}
		}
		else
		{
			if (patches.TryGetValue(patch, out Citation citation))
			{
				patches.Remove(patch);

				var info = citations[citation];
				info.count--;
				if (info.count == 0)
				{
					citations.Remove(citation);
					Destroy(info.go);
				}
			}
		}

		GuiUtils.RebuildLayout(transform);
	}
	
}
