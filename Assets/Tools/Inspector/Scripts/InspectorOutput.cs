// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

using LineInfo = LineInspector.LineInspectorInfo;
using AreaInfo = AreaInspector.AreaInspectorInfo;

class InspectorOutputItem
{
    public string name;
    public float maxVal;
    public float minVal;
    public float medianVal;
    public string units;
	public Color dotColor;
}

class InspectorOutputDropdown
{
	public Dropdown dropdown;
	public string[] options;

	public InspectorOutputDropdown(Dropdown dropdown, string[] options)
	{
		this.dropdown = dropdown;
		this.options = options;
	}
}

class PropertiesAndSummaries
{
	public double totalLength;
	public int totalDataCount;
	public float noDataLength;
	public Dictionary<string, float> maxVal = new Dictionary<string, float>();
	public Dictionary<string, float> minVal = new Dictionary<string, float>();
	public Dictionary<string, float> medianVal = new Dictionary<string, float>();
}

public class InspectorOutput : MonoBehaviour, IOutput {

	[Header("Prefabs")]
	public InspectorOutputItemLabel itemPrefab;

	[Header("UI References")]
	public Transform container;
	public Transform lineInspectorEntryInfo;
	public Transform areaInspectorEntryInfo;
	public Transform summaryValuesHeader;
	public Dropdown summaryDropdown;
	public Transform metricsValuesHeader;
	public Dropdown metricsDropdown;
	public Text propertiesLabel;
	public Text summaryLabel;
	public Text totalLengthVal;
	public Text noDataLengthVal;
	public Text inspectionHeader;

	// Data lists
	private readonly Dictionary<string, InspectorOutputItemLabel> inspectorOutputItemLabels = new Dictionary<string, InspectorOutputItemLabel>();
	private Dictionary<string, List<float>> dictVals;
	private Dictionary<string, string> dictUnits;
	private Dictionary<string, Color> dictColors;
	private Dictionary<string, int> dictNoDatas;

	// Output
	private IEnumerable<InspectorOutputItem> itemslist;

	// Constants
	private const int SelectedLine = 0;
	private const int AllLinesCombined = 1;
	private const int AllLinesCompared = 2;

	private readonly string[] summaryOptions = {
		"Selected Line"/*translatable*/,
		"All Lines Combined"/*translatable*/,
	  //"All Lines Compared/*translatable*/"
	};
	private readonly string[] metricsOptions = {
		"Maximum"/*translatable*/,
		"Median"/*translatable*/,
		"Minimum"/*translatable*/
	};
	private readonly string[] propertiesLabels = {
		"Line Properties"/*translatable*/,
		"Combined Properties"/*translatable*/,
		"Line Properties Compared"/*translatable*/
	};
	private readonly string[] summaryLabels = {
		"Inspection Summary"/*translatable*/,
		"Combined Inspection Summary"/*translatable*/,
		"Inspection Summary Comparison"/*translatable*/
	};

	private InspectorTool inspectorTool;
	private ITranslator translator;
	private int currLineInspectionIndex = 0;
	private InspectorOutputDropdown[] inspectorOutputDropdowns;
	private PropertiesAndSummaries[] propertiesAndSummaries;

	//
	// Unity Methods
	//

	private void Awake()
	{
		// Components
		inspectorTool = ComponentManager.Instance.Get<InspectorTool>();
		translator = LocalizationManager.Instance;
		LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;

		// Initialize dropdowns
		inspectorOutputDropdowns = new InspectorOutputDropdown[]
		{
			new InspectorOutputDropdown(summaryDropdown, summaryOptions),
			new InspectorOutputDropdown(metricsDropdown, metricsOptions)
		};
		InitDropdowns();
		summaryDropdown.onValueChanged.AddListener(UpdatePanel);

		// Initialize properties and summaries
		int maxInspectionCount = inspectorTool.maxInspectionCount;
		propertiesAndSummaries = new PropertiesAndSummaries[maxInspectionCount];
		for (int i = 0; i < maxInspectionCount; ++i)
		{
			propertiesAndSummaries[i] = new PropertiesAndSummaries();
		}

		// Default panel display
		UpdatePanel(SelectedLine);
	}

	private void OnDestroy()
	{
		LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
		ResetAndClearOutput();
	}

	//
	// Event Methods
	//

	private void OnLanguageChanged()
	{
		// Translate elements of panel
		UpdateInspectionHeader(summaryDropdown.value);
		UpdatePropertiesLabel(summaryDropdown.value);
		UpdateSummaryLabel(summaryDropdown.value);
		UpdateDropdowns();
	}

	//
	// Public Methods
	//

	public void OutputToCSV(TextWriter csv)
	{
		var translator = LocalizationManager.Instance;
		foreach (var inspectorItem in itemslist)
		{
			string max = inspectorItem.maxVal.ToString() + " " + inspectorItem.units;
			string min = inspectorItem.minVal.ToString() + " " + inspectorItem.units;
			string median = inspectorItem.medianVal.ToString() + " " + inspectorItem.units;

			csv.WriteLine("{0}", inspectorItem.name);
			csv.WriteLine("{0},{1}", translator.Get("Max"), max);
			csv.WriteLine("{0},{1}", translator.Get("Min"), min);
			csv.WriteLine("{0},{1}", translator.Get("Median"), median);
		}
	}

	public void UpdateLineInspectorOutput(LineInfo lineInfo, DataLayers dataLayers)
	{
		Dictionary<string, List<float>> idToValueList = new Dictionary<string, List<float>>();
		Dictionary<string, int> idToNoDataList = new Dictionary<string, int>();
		Dictionary<string, string> idToUnitsList = new Dictionary<string, string>();
		Dictionary<string, Color> idToDotColorList = new Dictionary<string, Color>();

		if (lineInfo != null)
		{
			var lineGrids = lineInfo.mapLayer.grids;
			var lineInspectedGrids = lineInfo.mapLayer.inspectedGridsData;

			int lineGridsLength = lineGrids.Count;
			for (int i = 0; i < lineGridsLength; ++i)
			{
				var layerName = dataLayers.activeLayerPanels[i].name;
				var dotColor = dataLayers.activeLayerPanels[i].dot.color;

				int lineInspectedGridsLength = lineInspectedGrids[lineGrids[i]].Count;
				for (int j = 0; j < lineInspectedGridsLength; ++j)
				{
					// Add all cells to list
					if (!idToValueList.ContainsKey(layerName))
					{
						List<float> data = new List<float>();
						int noDataCount = 0;
						float value = lineInspectedGrids[lineGrids[i]][j];
						if (value > 0.0f)
							data.Add(value);
						else
							++noDataCount;
						idToValueList.Add(layerName, data);
						idToNoDataList.Add(layerName, noDataCount);
					}
					else
					{
						var list = idToValueList[layerName];
						float value = lineInspectedGrids[lineGrids[i]][j];
						if (value > 0.0f)
							list.Add(value);
						else
							++idToNoDataList[layerName];
					}
				}

				if (!idToUnitsList.ContainsKey(layerName))
				{
					idToUnitsList.Add(layerName, lineGrids[i].units);
				}

				if (!idToDotColorList.ContainsKey(layerName))
				{
					idToDotColorList.Add(layerName, dotColor);
				}
			}
			ComputeAndUpdateNoDataLength(idToValueList, idToNoDataList);
			SetData(idToValueList, idToUnitsList, idToDotColorList, idToNoDataList);
		}
	}

	public void UpdateAreaInspectorOutput(AreaInfo areaInfo, DataLayers dataLayers)
	{
		Dictionary<string, List<float>> idToValueList = new Dictionary<string, List<float>>();
		Dictionary<string, int> idToNoDataList = new Dictionary<string, int>();
		Dictionary<string, string> idToUnitsList = new Dictionary<string, string>();
		Dictionary<string, Color> idToDotColorList = new Dictionary<string, Color>();

		// Get information here

		SetData(idToValueList, idToUnitsList, idToDotColorList, idToNoDataList);
	}

	public void ActivateEntryInfo(InspectorTool.InspectorType inspectorType)
	{
		bool isLineType = inspectorType == InspectorTool.InspectorType.Line;
		lineInspectorEntryInfo.gameObject.SetActive(isLineType ? true : false);
		areaInspectorEntryInfo.gameObject.SetActive(isLineType ? false : true);
	}

	public void ShowSummaryHeaderAndDropdown(bool show)
	{
		summaryValuesHeader.gameObject.SetActive(show);
		summaryDropdown.gameObject.SetActive(show);
	}

	public void ShowMetricsHeaderAndDropdown(bool show)
	{
		metricsValuesHeader.gameObject.SetActive(show);
		metricsDropdown.gameObject.SetActive(show);
	}

	public void ShowHeader(bool show)
	{
		inspectionHeader.gameObject.SetActive(show);
	}

	public void ShowPropertiesAndSummaryLabel(bool show)
	{
		propertiesLabel.gameObject.SetActive(show);
		summaryLabel.gameObject.SetActive(show);
	}

	public void ShowInspectorOutputItemLabel(string itemName, bool show)
	{
		inspectorOutputItemLabels.TryGetValue(itemName, out InspectorOutputItemLabel item);
		item?.gameObject.SetActive(show);
	}

	public void SetDropDownInteractive(bool isOn)
	{
		summaryDropdown.interactable = isOn;
	}

	public void ComputeAndUpdateTotalLength(LineInfo[] lineInfos, int index)
	{
		currLineInspectionIndex = index;
		LineInfo lineInfo = lineInfos[index];

		// Computations
		double lon1 = lineInfo.coords[0].Longitude, lat1 = lineInfo.coords[0].Latitude,
			   lon2 = lineInfo.coords[1].Longitude, lat2 = lineInfo.coords[1].Latitude,
			   totalLength = GeoCalculator.GetDistanceInMeters(lon1, lat1, lon2, lat2) / 1000.0;

		propertiesAndSummaries[currLineInspectionIndex].totalLength = totalLength;

		// Update total length shown
		if (summaryDropdown.value == SelectedLine)
			SelectedLineTotalLength();
		else if (summaryDropdown.value == AllLinesCombined)
			AllLinesCombinedTotalLength();
	}

	public void ComputeAndUpdateMetrics()
	{
		if (summaryDropdown.value == SelectedLine)
			SelectedLineMetrics();
		else if (summaryDropdown.value == AllLinesCombined)
			AllLinesCombinedMetrics();
	}

	public void SetLineInspection(int index)
	{
		currLineInspectionIndex = index;
		UpdateInspectionHeader(summaryDropdown.value);
	}

	public void ResetAndClearOutput()
	{
		totalLengthVal.text = "0 km";
		noDataLengthVal.text = "0";

		foreach (var item in inspectorOutputItemLabels)
		{
			Destroy(item.Value.gameObject);
		}
		inspectorOutputItemLabels.Clear();

		dictVals.Clear();
		dictUnits.Clear();
		dictColors.Clear();
		dictNoDatas.Clear();

		summaryDropdown.SetValueWithoutNotify(SelectedLine);
		ShowSummaryHeaderAndDropdown(false);
		ShowMetricsHeaderAndDropdown(false);
		ShowHeader(false);
		ShowPropertiesAndSummaryLabel(false);
	}

	//
	// Private Methods
	//

	private void ComputeAndUpdateNoDataLength(Dictionary<string, List<float>> vals, Dictionary<string, int> noDataCounts)
	{
		currLineInspectionIndex = inspectorTool.lineInspectorPanel.lineInspector.CurrLineInspection;

		if (currLineInspectionIndex == -1)
			return;

		int noDataCount = 0, hasDataCount = 0;
		foreach (var pair in vals)
		{
			hasDataCount += pair.Value.Count;
			noDataCount += noDataCounts[pair.Key];
		}
		int totalDataCount = hasDataCount + noDataCount;
		propertiesAndSummaries[currLineInspectionIndex].totalDataCount = totalDataCount;

		// Line drawn on area where there is no data
		propertiesAndSummaries[currLineInspectionIndex].noDataLength = (totalDataCount == 0) ? 1.0f : noDataCount / (float)totalDataCount;

		// Update total length shown
		if (summaryDropdown.value == SelectedLine)
			SelectedLineNoDataLength();
		else if (summaryDropdown.value == AllLinesCombined)
			AllLinesCombinedNoDataLength();
	}

	private void UpdateInspectionHeader(int option)
	{
		switch (option)
		{
			case SelectedLine:
			default:
				SelectedLineHeader();
				break;
			case AllLinesCombined:
				inspectionHeader.text = translator.Get("All Lines Combined");
				break;
			case AllLinesCompared:
				inspectionHeader.text = translator.Get("All Lines Compared");
				break;
		}
	}

	private void UpdatePropertiesLabel(int option)
	{
		propertiesLabel.text = translator.Get(propertiesLabels[option]);
	}

	private void UpdateSummaryLabel(int option)
	{
		summaryLabel.text = translator.Get(summaryLabels[option]);
	}

	private void InitDropdowns()
	{
		foreach (InspectorOutputDropdown item in inspectorOutputDropdowns)
		{
			Dropdown dropdown = item.dropdown;
			string[] options = item.options;
			dropdown.ClearOptions();
			foreach (string option in options)
			{
				dropdown.options.Add(new Dropdown.OptionData(translator.Get(option)));
			}
		}
		SetDropDownInteractive(false);
	}

	private void UpdateDropdowns()
	{
		foreach (InspectorOutputDropdown item in inspectorOutputDropdowns)
		{
			Dropdown dropdown = item.dropdown;
			string[] options = item.options;
			int length = item.options.Length;
			for (int i = 0; i < length; ++i)
			{
				dropdown.options[i].text = translator.Get(options[i]);
			}
			dropdown.captionText.text = dropdown.options[dropdown.value].text;
		}
	}

	private float ComputeMedian(List<float> vals)
	{
		vals.Sort();
		double mid = (vals.Count - 1) / 2.0;
		return (vals[(int)(mid)] + vals[(int)(mid + 0.5)]) / 2;
	}

	private void PlotData()
	{
		currLineInspectionIndex = inspectorTool.lineInspectorPanel.lineInspector.CurrLineInspection;

		if (currLineInspectionIndex == -1)
			return;

		PropertiesAndSummaries currPropertiesAndSummaries = propertiesAndSummaries[currLineInspectionIndex];

		foreach (var inspectorOutputItem in itemslist)
		{
			InspectorOutputItemLabel inspectorOutputItemLabel = null;
			string itemName = inspectorOutputItem.name;

			if (inspectorOutputItemLabels.ContainsKey(itemName))
			{
				inspectorOutputItemLabel = inspectorOutputItemLabels[itemName];
			}
			else
			{
				inspectorOutputItemLabel = Instantiate(itemPrefab, container, false);

				inspectorOutputItemLabel.name = itemName;
				inspectorOutputItemLabel.SetName(itemName);
				inspectorOutputItemLabel.SetUnitsValue(inspectorOutputItem.units);
				inspectorOutputItemLabel.SetDotColor(inspectorOutputItem.dotColor);

				inspectorOutputItemLabels.Add(itemName, inspectorOutputItemLabel);
			}

			float max = inspectorOutputItem.maxVal,
				  min = inspectorOutputItem.minVal,
				  median = inspectorOutputItem.medianVal;

			inspectorOutputItemLabel.SetMaxValue(max.ToString("#,##0.##"));
			inspectorOutputItemLabel.SetMedianValue(median.ToString("#,##0.##"));
			inspectorOutputItemLabel.SetMinValue(min.ToString("#,##0.##"));

			AddOrUpdateMetric(currPropertiesAndSummaries.maxVal, itemName, max);
			AddOrUpdateMetric(currPropertiesAndSummaries.minVal, itemName, min);
			AddOrUpdateMetric(currPropertiesAndSummaries.medianVal, itemName, median);
		}
	}

	private void AddOrUpdateMetric(Dictionary<string, float> metric, string key, float value)
	{
		if (metric.ContainsKey(key))
			metric[key] = value;
		else
			metric.Add(key, value);
	}

	private void UpdateData()
	{
		List<InspectorOutputItem> inspectorOutputItems = new List<InspectorOutputItem>();

		foreach (var pair in dictVals)
		{
			float max = 0.0f;
			float min = 0.0f;
			float median = 0.0f;
			string unit = "";
			Color color = Color.black;

			if (pair.Value.Any())
			{
				max = pair.Value.Max();
				min = pair.Value.Min();
				median = ComputeMedian(pair.Value);
				unit = dictUnits.ContainsKey(pair.Key) ? dictUnits[pair.Key] : "";
				color = dictColors.ContainsKey(pair.Key) ? dictColors[pair.Key] : Color.black;
			}

			inspectorOutputItems.Add(new InspectorOutputItem {
				name = pair.Key,
				maxVal = max,
				minVal = min,
				medianVal = median,
				units = unit,
				dotColor = color
			});
		}

		itemslist = inspectorOutputItems;
		PlotData();
	}

	private void SetData(Dictionary<string, List<float>> vals, Dictionary<string, string> units, Dictionary<string, Color> colors, Dictionary<string, int> noDataCounts)
	{
		dictVals = vals;
		dictUnits = units;
		dictColors = colors;
		dictNoDatas = noDataCounts;
		UpdateData();
	}

	private void SelectedLineHeader()
	{
		inspectionHeader.text = translator.Get("Inspection Line") + " " + (currLineInspectionIndex + 1);
	}

	private void SelectedLineTotalLength()
	{
		totalLengthVal.text = propertiesAndSummaries[currLineInspectionIndex].totalLength.ToString("#,##0.##") + " km";
	}

	private void SelectedLineNoDataLength()
	{
		noDataLengthVal.text = (propertiesAndSummaries[currLineInspectionIndex].noDataLength * 100).ToString("0.##") + "%";
	}

	private void SelectedLineMetrics()
	{
		currLineInspectionIndex = inspectorTool.lineInspectorPanel.lineInspector.CurrLineInspection;

		if (currLineInspectionIndex == -1 || itemslist == null)
			return;

		PropertiesAndSummaries currPropertiesAndSummaries = propertiesAndSummaries[currLineInspectionIndex];
		foreach (var inspectorOutputItem in itemslist)
		{
			string itemName = inspectorOutputItem.name;
			InspectorOutputItemLabel inspectorOutputItemLabel = inspectorOutputItemLabels[itemName];

			inspectorOutputItemLabel.SetMaxValue(currPropertiesAndSummaries.maxVal[itemName].ToString("#,##0.##"));
			inspectorOutputItemLabel.SetMedianValue(currPropertiesAndSummaries.medianVal[itemName].ToString("#,##0.##"));
			inspectorOutputItemLabel.SetMinValue(currPropertiesAndSummaries.minVal[itemName].ToString("#,##0.##"));
		}
	}

	private void AllLinesCombinedTotalLength()
	{
		double overallTotalLength = 0.0;
		foreach (var pair in propertiesAndSummaries)
		{
			overallTotalLength += pair.totalLength;
		}

		totalLengthVal.text = overallTotalLength.ToString("#,##0.##") + " km";
	}

	private void AllLinesCombinedNoDataLength()
	{
		float overallNoDataCount = 0.0f, overallTotalDataCount = 0.0f;
		foreach (var pair in propertiesAndSummaries)
		{
			overallNoDataCount += pair.noDataLength * pair.totalDataCount;
			overallTotalDataCount += pair.totalDataCount;
		}
		float overallNoDatalLength = overallNoDataCount / overallTotalDataCount;

		noDataLengthVal.text = (overallNoDatalLength * 100).ToString("0.##") + "%";
	}

	private void AllLinesCombinedMetrics()
	{
		foreach (var inspectorOutputItem in itemslist)
		{
			string itemName = inspectorOutputItem.name;
			InspectorOutputItemLabel inspectorOutputItemLabel = inspectorOutputItemLabels[itemName];

			List<float> allMaxVals = new List<float>();
			List<float> allMinVals = new List<float>();
			List<float> allMedianVals = new List<float>();

			foreach (var propertyAndSummary in propertiesAndSummaries)
			{
				if(propertyAndSummary.maxVal.TryGetValue(itemName, out float maxVal))
					allMaxVals.Add(maxVal);
				if (propertyAndSummary.minVal.TryGetValue(itemName, out float minVal))
					allMinVals.Add(minVal);
				if(propertyAndSummary.medianVal.TryGetValue(itemName, out float medianVal))
					allMedianVals.Add(medianVal);
			}

			float max = allMaxVals.Max();
			float min = allMinVals.Min();
			float median = ComputeMedian(allMedianVals);

			inspectorOutputItemLabel.SetMaxValue(max.ToString("#,##0.##"));
			inspectorOutputItemLabel.SetMedianValue(median.ToString("#,##0.##"));
			inspectorOutputItemLabel.SetMinValue(min.ToString("#,##0.##"));
		}
	}

	private void SelectedLineOutput()
	{
		ShowMetricsHeaderAndDropdown(false);
		UpdateInspectionHeader(SelectedLine);
		SelectedLineTotalLength();
		SelectedLineNoDataLength();
		SelectedLineMetrics();
	}

	private void AllLinesCombinedOutput()
	{
		ShowMetricsHeaderAndDropdown(false);
		UpdateInspectionHeader(AllLinesCombined);
		AllLinesCombinedTotalLength();
		AllLinesCombinedNoDataLength();
		AllLinesCombinedMetrics();
	}

	private void AllLinesComparedOutput()
	{
		ShowMetricsHeaderAndDropdown(true);
	}

	private void UpdatePanel(int option)
	{
		UpdateInspectionHeader(option);
		UpdatePropertiesLabel(option);
		UpdateSummaryLabel(option);

		switch(option)
		{
			case SelectedLine:
			default:
				SelectedLineOutput();
				break;
			case AllLinesCombined:
				AllLinesCombinedOutput();
				break;
			case AllLinesCompared:
				AllLinesComparedOutput();
				break;
		}
	}
}
