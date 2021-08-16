// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class NDASetupPanel : MonoBehaviour
{
    [Header("UI References")]
    public Button closeButton;
    [SerializeField] private Button yesButton = default;
    [SerializeField] private Button noButton = default;

    //[Header("Prefabs")]

    private ReachabilityTool reachabilityTool;

    //
    // Unity Methods
    //

    private void Start()
    {
        reachabilityTool = ComponentManager.Instance.Get<ReachabilityTool>();
        
        closeButton.onClick.AddListener(OnCloseClick);
        yesButton.onClick.AddListener(OnYesClicked);
        noButton.onClick.AddListener(OnNoClick);
    }

    //
    // Event Methods
    //

    private void OnCloseClick()
    {
        reachabilityTool.networkDisruptionAnalysisToggle.isOn = false;
        Destroy(gameObject);
    }

    private void OnYesClicked()
    {
#if !UNITY_WEBGL
        NetworkDisruptionAnalysis.SetupNDA();
#endif
        reachabilityTool.networkDisruptionAnalysisToggle.isOn = false;
        reachabilityTool.networkDisruptionAnalysisToggle.interactable = false;
        Destroy(gameObject);
    }

    private void OnNoClick()
    {
        OnCloseClick();
    }

    //
    // Public Methods
    //


    //
    // Private Methods
    //

    private void Reset()
    {
        reachabilityTool.networkDisruptionAnalysisToggle.interactable = true;
        reachabilityTool.networkDisruptionAnalysisToggle.isOn = false;
    }
}