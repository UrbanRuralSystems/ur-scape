// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ColourSelector : MonoBehaviour, IDeselectHandler
{
	[Header("UI References")]
	public Image colorSample;
	public Button openButton;
	public Button random;
	public ColourPicker picker;
	public Slider slider;
	public GameObject brightness;

	[Header("Settings")]
	public bool autoHide = false;

	public Color Color { get; private set; } = Color.gray;

	public event UnityAction<Color> OnColorChanged;
	public event UnityAction<Color> OnFinishColorEdit;

	private bool isUIVisible;
	private float sliderHandleSize;


	//
	// Unity Methods
	//

	private void Start()
	{
		isUIVisible = picker.gameObject.activeSelf;
		if (isUIVisible)
		{
			if (autoHide)
				ToggleVisibility();
			else
				InitUI();
		}

		if (autoHide)
			openButton.onClick.AddListener(OnOpenClick);

		slider.onValueChanged.AddListener(OnBrightnessChanged);
		random.onClick.AddListener(OnRandomClick);
	}

	//
	// Inheritance Methods
	//

	public void OnDeselect(BaseEventData eventData)
	{
		if (autoHide)
			StartCoroutine(CheckSelected());
	}


	//
	// Event Methods
	//

	private void OnOpenClick()
	{
		ToggleVisibility();
	}

	private void OnPickerColorChanged(Color c)
	{
		UpdateColor(c);
	}

	private void OnBrightnessChanged(float value)
	{
		UpdateSliderSize();

		picker.SetBrightness(Mathf.Lerp(picker.brightnessRange.x, picker.brightnessRange.y, value));
	}

	private void OnRandomClick()
	{
		Vector3 hsb;
		hsb.x = Random.value;
		hsb.y = Mathf.Lerp(picker.saturationRange.x, picker.saturationRange.y, Random.value);
		hsb.z = Mathf.Lerp(picker.brightnessRange.x, picker.brightnessRange.y, Random.value);
		picker.SetHSB(hsb);

		UpdateSlider();
	}


	//
	// Public Methods
	//

	public void SetColor(Color c)
	{
		if (isUIVisible)
			picker.SetColor(c);

		UpdateColor(c);
	}


	//
	// Private Methods
	//

	private void UpdateColor(Color c)
	{
		Color = c;

		// Update color sample
		colorSample.color = c;

		OnColorChanged?.Invoke(c);
	}

	private void InitUI()
	{
		if (sliderHandleSize == 0)
			sliderHandleSize = slider.handleRect.sizeDelta.y;

		picker.SetColor(Color);

		UpdateSlider();

		picker.OnColorChanged += OnPickerColorChanged;
		EventSystem.current.SetSelectedGameObject(gameObject);
	}

	private void UpdateSlider()
	{
		slider.onValueChanged.RemoveListener(OnBrightnessChanged);
		slider.value = Mathf.InverseLerp(picker.brightnessRange.x, picker.brightnessRange.y, picker.Brightness);
		UpdateSliderSize();
		slider.onValueChanged.AddListener(OnBrightnessChanged);
	}

	private void UpdateSliderSize()
	{
		// Update slider's handle size
		var size = slider.handleRect.sizeDelta;
		size.y = sliderHandleSize * (slider.value * 0.5f + 0.5f);
		slider.handleRect.sizeDelta = size;
	}

	private void ToggleVisibility()
	{
		isUIVisible = !isUIVisible;

		picker.gameObject.SetActive(isUIVisible);
		random.gameObject.SetActive(isUIVisible);
		brightness.gameObject.SetActive(isUIVisible);

		if (isUIVisible)
		{
			InitUI();
		}
		else
		{
			picker.OnColorChanged -= OnPickerColorChanged;
		}
	}

	private IEnumerator CheckSelected()
	{
		yield return null;

		// Re-select the color selector if the newly selected object is one of its own
		var current = EventSystem.current.currentSelectedGameObject;
		if (current == picker.gameObject ||
			current == random.gameObject ||
			current == slider.gameObject)
		{
			EventSystem.current.SetSelectedGameObject(gameObject);
		}
		else if (current != colorSample.gameObject)
		{
			if (isUIVisible)
				ToggleVisibility();

			OnFinishColorEdit?.Invoke(Color);
		}
	}

}
