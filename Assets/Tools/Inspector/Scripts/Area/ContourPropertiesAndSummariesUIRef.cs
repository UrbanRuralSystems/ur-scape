// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class ContourPropertiesAndSummariesUIRef : MonoBehaviour
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

    public void SelectedTotalArea(ContourPropertiesAndSummaries contourPS)
	{
        var suffix = "";
        float area = (float)(contourPS.totalArea * 0.000001);
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

	public void SelectedNoDataArea(ContourPropertiesAndSummaries contourPS)
	{
		noDataAreaVal.text = (contourPS.noDataArea * 100).ToString("0.##") + "%";
	}
}