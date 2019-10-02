// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)

using System;
using UnityEngine;
using UnityEngine.UI;

public class BudgetLabel: MonoBehaviour
{
    public Text nameLabel;
    public Text valueLabel;

    public void SetName(string nameString)
    {
        this.nameLabel.text = nameString; 
    }

    public void SetValue(float value)
    {
        var outputValue = Math.Round(value, 2).ToString("0.##");
        this.valueLabel.text = outputValue + " %";
    }
}
