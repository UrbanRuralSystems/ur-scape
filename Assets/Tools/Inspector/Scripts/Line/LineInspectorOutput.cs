// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

using LineInfo = LineInspector.LineInspectorInfo;
using PSO = PropertiesAndSummariesOptions;
using PS = PropertiesAndSummaries;
using LinePS = LinePropertiesAndSummaries;
using LinePSUIRef = LinePropertiesAndSummariesUIRef;
using System;
using System.Collections;

public class LineInspectorOutput : MonoBehaviour
{
    [Header("Prefabs")]
    public InspectorOutputItemLabel itemPrefab;
    public CorrelationPairItem correlationPairPrefab;

    [Header("UI References")]
    [SerializeField]
    private LinePSUIRef linePSUIRef = default;
    [SerializeField]
    private GameObject footnote = default;

    private InspectorTool inspectorTool;
    private DataLayers dataLayers;
    private GridLayerController gridLayerController;
    private ITranslator translator;
    private int currLineInspectionIndex = 0;
    private InspectorOutputDropdown[] inspectorOutputDropdowns;
    private LinePS[] linePS;
    private LineDataOutput lineDO = new LineDataOutput();

    //
    // Unity Methods
    //

    private void Awake()
    {
        // Components
        var componentManager = ComponentManager.Instance;
        inspectorTool = componentManager.Get<InspectorTool>();
        dataLayers = componentManager.Get<DataLayers>();
        gridLayerController = inspectorTool.Map.GetLayerController<GridLayerController>();
        translator = LocalizationManager.Instance;

        linePSUIRef.Init(translator);

        // Initialize dropdowns
        inspectorOutputDropdowns = new InspectorOutputDropdown[]
        {
            new InspectorOutputDropdown(linePSUIRef.summaryDropdown, PSO.LineSummaryOptions),
            new InspectorOutputDropdown(linePSUIRef.metricsDropdown, PSO.MetricsOptions)
        };
        InitDropdowns();

        // Initialize properties and summaries
        int maxInspectionCount = inspectorTool.maxInspectionCount;
        linePS = new LinePS[maxInspectionCount];
        for (int i = 0; i < maxInspectionCount; ++i)
        {
            linePS[i] = new LinePS();
        }

        // Initialize listeners
        gridLayerController.OnShowGrid += OnShowGrid;
        LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        linePSUIRef.summaryDropdown.onValueChanged.AddListener(UpdateLinePanel);
        linePSUIRef.computeCorrelationButton.onClick.AddListener(OnComputeCorrelationClicked);

        // Default panel display
        UpdateLinePanel(LinePS.SelectedLine);
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
        UpdateLineInspectionHeader(linePSUIRef.summaryDropdown.value);
        linePSUIRef.UpdatePropertiesLabel(PSO.LinePropertiesLabels[linePSUIRef.summaryDropdown.value]);
        linePSUIRef.UpdateSummaryLabel(PSO.SummaryLabels[linePSUIRef.summaryDropdown.value]);
        UpdateDropdowns();
    }

    private void OnComputeCorrelationClicked()
    {
        if (gridLayerController.mapLayers.Count < 2)
            linePSUIRef.correlationWarning.gameObject.SetActive(true);
        else
        {
            linePSUIRef.correlationWarning.gameObject.SetActive(false);

            // Clear correlation list
            var count = linePSUIRef.correlationGroup.transform.childCount - 1;
            for (int i = count; i >= 0; --i)
            {
                var child = linePSUIRef.correlationGroup.transform.GetChild(i);
                Destroy(child.gameObject);
            }

            StartCoroutine(CalculateSpearmanCorrelation());
        }
    }

    private void OnShowGrid(GridMapLayer mapLayer, bool show)
    {
        if (gridLayerController.mapLayers.Count >= 2 && linePSUIRef.correlationWarning != null)
            linePSUIRef.correlationWarning.gameObject.SetActive(false);
    }

    //
    // Public Methods
    //

    public void OutputToCSV(TextWriter csv)
    {
        var translator = LocalizationManager.Instance;
        csv.WriteLine("{0}", translator.Get("Line Inspection Tool"));
        foreach (var inspectorItem in lineDO.itemslist)
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
        if (!linePSUIRef.summaryDropdown.value.Equals(LinePS.SelectedLine))
            return;

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
            lineDO.SetData(idToValueList, idToUnitsList, idToDotColorList, idToNoDataList, PlotLineData);
        }
    }

    public void ShowSummaryHeaderAndDropdown(bool show)
    {
        linePSUIRef.summaryValuesHeader.gameObject.SetActive(show);
        linePSUIRef.summaryDropdown.gameObject.SetActive(show);
    }

    public void ShowMetricsHeaderAndDropdown(bool show)
    {
        linePSUIRef.metricsValuesHeader.gameObject.SetActive(show);
        linePSUIRef.metricsDropdown.gameObject.SetActive(show);
    }

    public void ShowHeader(bool show)
    {
        linePSUIRef.inspectionHeader.gameObject.SetActive(show);
    }

    public void ShowPropertiesAndSummaryLabel(bool show)
    {
        linePSUIRef.propertiesLabel.gameObject.SetActive(show);
        linePSUIRef.summaryLabel.gameObject.SetActive(show);
    }

    public void ShowInspectorOutputItemLabel(string itemName, bool show)
    {
        lineDO.inspectorOutputItemLabels.TryGetValue(itemName, out InspectorOutputItemLabel item);
        item?.gameObject.SetActive(show);
    }

    public void SetDropDownInteractive(bool isOn)
    {
        linePSUIRef.summaryDropdown.interactable = isOn;
    }

    public void ComputeAndUpdateTotalLength(LineInfo[] lineInfos, int index)
    {
        currLineInspectionIndex = index;
        LineInfo lineInfo = lineInfos[index];

        // Computations
        double lon1 = lineInfo.coords[0].Longitude, lat1 = lineInfo.coords[0].Latitude,
               lon2 = lineInfo.coords[1].Longitude, lat2 = lineInfo.coords[1].Latitude,
               totalLength = GeoCalculator.GetDistanceInMeters(lon1, lat1, lon2, lat2) / 1000.0;

        linePS[currLineInspectionIndex].totalLength = totalLength;

        // Update total length shown
        if (linePSUIRef.summaryDropdown.value == LinePS.SelectedLine)
            linePSUIRef.SelectedTotalLength(linePS[currLineInspectionIndex]);
        else if (linePSUIRef.summaryDropdown.value == LinePS.AllLinesCombined)
            lineDO.AllCombinedTotalLength(linePS, linePSUIRef);
    }

    public void ComputeAndUpdateMetrics()
    {
        if (linePSUIRef.summaryDropdown.value == LinePS.SelectedLine)
            lineDO.SelectedMetrics(linePS[currLineInspectionIndex]);
        else if (linePSUIRef.summaryDropdown.value == LinePS.AllLinesCombined)
            lineDO.AllCombinedMetrics(linePS);
    }

    public void SetLineInspection(int index)
    {
        currLineInspectionIndex = index;
        UpdateLineInspectionHeader(linePSUIRef.summaryDropdown.value);
    }

    public void ResetAndClearOutput()
    {
        linePSUIRef.totalLengthVal.text = "0 km";
        linePSUIRef.noDataLengthVal.text = "0";

        foreach (var item in lineDO.inspectorOutputItemLabels)
        {
            Destroy(item.Value.gameObject);
        }
        lineDO.inspectorOutputItemLabels.Clear();

        lineDO.dictVals?.Clear();
        lineDO.dictUnits?.Clear();
        lineDO.dictColors?.Clear();
        lineDO.dictNoDatas?.Clear();

        linePSUIRef.summaryDropdown.SetValueWithoutNotify(LinePS.SelectedLine);
        linePSUIRef.ShowSummaryHeaderAndDropdown(false);
        linePSUIRef.ShowMetricsHeaderAndDropdown(false);
        linePSUIRef.ShowHeader(false);
        linePSUIRef.ShowPropertiesAndSummaryLabel(false);
    }

    //
    // Private Methods
    //

    private void ComputeAndUpdateNoDataLength(Dictionary<string, List<float>> vals, Dictionary<string, int> noDataCounts)
    {
        // TODO: Compute 'NoData' percentage properly
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
        linePS[currLineInspectionIndex].totalDataCount = totalDataCount;

        // Line drawn on area where there is no data
        linePS[currLineInspectionIndex].noDataLength = (totalDataCount == 0) ? 1.0f : noDataCount / (float)totalDataCount;

        // Update total length shown
        if (linePSUIRef.summaryDropdown.value == LinePS.SelectedLine)
            linePSUIRef.SelectedNoDataLength(linePS[currLineInspectionIndex]);
        else if (linePSUIRef.summaryDropdown.value == LinePS.AllLinesCombined)
            lineDO.AllCombinedNoDataLength(linePS, linePSUIRef);
    }

    private void UpdateLineInspectionHeader(int option)
    {
        if (option.Equals(LinePS.SelectedLine))
            linePSUIRef.SelectedHeader("Inspection Line", currLineInspectionIndex);
        else if (option.Equals(LinePS.AllLinesCombined))
            linePSUIRef.inspectionHeader.text = translator.Get("All Lines Combined");
        else
            linePSUIRef.inspectionHeader.text = translator.Get("All Lines Compared");
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
        // linePSUIRef.SetDropDownInteractive(false);
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

    private void PlotLineData()
    {
        currLineInspectionIndex = inspectorTool.lineInspectorPanel.lineInspector.CurrLineInspection;

        if (currLineInspectionIndex == -1)
            return;

        LinePS currPS = linePS[currLineInspectionIndex];

        foreach (var inspectorOutputItem in lineDO.itemslist)
        {
            InspectorOutputItemLabel inspectorOutputItemLabel = null;
            string itemName = inspectorOutputItem.name;

            if (lineDO.inspectorOutputItemLabels.ContainsKey(itemName))
            {
                inspectorOutputItemLabel = lineDO.inspectorOutputItemLabels[itemName];
            }
            else
            {
                inspectorOutputItemLabel = Instantiate(itemPrefab, linePSUIRef.container, false);

                inspectorOutputItemLabel.name = itemName;
                inspectorOutputItemLabel.SetName(itemName);
                inspectorOutputItemLabel.SetUnitsValue(inspectorOutputItem.units);
                inspectorOutputItemLabel.SetDotColor(inspectorOutputItem.dotColor);

                lineDO.inspectorOutputItemLabels.Add(itemName, inspectorOutputItemLabel);
            }

            float max = inspectorOutputItem.maxVal,
                  min = inspectorOutputItem.minVal,
                  median = inspectorOutputItem.medianVal,
                  mean = inspectorOutputItem.meanVal,
                  sum = inspectorOutputItem.sumVal;

            inspectorOutputItemLabel.SetMaxValue(max.ToString("#,##0.##"));
            inspectorOutputItemLabel.SetMedianValue(median.ToString("#,##0.##"));
            inspectorOutputItemLabel.SetMinValue(min.ToString("#,##0.##"));
            inspectorOutputItemLabel.SetMeanValue(mean.ToString("#,##0.##"));
            inspectorOutputItemLabel.SetSumValue(sum.ToString("#,##0.##"));

            PS.AddOrUpdateMetric(currPS.maxVal, itemName, max);
            PS.AddOrUpdateMetric(currPS.minVal, itemName, min);
            PS.AddOrUpdateMetric(currPS.medianVal, itemName, median);
            PS.AddOrUpdateMetric(currPS.meanVal, itemName, mean);
            PS.AddOrUpdateMetric(currPS.sumVal, itemName, sum);
        }
    }

    private void SelectedLineOutput()
    {
        linePSUIRef.ShowMetricsHeaderAndDropdown(false);
        UpdateLineInspectionHeader(LinePS.SelectedLine);
        linePSUIRef.SelectedTotalLength(linePS[currLineInspectionIndex]);
        linePSUIRef.SelectedNoDataLength(linePS[currLineInspectionIndex]);
        lineDO.SelectedMetrics(linePS[currLineInspectionIndex]);
    }

    private void AllLinesCombinedOutput()
    {
        linePSUIRef.ShowMetricsHeaderAndDropdown(false);
        UpdateLineInspectionHeader(LinePS.AllLinesCombined);
        lineDO.AllCombinedTotalLength(linePS, linePSUIRef);
        lineDO.AllCombinedNoDataLength(linePS, linePSUIRef);
        lineDO.AllCombinedMetrics(linePS);
    }

    private void AllLinesComparedOutput()
    {
        ShowMetricsHeaderAndDropdown(true);
    }

    private void ShowCorrelationOutput(bool show)
    {
        linePSUIRef.correlationLabel.gameObject.SetActive(show);
        linePSUIRef.correlationGroup.SetActive(show);

        linePSUIRef.ShowMetricsHeaderAndDropdown(false);
        linePSUIRef.ShowHeader(!show);
        linePSUIRef.ShowPropertiesAndSummaryLabel(!show);
    }

    private void UpdateLinePanel(int option)
    {
        if (option.Equals(LinePS.Correlation))
        {
            footnote.SetActive(false);
            ShowCorrelationOutput(true);
        }
        else
        {
            footnote.SetActive(true);
            ShowCorrelationOutput(false);

            UpdateLineInspectionHeader(option);
            linePSUIRef.UpdatePropertiesLabel(PSO.LinePropertiesLabels[option]);
            linePSUIRef.UpdateSummaryLabel(PSO.SummaryLabels[option]);

            if (option.Equals(LinePS.SelectedLine))
                SelectedLineOutput();
            else if (option.Equals(LinePS.AllLinesCombined))
                AllLinesCombinedOutput();
            // else
            //     AllLinesComparedOutput();
        }
    }

    private void PopulateCorrelationGroup()
    {
        var activeDataLayers = dataLayers.activeLayerPanels;
        var count = activeDataLayers.Count;

        // For each data layer, loop runs for a total of totalCoeffs * N iterations
        for (int i = 0; i < count; ++i)
        {
            // Run against another data layer
            for (int j = i + 1; j < count; ++j)
            {
                if (!dataLayers.CorrCoeffs[i, j].HasValue)
                    continue;

                var correlationPair = Instantiate(correlationPairPrefab, linePSUIRef.correlationGroup.transform);

                var dataLayer1 = activeDataLayers[i].DataLayer;
                correlationPair.SetDotColor1(dataLayer1.Color);
                correlationPair.SetName1(dataLayer1.Name);

                var dataLayer2 = activeDataLayers[j].DataLayer;
                correlationPair.SetDotColor2(dataLayer2.Color);
                correlationPair.SetName2(dataLayer2.Name);

                correlationPair.SetCoefficientValue(dataLayers.CorrCoeffs[i, j].Value.ToString("#,##0.##"));
            }
        }
    }

    private IEnumerator CalculateSpearmanCorrelation()
    {
        linePSUIRef.StartProgress();

        var activeDataLayers = dataLayers.activeLayerPanels;
        var count = activeDataLayers.Count;
        dataLayers.CorrCoeffs = new float?[count, count];

        linePSUIRef.SetCorrelationProgress(0.3f);
        yield return null;

        inspectorTool.InspectOutput.ComputeCorrelations(dataLayers);
        while (inspectorTool.InspectOutput.StillRunning())
        {
            linePSUIRef.computeCorrelationButton.interactable = false;
            linePSUIRef.summaryDropdown.interactable = false;
            linePSUIRef.SetCorrelationProgress(0.5f);
            yield return null;
        }
        linePSUIRef.SetCorrelationProgress(0.99f);
        yield return null;

        linePSUIRef.StopProgress();

        PopulateCorrelationGroup();
        linePSUIRef.summaryDropdown.interactable = true;
        linePSUIRef.computeCorrelationButton.interactable = true;
    }
}