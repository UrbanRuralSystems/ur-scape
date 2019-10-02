// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          Muhammad Salihin Bin Zaol-kefli (mzaolkefli@ethz.ch)

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class PlanningOutput : MonoBehaviour, IOutput
{
	[Header("UI References")]
	public Text selectedTypologyHeader;
	public Text selectedTypology;
	public Text attributesHeader;
	public Text planningOutcomesHeader;
	public RectTransform attributesContainer;
	public RectTransform planningOutcomesContainer;
	public Dropdown unitsDropdown;

	[Header("Prefabs")]
	public AttributeItem attributeItemPrefab;
	public PlanningOutcomeItem planningOutcomeItemPrefab;

	[Header("Settings")]
	public AreaUnit[] units = new AreaUnit[]
	{
			new AreaUnit
			{
				name = "Hectares"/*translatable*/,
				symbol = "ha",
				factor = 0.0001
			},
			new AreaUnit
			{
				name = "Square Kilometers"/*translatable*/,
				symbol = "km\u00B2",
				factor = 0.000001
			},
			new AreaUnit
			{
				name = "Square Meters"/*translatable*/,
				symbol = "m\u00B2",
				factor = 1
			}
	};

	private List<Typology> typologies;
	private Dictionary<string, float> targetValues;
	private Dictionary<string, float> summaryValues;
	private readonly Dictionary<string, AttributeItem> attributeItems = new Dictionary<string, AttributeItem>();
	private readonly Dictionary<string, PlanningOutcomeItem> planningOutcomeItems = new Dictionary<string, PlanningOutcomeItem>();
	private Dictionary<string, float> gridValues;

	private int typologyIndex = 0;
	private AreaUnit selectedUnit = null;
	public AreaUnit SelectedUnit { get { return selectedUnit; } set { this.selectedUnit = value; } }

	private ITranslator translator;

	//
	// Unity Methods
	//

	private void Awake()
	{
		translator = LocalizationManager.Instance;
		LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
	}

	private void OnDestroy()
	{
		LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
	}

	//
	// Event Methods
	//

	private void OnLanguageChanged()
	{
		UpdateAttributesHeader();
		UpdateDropdown();
		foreach (var pair in typologies[typologyIndex].values)
		{
			if (planningOutcomeItems.TryGetValue(pair.Key, out PlanningOutcomeItem item))
				UpdatePlanningOutcomeItem(pair, item);
		}
	}

	//
	// Public Methods
	//

	public void Init()
	{
		UpdateAttributesHeader();

		unitsDropdown.ClearOptions();
		if (units.Length > 0)
		{
			foreach (var unit in units)
			{
				unitsDropdown.options.Add(new Dropdown.OptionData(translator.Get(unit.name) + " (" + unit.symbol + ")"));
			}
			selectedUnit = units[0];
		}
	}

	public void SetTypology(int index)
	{
		selectedTypology.text = typologies[index].name;
		typologyIndex = index;

		UpdateOutput();

		GuiUtils.RebuildLayout(gameObject.transform);
	}

	public void SetTypologies(List<Typology> typologies)
	{
		this.typologies = typologies;
	}

	public void SetTargetValues(Dictionary<string, float> values)
	{
		targetValues = values;
	}

	public void SetSummaryValues(Dictionary<string, float> values)
	{
		summaryValues = values;
	}

	public void SetGridValues(Dictionary<string, float> values)
	{
		gridValues = values;
	}

	public void UpdateOutput()
	{
		foreach (var pair in typologies[typologyIndex].values)
		{
			// AttributeItem
			AddOrUpdateAttributeItem(pair);

			// PlanningOutcomeItem
			AddOrUpdatePlanningOutcomeItem(pair);
		}

		GuiUtils.RebuildLayout(planningOutcomesContainer);
	}

	public void OutputToCSV(TextWriter csv)
	{
		csv.WriteLine(selectedTypologyHeader.text);
		csv.WriteLine(selectedTypology.text);
		csv.WriteLine();

		// Attributes
		csv.WriteLine(attributesHeader.text);
		foreach (var pair in attributeItems)
		{
			csv.WriteLine("{0},{1}", pair.Key, pair.Value.attribVal.text);
		}
		csv.WriteLine();

		// Planning Outcomes
		csv.WriteLine(planningOutcomesHeader.text);
		foreach (var pair in planningOutcomeItems)
		{
			csv.WriteLine(pair.Value.header.text);
			// Items
			foreach (var item in pair.Value.Items)
			{
				csv.WriteLine("{0},{1}", translator.Get(item.Key), item.Value.GetChild(0).GetComponent<Text>().text);
			}
			// Bars
			foreach (var bar in pair.Value.Graphs)
			{
				if (!bar.Key.Contains("Dot"))
					csv.WriteLine("{0},{1}", bar.Key[0].ToString(), bar.Value.GetComponent<DistributionBar>().percentage.text);
			}
		}
	}

	//
	// Private Methods
	//

	private void UpdateAttributesHeader()
	{
		attributesHeader.text = translator.Get("Attributes per hectare");
	}

	private void UpdateDropdown()
	{
		for (int i = 0; i < units.Length; i++)
		{
			unitsDropdown.options[i].text = translator.Get(units[i].name) + " (" + units[i].symbol + ")";
		}
		unitsDropdown.captionText.text = unitsDropdown.options[unitsDropdown.value].text;
	}

	private void AddOrUpdateAttributeItem(KeyValuePair<string, float> pair)
	{
		if (attributeItems.TryGetValue(pair.Key, out AttributeItem item))
			UpdateAttributeItem(pair, item);
		else
			NewAttributeItem(pair);
	}

	private void NewAttributeItem(KeyValuePair<string, float> pair)
	{
		AttributeItem item = Instantiate(attributeItemPrefab, attributesContainer, false);
		UpdateAttributeItem(pair, item);
		attributeItems.Add(pair.Key, item);
	}

	private void UpdateAttributeItem(KeyValuePair<string, float> pair, AttributeItem item)
	{
		item.attribName.text = pair.Key;
		item.attribVal.text = pair.Value.ToString("#,##0.##") + " " + Typology.info[pair.Key].units;
	}

	private void AddOrUpdatePlanningOutcomeItem(KeyValuePair<string, float> pair)
	{
		if (planningOutcomeItems.TryGetValue(pair.Key, out PlanningOutcomeItem item))
			UpdatePlanningOutcomeItem(pair, item);
		else
			NewPlanningOutcomeItem(pair);
	}

	private void NewPlanningOutcomeItem(KeyValuePair<string, float> pair)
	{
		PlanningOutcomeItem item = Instantiate(planningOutcomeItemPrefab, planningOutcomesContainer, false);
		item.Init();

		UpdatePlanningOutcomeItem(pair, item);

		planningOutcomeItems.Add(pair.Key, item);
	}

	private void UpdatePlanningOutcomeItem(KeyValuePair<string, float> pair, PlanningOutcomeItem item)
	{
		item.SetHeaderValue(pair.Key);

		// Items
		item.SetItemValue("Units", Typology.info[pair.Key].units);

		// Values obtained from data layers existing in site with matching name to attribute
		bool hasOrigVals = (gridValues != null && gridValues.ContainsKey(pair.Key));
		float origVal = hasOrigVals ? gridValues[pair.Key] : 0.0f;
		item.SetItemValue("Original", hasOrigVals ? origVal.ToString("#,##0.##") : translator.Get("N/A"));

		// Values obtained from summaryValues
		bool hasCurrVals = (summaryValues != null && summaryValues.ContainsKey(pair.Key));
		float currVal = hasCurrVals ? summaryValues[pair.Key] : 0.0f;
		item.SetItemValue("Current", hasCurrVals ? currVal.ToString("#,##0.##") : translator.Get("N/A"));

		// Values obtained from targetValues
		bool hasTargetVals = (targetValues != null && targetValues.ContainsKey(pair.Key));
		float targetVal = hasTargetVals ? targetValues[pair.Key] : 0.0f;
		item.SetItemValue("Target", hasTargetVals ? targetVal.ToString("#,##0.##") : translator.Get("N/A"));
		item.ShowItem("Target", hasTargetVals);

		// Graphs
		// Compute percentages and widths
		if (!hasTargetVals)
		{
			if (hasOrigVals)
			{
				HasOrigCurrVals(item, origVal, currVal);
			}
			else
			{
				item.SetGraphProperties("CurrentGraphBar", (currVal * 0.01f) > 0.0f ? 1.0f : 0.0f, item.MaxDistributionGraphWidth, false);

				item.ShowGraph("OriginalGraphDot", false);
				item.ShowGraph("CurrentGraphBar", false);
				item.ShowGraph("TargetGraphDot", false);
				item.ShowGraph("OrigTargetSameGraphDot", false);
				item.ShowOrigTargetDiffGraphDot(false);
			}
		}
		else
		{
			if(hasOrigVals)
			{
				HasOrigCurrTargetVals(item, origVal, currVal, targetVal);
			}
			else
			{
				HasCurrTargetVals(item, currVal, targetVal);
			}
		}
	}

	private float Shortage_ExcessPercentage(float firstVal, float secondVal)
	{
		return secondVal.Equals(0.0f) ? 1.0f : (firstVal - secondVal) / secondVal;
	}

	private void HasOrigCurrVals(PlanningOutcomeItem item, float origVal, float currVal)
	{
		float maxDistributionGraphWidth = item.MaxDistributionGraphWidth;

		if (!origVal.Equals(0.0f))
		{
			float origPercent = 1.0f, currPercent = currVal / origVal;
			float origGraphWidth = maxDistributionGraphWidth, currGraphWidth = currPercent * maxDistributionGraphWidth;

			if (currVal > origVal)
			{
				origGraphWidth = origVal / currVal * maxDistributionGraphWidth;
				currGraphWidth = maxDistributionGraphWidth;
			}

			item.SetGraphProperties("OriginalGraphDot", origPercent, origGraphWidth);
			item.SetGraphProperties("CurrentGraphBar", Shortage_ExcessPercentage(currVal, origVal), currGraphWidth, true);

			bool cond = !currVal.Equals(0.0f) && !origVal.Equals(0.0f);
			item.ShowGraph("OriginalGraphDot", cond);
			item.ShowGraph("CurrentGraphBar", cond);
			item.ShowGraph("TargetGraphDot", false);
			item.ShowGraph("OrigTargetSameGraphDot", false);
			item.ShowOrigTargetDiffGraphDot(false);
		}
	}

	private void HasCurrTargetVals(PlanningOutcomeItem item, float currVal, float targetVal)
	{
		float maxDistributionGraphWidth = item.MaxDistributionGraphWidth;
		float targetGraphWidth = maxDistributionGraphWidth;

		if(!targetVal.Equals(0.0f))
		{
			float currPercent = currVal / targetVal;
			float currGraphWidth;
			if (currVal > targetVal)
			{
				targetGraphWidth = targetVal / currVal * maxDistributionGraphWidth;
				currGraphWidth = maxDistributionGraphWidth;
			}
			else
				currGraphWidth = currPercent * maxDistributionGraphWidth;

			item.SetGraphProperties("CurrentGraphBar", currPercent, currGraphWidth);
			item.SetGraphProperties("TargetGraphDot", (targetVal * 0.01f) > 0.0f ? 1.0f : 0.0f, targetGraphWidth);

			bool cond = !currVal.Equals(0.0f) && !targetVal.Equals(0.0f);
			item.ShowGraph("OriginalGraphDot", false);
			item.ShowGraph("CurrentGraphBar", cond);
			item.ShowGraph("TargetGraphDot", cond);
			item.ShowGraph("OrigTargetSameGraphDot", false);
			item.ShowOrigTargetDiffGraphDot(false);
		}
	}

	private void HasOrigCurrTargetVals(PlanningOutcomeItem item, float origVal, float currVal, float targetVal)
	{
		if(origVal.Equals(targetVal))
		{
			OrigTargetSameVal(item, origVal, currVal);
		}
		else
		{
			OrigTargetDiffVal(item, origVal, currVal, targetVal);
		}
	}

	private void OrigTargetSameVal(PlanningOutcomeItem item, float origVal, float currVal)
	{
		float maxDistributionGraphWidth = item.MaxDistributionGraphWidth;
		float origTargetGraphWidth = maxDistributionGraphWidth;

		if (!origVal.Equals(0.0f))
		{
			float currPercent = currVal / origVal;
			float currGraphWidth;
			if (currVal > origVal)
			{
				origTargetGraphWidth = origVal / currVal * maxDistributionGraphWidth;
				currGraphWidth = maxDistributionGraphWidth;
			}
			else
				currGraphWidth = currPercent * maxDistributionGraphWidth;

			item.SetGraphProperties("CurrentGraphBar", currPercent, currGraphWidth);
			item.SetGraphProperties("OrigTargetSameGraphDot", 1.0f, origTargetGraphWidth);

			bool cond = !currVal.Equals(0.0f) && !origVal.Equals(0.0f);
			item.ShowGraph("OriginalGraphDot", false);
			item.ShowGraph("CurrentGraphBar", cond);
			item.ShowGraph("TargetGraphDot", false);
			item.ShowGraph("OrigTargetSameGraphDot", cond);
			item.ShowOrigTargetDiffGraphDot(false);
		}
	}

	private void OrigTargetDiffVal(PlanningOutcomeItem item, float origVal, float currVal, float targetVal)
	{
		float maxDistributionGraphWidth = item.MaxDistributionGraphWidth;
		float origTargetGraphWidth = maxDistributionGraphWidth;
		float origTargetDiffVal = Mathf.Abs(origVal - targetVal);
		float minVal = Mathf.Min(origVal, targetVal);
		float currMinDiffVal = currVal - minVal;
		float currMinDiffAbsVal = Mathf.Abs(currMinDiffVal);

		if (!origTargetDiffVal.Equals(0.0f))
		{
			float currPercent = currMinDiffVal / origTargetDiffVal, currGraphWidth;
			if (currMinDiffAbsVal > origTargetDiffVal)
			{
				origTargetGraphWidth = origTargetDiffVal / currMinDiffAbsVal * maxDistributionGraphWidth;
				currGraphWidth = maxDistributionGraphWidth;
			}
			else
			{
				currGraphWidth = Mathf.Abs(currPercent) * maxDistributionGraphWidth;
			}

			item.SetGraphProperties("CurrentGraphBar", currPercent, currGraphWidth);
			item.SetOrigTargetDiffGraphDotProperties(origVal, targetVal, origTargetGraphWidth);

			bool cond = !currMinDiffVal.Equals(0.0f) && !origTargetDiffVal.Equals(0.0f);
			item.ShowGraph("OriginalGraphDot", false);
			item.ShowGraph("CurrentGraphBar", cond);
			item.ShowGraph("TargetGraphDot", false);
			item.ShowGraph("OrigTargetSameGraphDot", false);
			item.ShowOrigTargetDiffGraphDot(cond);
		}
	}
}
