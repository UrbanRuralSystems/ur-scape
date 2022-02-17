// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class EditSpeedPanel : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] GameObject speedInputPrefab = default;

    [Header("UI References")]
    [SerializeField] private Button closeButton = default;
    [SerializeField] private ScrollRect layersList = default;
    [SerializeField] private Button saveButton = default;

    private ReachabilityTool reachabilityTool;
    private SiteBrowser siteBrowser;

    //
    // Unity Methods
    //

    private void Start()
    {
        // Window events
        closeButton.onClick.AddListener(OnCloseClick);
        saveButton.onClick.AddListener(OnSaveClick);

        // Component references
        var componentManager = ComponentManager.Instance;
        reachabilityTool = componentManager.Get<ReachabilityTool>();
        siteBrowser = componentManager.Get<SiteBrowser>();

        InitUI();
    }

    private void OnEnable()
    {
        reachabilityTool?.LoadSiteMobilityModes();
    }

    //
    // Event Methods
    //

    private void OnCloseClick()
    {
        if (reachabilityTool)
            reachabilityTool.editSpeedToggle.isOn = false;
        gameObject.SetActive(false);

        reachabilityTool?.LoadSiteMobilityModes();
    }

    private void OnSaveClick()
    {
        string activeSiteName = siteBrowser.ActiveSite.Name;
        string filename = Paths.Data + "Reachability" + Path.DirectorySeparatorChar + activeSiteName + ".csv";

        var mobilityModes = new List<MobilityMode>(reachabilityTool.mobilityModes);
        foreach (var mode in mobilityModes)
        {
            for (int i = 0; i < mode.speeds.Length; ++i)
            {
                mode.speeds[i] /= ReachabilityTool.kmPerHourToMetersPerMin;
            }
        }
        ReachabilityIO.Save(mobilityModes, filename);
    }

    //
    // Public Methods
    //

    public void OnLanguageChanged()
    {
        var translator = LocalizationManager.Instance;
        var layersListContainer = layersList.content;

        if (layersListContainer.transform.childCount > 0)
        {
            for (int i = 0; i < (int)ClassificationIndex.Count; ++i)
            {
                var speedInput = layersListContainer.transform.GetChild(i);
                var classification = speedInput.transform.GetChild(0).GetComponent<Text>();
                classification.text = translator.Get(((ClassificationIndex)i).ToString());
            }
        }
    }

    public void UpdateSpeeds(int mode)
    {
        var layersListContainer = layersList.content;

        if (layersListContainer.transform.childCount.Equals(0))
            return;

        var translator = LocalizationManager.Instance;
        for (int i = 0; i < (int)ClassificationIndex.Count; ++i)
        {
            var mobilityMode = reachabilityTool.mobilityModes[mode];

            var speedInput = layersListContainer.transform.GetChild(i);
            var speed = speedInput.transform.GetChild(1).GetComponent<InputField>();
            speed.text = (mobilityMode.speeds[i] / ReachabilityTool.kmPerHourToMetersPerMin).ToString();
        }
    }

    //
    // Private Methods
    //

    private void InitUI()
    {
        var translator = LocalizationManager.Instance;
        var layersListContainer = layersList.content;

        for (int i = 0; i < (int)ClassificationIndex.Count; ++i)
        {
            var mode = reachabilityTool.mobilityModes[0];

            var speedInput = Instantiate(speedInputPrefab, layersListContainer, false);
            int index = i;
            speedInput.name = ((ClassificationIndex)index).ToString();
            var classification = speedInput.transform.GetChild(0).GetComponent<Text>();
            classification.text = translator.Get(speedInput.name);
            var speed = speedInput.transform.GetChild(1).GetComponent<InputField>();
            speed.text = (mode.speeds[index] / ReachabilityTool.kmPerHourToMetersPerMin).ToString();
            speed.onValueChanged.AddListener((val) => reachabilityTool.OnSpeedInputChanged(index, val));
        }
    }
}