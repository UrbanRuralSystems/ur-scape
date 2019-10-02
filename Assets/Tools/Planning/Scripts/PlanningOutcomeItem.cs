// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlanningOutcomeItem : MonoBehaviour
{
	[Header("UI References")]
	public Text header;
	public RectTransform units;
	public RectTransform original;
	public RectTransform current;
	public RectTransform target;
	public RectTransform originalGraphDot;
	public RectTransform currentGraphBar;
	public RectTransform targetGraphDot;
	public RectTransform origTargetSameGraphDot;
	public RectTransform origTargetDiffGraphDot;
	public Text origTargetSameGraphDotLabel;
	public Text origTargetDiffGraphDotMinLabel;
	public Text origTargetDiffGraphDotMaxLabel;

	private readonly Dictionary<string, RectTransform> items = new Dictionary<string, RectTransform>();
	private readonly Dictionary<string, RectTransform> garphs = new Dictionary<string, RectTransform>();

	private float maxDistributionGraphWidth;
	public float MaxDistributionGraphWidth { get { return maxDistributionGraphWidth; } }

	public Dictionary<string, RectTransform> Items { get { return items; } }
	public Dictionary<string, RectTransform> Graphs { get { return garphs; } }

	//
	// Unity Methods
	//

	private void Awake()
    {
		maxDistributionGraphWidth = currentGraphBar.GetComponent<RectTransform>().rect.width;
	}

	//
	// Public Methods
	//

	public void Init()
	{
		// Items
		items.Add("Units", units);
		items.Add("Original", original);
		items.Add("Current", current);
		items.Add("Target", target);

		// Graphs
		garphs.Add("OriginalGraphDot", originalGraphDot);
		garphs.Add("CurrentGraphBar", currentGraphBar);
		garphs.Add("TargetGraphDot", targetGraphDot);
		garphs.Add("OrigTargetSameGraphDot", origTargetSameGraphDot);
	}

	public void ShowItem(string itemName, bool show)
	{
		if (items.TryGetValue(itemName, out RectTransform item))
			item.gameObject.SetActive(show);
		else
			Debug.LogWarning(itemName + " doesn't exist. Eligible itemNames are Units, Original, Current, Target");
	}

	public void ShowGraph(string barName, bool show)
	{
		if (garphs.TryGetValue(barName, out RectTransform bar))
			bar.transform.parent.gameObject.SetActive(show);
		else
			Debug.LogWarning(barName + " doesn't exist. Eligible barNames are OriginalGraphDot, CurrentGraphBar, TargetGraphDot, OrigTargetSameGraphDot");
	}

	public void SetHeaderValue(string value)
	{
		header.text = value;
	}

	public void SetItemValue(string itemName, string value)
	{
		if (items.TryGetValue(itemName, out RectTransform item))
			item.GetChild(0).GetComponent<Text>().text = value;
		else
			Debug.LogWarning(itemName + " doesn't exist. Eligible itemNames are Units, Original, Current, Target");
	}

	public void SetGraphProperties(string barName, float percentageVal, float width, bool prefix = false)
	{
		if (garphs.TryGetValue(barName, out RectTransform bar))
		{
			if (!barName.Contains("Dot"))
			{
				DistributionBar dBar = bar.GetComponent<DistributionBar>();
				dBar.SetPercentageVal(percentageVal, prefix);
				dBar.SetDistribBarWidth(width);
			}
			else
				bar.sizeDelta = new Vector2(width, bar.rect.height);
		}
		else
			Debug.LogWarning(barName + " doesn't exist. Eligible barNames are OriginalGraphDot, CurrentGraphBar, TargetGraphDot, OrigTargetSameGraphDot");
	}

	public void ShowOrigTargetDiffGraphDot(bool show)
	{
		origTargetDiffGraphDot.transform.parent.gameObject.SetActive(show);
	}

	public void SetOrigTargetDiffGraphDotProperties(float origVal, float targetVal, float width)
	{
		origTargetDiffGraphDotMinLabel.text = (origVal < targetVal) ? "O" : "T";
		origTargetDiffGraphDotMaxLabel.text = (origVal > targetVal) ? "O" : "T";

		origTargetDiffGraphDot.sizeDelta = new Vector2(width, origTargetDiffGraphDot.rect.height);
	}
}