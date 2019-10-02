// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public class CollapsePanel : MonoBehaviour
{
    public GameObject panel;

    private void Awake()
    {
        GetComponent<Toggle>().onValueChanged.AddListener((on) => panel.SetActive(on));
    }
}
