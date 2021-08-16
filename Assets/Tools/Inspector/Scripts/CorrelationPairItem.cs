// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class CorrelationPairItem : MonoBehaviour
{
    [Header("UI References")]
	public CellValueItemLabel outputDataLayer1;
	public CellValueItemLabel outputDataLayer2;
	public Text coefficientValue;

    public void SetName1(string nameString)
    {
		outputDataLayer1.SetName(nameString);
	}

    public void SetName2(string nameString)
    {
		outputDataLayer2.SetName(nameString);
	}

    public void SetDotColor1(Color color)
	{
		outputDataLayer1.SetDotColor(color);
	}

    public void SetDotColor2(Color color)
	{
		outputDataLayer2.SetDotColor(color);
	}

    public void SetCoefficientValue(string val)
	{
		coefficientValue.text = val;
	}
}