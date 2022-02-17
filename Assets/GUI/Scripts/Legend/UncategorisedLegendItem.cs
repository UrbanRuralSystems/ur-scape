// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System;
using UnityEngine;
using UnityEngine.UI;

public class UncategorisedLegendItem : LegendItem
{
    [SerializeField] private GameObject filterInfo = default;
    [SerializeField] private Text min = default;
    [SerializeField] private Image spectrum = default;
    [SerializeField] private Text max = default;
    [SerializeField] private Text units = default;
    [SerializeField] private Shader colorSpectrum = default;

    //
    // Unity Methods
    //

    private void Awake()
    {
        Init();
        spectrum.material = new Material(colorSpectrum);
        SetMinAlpha(0.0f);
        SetMaxAlpha(1.0f);
    }

    //
    // Event Methods
    //

    protected override void OnGroupToggleChanged(bool isOn)
    {
        arrow.rectTransform.eulerAngles = new Vector3(0.0f, 0.0f, isOn ? 0.0f : -90.0f);
        filterInfo.SetActive(isOn);
        StartCoroutine(DelayedReEnableLayout(2));
    }

    //
    // Public Methods
    //

    public void SetMin(float min)
    {
        FormatValue(this.min, min);
    }

    public void SetMax(float max)
    {
        FormatValue(this.max, max);
    }

    public void SetMinAlpha(float alpha, bool reverse = false)
    {
        Color color = dot.color;
        color.a = reverse ? 1 - alpha : alpha;
        spectrum.materialForRendering.SetColor("MinColor", color);
    }

    public void SetMaxAlpha(float alpha, bool reverse = false)
    {
        Color color = dot.color;
        color.a = reverse ? 1 - alpha : alpha;
        spectrum.materialForRendering.SetColor("MaxColor", color);
    }

    public void SetUnits(string units)
    {
        this.units.text = units;
    }

    //
    // Private Methods
    //

    private void FormatValue(Text text, float value)
    {
        var absValue = Math.Abs(value);
        if (absValue >= 1e+12)
            text.text = ((int)(value * 1e-12)).ToString("0 T");
        else if (absValue >= 1e+11)
            text.text = ((int)(value * 1e-9)).ToString("0 B");
        else if (absValue >= 1e+9)
            text.text = ((int)(value * 1e-9)).ToString("0 B");
        else if (absValue >= 1e+8)
            text.text = ((int)(value * 1e-6)).ToString("0 M");
        else if (absValue >= 1e+6)
            text.text = ((int)(value * 1e-6)).ToString("0 M");
        else if (absValue >= 1e+5)
            text.text = ((int)(value * 1e-3)).ToString("0 K");
        else if (absValue >= 1e+3)
            text.text = ((int)(value * 1e-3)).ToString("0 K");
        else if (absValue < 0.0001f)
            text.text = "0";
        else if (absValue < 0.1f)
            text.text = ((int)value).ToString();
        else
        {
            text.text = ((int)value).ToString();
        }
    }
}