// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//          Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FilterPanel : LayerOptionsPanel
{
	public static bool UseNonlinearDistribution = true;
	private static readonly float MeanThreshold = 0.1f;
	private static readonly float InvMeanThresholdLog = 1f / Mathf.Log(MeanThreshold);


	[Header("UI References")]
    public Text unitsLabel;
    public SliderEx minSlider;
    public SliderEx maxSlider;
    public InputField inputMin;
    public InputField inputMax;
    public DistributionChart chart;

    [Header("Settings")]
    public int chartSize = 100;   

    private float distributionPower = 1f;
    private int currentLevel = -1;
    private float siteMinValue;
    private float siteMaxValue;
	private float siteMean;
	private float minFilter;
    private float maxFilter;
    private bool avoidSliderUpdate = false;

	private float nextDoubleClickTime;

    private float[] chartValues;
    private float maxChartValue;
    private bool chartNeedsUpdate;

	private static readonly float ChartUpdateInterval = 0.2f;
	private static readonly WaitForSeconds ChartUpdateDelay = new WaitForSeconds(ChartUpdateInterval + 0.02f);
	private bool waitingForChartUpdate;
	private float nextValidUpdateTime;
	private Coroutine delayedChartUpdateCR;

    //
    // Inheritance Methods
    //

    public override void Init(DataLayer dataLayer)
    {
		minSlider.value = dataLayer.MinFilter;
		maxSlider.value = dataLayer.MaxFilter;

		base.Init(dataLayer);

		inputMin.characterValidation = InputField.CharacterValidation.Decimal;
        inputMax.characterValidation = InputField.CharacterValidation.Decimal;

        chartValues = new float[chartSize];
        maxChartValue = 0;

        chart.Init(dataLayer.color);

		if (IsActive)
		{
			ResetUI();
		}
    }

	public override void Show(bool show)
	{
		if (show)
		{
			minSlider.value = GetLinearValue(dataLayer.MinFilter);
			maxSlider.value = GetLinearValue(dataLayer.MaxFilter);
		}

		base.Show(show);

		if (IsActive)
		{
			ResetUI();
        }
	}

	protected override void EnableListeners(bool enable)
    {
        base.EnableListeners(enable);

        if (enable)
        {
			foreach (var site in dataLayer.loadedPatchesInView)
			{
				var data = site.Data as GridData;
				data.OnGridChange += OnGridChange;
				data.OnValuesChange += OnValuesChange;
			}

			minSlider.onValueChanged.AddListener(OnMinSliderChanged);
            maxSlider.onValueChanged.AddListener(OnMaxSliderChanged);

            minSlider.OnClick += OnSliderClick;
            maxSlider.OnClick += OnSliderClick;

            inputMin.onEndEdit.AddListener(OnInputMinChanged);
            inputMax.onEndEdit.AddListener(OnInputMaxChanged);
        }
        else
        {
			RemoveVisibleGridEvents();

            minSlider.onValueChanged.RemoveListener(OnMinSliderChanged);
            maxSlider.onValueChanged.RemoveListener(OnMaxSliderChanged);

            minSlider.OnClick -= OnSliderClick;
            maxSlider.OnClick -= OnSliderClick;

            inputMin.onEndEdit.RemoveListener(OnInputMinChanged);
            inputMax.onEndEdit.RemoveListener(OnInputMaxChanged);
        }
    }

	protected override void OnPanelVisibilityChange(bool visible)
	{
		if (!visible)
		{
			if (waitingForChartUpdate)
			{
				StopCoroutine(delayedChartUpdateCR);
				waitingForChartUpdate = false;
			}
			nextValidUpdateTime = 0;
		}
	}

	//
	// Event Methods
	//

	protected override void OnPatchVisibilityChange(DataLayer dataLayer, Patch patch, bool visible)
    {
        chartNeedsUpdate = true;

        base.OnPatchVisibilityChange(dataLayer, patch, visible);

		var data = patch.Data as GridData;

		if (visible)
        {
			bool changedLevel = patch.level != currentLevel;

			ResetUI(patch);

            if (changedLevel)
            {
                SetMinMaxFilters(GetNonlinearValue(minSlider.value), GetNonlinearValue(maxSlider.value));
            }

			data.OnGridChange += OnGridChange;
			data.OnValuesChange += OnValuesChange;
		}
		else
		{
			data.OnGridChange -= OnGridChange;
			data.OnValuesChange -= OnValuesChange;

			UpdateDistributionChart();
		}
    }

	private void OnGridChange(GridData grid)
	{
		OnValuesChange(grid);
	}

	private void OnValuesChange(GridData grid)
	{
		UpdateValueRange(grid.patch);
		UpdateFilters();
		UpdateChart();
	}

	private void OnSliderClick()
    {
        if (Time.time < nextDoubleClickTime)
        {
			minSlider.value = 0;
            maxSlider.value = 1;
            return;
        }
        nextDoubleClickTime = Time.time + 0.25f;
    }

    private void OnMinSliderChanged(float normalizedMin)
    {
		if (avoidSliderUpdate)
            return;

		if (normalizedMin > maxSlider.value)
        {
            minSlider.value = maxSlider.value;
            return;
        }

		normalizedMin = GetNonlinearValue(normalizedMin);

        minFilter = Mathf.Lerp(siteMinValue, siteMaxValue, normalizedMin);

        SetMinMaxFilters(normalizedMin, GetNonlinearValue(maxSlider.value));

        chart.SetMinFilter(normalizedMin);

		UpdateInputField(inputMin, minFilter);
     }

    private void OnMaxSliderChanged(float normalizedMax)
    {
		if (avoidSliderUpdate)
            return;

        if (normalizedMax < minSlider.value)
        {
            maxSlider.value = minSlider.value;
            return;
        }

		normalizedMax = GetNonlinearValue(normalizedMax);

        maxFilter = Mathf.Lerp(siteMinValue, siteMaxValue, normalizedMax);

        SetMinMaxFilters(GetNonlinearValue(minSlider.value), normalizedMax);

        chart.SetMaxFilter(normalizedMax);

		UpdateInputField(inputMax, maxFilter);
	}

	private void OnInputMinChanged(string minString)
    {
        float min = float.Parse(minString);

        minFilter = Mathf.Clamp(min, siteMinValue, maxFilter);
        if (minFilter != min)
        {
			UpdateInputField(inputMin, minFilter);
        }

        float normalizedMin = Mathf.InverseLerp(siteMinValue, siteMaxValue, minFilter);

        SetMinMaxFilters(normalizedMin, GetNonlinearValue(maxSlider.value));

        avoidSliderUpdate = true;
        minSlider.value = GetLinearValue(normalizedMin);
        chart.SetMinFilter(normalizedMin);
        avoidSliderUpdate = false;
    }

    private void OnInputMaxChanged(string maxString)
    {
        float max = float.Parse(maxString);

        maxFilter = Mathf.Clamp(max, minFilter, siteMaxValue);

        if (maxFilter != max)
        {
			UpdateInputField(inputMax, maxFilter);
        }

        float normalizedMax = Mathf.InverseLerp(siteMinValue, siteMaxValue, maxFilter);

        SetMinMaxFilters(GetNonlinearValue(minSlider.value), normalizedMax);

        avoidSliderUpdate = true;
        maxSlider.value = GetLinearValue(normalizedMax);
        chart.SetMaxFilter(normalizedMax);
        avoidSliderUpdate = false;
    }

    //
    // Public Methods
    //

    public void RefreshDistributionChart()
    {
		UpdateDistributionPower();

		// Update the position of the sliders
		avoidSliderUpdate = true;
        float normalizedMin = Mathf.InverseLerp(siteMinValue, siteMaxValue, minFilter);
        minSlider.value = GetLinearValue(normalizedMin);
        float normalizedMax = Mathf.InverseLerp(siteMinValue, siteMaxValue, maxFilter);
        maxSlider.value = GetLinearValue(normalizedMax);
		avoidSliderUpdate = false;
	}

    public void UpdateDistributionChart()
    {
        if (!chartNeedsUpdate || !IsActive)
			return;

		chartNeedsUpdate = false;
        UpdateChart();
    }

	//
	// Private Methods
	//

	private void ResetUI()
    {
		ResetUI(dataLayer.patchesInView[0]);

		minSlider.value = GetLinearValue(dataLayer.MinFilter);
		maxSlider.value = GetLinearValue(dataLayer.MaxFilter);
	}

	private void ResetUI(Patch patch)
	{
		currentLevel = patch.level;

		unitsLabel.text = patch.Data.units;

        UpdateValueRange(patch);
		UpdateFilters();
		UpdateChart();
    }

	private void RemoveVisibleGridEvents()
	{
		foreach (var site in dataLayer.loadedPatchesInView)
		{
			var data = site.Data as GridData;
			data.OnGridChange -= OnGridChange;
			data.OnValuesChange -= OnValuesChange;
		}
	}

	private void UpdateValueRange(Patch patch)
    {
		var layerSite = patch.siteRecord.layerSite;
		siteMinValue = layerSite.minValue;
		siteMaxValue = layerSite.maxValue;
		siteMean = layerSite.mean;
	}

	private void UpdateInputField(InputField input, float value)
	{
		float range = siteMaxValue - siteMinValue;
		float epsilon = range * 0.001f;

		if (value + range <= siteMinValue)
			input.text = siteMinValue.ToString("0.##");
		else if (value - epsilon >= siteMaxValue)
			input.text = siteMaxValue.ToString("0.##");
		else
		{
			int iValue = Mathf.FloorToInt(value);
			if (iValue == value || range > 10f)
				input.text = iValue.ToString();
			else if (range > 1f)
				input.text = value.ToString("0.0");
			else if (range > 0.1f)
				input.text = value.ToString("0.00");
			else
				input.text = value.ToString("0.000");
		}
	}

	private void UpdateFilters()
    {
		UpdateDistributionPower();

		float normalizedMin = GetNonlinearValue(minSlider.value);
		float normalizedMax = GetNonlinearValue(maxSlider.value);

		minFilter = Mathf.Lerp(siteMinValue, siteMaxValue, normalizedMin);
        maxFilter = Mathf.Lerp(siteMinValue, siteMaxValue, normalizedMax);
        UpdateInputField(inputMin, minFilter);
		UpdateInputField(inputMax, maxFilter);

        chart.SetMinFilter(normalizedMin);
        chart.SetMaxFilter(normalizedMax);
    }

    private void SetMinMaxFilters(float min, float max)
    {
		dataLayer.SetMinMaxFilters(min, max);
    }

    private float GetNonlinearValue(float value)
    { 
        return Mathf.Pow(value, distributionPower);
    }

    private float GetLinearValue(float value)
    {
        return Mathf.Pow(value, 1f / distributionPower);
    }

    private void UpdateDistributionPower()
    {
		// Some sites may not have a mean (e.g. Reachibility)
		if (UseNonlinearDistribution && siteMean > 0)
			distributionPower = Mathf.Log(siteMean) * InvMeanThresholdLog;
        else
			distributionPower = 1;
		chart.SetPower(distributionPower);
	}

	private void ClearChartValues()
    {
        maxChartValue = 0;
        Array.Clear(chartValues, 0, chartValues.Length);
    }

	private IEnumerator DelayedChartUpdate()
	{
		waitingForChartUpdate = true;
		yield return ChartUpdateDelay;
		waitingForChartUpdate = false;

		if (IsActive)
			_UpdateChart();

		delayedChartUpdateCR = null;
	}

	private void UpdateChart()
	{
		if (Time.time < nextValidUpdateTime)
		{
			if (!waitingForChartUpdate)
				delayedChartUpdateCR = StartCoroutine(DelayedChartUpdate());
			return;
		}
		_UpdateChart();
	}

	private void _UpdateChart()
	{
		nextValidUpdateTime = Time.time + ChartUpdateInterval;

		ClearChartValues();

        float minValue = float.MaxValue;
        float maxValue = float.MinValue;
		float minRange;
		float maxRange;

		for (int i = 0; i < dataLayer.loadedPatchesInView.Count; i++)
        {
            GridedPatch patch = dataLayer.loadedPatchesInView[i] as GridedPatch;
            if (patch == null)
                continue;

            var grid = patch.grid;

			if (grid.minValue != float.MaxValue)
			{
				minValue = Mathf.Min(minValue, grid.minValue);
				maxValue = Mathf.Max(maxValue, grid.maxValue);
				minRange = Mathf.InverseLerp(siteMinValue, siteMaxValue, grid.minValue);
				maxRange = Mathf.InverseLerp(siteMinValue, siteMaxValue, grid.maxValue);
			}
			else
			{
				minValue = 0;
				maxValue = 0;
				minRange = 0;
				maxRange = 0;
			}

			var dv = grid.DistributionValues;
            int distribSize = 0;
            if (dv != null)
                distribSize = dv.Length;
            float invSize = 1f / distribSize;
            float f1 = minRange * chartSize;
            int ci1 = (int)f1;
            float f2;
            int ci2;
            float countPerChartSlot = distribSize / ((maxRange - minRange) * chartSize);
            for (int j = 1; j <= distribSize; j++)
            {
                f2 = chartSize * Mathf.Lerp(minRange, maxRange, j * invSize);
                ci2 = (int)f2;

                if (ci1 == ci2)
                {
                    chartValues[ci1] += dv[j - 1];
                    maxChartValue = Mathf.Max(maxChartValue, chartValues[ci1]);
                }
                else
                {
                    float x = dv[j - 1] * countPerChartSlot;
                    float p = ci1 - f1 + 1f;

                    chartValues[ci1] += p * x;
                    maxChartValue = Mathf.Max(maxChartValue, chartValues[ci1]);

                    for (int ci = ci1 + 1; ci < ci2; ci++)
                    {
                        chartValues[ci] += x;
                        maxChartValue = Mathf.Max(maxChartValue, chartValues[ci]);
                    }

                    if (ci2 < chartSize)
                    {
                        p = f2 - ci2;
                        chartValues[ci2] += p * x;
                        maxChartValue = Mathf.Max(maxChartValue, chartValues[ci2]);
                    }
                }

                f1 = f2;
                ci1 = ci2;
            }
        }

		chart.SetMinRange(Mathf.InverseLerp(siteMinValue, siteMaxValue, minValue));
		chart.SetMaxRange(Mathf.InverseLerp(siteMinValue, siteMaxValue, maxValue));
		chart.SetData(chartValues, maxChartValue);
	}

}
