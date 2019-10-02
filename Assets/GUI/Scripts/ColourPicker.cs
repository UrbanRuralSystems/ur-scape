// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum ColourGradient
{
	HueSaturation,
	HueBrightness,
	SaturationBrightness
}

public class ColourPicker : MonoBehaviour, IDragHandler, IPointerDownHandler
{
	[Header("UI References")]
	public Image gradient;
	public Image selector;

	[Header("Settings")]
	public ColourGradient mode = ColourGradient.HueSaturation;

	[MinMaxRange(0f, 1f)]
	public Vector2 saturationRange = Vector2.up;
	[MinMaxRange(0f, 1f)]
	public Vector2 brightnessRange = Vector2.up;

	private Color color = Color.gray;
	public Color Color { get { return color; } }
	private Vector3 hsb = new Vector3(0, 0, 0.5f);
	public Vector3 HSB { get { return hsb; } }
	public float Hue { get { return hsb.x; } }
	public float Saturation { get { return hsb.y; } }
	public float Brightness { get { return hsb.z; } }

	private RectTransform gradientRT;
	private RectTransform selectorRT;
	private Vector2 gradientSize;
	private Vector2 halfSelector;
	private string modeKeyword;

	public event UnityAction<Color> OnColorChanged;


	//
	// Unity Methods
	//

	private void Awake()
	{
		var mat = new Material(gradient.material.shader);
		mat.hideFlags = HideFlags.HideAndDontSave;
		gradient.material = mat;

		UpdateMaterialMinMaxValues();
	}

	// Comment this out to avoid .mat being modified by the editor.
	//private void OnValidate()
	//{
	//	UpdateMaterialMinMaxValues();
	//}

	private void OnEnable()
	{
		gradientRT = transform as RectTransform;
		selectorRT = selector.GetComponent<RectTransform>();

		halfSelector = selectorRT.sizeDelta * 0.5f;
		gradientSize = gradientRT.rect.size;
		gradientSize.x = Mathf.Abs(gradientSize.x);
		gradientSize.y = Mathf.Abs(gradientSize.y);
	}


	//
	// Inheritance Methods
	//

	public void OnPointerDown(PointerEventData eventData)
	{
		OnSelectorPositionChanged(eventData.position);
	}

	public void OnDrag(PointerEventData eventData)
	{
		OnSelectorPositionChanged(eventData.position);
	}


	//
	// Public Methods
	//

	public void SetColor(Color c)
	{
		color = c;
		UpdateHSBfromColor();
	}

	public void SetRed(float red)
	{
		color.r = red;
		UpdateHSBfromColor();
	}

	public void SetGreen(float green)
	{
		color.g = green;
		UpdateHSBfromColor();
	}

	public void SetBlue(float blue)
	{
		color.b = blue;
		UpdateHSBfromColor();
	}

	public void SetHSB(Vector3 hsb)
	{
		this.hsb = hsb;

		ClampHSB();
		UpdateMaterialValue();
		UpdateSelectorPosition();
		UpdateColorFromHSB();
	}

	public void SetHue(float value)
	{
		UpdateHSB(0, value, mode == ColourGradient.SaturationBrightness);
	}

	public void SetSaturation(float value)
	{
		UpdateHSB(1, value, mode == ColourGradient.HueBrightness);
	}

	public void SetBrightness(float value)
	{
		UpdateHSB(2, value, mode == ColourGradient.HueSaturation);
	}


	//
	// Private Methods
	//

	private void UpdateHSBfromColor()
	{
		Color.RGBToHSV(color, out hsb.x, out hsb.y, out hsb.z);
		SetHSB(hsb);
	}

	private void UpdateHSB(int valueIndex, float value, bool isValue)
	{
		hsb[valueIndex] = value;

		ClampHSB();

		if (isValue)
			UpdateMaterialValue();
		else
			UpdateSelectorPosition();

		UpdateColorFromHSB();
	}

	private void ClampHSB()
	{
		hsb.x = Mathf.Clamp01(hsb.x);
		hsb.y = Mathf.Clamp(hsb.y, saturationRange.x, saturationRange.y);
		hsb.z = Mathf.Clamp(hsb.z, brightnessRange.x, brightnessRange.y);
	}

	private void OnSelectorPositionChanged(Vector3 pos)
	{
		// Clamp the new position within the gradient rect
		pos = gradientRT.InverseTransformPoint(pos);
		pos.x = Mathf.Clamp(pos.x, halfSelector.x, gradientSize.x - halfSelector.x);
		pos.y = Mathf.Clamp(pos.y, halfSelector.y - gradientSize.y, -halfSelector.y);
		selectorRT.localPosition = pos;

		float x = (pos.x - halfSelector.x) / (gradientSize.x - selectorRT.sizeDelta.x);
		float y = (pos.y + halfSelector.y) / (gradientSize.y - selectorRT.sizeDelta.y) + 1;

		// Update HSV
		switch (mode)
		{
			case ColourGradient.HueSaturation:
				UpdateHue(x);
				UpdateSaturation(y);
				break;

			case ColourGradient.HueBrightness:
				UpdateHue(x);
				UpdateBrightness(y);
				break;

			case ColourGradient.SaturationBrightness:
				UpdateSaturation(x);
				UpdateBrightness(y);
				break;
		}

		// Update Color
		UpdateColorFromHSB();
	}

	private void UpdateHue(float value)
	{
		hsb.x = value;
	}

	private void UpdateSaturation(float value)
	{
		hsb.y = Mathf.Lerp(saturationRange.x, saturationRange.y, value);
	}

	private void UpdateBrightness(float value)
	{
		hsb.z = Mathf.Lerp(brightnessRange.x, brightnessRange.y, value);
	}

	private void UpdateMaterialValue()
	{
		switch (mode)
		{
			case ColourGradient.HueSaturation:
				UpdateMaterialValue(Mathf.InverseLerp(brightnessRange.x, brightnessRange.y, hsb.z));
				break;

			case ColourGradient.HueBrightness:
				UpdateMaterialValue(Mathf.InverseLerp(saturationRange.x, saturationRange.y, hsb.y));
				break;

			case ColourGradient.SaturationBrightness:
				UpdateMaterialValue(hsb.x);
				break;
		}
	}

	private void UpdateMaterialValue(float value)
	{
		gradient.materialForRendering.SetFloat("Value", value);
	}

	private void UpdateColorFromHSB()
	{
		color = Color.HSVToRGB(hsb.x, hsb.y, hsb.z);

		// Calculate Luma
		var Y = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;

		// Update selector's color (black or white)
		selector.color = Y > 0.5f? Color.black : Color.white;

		OnColorChanged?.Invoke(color);
	}

	private void UpdateSelectorPosition()
	{
		Vector3 pos = Vector3.zero;
		switch (mode)
		{
			case ColourGradient.HueSaturation:
				pos.x = hsb.x;
				pos.y = Mathf.InverseLerp(saturationRange.x, saturationRange.y, hsb.y);
				break;

			case ColourGradient.HueBrightness:
				pos.x = hsb.x;
				pos.y = Mathf.InverseLerp(brightnessRange.x, brightnessRange.y, hsb.z);
				break;

			case ColourGradient.SaturationBrightness:
				pos.x = Mathf.InverseLerp(saturationRange.x, saturationRange.y, hsb.y);
				pos.y = Mathf.InverseLerp(brightnessRange.x, brightnessRange.y, hsb.z);
				break;
		}

		pos.x = Mathf.Lerp(halfSelector.x, gradientSize.x - halfSelector.x, pos.x);
		pos.y = Mathf.Lerp(halfSelector.y - gradientSize.y, -halfSelector.y, pos.y);

		selectorRT.localPosition = pos;
	}

	private void UpdateMaterialMinMaxValues()
	{
		var mat = gradient.materialForRendering;
		mat.SetFloat("MinSaturation", saturationRange.x);
		mat.SetFloat("MaxSaturation", saturationRange.y);
		mat.SetFloat("MinBrightness", brightnessRange.x);
		mat.SetFloat("MaxBrightness", brightnessRange.y);
		mat.SetFloat("Mode", (float)mode);

		if (modeKeyword != null)
			mat.DisableKeyword(modeKeyword);

		switch (mode)
		{
			case ColourGradient.HueSaturation:
				modeKeyword = "MODE_HUESATURATION";
				break;

			case ColourGradient.HueBrightness:
				modeKeyword = "MODE_HUEBRIGHTNESS";
				break;

			case ColourGradient.SaturationBrightness:
				modeKeyword = "MODE_SATURATIONBRIGHTNESS";
				break;
		}
		mat.EnableKeyword(modeKeyword);
	}

}
