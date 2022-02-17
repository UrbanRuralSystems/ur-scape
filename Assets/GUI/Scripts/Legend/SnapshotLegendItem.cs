// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class SnapshotLegendItem : LegendItem
{
    [SerializeField] private Text area = default;

    //
    // Unity Methods
    //

    private void Awake()
    {
        Init();
    }

    //
    // Event Methods
    //

    protected override void OnGroupToggleChanged(bool isOn)
    {
        arrow.rectTransform.eulerAngles = new Vector3(0.0f, 0.0f, isOn ? 0.0f : -90.0f);
        area.transform.parent.gameObject.SetActive(isOn);
        StartCoroutine(DelayedReEnableLayout(2));
    }

    //
    // Public Methods
    //

    public void SetArea(double sqm1, AreaUnit selectedUnit, bool percentage = false, ContoursInfoPanel.InfoPanelEntry selectedEntry = null)
    {
        var sufix = "";
        float area = !percentage ?
                        (float)(sqm1 * selectedUnit.factor) :
                        (float)(sqm1 * selectedUnit.factor / selectedEntry?.sqm.Value);

        if (!percentage)
        {
            if (area > 1e+12)
            {
                area *= 1e-12f;
                sufix = Translator.Get("trillion");
            }
            else if (area > 1e+9)
            {
                area *= 1e-9f;
                sufix = Translator.Get("billion");
            }
            else if (area > 1e+6)
            {
                area *= 1e-6f;
                sufix = Translator.Get("million");
            }

            this.area.text = area.ToString("N") + " " + sufix + " " + selectedUnit.symbol;
        }
        else
            this.area.text = $"{(int)area} {selectedUnit.symbol} of {selectedEntry?.snapshotInfo.GetTitle()}";
    }

    //
    // Private Methods
    //



}