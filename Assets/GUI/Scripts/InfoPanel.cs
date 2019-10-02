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
using UnityEngine;
using UnityEngine.UI;

public class InfoPanel : UrsComponent
{
	[Header("UI References")]
	public RectTransform neck;
	public RectTransform panel;

	[Header("Prefabs")]
	public Text keyPrefab;
	public InputField valuePrefab;

	private MapViewArea mapView;
	private CitationsManager citationsManager;
	private ITranslator translator;
	private List<InfoRow> infoRows = new List<InfoRow>();
	private int currentRow = 0;
	private Image anchor;
	private Vector2 panelPos = Vector2.zero;
	private Coroutine delayedHideCoroutine;

	private class InfoRow
	{
		public readonly Text key;
		public readonly InputField value;

		public InfoRow(Text key, InputField value)
		{
			this.key = key;
			this.value = value;
		}
	}

	//
	// Unity Methods
	//

	private void Start()
	{
		ComponentManager.Instance.OnRegistrationFinished += OnRegistrationFinished;
		LocalizationManager.WaitAndRun(() => translator = LocalizationManager.Instance);

		panel.GetComponent<InfoLayout>().OnLayoutChange += OnLayoutChange;
		panel.GetComponent<HoverHandler>().OnHover += OnPanelHover;
		neck.GetComponent<HoverHandler>().OnHover += OnNeckHover;

		panelPos.x = panel.anchoredPosition.x;
	}

	//
	// Event Methods
	//

	private void OnRegistrationFinished()
	{
		var componentManager = ComponentManager.Instance;

		citationsManager = componentManager.Get<CitationsManager>();
		mapView = componentManager.Get<MapViewArea>();
	}

	private void OnPanelHover(bool isHovering)
	{
		if (!isHovering && !IsMouseInside(neck) && !IsMouseInside(anchor.GetComponent<RectTransform>()))
		{
			Hide();
		}
	}

	private void OnNeckHover(bool isHovering)
	{
		if (!isHovering && !IsMouseInside(panel) && !IsMouseInside(anchor.GetComponent<RectTransform>()))
		{
			Hide();
		}
	}

	private float panelHeight = 0;
	private void OnLayoutChange()
	{
		if (panelHeight != panel.rect.height)
		{
			panelHeight = panel.rect.height;
			AdjustPanel();
		}
	}

	//
	// Public Methods
	//

	private IEnumerator DelayedHide()
	{
		for (int i = 0; i < 4; i++)
		{
			yield return null;
		}

		neck.gameObject. SetActive(false);
		panel.gameObject.SetActive(false);

		// Hide anchor image
		if (anchor != null)
		{
			SetImageTransparency(anchor, 0);
			anchor = null;
		}

		delayedHideCoroutine = null;
	}

	public bool IsMouseInside()
	{
		return IsMouseInside(panel) || IsMouseInside(neck);
	}

	public void Hide()
	{
		if (delayedHideCoroutine == null)
		{
			delayedHideCoroutine = StartCoroutine(DelayedHide());
		}
	}

	public void ShowData(DataLayer dataLayer, RectTransform rt)
	{
		if (delayedHideCoroutine != null)
		{
			StopCoroutine(delayedHideCoroutine);
			delayedHideCoroutine = null;
		}

		transform.position = rt.position;

		currentRow = 0;

		bool allPatchesHaveSameValue = true;

		var patchCount = dataLayer.loadedPatchesInView.Count;
		if (patchCount == 0)
		{
			Add(translator.Get("No Info"), "");
		}
		else
		{
			var firstPatch = dataLayer.loadedPatchesInView[0];
			var firstPatchData = firstPatch.Data;

			// Size
			double north = firstPatchData.north;
			double south = firstPatchData.south;
			double east = firstPatchData.east;
			double west = firstPatchData.west;
			for (int i = 1; i < patchCount; i++)
			{
				var data = dataLayer.loadedPatchesInView[i].Data;
				north = Math.Max(north, data.north);
				south = Math.Min(south, data.south);
				east = Math.Max(east, data.east);
				west = Math.Min(west, data.west);
			}
			Add(translator.Get("Extents"), "N  " + north.ToString("0.0000") +
				"\nS  " + south.ToString("0.0000") + 
				"\nE  " + east.ToString("0.0000") +
				"\nW  " + west.ToString("0.0000"));

			var size = GeoCalculator.LonLatToMeters(east, north) - GeoCalculator.LonLatToMeters(west, south);
			var unit = "m";
			if (size.x >= 1000 && size.y >= 1000)
			{
				size *= 0.001;
				unit = "km";
			}
			Add(translator.Get("Size"), size.x.ToString("0.#") + " x " + size.y.ToString("0.#") + " " + unit);

			if (firstPatchData is GridData)
			{
				var firstGridData = firstPatchData as GridData;

				// Resolution
				allPatchesHaveSameValue = true;
				var cellWidth = firstGridData.GetCellWidth();
				for (int i = 1; i < patchCount; i++)
				{
					var otherCellWidth = (dataLayer.loadedPatchesInView[i].Data as GridData).GetCellWidth();
					if (Math.Abs(otherCellWidth - cellWidth) > 0.000001f)
					{
						allPatchesHaveSameValue = false;
						break;
					}
				}
				if (allPatchesHaveSameValue)
				{
					var resolution = (float)(GeoCalculator.Deg2Meters * cellWidth);
					unit = "m";
					if (resolution > 1000)
					{
						unit = "km";
						resolution *= 0.001f;
					}

					int number = Mathf.RoundToInt(resolution);
					Add(translator.Get("Resolution"), number + " x " + number + " " + unit);
				}
				else
				{
					AddMultiValue(translator.Get("Resolution"));
				}

				// Units
				if (!firstGridData.IsCategorized)
				{
					allPatchesHaveSameValue = true;
					var units = firstGridData.units;
					for (int i = 1; i < patchCount; i++)
					{
						var otherUnits = (dataLayer.loadedPatchesInView[i].Data as GridData).units;
						if (otherUnits != units)
						{
							allPatchesHaveSameValue = false;
							break;
						}
					}
					if (allPatchesHaveSameValue)
					{
						Add(translator.Get("Units"), units);
					}
					else
					{
						AddMultiValue(translator.Get("Units"));
					}
				}
			}

			// Year
			allPatchesHaveSameValue = true;
			var year = firstPatch.Year;
			for (int i = 1; i < patchCount; i++)
			{
				var otherYear = dataLayer.loadedPatchesInView[i].Year;
				if (otherYear != year)
				{
					allPatchesHaveSameValue = false;
					break;
				}
			}
			if (allPatchesHaveSameValue)
			{
				Add(translator.Get("Year"), year.ToString());
			}
			else
			{
				AddMultiValue(translator.Get("Year"));
			}

			// Records
			HashSet<int> years = new HashSet<int>();
			foreach (var patch in dataLayer.loadedPatchesInView)
			{
				foreach (var recordYear in patch.SiteRecord.layerSite.records.Keys)
				{
					if (!years.Contains(recordYear))
						years.Add(recordYear);
				}
			}
			if (years.Count > 1)
			{
				List<int> sortedYears = new List<int>(years);
				sortedYears.Sort();

				var records = "";
				foreach (var recordYear in sortedYears)
				{
					records += recordYear + ", ";
				}
				Add(translator.Get("Records"), records.Remove(records.Length - 2));
			}

			string citationStr = null;

			// Add metadata
			if (firstPatchData.metadata != null)
			{
				foreach (var row in firstPatchData.metadata)
				{
					allPatchesHaveSameValue = true;
					var value = row.Value;
					for (int i = 1; i < patchCount; i++)
					{
						var otherValue = dataLayer.loadedPatchesInView[i].Data.metadata.Get(row.Key);
						if (!otherValue.Equals(value))
						{
							allPatchesHaveSameValue = false;
							break;
						}
					}

					if (row.Key != "Citation" && row.Key != "MandatoryCitation")
					{
					if (allPatchesHaveSameValue)
					{
						Add(translator.Get(row.Key, false), row.Value);
					}
					else
					{
						AddMultiValue(translator.Get(row.Key, false));
					}
					}

					// Source/Citation
					if (row.Key == "Source" && citationStr == null)
					{
						if (allPatchesHaveSameValue)
						{
							if (citationsManager.TryGet(firstPatch, out Citation citation))
								citationStr = citation.text;
						}
						else
						{
							AddMultiValue(translator.Get("Citation"));
						}
					}
					else if (row.Key == "Citation")
					{
						if (allPatchesHaveSameValue)
							citationStr = value;
						else
							citationStr = AddAllValues(dataLayer, row.Key, value);
					}
					else if (row.Key == "MandatoryCitation" && citationStr == null)
					{
						if (allPatchesHaveSameValue)
							citationStr = value;
						else
							citationStr = AddAllValues(dataLayer, row.Key, value);
				}
			}
		}

			if (!string.IsNullOrWhiteSpace(citationStr))
			{
				Add(translator.Get("Citation"), citationStr);
			}
		}

		// DO NOT DELETE OR CHANGE THIS COMMENT BLOCK
		// The following lines will force the LocalizationManager to export the quoted text:
		// "Source"/*translatable*/
		// "Layer Name"/*translatable*/

		// Hide remaining rows
		for (int i = infoRows.Count - 1; i >= currentRow; i--)
		{
			infoRows[i].key.gameObject.SetActive(false);
			infoRows[i].value.gameObject.SetActive(false);
			infoRows[i].key.text = infoRows[i].value.text = "";
		}

		neck.gameObject.SetActive(true);
		panel.gameObject.SetActive(true);

		// Hide previous anchor image
		if (anchor != null)
		{
			SetImageTransparency(anchor, 0);
		}

		// Show new anchor image
		anchor = rt.GetComponent<Image>();
		if (anchor != null)
		{
			SetImageTransparency(anchor, 1);
		}

		panelHeight = panel.rect.height;
		AdjustPanel();
	}

	private void AdjustPanel()
	{
		var screenPos = panel.TransformPoint(0, panel.rect.yMin, 0);
		var mapPos = mapView.WorldToLocal(screenPos);
		panelPos.y = -Mathf.Min(0, mapView.Rect.height + mapPos.y - 3 - panel.anchoredPosition.y);
		panel.anchoredPosition = panelPos;
	}


	//
	// Private Methods
	//

	private void Add(string key, string value)
	{
		InfoRow row;
		if (currentRow < infoRows.Count)
		{
			row = infoRows[currentRow];
			row.key.gameObject.SetActive(true);
			row.value.gameObject.SetActive(true);
		}
		else
		{
			var container = panel.transform;
			var k = Instantiate(keyPrefab, container, false);
			var v = Instantiate(valuePrefab, container, false);
			row = new InfoRow(k, v);
			infoRows.Add(row);
		}

		row.key.text = key;
		row.value.text = value;

		currentRow++;
	}

	private void AddMultiValue(string key)
	{
		Add(key, translator.Get("(multiple values)"));
	}

	private static bool IsMouseInside(RectTransform rt)
	{
		return rt.rect.Contains(rt.InverseTransformPoint(Input.mousePosition));
	}

	private static void SetImageTransparency(Image img, float alpha)
	{
		var c = img.color;
		c.a = alpha;
		img.color = c;
	}

	private string AddAllValues(DataLayer dataLayer, string key, string value)
	{
		var uniqueCitations = new HashSet<string>();
		string multiValues = value;
		uniqueCitations.Add(value);

		var patchCount = dataLayer.loadedPatchesInView.Count;
		for (int i = 1; i < patchCount; i++)
		{
			var otherValue = dataLayer.loadedPatchesInView[i].Data.metadata.Get(key);
			if (uniqueCitations.AddOnce(otherValue))
				multiValues += "\n" + otherValue;
		}
		return multiValues;
	}
}
