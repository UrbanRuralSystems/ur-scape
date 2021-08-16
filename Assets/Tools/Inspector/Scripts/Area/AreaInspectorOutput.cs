// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

using AreaInfo = AreaInspector.AreaInspectorInfo;
using PSO = PropertiesAndSummariesOptions;
using PS = PropertiesAndSummaries;
using AreaPS = AreaPropertiesAndSummaries;
using ContourPS = ContourPropertiesAndSummaries;
using AreaPSUIRef = AreaPropertiesAndSummariesUIRef;
using ContourPSUIRef = ContourPropertiesAndSummariesUIRef;

public class AreaInspectorOutput : MonoBehaviour
{
    [Header("Prefabs")]
    public InspectorOutputItemLabel itemPrefab;
    public CorrelationPairItem correlationPairPrefab;

    [Header("UI References")]
    public Transform areaInspectorEntryInfo = default;
    [SerializeField]
    private Transform areaTypeHeader = default;
    [SerializeField]
    private Dropdown areaTypeDropdown = default;
    [SerializeField]
    private AreaPSUIRef areaPSUIRef = default;
    [SerializeField]
    private ContourPSUIRef contourPSUIRef = default;
    [SerializeField]
    private GameObject footnote = default;

    // Constants

    public static readonly int Area = 0;
    public static readonly int Contour = 1;


    private readonly string[] AreaTypeOptions = {
        "Area"/*translatable*/,
        "Contour"/*translatable*/,
    };

    private InspectorTool inspectorTool;
    private ContoursTool contoursTool;
    private DataLayers dataLayers;
    private GridLayerController gridLayerController;
    private ITranslator translator;
    private int currAreaInspectionIndex = 0;
    private InspectorOutputDropdown[] inspectorOutputDropdowns;
    private AreaPS[] areaPS;
    private ContourPS contourPS;
    private AreaDataOutput areaDO = new AreaDataOutput();
    private ContourDataOutput contourDO = new ContourDataOutput();

    //
    // Unity Methods
    //

    private void Awake()
    {
        // Components
        var componentManager = ComponentManager.Instance;
        inspectorTool = componentManager.Get<InspectorTool>();
        contoursTool = componentManager.Get<ContoursTool>();
        dataLayers = componentManager.Get<DataLayers>();
        gridLayerController = inspectorTool.Map.GetLayerController<GridLayerController>();
        translator = LocalizationManager.Instance;

        areaPSUIRef.Init(translator);
        contourPSUIRef.Init(translator);

        // Initialize dropdowns
        inspectorOutputDropdowns = new InspectorOutputDropdown[]
        {
            new InspectorOutputDropdown(areaTypeDropdown, AreaTypeOptions),
            new InspectorOutputDropdown(areaPSUIRef.summaryDropdown, PSO.AreaSummaryOptions),
            new InspectorOutputDropdown(areaPSUIRef.metricsDropdown, PSO.MetricsOptions),
        };
        InitDropdowns();

        // Initialize properties and summaries
        int maxInspectionCount = inspectorTool.maxInspectionCount;
        areaPS = new AreaPS[maxInspectionCount];
        for (int i = 0; i < maxInspectionCount; ++i)
        {
            areaPS[i] = new AreaPS();
        }
        contourPS = new ContourPS();

        // Initialize listeners
        gridLayerController.OnShowGrid += OnShowGrid;
        LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        areaTypeDropdown.onValueChanged.AddListener(UpdatePropertiesAndSummariesPanel);
        areaPSUIRef.summaryDropdown.onValueChanged.AddListener(UpdateAreaPanel);
        areaPSUIRef.computeCorrelationButton.onClick.AddListener(OnComputeCorrelationClicked);

        // Update panels
        UpdatePropertiesAndSummariesPanel(Area);
        UpdateAreaPanel(AreaPS.SelectedArea);
        UpdateContourPanel();

        ResetAndClearOutput();

        // If contours tool is already open and there is a selected contour
        if (contoursTool != null && inspectorTool.InspectOutput != null)
            inspectorTool.InspectOutput.AreaOutput.UpdateContourInspectorOutput(dataLayers);
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
        UpdateAreaInspectionHeader(areaPSUIRef.summaryDropdown.value);
        areaPSUIRef.UpdatePropertiesLabel(PSO.AreaPropertiesLabels[areaPSUIRef.summaryDropdown.value]);
        areaPSUIRef.UpdateSummaryLabel(PSO.SummaryLabels[areaPSUIRef.summaryDropdown.value]);
        UpdateDropdowns();

        UpdateContourInspectionHeader();
        contourPSUIRef.UpdatePropertiesLabel(PSO.ContourPropertiesLabel);
        contourPSUIRef.UpdateSummaryLabel(PSO.ContourSummaryLabel);
    }

    private void OnComputeCorrelationClicked()
    {
        if (gridLayerController.mapLayers.Count < 2)
            areaPSUIRef.correlationWarning.gameObject.SetActive(true);
        else
        {
            areaPSUIRef.correlationWarning.gameObject.SetActive(false);

            // Clear correlation list
            var count = areaPSUIRef.correlationGroup.transform.childCount - 1;
            for (int i = count; i >= 0; --i)
            {
                var child = areaPSUIRef.correlationGroup.transform.GetChild(i);
                Destroy(child.gameObject);
            }

            StartCoroutine(CalculateSpearmanCorrelation());
        }
    }

    private void OnShowGrid(GridMapLayer mapLayer, bool show)
    {
        if (gridLayerController.mapLayers.Count >= 2 && areaPSUIRef.correlationWarning != null)
            areaPSUIRef.correlationWarning.gameObject.SetActive(false);
    }

    //
    // Public Methods
    //

    public void OutputToCSV(TextWriter csv)
    {
        var translator = LocalizationManager.Instance;
        csv.WriteLine("{0}", translator.Get("Area Inspection Tool"));
        foreach (var inspectorItem in areaDO.itemslist)
        {
            string max = inspectorItem.maxVal.ToString() + " " + inspectorItem.units;
            string min = inspectorItem.minVal.ToString() + " " + inspectorItem.units;
            string median = inspectorItem.medianVal.ToString() + " " + inspectorItem.units;

            csv.WriteLine("{0}", inspectorItem.name);
            csv.WriteLine("{0},{1}", translator.Get("Max"), max);
            csv.WriteLine("{0},{1}", translator.Get("Min"), min);
            csv.WriteLine("{0},{1}", translator.Get("Median"), median);
        }

        if (contoursTool)
        {
            csv.WriteLine("{0}", translator.Get("Selected Contour"));
            foreach (var inspectorItem in contourDO.itemslist)
            {
                string max = inspectorItem.maxVal.ToString() + " " + inspectorItem.units;
                string min = inspectorItem.minVal.ToString() + " " + inspectorItem.units;
                string median = inspectorItem.medianVal.ToString() + " " + inspectorItem.units;
                string mean = inspectorItem.meanVal.ToString() + " " + inspectorItem.units;
                string sum = inspectorItem.sumVal.ToString() + " " + inspectorItem.units;

                csv.WriteLine("{0}", inspectorItem.name);
                csv.WriteLine("{0},{1}", translator.Get("Max"), max);
                csv.WriteLine("{0},{1}", translator.Get("Min"), min);
                csv.WriteLine("{0},{1}", translator.Get("Median"), median);
                csv.WriteLine("{0},{1}", translator.Get("Mean"), mean);
                csv.WriteLine("{0},{1}", translator.Get("Sum"), sum);
            }
        }
    }

    public void UpdateAreaInspectorOutput(AreaInfo areaInfo, DataLayers dataLayers)
    {
        if (!areaPSUIRef.summaryDropdown.value.Equals(AreaPS.SelectedArea))
            return;

        Dictionary<string, List<float>> idToValueList = new Dictionary<string, List<float>>();
        Dictionary<string, int> idToNoDataList = new Dictionary<string, int>();
        Dictionary<string, string> idToUnitsList = new Dictionary<string, string>();
        Dictionary<string, Color> idToDotColorList = new Dictionary<string, Color>();

        if (areaInfo != null)
        {
            var areaGrids = areaInfo.mapLayer.grids;
            var areaInspectedGrids = areaInfo.mapLayer.inspectedGridsData;

            int areaGridsLength = areaGrids.Count;
            for (int i = 0; i < areaGridsLength; ++i)
            {
                var layerName = dataLayers.activeLayerPanels[i].name;
                var dotColor = dataLayers.activeLayerPanels[i].dot.color;

                int areaInspectedGridsLength = areaInspectedGrids[areaGrids[i]].Count;
                for (int j = 0; j < areaInspectedGridsLength; ++j)
                {
                    // Add all cells to list
                    if (!idToValueList.ContainsKey(layerName))
                    {
                        List<float> data = new List<float>();
                        int noDataCount = 0;
                        float value = areaInspectedGrids[areaGrids[i]][j];
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
                        float value = areaInspectedGrids[areaGrids[i]][j];
                        if (value > 0.0f)
                            list.Add(value);
                        else
                            ++idToNoDataList[layerName];
                    }
                }

                if (!idToUnitsList.ContainsKey(layerName))
                {
                    idToUnitsList.Add(layerName, areaGrids[i].units);
                }

                if (!idToDotColorList.ContainsKey(layerName))
                {
                    idToDotColorList.Add(layerName, dotColor);
                }
            }
            ComputeAndUpdateNoDataArea(idToValueList, idToNoDataList);
            areaDO.SetData(idToValueList, idToUnitsList, idToDotColorList, idToNoDataList, PlotAreaData);
        }
    }

    public void UpdateContourInspectorOutput(DataLayers dataLayers)
    {
        Dictionary<string, List<float>> idToValueList = new Dictionary<string, List<float>>();
        Dictionary<string, int> idToNoDataList = new Dictionary<string, int>();
        Dictionary<string, string> idToUnitsList = new Dictionary<string, string>();
        Dictionary<string, Color> idToDotColorList = new Dictionary<string, Color>();

        if (contoursTool)
        {
            if (contoursTool.ContoursLayer == null)
                return;

            var contourGrids = contoursTool.ContoursLayer.grids;
            var contourInspectedGrids = contoursTool.ContoursLayer.inspectedGridsData;

            int contourGridsLength = contourGrids.Count;
            for (int i = 0; i < contourGridsLength; ++i)
            {
                var layerName = dataLayers.activeLayerPanels[i].name;
                var dotColor = dataLayers.activeLayerPanels[i].dot.color;

                int contourInspectedGridsLength = contourInspectedGrids[contourGrids[i]].Count;
                for (int j = 0; j < contourInspectedGridsLength; ++j)
                {
                    // Add all cells to list
                    if (!idToValueList.ContainsKey(layerName))
                    {
                        List<float> data = new List<float>();
                        int noDataCount = 0;
                        float value = contourInspectedGrids[contourGrids[i]][j];
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
                        float value = contourInspectedGrids[contourGrids[i]][j];
                        if (value > 0.0f)
                            list.Add(value);
                        else
                            ++idToNoDataList[layerName];
                    }
                }

                if (!idToUnitsList.ContainsKey(layerName))
                {
                    idToUnitsList.Add(layerName, contourGrids[i].units);
                }

                if (!idToDotColorList.ContainsKey(layerName))
                {
                    idToDotColorList.Add(layerName, dotColor);
                }

                contourDO.SetData(idToValueList, idToUnitsList, idToDotColorList, idToNoDataList, PlotContourData);
            }
        }
    }

    public void ShowAreaTypeHeaderAndDropdown(bool show)
    {
        areaTypeHeader.gameObject.SetActive(show);
        areaTypeDropdown.gameObject.SetActive(show);
    }

    public void ShowSummaryHeaderAndDropdown(bool show)
    {
        if (areaTypeDropdown.value.Equals(Area))
            areaPSUIRef.ShowSummaryHeaderAndDropdown(show);
        else
        {
            if (contoursTool)
                contourPSUIRef.ShowSummaryHeaderAndDropdown(show && contoursTool.IsToggled);
        }
    }

    public void ShowMetricsHeaderAndDropdown(bool show)
    {
        if (areaTypeDropdown.value.Equals(Area))
            areaPSUIRef.ShowMetricsHeaderAndDropdown(show);
    }

    public void ShowHeader(bool show)
    {
        if (areaTypeDropdown.value.Equals(Area))
            areaPSUIRef.ShowHeader(show && !areaPSUIRef.summaryDropdown.value.Equals(AreaPS.Correlation));
        else
        {
            if (contoursTool)
                contourPSUIRef.ShowHeader(show && contoursTool.IsToggled);
        }
    }

    public void ShowPropertiesAndSummaryLabel(bool show)
    {
        if (areaTypeDropdown.value.Equals(Area))
            areaPSUIRef.ShowPropertiesAndSummaryLabel(show && !areaPSUIRef.summaryDropdown.value.Equals(AreaPS.Correlation), areaTypeDropdown.value);
        else
        {
            if (contoursTool)
                contourPSUIRef.ShowPropertiesAndSummaryLabel(show && contoursTool.IsToggled, areaTypeDropdown.value);
        }
    }

    public void ShowInspectorOutputItemLabel(string itemName, bool show)
    {
        areaDO.inspectorOutputItemLabels.TryGetValue(itemName, out InspectorOutputItemLabel item);
        item?.gameObject.SetActive(show);
    }

    public void ComputeAndUpdateTotalArea(AreaInfo[] areaInfos, int index)
    {
        currAreaInspectionIndex = index;
        AreaInfo areaInfo = areaInfos[index];

        // Compute area
        areaPS[currAreaInspectionIndex].totalArea = areaInfo.mapLayer.ComputeTotalArea();

        // Update total area shown
        if (areaPSUIRef.summaryDropdown.value == AreaPS.SelectedArea)
            areaPSUIRef.SelectedTotalArea(areaPS[currAreaInspectionIndex]);
        else if (areaPSUIRef.summaryDropdown.value == AreaPS.AllAreasCombined)
            areaDO.AllCombinedTotalArea(areaPS, areaPSUIRef);
    }

    public void ComputeAndUpdateMetrics()
    {
        if (areaPSUIRef.summaryDropdown.value == AreaPS.SelectedArea)
            areaDO.SelectedMetrics(areaPS[currAreaInspectionIndex]);
        else if (areaPSUIRef.summaryDropdown.value == AreaPS.AllAreasCombined)
            areaDO.AllCombinedMetrics(areaPS);
    }

    public void SetAreaInspection(int index)
    {
        currAreaInspectionIndex = index;
        UpdateAreaInspectionHeader(areaPSUIRef.summaryDropdown.value);
    }

    public void SetDropDownInteractive(bool isOn)
    {
        if (areaTypeDropdown.value.Equals(Area))
            areaPSUIRef.SetDropDownInteractive(isOn);
    }

    public void ResetAndClearAreaOutput()
    {
        areaPSUIRef.totalAreaVal.text = "0 km\xB2";
        areaPSUIRef.noDataAreaVal.text = "0";

        foreach (var item in areaDO.inspectorOutputItemLabels)
        {
            Destroy(item.Value.gameObject);
        }
        areaDO.inspectorOutputItemLabels.Clear();

        areaDO.dictVals?.Clear();
        areaDO.dictUnits?.Clear();
        areaDO.dictColors?.Clear();
        areaDO.dictNoDatas?.Clear();

        areaPSUIRef.summaryDropdown.SetValueWithoutNotify(AreaPS.SelectedArea);
        areaPSUIRef.ShowSummaryHeaderAndDropdown(false);
        areaPSUIRef.ShowMetricsHeaderAndDropdown(false);
        areaPSUIRef.ShowHeader(false);
        areaPSUIRef.ShowPropertiesAndSummaryLabel(false, areaTypeDropdown.value);
    }

    public void ResetAndClearContourOutput()
    {
        contourPSUIRef.totalAreaVal.text = "0 km\xB2";
        contourPSUIRef.noDataAreaVal.text = "0";

        foreach (var item in contourDO.inspectorOutputItemLabels)
        {
            Destroy(item.Value.gameObject);
        }
        contourDO.inspectorOutputItemLabels.Clear();

        contourDO.dictVals?.Clear();
        contourDO.dictUnits?.Clear();
        contourDO.dictColors?.Clear();
        contourDO.dictNoDatas?.Clear();

        contourPSUIRef.summaryDropdown.SetValueWithoutNotify(AreaPS.SelectedArea);
        contourPSUIRef.ShowSummaryHeaderAndDropdown(false);
        contourPSUIRef.ShowMetricsHeaderAndDropdown(false);
        contourPSUIRef.ShowHeader(false);
        contourPSUIRef.ShowPropertiesAndSummaryLabel(false, areaTypeDropdown.value);
    }

    public void ResetAndClearOutput()
    {
        ResetAndClearAreaOutput();
        ResetAndClearContourOutput();
    }

    //
    // Private Methods
    //

    private void ComputeAndUpdateNoDataArea(Dictionary<string, List<float>> vals, Dictionary<string, int> noDataCounts)
    {
        // TODO: Compute 'NoData' percentage properly
        currAreaInspectionIndex = inspectorTool.areaInspectorPanel.areaInspector.CurrAreaInspection;

        if (currAreaInspectionIndex == -1)
            return;

        int noDataCount = 0, hasDataCount = 0;
        foreach (var pair in vals)
        {
            hasDataCount += pair.Value.Count;
            noDataCount += noDataCounts[pair.Key];
        }
        int totalDataCount = hasDataCount + noDataCount;
        // Debug.Log($"NoData: {noDataCount}, Total: {totalDataCount}, Percent: {((float)noDataCount / totalDataCount) * 100f}");
        areaPS[currAreaInspectionIndex].totalDataCount = totalDataCount;

        // Area drawn on area where there is no data
        areaPS[currAreaInspectionIndex].noDataArea = (totalDataCount == 0) ? 1.0f : noDataCount / (float)totalDataCount;

        // Update total length shown
        if (areaPSUIRef.summaryDropdown.value == AreaPS.SelectedArea)
            areaPSUIRef.SelectedNoDataArea(areaPS[currAreaInspectionIndex]);
        else if (areaPSUIRef.summaryDropdown.value == AreaPS.AllAreasCombined)
            areaDO.AllCombinedNoDataArea(areaPS, areaPSUIRef);
    }

    private void UpdateAreaInspectionHeader(int option)
    {
        if (option.Equals(AreaPS.SelectedArea))
            areaPSUIRef.SelectedHeader("Inspection Area", currAreaInspectionIndex);
        else if (option.Equals(AreaPS.AllAreasCombined))
            areaPSUIRef.inspectionHeader.text = translator.Get("All Areas Combined");
        else
            areaPSUIRef.inspectionHeader.text = translator.Get("All Areas Compared");
    }

    private void UpdateContourInspectionHeader()
    {
        contourPSUIRef.inspectionHeader.text = translator.Get("Selected Contour");
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
        // areaPSUIRef.SetDropDownInteractive(false);
        // contourPSUIRef.SetDropDownInteractive(false);
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

    private void PlotAreaData()
    {
        currAreaInspectionIndex = inspectorTool.areaInspectorPanel.areaInspector.CurrAreaInspection;

        if (currAreaInspectionIndex == -1)
            return;

        AreaPS currPS = areaPS[currAreaInspectionIndex];

        foreach (var inspectorOutputItem in areaDO.itemslist)
        {
            InspectorOutputItemLabel inspectorOutputItemLabel = null;
            string itemName = inspectorOutputItem.name;

            if (areaDO.inspectorOutputItemLabels.ContainsKey(itemName))
            {
                inspectorOutputItemLabel = areaDO.inspectorOutputItemLabels[itemName];
            }
            else
            {
                inspectorOutputItemLabel = Instantiate(itemPrefab, areaPSUIRef.container, false);

                inspectorOutputItemLabel.name = itemName;
                inspectorOutputItemLabel.SetName(itemName);
                inspectorOutputItemLabel.SetUnitsValue(inspectorOutputItem.units);
                inspectorOutputItemLabel.SetDotColor(inspectorOutputItem.dotColor);

                areaDO.inspectorOutputItemLabels.Add(itemName, inspectorOutputItemLabel);
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

    private void PlotContourData()
    {
        foreach (var inspectorOutputItem in contourDO.itemslist)
        {
            InspectorOutputItemLabel inspectorOutputItemLabel = null;
            string itemName = inspectorOutputItem.name;

            if (contourDO.inspectorOutputItemLabels.ContainsKey(itemName))
            {
                inspectorOutputItemLabel = contourDO.inspectorOutputItemLabels[itemName];
            }
            else
            {
                inspectorOutputItemLabel = Instantiate(itemPrefab, contourPSUIRef.container, false);

                inspectorOutputItemLabel.name = itemName;
                inspectorOutputItemLabel.SetName(itemName);
                inspectorOutputItemLabel.SetUnitsValue(inspectorOutputItem.units);
                inspectorOutputItemLabel.SetDotColor(inspectorOutputItem.dotColor);

                contourDO.inspectorOutputItemLabels.Add(itemName, inspectorOutputItemLabel);
            }

            float max = inspectorOutputItem.maxVal,
                  min = inspectorOutputItem.minVal,
                  mean = inspectorOutputItem.meanVal,
                  median = inspectorOutputItem.medianVal,
                  sum = inspectorOutputItem.sumVal;

            inspectorOutputItemLabel.SetMaxValue(max.ToString("#,##0.##"));
            inspectorOutputItemLabel.SetMeanValue(mean.ToString("#,##0.##"));
            inspectorOutputItemLabel.SetMedianValue(median.ToString("#,##0.##"));
            inspectorOutputItemLabel.SetMinValue(min.ToString("#,##0.##"));
            inspectorOutputItemLabel.SetSumValue(sum.ToString("#,##0.##"));

            PS.AddOrUpdateMetric(contourPS.maxVal, itemName, max);
            PS.AddOrUpdateMetric(contourPS.minVal, itemName, min);
            PS.AddOrUpdateMetric(contourPS.meanVal, itemName, mean);
            PS.AddOrUpdateMetric(contourPS.medianVal, itemName, median);
            PS.AddOrUpdateMetric(contourPS.sumVal, itemName, sum);
        }
    }

    private void SelectedAreaOutput()
    {
        areaPSUIRef.ShowMetricsHeaderAndDropdown(false);
        UpdateAreaInspectionHeader(AreaPS.SelectedArea);
        areaPSUIRef.SelectedTotalArea(areaPS[currAreaInspectionIndex]);
        areaPSUIRef.SelectedNoDataArea(areaPS[currAreaInspectionIndex]);
        areaDO.SelectedMetrics(areaPS[currAreaInspectionIndex]);
    }

    private void SelectedContourOutput()
    {
        contourPSUIRef.ShowMetricsHeaderAndDropdown(false);
        UpdateContourInspectionHeader();
        //contourPSUIRef.SelectedTotalArea(areaPS[currAreaInspectionIndex]);
        //contourPSUIRef.SelectedNoDataArea(areaPS[currAreaInspectionIndex]);
        contourDO.SelectedMetrics(contourPS);
    }

    private void AllAreasCombinedOutput()
    {
        areaPSUIRef.ShowMetricsHeaderAndDropdown(false);
        UpdateAreaInspectionHeader(AreaPS.AllAreasCombined);
        areaDO.AllCombinedTotalArea(areaPS, areaPSUIRef);
        areaDO.AllCombinedNoDataArea(areaPS, areaPSUIRef);
        areaDO.AllCombinedMetrics(areaPS);
    }

    private void AllAreasComparedOutput()
    {
        areaPSUIRef.ShowMetricsHeaderAndDropdown(true);
    }

    private void UpdatePropertiesAndSummariesPanel(int type)
    {
        bool isArea = type.Equals(Area);

        areaPSUIRef.gameObject.SetActive(isArea);
        contourPSUIRef.gameObject.SetActive(!isArea);

        if (isArea)
        {
            var areaInspector = inspectorTool.areaInspectorPanel.areaInspector;
            bool areaAvailable = areaInspector.CurrAreaInspection > -1;
            bool correlationEnabled = areaPSUIRef.summaryDropdown.value.Equals(AreaPS.Correlation);

            areaPSUIRef.ShowHeader(areaAvailable && !correlationEnabled);
            areaPSUIRef.ShowPropertiesAndSummaryLabel(areaAvailable && !correlationEnabled, areaTypeDropdown.value);
            ShowCorrelationOutput(correlationEnabled);
        }
        else
        {
            if (contoursTool)
            {
                bool isContourSelected = contoursTool.ContoursLayer.SelectedContour > 1;

                contourPSUIRef.ShowHeader(isContourSelected);
                contourPSUIRef.ShowPropertiesAndSummaryLabel(isContourSelected, areaTypeDropdown.value);
            }
        }
    }

    private void ShowCorrelationOutput(bool show)
    {
        areaPSUIRef.correlationLabel.gameObject.SetActive(show);
        areaPSUIRef.correlationGroup.SetActive(show);

        areaPSUIRef.ShowMetricsHeaderAndDropdown(false);
        areaPSUIRef.ShowHeader(!show);
        ShowPropertiesAndSummaryLabel(!show);
    }

    private void UpdateAreaPanel(int option)
    {
        if (option.Equals(AreaPS.Correlation))
        {
            footnote.SetActive(false);
            ShowCorrelationOutput(true);
        }
        else
        {
            footnote.SetActive(true);
            ShowCorrelationOutput(false);

            UpdateAreaInspectionHeader(option);
            areaPSUIRef.UpdatePropertiesLabel(PSO.AreaPropertiesLabels[option]);
            areaPSUIRef.UpdateSummaryLabel(PSO.SummaryLabels[option]);

            if (option.Equals(AreaPS.SelectedArea))
                SelectedAreaOutput();
            else if (option.Equals(AreaPS.AllAreasCombined))
                AllAreasCombinedOutput();
            else
                AllAreasComparedOutput();
        }
    }

    private void UpdateContourPanel()
    {
        UpdateContourInspectionHeader();
        contourPSUIRef.UpdatePropertiesLabel(PSO.ContourPropertiesLabel);
        contourPSUIRef.UpdateSummaryLabel(PSO.ContourSummaryLabel);

        SelectedContourOutput();
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

                var correlationPair = Instantiate(correlationPairPrefab, areaPSUIRef.correlationGroup.transform);

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
        areaPSUIRef.StartProgress();

        var activeDataLayers = dataLayers.activeLayerPanels;
        var count = activeDataLayers.Count;
        dataLayers.CorrCoeffs = new float?[count, count];

        areaPSUIRef.SetCorrelationProgress(0.3f);
        yield return null;

        inspectorTool.InspectOutput.ComputeCorrelations(dataLayers);
        while (inspectorTool.InspectOutput.StillRunning())
        {
            areaPSUIRef.computeCorrelationButton.interactable = false;
            areaPSUIRef.summaryDropdown.interactable = false;
            areaTypeDropdown.interactable = false;
            areaPSUIRef.SetCorrelationProgress(0.5f);
            yield return null;
        }
        areaPSUIRef.SetCorrelationProgress(0.99f);
        yield return null;

        areaPSUIRef.StopProgress();

        PopulateCorrelationGroup();
        areaTypeDropdown.interactable = true;
        areaPSUIRef.summaryDropdown.interactable = true;
        areaPSUIRef.computeCorrelationButton.interactable = true;
    }
}