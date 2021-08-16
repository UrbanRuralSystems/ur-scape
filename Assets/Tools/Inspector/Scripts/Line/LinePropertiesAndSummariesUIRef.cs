// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class LinePropertiesAndSummariesUIRef : MonoBehaviour
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
    public Text totalLengthVal;
    public Text noDataLengthVal;
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

    public void ShowPropertiesAndSummaryLabel(bool show)
	{
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

    public void SelectedTotalLength(LinePropertiesAndSummaries linePS)
	{
		totalLengthVal.text = linePS.totalLength.ToString("#,##0.##") + " km";
	}

	public void SelectedNoDataLength(LinePropertiesAndSummaries linePS)
	{
		noDataLengthVal.text = (linePS.noDataLength * 100).ToString("0.##") + "%";
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