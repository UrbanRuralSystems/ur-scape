// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli

#define USE_TASKS

using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class InspectorOutputItem
{
    public string name;
    public float maxVal;
    public float minVal;
    public float meanVal;
    public float medianVal;
    public float sumVal;
    public string units;
	public Color dotColor;
}

public class InspectorOutputDropdown
{
	public Dropdown dropdown;
	public string[] options;

	public InspectorOutputDropdown(Dropdown dropdown, string[] options)
	{
		this.dropdown = dropdown;
		this.options = options;
	}
}

public class InspectorOutput : MonoBehaviour, IOutput {

    [Header("UI References")]
    [SerializeField]
    private LineInspectorOutput lineInspectorOutput = default;
    [SerializeField]
    private AreaInspectorOutput areaInspectorOutput = default;
    
    public LineInspectorOutput LineOutput { get { return lineInspectorOutput; } }
    public AreaInspectorOutput AreaOutput { get { return areaInspectorOutput; } }

    private readonly CorrelationTaskScheduler scheduler = new CorrelationTaskScheduler();
    public bool StillRunning() => scheduler.IsRunning;

	//
	// Public Methods
	//

	public void OutputToCSV(TextWriter csv)
	{
        lineInspectorOutput.OutputToCSV(csv);
        areaInspectorOutput.OutputToCSV(csv);
	}

	public void ActivateOutputPanel(InspectorTool.InspectorType inspectorType)
	{
		bool isLineType = inspectorType == InspectorTool.InspectorType.Line;
        lineInspectorOutput.gameObject.SetActive(isLineType ? true : false);
        areaInspectorOutput.gameObject.SetActive(isLineType ? false : true);
	}

	public void ShowSummaryHeaderAndDropdown(InspectorTool.InspectorType inspectorType, bool show)
	{
        if (inspectorType == InspectorTool.InspectorType.Line)
            lineInspectorOutput.ShowSummaryHeaderAndDropdown(show);
        else
            areaInspectorOutput.ShowSummaryHeaderAndDropdown(show);
	}

	public void ShowMetricsHeaderAndDropdown(InspectorTool.InspectorType inspectorType, bool show)
	{
        if (inspectorType == InspectorTool.InspectorType.Line)
            lineInspectorOutput.ShowMetricsHeaderAndDropdown(show);
        else
            areaInspectorOutput.ShowMetricsHeaderAndDropdown(show);
	}

	public void ShowHeader(InspectorTool.InspectorType inspectorType, bool show)
	{
        if (inspectorType == InspectorTool.InspectorType.Line)
            lineInspectorOutput.ShowHeader(show);
        else
            areaInspectorOutput.ShowHeader(show);
	}

	public void ShowPropertiesAndSummaryLabel(InspectorTool.InspectorType inspectorType, bool show)
	{
        if (inspectorType == InspectorTool.InspectorType.Line)
            lineInspectorOutput.ShowPropertiesAndSummaryLabel(show);
        else
            areaInspectorOutput.ShowPropertiesAndSummaryLabel(show);
	}

	public void ShowInspectorOutputItemLabel(InspectorTool.InspectorType inspectorType, string itemName, bool show)
	{
        if (inspectorType == InspectorTool.InspectorType.Line)
            lineInspectorOutput.ShowInspectorOutputItemLabel(itemName, show);
        else
            areaInspectorOutput.ShowInspectorOutputItemLabel(itemName, show);
	}

	public void SetDropDownInteractive(InspectorTool.InspectorType inspectorType, bool isOn)
	{
        if (inspectorType == InspectorTool.InspectorType.Line)
            lineInspectorOutput.SetDropDownInteractive(isOn);
        else
            areaInspectorOutput.SetDropDownInteractive(isOn);
	}

	public void ComputeAndUpdateMetrics(InspectorTool.InspectorType inspectorType)
	{
        if (inspectorType == InspectorTool.InspectorType.Line)
            lineInspectorOutput.ComputeAndUpdateMetrics();
        else
            areaInspectorOutput.ComputeAndUpdateMetrics();
	}

	public void SetInspection(InspectorTool.InspectorType inspectorType, int index)
	{
        if (inspectorType == InspectorTool.InspectorType.Line)
            lineInspectorOutput.SetLineInspection(index);
        else
            areaInspectorOutput.SetAreaInspection(index);
	}

	public void ResetAndClearOutput(InspectorTool.InspectorType inspectorType)
	{
        if (inspectorType == InspectorTool.InspectorType.Line)
            lineInspectorOutput.ResetAndClearOutput();
        else
            areaInspectorOutput.ResetAndClearOutput();
	}

    public void ComputeCorrelations(DataLayers dataLayers)
    {
        var activeDataLayers = dataLayers.activeLayerPanels;
        var count = activeDataLayers.Count;

        // For each data layer, loop runs for a total of totalCoeffs * N iterations
        for (int i = 0; i < count; ++i)
        {
            // Coeffs of same data layer will always be 1
            dataLayers.CorrCoeffs[i, i] = 1.0f;

            // Run against another demo
            for (int j = i + 1; j < count; ++j)
            {
                var taskInfo = new CorrelationTaskInfo(SpearmanCorrelation.CorrelationCalculationTask)
                {
                    dataLayers = dataLayers,
                    a = i,
                    b = j,
                };
#if USE_TASKS
                scheduler.Add(taskInfo);
#else
                taskInfo.task(taskInfo);
#endif
            }
        }

#if USE_TASKS
		// Start Scheduler
        scheduler.Run(this, null);
#endif
    }
}
