// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//          Michael Joos  (joos@arch.ethz.ch)
//          Muhammad Salihin Bin Zaol-kefli

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlanningTool : Tool
{
    public static readonly string TYPOLOGIES_FILE = "typologies.csv";

    [Header("Prefabs")]
    public Planner plannerPrefab;
    public GridMapLayer planningLayerPrefab;
    public TypologyToggleWithText typologyTogglePrefab;
    public LassoTool lassoToolPrefab;
    public BrushTool brushToolPrefab;
    public TargetPanel targetPanelPrefab;
	public ToggleGroup typologyGroupPrefab;

	[Header("UI References")]
    public ToggleGroup toolsGroup;
    public Transform toogleTarget;
    public GameObject buttonReturnPanel;
	public Toggle displayPlanningSummaryToggle;

    public TypologyLibrary typologyLibrary;

    // Prefab Instances
    private Planner planner;
    private GridMapLayer planningLayer;
    private readonly List<Toggle> toggles = new List<Toggle>();
    private LassoTool lassoTool;
    private BrushTool brushTool;
    private TargetPanel targetPanel;
	private ToggleGroup typologyGroup;

	// Misc
	private List<Typology> typologies;
    private DrawingTool selectedTool;
    private DrawingTool lastSelectedTool;
	private Transform centerTopContainerT;
	SiteBrowser siteBrowser;

    //
    // Unity Methods
    //

    protected override void Awake()
    {
        base.Awake();

		centerTopContainerT = GameObject.Find("CenterTopContainer").transform;

		// Initialize typologies
		typologyGroup = Instantiate(typologyGroupPrefab, centerTopContainerT, false);
		typologyGroup.gameObject.SetActive(false);
		Loader.Create(LoadTypologies(), true);

		siteBrowser = ComponentManager.Instance.Get<SiteBrowser>();
	}


    //
    // Events
    //

    public override void OnToggleTool(bool isOn)
    {
        if (isOn)
        {
            TurnOn();
        }
        else
        {
            TurnOff();
        }
    }

    public override void OnActiveTool(bool isActive)
    {
		base.OnActiveTool(isActive);

		if (isActive)
		{
			siteBrowser.OnBeforeActiveSiteChange += OnBeforeActiveSiteChange;
			siteBrowser.OnAfterActiveSiteChange += OnAfterActiveSiteChange;
		}
		else
		{
			siteBrowser.OnBeforeActiveSiteChange -= OnBeforeActiveSiteChange;
			siteBrowser.OnAfterActiveSiteChange -= OnAfterActiveSiteChange;
		}

		if (planner != null)
        {
            planner.SetOutput(isActive);
			if(isActive)
				ToggleSiteSpecificTypologies();
			typologyGroup.gameObject.SetActive(isActive);
        }
	}

	private void OnTypologyGroupChanged(bool isOn)
    {
        // deaktivate erase mode if usere click on typology of on;
        if(selectedTool != null)
        {
            if (selectedTool.Erasing == true)
            {
                selectedTool.OnCancel -= OnCancelBrushTool;
                var tools = toolsGroup.GetComponentsInChildren<Toggle>();
                // if erase true, toolsGroup should be always only 1 item
                tools[0].isOn = false;
                // activate last used tool before erasing
                if (lastSelectedTool != null)
                {
                    selectedTool = lastSelectedTool;
                    selectedTool.Activate();
                    selectedTool.Erasing = false;
                    planner.SetDrawingTool(selectedTool);
                }
            }
        }

        for (int i = 0; i < toggles.Count; ++i)
        {
            if (!planner)
                break;

			if (!toggles[i].IsActive())
				continue;

            var toggle = toggles[i].GetComponentInChildren<Toggle>();
            if (toggle.isOn)
            {
                planner.SetTypology(i);
                return;
            }
        }
    }

    private void OnToolChange(bool isOn, int index)
    {
        if (isOn)
        {
            switch (index)
            {
                case 0:
                    selectedTool = brushTool;
                    selectedTool.Erasing = false;
                    break;
                case 1:
                    selectedTool = lassoTool;
                    break;
                case 2:
					lastSelectedTool = selectedTool;
                    selectedTool = brushTool;
                    selectedTool.Erasing = true;
                    break;
            }
            if (selectedTool != null)
            {
                selectedTool.OnCancel += OnCancelBrushTool;
            }

			typologyGroup.gameObject.SetActive(true);
		}
        else
        {
            if (selectedTool != null)
            {
                selectedTool.OnCancel -= OnCancelBrushTool;
                if (index == 2)
                {
                    selectedTool.Erasing = false;
                    planner.SetDrawingTool(lastSelectedTool);

                    return;
                }
                else
                    selectedTool = null;
            }
        }

        if (planner != null)
            planner.SetDrawingTool(selectedTool);
    }

    private void OnCancelBrushTool()
    {
        toolsGroup.SetAllTogglesOff();
    }


    private void OnToggleTargetChange(bool isOn)
    {
        if (isOn)
        {
			targetPanel = ComponentManager.Instance.Get<ModalDialogManager>().NewDialog(targetPanelPrefab);
			targetPanel.Show(planner.targetValues, typologies[0], OnTargetUpdated);
            toogleTarget.GetComponentInChildren<Toggle>().interactable = false;
        }
    }

    private void OnTargetUpdated(DialogStatus status)
    {
        if (status == DialogStatus.OK)
        {
            planner.UpdateOutput();
            toogleTarget.GetComponentInChildren<Toggle>().isOn = false;
            toogleTarget.GetComponentInChildren<Toggle>().interactable = true;
        }
    }

    private void OnClickReturn()
    {
        if (planner != null)
        {
            selectedTool = null;
            planner.SetDrawingTool(selectedTool);
        }

        // hide typologies, show tools
        for (int i = 0; i < (toolsGroup.transform.childCount - 3); ++i) // - 3 to skip erase, selected contours fill and all contours
        {
            toolsGroup.transform.GetChild(i).gameObject.SetActive(true);
            toolsGroup.transform.GetChild(i).GetComponentInChildren<ToggleButton>().isOn = false;
        }
        toogleTarget.gameObject.SetActive(true);
        typologyGroup.gameObject.SetActive(false);
        buttonReturnPanel.SetActive(false);
    }

	private void OnDisplayPlanningSummary(bool isOn)
	{
		planner.ShowAllFlags(isOn);
	}

	private void OnBeforeActiveSiteChange(Site nextSite, Site previousSite)
	{
		TurnOff();
	}

	private void OnAfterActiveSiteChange(Site nextSite, Site previousSite)
	{
		TurnOn();
	}

	//
	// Private Methods
	//

	private IEnumerator LoadTypologies()
    {
        string typologiesFile = Paths.Data + TYPOLOGIES_FILE;

        // Load groups
        yield return TypologyConfig.Load(typologiesFile, (t) => typologies = t);

        InitTypologyToggles();
    }

    private void InitTypologyToggles()
    {
        foreach (var typology in typologies)
        {
			// Create a new toggle
			TypologyToggleWithText typologyEntry = Instantiate(typologyTogglePrefab, typologyGroup.transform, false);
            typologyEntry.text.text = typology.name;
            typologyEntry.toggle.name = typology.name;
            typologyEntry.image.color= typology.color;
			typologyEntry.toggle.group = typologyGroup;
            typologyEntry.toggle.onValueChanged.AddListener(OnTypologyGroupChanged);
            toggles.Add(typologyEntry.toggle);
        }
	}

	private void ToggleSiteSpecificTypologies()
	{
		// Obtain unique set of site names from all typologies
		HashSet<string> sites = new HashSet<string>();
		foreach (var typology in typologies)
		{
			foreach(var site in typology.sites)
			{
				sites.Add(site);
			}
		}

		int typologiesCount = typologies.Count;
		string activeSiteName = (siteBrowser.ActiveSite != null) ? siteBrowser.ActiveSite.Name : siteBrowser.defaultSiteName;
		bool foundFirstActiveToggle = false;
		int firstActiveToggleIndex = 0;

		// Toggle should be active depending on site specified
		for (int i = 0; i < typologiesCount; ++i)
		{
			bool foundSiteName(string s) => 
				string.Equals(s, activeSiteName, StringComparison.OrdinalIgnoreCase);

			bool existInSite = (typologies[i].sites.Count == 0) ?
				!sites.Any(foundSiteName) : typologies[i].sites.Any(foundSiteName);

			toggles[i].gameObject.SetActive(existInSite);

			// Obtain index of first active typology toggle
			if (!foundFirstActiveToggle && toggles[i].gameObject.activeSelf)
			{
				firstActiveToggleIndex = i;
				foundFirstActiveToggle = true;
			}
		}

		// Update typology and info panels
		toggles[firstActiveToggleIndex].isOn = true;
		planner.SetTypology(firstActiveToggleIndex);
	}

	private void TurnOn()
    {
		var gridLayerController = map.GetLayerController<GridLayerController>();

		toogleTarget.GetComponentInChildren<Toggle>().onValueChanged.AddListener(OnToggleTargetChange);

		displayPlanningSummaryToggle.onValueChanged.AddListener(OnDisplayPlanningSummary);

		// Check that we are in level D or closer to the ground
		if (map.CurrentLevel < 3)
        {
            string msg = Translator.Get("This tool is not available for this site");
			ShowMessage(msg, MessageType.Warning);
			return;
        }

		// Register UI
		buttonReturnPanel.GetComponentInChildren<Button>().onClick.AddListener(OnClickReturn);

		// Get Components
		var dataLayers = ComponentManager.Instance.Get<DataLayers>();

		// Check if there's at least one active data layer
		if (dataLayers.activeLayerPanels.Count == 0 ||
			gridLayerController.mapLayers.Count ==0)
        {
            ShowMessage(Translator.Get(ToolMessenger.NoDataLayersMessage), MessageType.Warning);
            gridLayerController.OnShowGrid += OnShowGrid;
            return;
        }
        

		FinishTurnOn(gridLayerController);
    }

    private void OnShowGrid(GridMapLayer mapLayer, bool show)
    {
        if (show)
        {
            var gridLayerController = map.GetLayerController<GridLayerController>();
            gridLayerController.OnShowGrid -= OnShowGrid;

            FinishTurnOn(gridLayerController);
        }
    }

    private void FinishTurnOn(GridLayerController gridLayerController)
    {
        var mapLayers = gridLayerController.mapLayers;

        // For now, just get any grid
        var referenceGridData = mapLayers[0].Grid;

        HideMessage();

		// Create the planning grid
		var grid = new GridData
		{
			countX = referenceGridData.countX,
			countY = referenceGridData.countY
		};
		grid.ChangeBounds(referenceGridData.west, referenceGridData.east, referenceGridData.north, referenceGridData.south);
        grid.InitGridValues(false);

        // Build the planning categories list
        int index = 0;
		List<IntCategory> cats = new List<IntCategory>
		{
			new IntCategory() { color = new Color(0, 0, 0, 0.2f), name = "noData", value = index++ },
			new IntCategory() { color = Color.black, name = "empty", value = index++ }
		};
		for (int i = 0; i < typologies.Count; ++i)
        {
            cats.Add(new IntCategory()
            {
                color = typologies[i].color,
                name = typologies[i].name,
                value = index++
            });
        }
        grid.categories = cats.ToArray();

        // Create planning layer
        planningLayer = Instantiate(planningLayerPrefab);
        planningLayer.name = planningLayerPrefab.name;
        planningLayer.SetCellSize(0.25f);

        // Add planning layer to tool layers
        toolLayers.Add(planningLayer, grid, "PlanningLayer", Color.white);

		var cellGroupGrid = new GridData
		{
			countX = grid.countX,
			countY = grid.countY
		};
		cellGroupGrid.ChangeBounds(grid.west, grid.east, grid.north, grid.south);
        cellGroupGrid.InitGridValues(false);

        // Create planner
        planner = Instantiate(plannerPrefab);
        planner.name = plannerPrefab.name;
        planner.Init(typologyLibrary, planningLayer.Grid, typologies);

        // set output after initialization
        planner.SetOutput(gameObject.activeInHierarchy);

		// Select the first typology in the list (only if none selected) 
		if (!typologyGroup.AnyTogglesOn() && typologies.Count > 0)
			typologyGroup.transform.GetChild(0).GetComponent<TypologyToggleWithText>().toggle.isOn = true;

		// Tools
		lassoTool = Instantiate(lassoToolPrefab);
        lassoTool.name = lassoToolPrefab.name;
        lassoTool.Deactivate();
        brushTool = Instantiate(brushToolPrefab);
        brushTool.name = brushToolPrefab.name;
        brushTool.Deactivate();

        // Setup tool callbacks
        index = 0;
        var tools = toolsGroup.GetComponentsInChildren<Toggle>();
        foreach (var tool in tools)
        {
            int id = index++;
            tool.onValueChanged.AddListener((isOn) => OnToolChange(isOn, id));
        }

		typologyGroup.gameObject.SetActive(true);

		ToggleSiteSpecificTypologies();
	}

	private void TurnOff()
    {
		var gridLayerController = map.GetLayerController<GridLayerController>();
		gridLayerController.OnShowGrid -= OnShowGrid;

		// Tools
		int index = 0;
        var tools = toolsGroup.GetComponentsInChildren<Toggle>();
        foreach (var tool in tools)
        {
            int id = index++;
            OnToolChange(false, id);
            tool.onValueChanged.RemoveAllListeners();
        }
		toolsGroup.SetAllTogglesOff();
        if (lassoTool != null)
        {
            Destroy(lassoTool.gameObject);
            lassoTool = null;
        }
        if (brushTool != null)
        {
            Destroy(brushTool.gameObject);
            brushTool = null;
        }

		// Reset typologyGroup to have first typology toggle to be on
		foreach (var toggle in toggles)
		{
			toggle.isOn = false;
		}
		toggles[0].isOn = true;

		// hide typologies, show tools
		OnClickReturn();

        // Kill the planner
        if (planner != null)
        {
			planner.ShowAllFlags(true);
            Destroy(planner.gameObject);
			planner = null;
        }

        // Destroy the planning layer
        if (planningLayer != null)
        {
            toolLayers.Remove(planningLayer);
            Destroy(planningLayer.gameObject);
            planningLayer = null;
        }

        toogleTarget.GetComponentInChildren<Toggle>().onValueChanged.RemoveListener(OnToggleTargetChange);

        if (targetPanel != null)
        {
            Destroy(targetPanel.gameObject);
        }

		// Additional settings
		displayPlanningSummaryToggle.onValueChanged.RemoveListener(OnDisplayPlanningSummary);
		displayPlanningSummaryToggle.isOn = true;

		//siteBrowser.OnBeforeActiveSiteChange -= OnBeforeActiveSiteChange;
		//siteBrowser.OnAfterActiveSiteChange -= OnAfterActiveSiteChange;
	}

    private void FillAllContours()
    {
        var contoursTool = ComponentManager.Instance.GetOrNull<ContoursTool>();
		if (contoursTool == null)
			return;

		ContoursMapLayer contoursLayer = contoursTool.ContoursLayer;
        if (contoursLayer != null)
        {
            var pg = planningLayer.Grid;
            var cg = contoursLayer.Grid;

            double planningResolutionX = pg.countX / (pg.east - pg.west);
            double planningResolutionY = pg.countY / (pg.south - pg.north);

            double invResolutionX = (cg.east - cg.west) / cg.countX;
            double invResolutionY = (cg.north - cg.south) / cg.countY;

            double coordsOffsetX = cg.west + 0.5 * invResolutionX;
            double coordsOffsetY = cg.north - 0.5 * invResolutionY;

            double scaleX = planningResolutionX * invResolutionX;
            double scaleY = planningResolutionY * invResolutionY;

            double offsetX = (cg.west - pg.west) * planningResolutionX + 0.5 * scaleX;
            double offsetY = (cg.north - pg.north) * planningResolutionY - 0.5 * scaleY;

            Coordinate coords;

            int contoursCount = cg.values.Length;
            for (int i = 0; i < contoursCount; i++)
            {
                float contoursValue = cg.values[i];
                if (contoursValue != 0)
                {
                    int y = i / cg.countX;
                    int x = i - y * cg.countX;

                    coords.Longitude = coordsOffsetX + x * invResolutionX;
                    coords.Latitude = coordsOffsetY - y * invResolutionY;

                    x = (int)Math.Floor(offsetX + x * scaleX);
                    y = (int)Math.Floor(offsetY - y * scaleY);
                    if (x >= 0 && x <= pg.countX && y >= 0 && y <= pg.countY)
                    {
                        int index = y * pg.countX + x;
                        planner.ChangeTypology(index, x, y, coords);
                    }
                }
            }

            planner.FinishChangingTypologies();
        }
    }


	//
	// Public Methods
	//

	public void ShowTypologyLabelAndCheckmark(bool show)
	{
		foreach (var toggle in toggles)
		{
			if (!toggle.gameObject.activeSelf)
				continue;

			TypologyToggleWithText typologyToggle = toggle.gameObject.GetComponentInParent<TypologyToggleWithText>();

			typologyToggle.text.gameObject.SetActive(show);
			typologyToggle.image.raycastTarget = show;
		}
	}
}
