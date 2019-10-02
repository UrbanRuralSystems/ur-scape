// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker(neudecker@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class WeightSlider : MonoBehaviour
{
	[Header("Settings")]
	public Color enabledColor = Color.white;
	public Color disabledColor = Color.gray;

	[Header("UI References")]
    public Slider slider;
    public Image sliderImage;
    public Text valueText;
    public Text labelText;
    public Toggle toggle;

    public delegate void OnSliderChangedDelegate(float value);
    public event OnSliderChangedDelegate OnSliderChanged;

	public float Value { get { return slider.value * multiplier; } }


    // Misc
    private bool showPercentage;
	private float multiplier = 1;

    public void Start()
    {
        slider.onValueChanged.AddListener(OnSliderChange);
        toggle.onValueChanged.AddListener(OnToggleChange);
    }

    public void Init(string name, Color color)
    {
        labelText.text = name;
        sliderImage.color = color;
    }

    public void SetDefault()
    {
        SetShowPercentage(true);
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = 1f;
		OnSliderChange(slider.value);
    }

    public void SetShowPercentage(bool showPercentage)
    {
        this.showPercentage = showPercentage;
    }

	public void Enable(bool enable)
	{
		slider.interactable = enable;
		toggle.interactable = enable;
		valueText.color = enable ? enabledColor : disabledColor;
		labelText.color = enable ? enabledColor : disabledColor;
	}

	//
	// Private Methods
	//

    private void OnSliderChange(float value)
    {
		value *= multiplier;

		if (showPercentage)
            valueText.text = (value * 100f).ToString("0") + " %";
        else
            valueText.text = value.ToString("0.##");

        if (OnSliderChanged != null)
            OnSliderChanged(value);
    }

    private void OnToggleChange(bool isOn)
    {
		multiplier = isOn ? 1f : -1f;
        OnSliderChange(slider.value);
    }
}
