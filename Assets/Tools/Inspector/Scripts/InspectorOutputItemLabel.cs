// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli

using UnityEngine;
using UnityEngine.UI;

public class InspectorOutputItemLabel: MonoBehaviour
{
	[Header("UI References")]
	public CellValueItemLabel outputDataLayer;
	public RectTransform panel;
	public Text unitsValue;
	public Text maxValue;
	public Text meanValue;
	public Text medianValue;
	public Text minValue;
	public Text sumValue;

	public void SetName(string nameString)
    {
		outputDataLayer.SetName(nameString);
	}

	public void SetUnitsValue(string val)
	{
		unitsValue.text = val;
	}

    public void SetMinValue(string val)
    {
		minValue.text = val;
    }

	public void SetMaxValue(string val)
	{
		maxValue.text = val;
	}

    public void SetMeanValue(string val)
	{
		meanValue.text = val;
	}

    public void ShowMean(bool show)
    {
        meanValue.transform.parent.gameObject.SetActive(show);
    }

	public void SetMedianValue(string val)
	{
		medianValue.text = val;
	}

    public void SetSumValue(string val)
	{
		sumValue.text = val;
	}

    public void ShowSum(bool show)
    {
        sumValue.transform.parent.gameObject.SetActive(show);
    }

	public void SetDotColor(Color color)
	{
		outputDataLayer.SetDotColor(color);
	}
}
