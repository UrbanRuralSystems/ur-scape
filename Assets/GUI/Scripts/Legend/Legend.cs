// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class Legend : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Toggle toggle = default;
    [SerializeField] private LegendPanel legendPanel = default;

    //
    // Unity Methods
    //

    private void Awake()
    {
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    //
    // Event Methods
    //

    private void OnToggleValueChanged(bool isOn)
    {
        legendPanel.gameObject.SetActive(isOn);
    }
}