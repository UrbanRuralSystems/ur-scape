// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System;
using System.Collections.Generic;
using SFB;
using UnityEngine;
using UnityEngine.UI;

public class NDAParametersPanel : MonoBehaviour
{
    [Serializable]
    private class OSMParameters
    {
        public InputField distanceInput = default;
        public InputField cityInfrastructurePathInput = default;
        public Button cityInfrastructureBrowseButton = default;
    }

    [Serializable]
    private class ExposureParameters
    {
        public InputField floodReturnPeriodInput = default;
        public InputField disruptionTypePathInput = default;
        public Button disruptionTypeBrowseButton = default;
    }

    [Serializable]
    private class AnalysisParamaters
    {
        public InputField scenarioTypePathInput = default;
        public Button scenarioBrowseButton = default;
        public InputField fieldInput = default;
        public InputField attributeInput = default;
        public Transform floodDisruptionsContainer = default;
        public InputField outputNameInput = default;
    }

    [Header("Prefabs")]
    [SerializeField] private GameObject floodDisruptionInputPrefab = default;

    [Header("UI References")]
    [SerializeField] private Button closeButton = default;
    public Button runButton = default;
    public Button cancelButton = default;
    public GameObject message = default;
    [SerializeField] private OSMParameters osmParams = default;
    [SerializeField] private ExposureParameters exposureParams = default;
    [SerializeField] private AnalysisParamaters analysisParams = default;

    private ReachabilityTool reachabilityTool;
    private LocalizationManager translator;

    // Constants
    private readonly string[] FloodDisruptions = { "No_flooding", "Slight_flooding", "Moderate_flooding", "Heavy_flooding" };

    // Properties
    public string Distance { get { return osmParams.distanceInput.text; } }
    public string CityInfrastructure { get { return osmParams.cityInfrastructurePathInput.text; } }
    public string ScenarioPath { get { return analysisParams.scenarioTypePathInput.text; } }
    public string Field { get { return analysisParams.fieldInput.text; } }
    public string Attribute { get { return analysisParams.attributeInput.text; } }
    public string FloodReturnPeriod { get { return exposureParams.floodReturnPeriodInput.text; } }
    public string DisruptionTypePath { get { return exposureParams.disruptionTypePathInput.text; } }
    public string OutputName { get { return analysisParams.outputNameInput.text; } }
    public string Disruptions { private set; get; }

    // Lists
    private List<Toggle> floodDisruptionsToggles = new List<Toggle>();

    //
    // Unity Methods
    //

    private void Start()
    {
        // Component references
        var componentManager = ComponentManager.Instance;
        reachabilityTool = componentManager.Get<ReachabilityTool>();
        translator = LocalizationManager.Instance;

        // Events
        translator.OnLanguageChanged += OnLanguageChanged;

        InitUI();
    }

    //
    // Event Methods
    //

    private void OnLanguageChanged()
    {
        int disruptionCount = analysisParams.floodDisruptionsContainer.transform.childCount;

        for (int i = 1; i < disruptionCount; ++i)
        {
            var disruptionInput = analysisParams.floodDisruptionsContainer.transform.GetChild(i);
            var disruptionLabel = disruptionInput.transform.GetChild(0).GetComponent<Text>();
            disruptionLabel.text = translator.Get(disruptionLabel.text);
        }
    }

    private void OnCloseClick()
    {
        if (reachabilityTool)
            reachabilityTool.networkDisruptionAnalysisToggle.isOn = false;
        gameObject.SetActive(false);
    }

    private void OnRunClick()
    {
        InitDisruptionsString();

#if !UNITY_WEBGL
        NetworkDisruptionAnalysis.RunNDAProcess(reachabilityTool);
#endif

        cancelButton.gameObject.SetActive(true);
        message.SetActive(true);
        runButton.gameObject.SetActive(false);
    }

    private void OnCancelClick()
    {
#if !UNITY_WEBGL
        NetworkDisruptionAnalysis.StopNDAProcess();
#endif

        runButton.gameObject.SetActive(true);
        message.SetActive(false);
        cancelButton.gameObject.SetActive(false);
    }

    private void OnBrowseCityInfrastructureClicked()
    {
        SelectFiles(OnLoadCityInfrastructureFiles);
    }

    private void OnBrowseScenarioTypeClicked()
    {
        SelectFiles(OnLoadScenarioTypeFiles);
    }

    private void OnBrowseDisruptionTypeClicked()
    {
        SelectFiles(OnLoadDisruptionTypeFiles);
    }

    private void OnLoadFiles(InputField input, string[] paths)
    {
        if (paths == null || paths.Length == 0 || string.IsNullOrWhiteSpace(paths[0]))
        {
            if (input.text == null)
                return;
        }

        input.text = paths[0];
    }

    private void OnLoadCityInfrastructureFiles(string[] paths)
    {
        OnLoadFiles(osmParams.cityInfrastructurePathInput, paths);
    }

    private void OnLoadScenarioTypeFiles(string[] paths)
    {
        OnLoadFiles(analysisParams.scenarioTypePathInput, paths);
    }

    private void OnLoadDisruptionTypeFiles(string[] paths)
    {
        OnLoadFiles(exposureParams.disruptionTypePathInput, paths);
    }

    private void OnScenarioPathInputChanged(string input)
    {
        runButton.interactable = IsAllInputFilled();
    }

    private void OnDisruptionTypePathInputChanged(string input)
    {
        runButton.interactable = IsAllInputFilled();
    }

    private void OnInputChanged()
    {
        runButton.interactable = IsAllInputFilled();
    }

    private void OnToggleChanged(bool isOn)
    {
        runButton.interactable = IsAllInputFilled();
    }

    //
    // Public Methods
    //



    //
    // Private Methods
    //

    private void InitUIEvents()
    {
        closeButton.onClick.AddListener(OnCloseClick);
        runButton.onClick.AddListener(OnRunClick);
        cancelButton.onClick.AddListener(OnCancelClick);
        
        osmParams.distanceInput.onValueChanged.AddListener((_) => OnInputChanged());
        osmParams.cityInfrastructurePathInput.onValueChanged.AddListener((_) => OnInputChanged());
        osmParams.cityInfrastructureBrowseButton.onClick.AddListener(OnBrowseCityInfrastructureClicked);
        
        exposureParams.floodReturnPeriodInput.onValueChanged.AddListener((_) => OnInputChanged());
        exposureParams.disruptionTypePathInput.onValueChanged.AddListener(OnDisruptionTypePathInputChanged);
        exposureParams.disruptionTypeBrowseButton.onClick.AddListener(OnBrowseDisruptionTypeClicked);
        
        analysisParams.scenarioTypePathInput.onValueChanged.AddListener(OnScenarioPathInputChanged);
        analysisParams.scenarioBrowseButton.onClick.AddListener(OnBrowseScenarioTypeClicked);
        analysisParams.fieldInput.onValueChanged.AddListener((_) => OnInputChanged());
        analysisParams.attributeInput.onValueChanged.AddListener((_) => OnInputChanged());
        analysisParams.outputNameInput.onValueChanged.AddListener((_) => OnInputChanged());
    }

    private void InitFloodDisruptionToggles()
    {
        foreach (var disruption in FloodDisruptions)
        {
            var disruptionInput = Instantiate(floodDisruptionInputPrefab, analysisParams.floodDisruptionsContainer, false);
            var disruptionLabel = disruptionInput.transform.GetChild(0).GetComponent<Text>();
            disruptionLabel.text = disruption.Replace("_", " ");
            var disruptionToggle = disruptionInput.transform.GetChild(1).GetComponent<Toggle>();
            disruptionToggle.isOn = false;

            disruptionToggle.onValueChanged.AddListener(OnToggleChanged);
            floodDisruptionsToggles.Add(disruptionToggle);
        }
    }

    private void InitUI()
    {
        InitUIEvents();
        InitFloodDisruptionToggles();
    }

    private void InitDisruptionsString()
    {
        Disruptions = "";
        int disruptionCount = analysisParams.floodDisruptionsContainer.transform.childCount;
        for (int i = 1; i < disruptionCount; ++i)
        {
            var disruptionInput = analysisParams.floodDisruptionsContainer.transform.GetChild(i);
            var disruptionToggle = disruptionInput.transform.GetChild(1).GetComponent<Toggle>();

            if (disruptionToggle.isOn)
            {
                Disruptions += FloodDisruptions[i - 1];
                if (i < (disruptionCount - 1))
                    Disruptions += ",";
            }
        }

        if (Disruptions.Length > 0 && Disruptions[Disruptions.Length - 1].Equals(','))
            Disruptions = Disruptions.Remove(Disruptions.Length - 1, 1);
        // Debug.Log(Disruptions);
    }

    private void SelectFiles(Action<string[]> cb)
    {
        List<string> allExtensions = new List<string>();
        allExtensions.Add("shp");
        var formats = Interpreter.DataFormats;
        var extFilters = new ExtensionFilter[1];
        extFilters[0] = new ExtensionFilter("Shapefile" + " ", allExtensions.ToArray());

        StandaloneFileBrowser.OpenFilePanelAsync(translator.Get("Select shape file"), "", extFilters, false, cb);
    }

    private bool IsAllInputFilled()
    {
        var hasDistance = osmParams.distanceInput.text.Length > 0;
        var hasCityInfrastructurePathInput = osmParams.cityInfrastructurePathInput.text.Length > 0;
        var hasFloodReturnPeriod = exposureParams.floodReturnPeriodInput.text.Length > 0;
        var hasDisruptionTypePath = (!string.IsNullOrEmpty(exposureParams.disruptionTypePathInput.text) || !string.IsNullOrWhiteSpace(exposureParams.disruptionTypePathInput.text));
        var hasScenarioPath = (!string.IsNullOrEmpty(analysisParams.scenarioTypePathInput.text) || !string.IsNullOrWhiteSpace(analysisParams.scenarioTypePathInput.text));
        var hasField = analysisParams.fieldInput.text.Length > 0;
        var hasAttribute = analysisParams.attributeInput.text.Length > 0;
        var hasOutputName = analysisParams.outputNameInput.text.Length > 0;
        var hasFloodDisruptionSelected = floodDisruptionsToggles.Find((toggle) => toggle.isOn == true) != null;
        
        return hasDistance &&
               hasCityInfrastructurePathInput &&
               hasFloodReturnPeriod &&
               hasDisruptionTypePath &&
               hasScenarioPath &&
               hasField &&
               hasAttribute &&
               hasOutputName &&
               hasFloodDisruptionSelected;
    }
}
