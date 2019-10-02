// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System;
using UnityEngine;
using UnityEngine.UI;

public class SummaryLayerInfo : MonoBehaviour
{
	[Header("UI References")]
	public Image typologyColor;
	public Text typologyName;
	public Text typologyValue;

	private double area = 0.0;
	private PlanningOutput planningOutput = null;
	private ITranslator translator;
	private string suffix = "";

	//
	// Public Methods
	//

	public void Init(PlanningOutput planningOutput)
	{
		this.planningOutput = planningOutput;
		translator = LocalizationManager.Instance;

		var unitsDropdown = this.planningOutput.unitsDropdown;
		OnUnitsChanged(unitsDropdown.value);

		unitsDropdown.onValueChanged.RemoveListener(OnUnitsChanged);
		unitsDropdown.onValueChanged.AddListener(OnUnitsChanged);
	}

    public void SetSummaryLayerInfo(Color color, string name, double value)
	{
		typologyColor.color = color;
		typologyName.text = name;
		SetValue(value);
	}

	public void SetValue(double value)
	{
		area = value;
		UpdateAreaVal();
	}

	//
	// Event Methods
	//

	private void OnUnitsChanged(int value)
	{
		planningOutput.SelectedUnit = planningOutput.units[value];
		UpdateAreaVal();
	}

	//
	// Private Methods
	//

	private float UpdatedAreaVal()
	{
		float updatedArea = (float)(area * planningOutput.SelectedUnit.factor);
		if (updatedArea > 1e+12)
		{
			updatedArea *= 1e-12f;
			suffix = translator.Get("trillion");
		}
		else if (updatedArea > 1e+9)
		{
			updatedArea *= 1e-9f;
			suffix = translator.Get("billion");
		}
		else if (updatedArea > 1e+6)
		{
			updatedArea *= 1e-6f;
			suffix = translator.Get("million");
		}

		return updatedArea;
	}

	private void UpdateAreaVal()
	{
		typologyValue.text = UpdatedAreaVal().ToString("N") + " " + suffix + " " + planningOutput.SelectedUnit.symbol;
	}
}