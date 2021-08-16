// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class AreaPropertiesAndSummariesUIRef : MonoBehaviour
{
    [Header("UI References")]
    public Transform container;
    public Transform summaryValuesHeader;
    public Dropdown summaryDropdown;
    public Transform metricsValuesHeader;
    public Dropdown metricsDropdown;
    public Text propertiesLabel;
    public Text summaryLabel;
    public Text inspectionHeader;
    public Text totalAreaVal;
    public Text noDataAreaVal;
    public Text correlationLabel;
    public Button computeCorrelationButton;
    public Text correlationWarning;
    public Scrollbar correlationProgress;
    public GameObject correlationGroup;

    private ITranslator translator;

    public void Init(ITranslator translator)
    {
        this.translator = translator;
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

    public void ShowPropertiesAndSummaryLabel(bool show, int type)
	{
        if (type.Equals(AreaInspectorOutput.Area))
		    propertiesLabel.gameObject.SetActive(show);
		summaryLabel.gameObject.SetActive(show);
	}

    public void SetDropDownInteractive(bool isOn)
	{
		summaryDropdown.interactable = isOn;
	}

    public void SelectedHeader(string val, int index)
	{
		inspectionHeader.text = translator.Get(val) + " " + (index + 1);
	}

    public void UpdatePropertiesLabel(string val)
	{
		propertiesLabel.text = translator.Get(val);
	}

	public void UpdateSummaryLabel(string val)
	{
		summaryLabel.text = translator.Get(val);
	}

    public void SelectedTotalArea(AreaPropertiesAndSummaries areaPS)
	{
        var suffix = "";
        float area = (float)(areaPS.totalArea * 0.000001);
        if (area > 1e+12)
        {
            area *= 1e-12f;
            suffix = " " + translator.Get("trillion");
        }
        else if (area > 1e+9)
        {
            area *= 1e-9f;
            suffix = " " + translator.Get("billion");
        }
        else if (area > 1e+6)
        {
            area *= 1e-6f;
            suffix = " " + translator.Get("million");
        }

		totalAreaVal.text = area.ToString("#,##0.##") + suffix + " km\xB2";
	}

	public void SelectedNoDataArea(AreaPropertiesAndSummaries areaPS)
	{
		noDataAreaVal.text = (areaPS.noDataArea * 100).ToString("0.##") + "%";
	}

    public void StartProgress()
    {
        correlationProgress.size = 0;
        correlationProgress.gameObject.SetActive(true);
    }

    public void SetCorrelationProgress(float progress)
    {
        if (progress >= 1)
        {
            // Hide progress
            correlationProgress.gameObject.SetActive(false);
		}
		else
        {
            correlationProgress.size = progress;
        }
    }

    public void StopProgress()
    {
        correlationProgress.gameObject.SetActive(false);
	}
}