// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using AreaInfo = AreaInspector.AreaInspectorInfo;

public class AreaInspectorPanel : MonoBehaviour
{
    [Header("Prefabs")]
    public AreaInspectionMapLayer areaInspectionMapLayerPrefab;
    public InspectorToggle inspectorTogglePrefab;
    public ToggleButton inspectionDelPrefab;
	public LassoTool lassoToolPrefab;

    [Header("UI References")]
    public Toggle createAreaInspectionToggle;
	public Toggle removeAreaInspectionToggle;

    // Constants
    private const float LineWidth = 0.01f;
    private const float HalfLineWidth = LineWidth * 0.5f;

    // Prefab Instances
	private LassoTool lassoTool;

    // Component references
    private DataLayers dataLayers;
    private InspectorTool inspectorTool;
    private MapViewArea mapViewArea;
    private SiteBrowser siteBrowser;
    private MapController mapController;
    private GridLayerController gridLayerController;
    private InspectorOutput inspectorOutput;

    private int maxInspectionCount = 0;

    // Area Inspector
	[HideInInspector]
    public AreaInspector areaInspector = new AreaInspector();
    [HideInInspector]
    public AreaInfo[] areaInfos;

    private Coroutine waitForLayout;

    //
    // Unity Methods
    //

    private void OnEnable()
    {
        // Reset toggles
        createAreaInspectionToggle.isOn = false;
        removeAreaInspectionToggle.isOn = false;
    }

    private void OnDisable()
    {
        // Reset
        createAreaInspectionToggle.interactable = true;
    }

    private void Update()
    {
        bool isCursorWithinMapViewArea = inspectorTool.IsPosWithinMapViewArea(Input.mousePosition);
        if (!isCursorWithinMapViewArea)
        {
            inspectorTool.SetCursorTexture(inspectorTool.cursorDefault);
        }
    }

    //
    // Event Methods
    //

    private void OnCreateInspectionChanged(bool isOn)
    {
        InspectorTool.Action tmpAction = (isOn) ? InspectorTool.Action.CreateAreaInspection : InspectorTool.Action.None;
        Texture2D otherTexture = (isOn) ? inspectorTool.cursorDraw : inspectorTool.cursorDefault;

        inspectorTool.SetCursorTexture(otherTexture);
        inspectorTool.SetAction(tmpAction);
    }

    private void OnRemoveInspectionChanged(bool isOn)
    {
        inspectorTool.SetAction((isOn) ? InspectorTool.Action.RemoveInspection : InspectorTool.Action.None);
    }

    private void OnDrawArea(DrawingInfo info)
    {
        var lassoInfo = info as LassoDrawingInfo;
        var areaInfo = areaInfos[areaInspector.AreaInspectionCount];
        int count = areaInspector.CreatedAreaInspectionCount + 1;
        areaInspector.CreateAreaInspection(areaInfo, count, inspectorTool.inspectionPrefab, inspectorTool.InspectContainer);
        areaInspector.CreateArea(areaInfo, count, inspectorTool.areaPrefab, lassoInfo.points);

        AddAreaInspection();
        UpdateAreasElements(areaInspector.CurrAreaInspection);
		ComputeAndUpdateAreaProperties();

        createAreaInspectionToggle.isOn = false;
        inspectorTool.SetCursorTexture(inspectorTool.cursorDefault);

		// Update inspection line info panel
        SetCurrInspection(areaInspector.CurrAreaInspection);
        inspectorOutput.ShowSummaryHeaderAndDropdown(InspectorTool.InspectorType.Area, true);
        // inspectorOutput.SetDropDownInteractive(InspectorTool.InspectorType.Area, areaInspector.AreaInspectionCount > 1);
        inspectorOutput.ShowHeader(InspectorTool.InspectorType.Area, areaInspector.AreaInspectionCount >= 1);
        inspectorOutput.ShowPropertiesAndSummaryLabel(InspectorTool.InspectorType.Area, areaInspector.AreaInspectionCount >= 1);
    }

    private void OnAreaToolCancel()
    {
        createAreaInspectionToggle.isOn = false;
    }

    public void OnRemoveAreaInspection(AreaInfo areaInfo)
	{
		--areaInspector.AreaInspectionCount;

        // if (areaInspector.AreaInspectionCount == 1)
        //     inspectorOutput.SetDropDownInteractive(InspectorTool.InspectorType.Area, false);

		if (areaInspector.AreaInspectionCount == 0)
		{
			inspectorTool.SetAction(InspectorTool.Action.None);
			areaInspector.CurrAreaInspection = -1;
			removeAreaInspectionToggle.interactable = false;

            inspectorOutput.ResetAndClearOutput(InspectorTool.InspectorType.Area);

            // Update output
            inspectorOutput.AreaOutput.UpdateAreaInspectorOutput(null, dataLayers);
		}
		else
		{
            // Do necessary swapping of elements in areaInfos array upon deletion of inspection line
            var inspectionArea = areaInfo.areaInspectionDelete.transform.parent;
			int index = inspectionArea.GetSiblingIndex();
            int count = areaInspector.AreaInspectionCount;
			if (index < count)
			{
				for (int i = index; i < count; ++i)
				{
					var temp = areaInfos[i];
					areaInfos[i] = areaInfos[i + 1];
                    var tmpinspectionArea = areaInfos[i + 1].areaInspectionDelete.transform.parent;

					var tempIndex = tmpinspectionArea.GetSiblingIndex();
                    inspectionArea.SetSiblingIndex(tempIndex);
                    areaInfos[i].inspectionIndex = i;

					areaInfos[i + 1] = temp;
				}
			}

			// Update currAreaInspection value
			if (areaInspector.CurrAreaInspection == index)
				areaInspector.CurrAreaInspection = Mathf.Clamp(index - 1, 0, 2);
			else
				Mathf.Clamp(--areaInspector.CurrAreaInspection, 0, 2);
		}
        areaInspector.RemoveAreaInspectorInfoProperties(areaInfo);
		SetCurrInspection(areaInspector.CurrAreaInspection);

		createAreaInspectionToggle.interactable = true;
	}

    private void OnOtherGridFilterChange(GridData grid)
    {
        UpdateOutput();
    }

    private void OnOtherGridChange(GridData grid)
    {
        UpdateOutput();
    }

    private void OnMapUpdate()
    {
        UpdateAreasPtsInspectionDel();

        if (areaInspector.CurrAreaInspection >= 0)
        {
            var currArea = areaInfos[areaInspector.CurrAreaInspection];
            if (currArea.mapLayer != null)
                currArea.mapLayer.Refresh(currArea.coords);
        }
    }

	private void OnMapViewAreaChange()
	{
        if (!gameObject.activeSelf)
            return;

		if (waitForLayout != null)
		{
			StopCoroutine(waitForLayout);
		}
		waitForLayout = StartCoroutine(WaitForLayoutToFinish());
	}

	private void OnBeforeActiveSiteChange(Site nextSite, Site previousSite)
    {
		if (inspectorOutput != null)
			inspectorOutput.ResetAndClearOutput(InspectorTool.InspectorType.Area);
		ClearAllInspections();
    }

    //
    // Public Methods
    //

    public void Init(ToolLayerController toolLayers, int maxInspectionCount)
    {
        // Initializations
        inspectorTool = ComponentManager.Instance.Get<InspectorTool>();
        this.maxInspectionCount = maxInspectionCount;

        areaInspector.Init(toolLayers, inspectionDelPrefab);
        InitAreaInspectorInfo();
    }

    public void InitComponentsAndListeners()
    {
        // Initialize component references
        mapController = inspectorTool.Map;
        gridLayerController = mapController.GetLayerController<GridLayerController>();
        inspectorOutput = inspectorTool.InspectOutput;
        dataLayers = ComponentManager.Instance.Get<DataLayers>();
        mapViewArea = ComponentManager.Instance.Get<MapViewArea>();
		siteBrowser = ComponentManager.Instance.Get<SiteBrowser>();

        // Add listeners
		mapController.OnMapUpdate += OnMapUpdate;
		mapViewArea.OnMapViewAreaChange += OnMapViewAreaChange;
        siteBrowser.OnBeforeActiveSiteChange += OnBeforeActiveSiteChange;
        createAreaInspectionToggle.onValueChanged.AddListener(OnCreateInspectionChanged);
        removeAreaInspectionToggle.onValueChanged.AddListener(OnRemoveInspectionChanged);
    }

    public void StartCreateAreaInspector()
	{
		if (lassoTool == null)
		{
			lassoTool = Instantiate(lassoToolPrefab);
			lassoTool.name = lassoToolPrefab.name;
			lassoTool.CanDraw = true;
			lassoTool.OnDraw += OnDrawArea;
			// lassoTool.OnCancel += OnAreaToolCancel;
		}

		lassoTool.Activate();
	}

    public void FinishCreateAreaInspector()
	{
		if (lassoTool != null)
		{
			lassoTool.Deactivate();
			lassoTool.CanDraw = false;
			lassoTool.OnDraw -= OnDrawArea;
			// lassoTool.OnCancel -= OnAreaToolCancel;

			Destroy(lassoTool.gameObject);
			lassoTool = null;
		}
	}

    public void ChangeAreaLine(AreaInfo areaInfo, bool solidThin)
    {
        var line = areaInfo.line;
        line.widthMultiplier = (solidThin) ? HalfLineWidth : LineWidth;
    }

    public void AllowRemoveAreaInspections(bool allow)
	{
		for (int i = 0; i < maxInspectionCount; ++i)
		{
			if (areaInfos[i].line == null)
				continue;

            areaInfos[i].areaInspectionDelete.ShowInspectionDelete(allow);
			if (allow)
			{
                ChangeAreaLine(areaInfos[i], true);
			}
			else
			{
				// Revert curr area
				if (i == areaInspector.CurrAreaInspection)
				{
                    ChangeAreaLine(areaInfos[i], false);
				}
			}
		}
	}

    public void ComputeAndUpdateAreaProperties()
    {
        inspectorOutput.AreaOutput.ComputeAndUpdateTotalArea(areaInfos, areaInspector.CurrAreaInspection);
		inspectorOutput.ComputeAndUpdateMetrics(InspectorTool.InspectorType.Area);
    }

    public void UpdateAreasPtsInspectionDel()
    {
        if (areaInfos == null)
            return;

        foreach (var areaInfo in areaInfos)
        {
            if (areaInfo.line == null)
                continue;

            int coordsCount = areaInfo.coords.Count;
            for (int i = 0; i < coordsCount; ++i)
            {
                var coordToWorldPos = mapController.GetUnitsFromCoordinates(areaInfo.coords[i]);
                Vector3 worldPos = new Vector3(coordToWorldPos.x, coordToWorldPos.z, coordToWorldPos.y);

                areaInfo.line.SetPosition(i, worldPos);
            }

            // Compute new inspection delete pos and update
            Vector3 firstPt = Camera.main.WorldToScreenPoint(areaInfo.line.GetPosition(0));
            areaInfo.areaInspectionDelete.UpdatePosition(firstPt);

            // Determine whether inspection delete should be shown
            bool showInspectionDel = areaInfo.areaInspectionDelete.IsRectWithinMapViewArea(firstPt) && removeAreaInspectionToggle.isOn;
            areaInfo.areaInspectionDelete.ShowInspectionDelete(showInspectionDel);
        }
    }

    public void UpdateGridsAndOutput(AreaInfo areaInfo, bool currArea)
    {
        if (areaInfo == null || areaInfo.mapLayer == null)
            return;

        if (currArea)
        {
			// Add visible grids to the budget layer
			foreach(var layer in gridLayerController.mapLayers)
			{
                AddGridData(layer.Grid);
			}
            areaInspector.AreaInspectorGrids.Clear();

            areaInfo.mapLayer.Show(true);
            // Update data values
            areaInfo.mapLayer.Refresh(areaInfo.coords);
        }
        else
        {
            // Hide at the beginning
            areaInfo.mapLayer.Show(false);

            // Move visible grids from budget layer to list
            if (areaInfo.mapLayer.grids.Count > 0)
            {
                areaInspector.AreaInspectorGrids.AddRange(areaInfo.mapLayer.grids);
                areaInfo.mapLayer.Clear();
            }
        }
        UpdateOutput();
    }

    public void AreaShowGridAndUpdateOutput(GridMapLayer mapLayer, bool show)
    {
        if (areaInspector.CurrAreaInspection >= 0)
        {
            var otherGrid = mapLayer.Grid;
            var currAreaInfo = areaInfos[areaInspector.CurrAreaInspection];

            if (show)
            {
                // Add to area inspection map layer
                if (currAreaInfo.mapLayer.IsVisible())
                {
                    AddGridData(otherGrid);
                    currAreaInfo.mapLayer.Refresh(currAreaInfo.coords);
                }
                else
                {
                    areaInspector.AreaInspectorGrids.Add(otherGrid);
                }
            }
            else
            {
                // Remove from layer
                if (currAreaInfo.mapLayer.IsVisible())
                {
                    RemoveGridData(otherGrid);
                    currAreaInfo.mapLayer.Refresh(currAreaInfo.coords);
                }
                else
                {
                    areaInspector.AreaInspectorGrids.Remove(otherGrid);
                }
            }

			string dataLayerName = mapLayer.Grid.patch.DataLayer.Name;
			inspectorOutput.ShowInspectorOutputItemLabel(InspectorTool.InspectorType.Area, dataLayerName, show);
			UpdateOutput();
        }
    }

    public void SetCurrInspection(int index)
    {
		areaInspector.CurrAreaInspection = index;
		UpdateAreasElements(areaInspector.CurrAreaInspection);
        inspectorOutput.SetInspection(InspectorTool.InspectorType.Area, areaInspector.CurrAreaInspection);
	}

    //
    // Private Methods
    //

    private void InitAreaInspectorInfo()
	{
		// Instantiate and initialize InspectorToggles
		areaInfos = new AreaInfo[maxInspectionCount];
		for (int i = 0; i < maxInspectionCount; ++i)
		{
			var areaInfo = new AreaInfo
			{
                areaInspectionDelete = null,
				areaInspection = null,
				coords = new List<Coordinate>(),
				line = null,
                inspectionArea = null,
				mapLayer = null,
                inspectionIndex = 0,
                mapViewAreaChanged = false
			};

			areaInfos[i] = areaInfo;
		}
	}

    private void AddGridData(GridData otherGrid)
    {
        if (otherGrid.patch != null && otherGrid.patch is GraphPatch)
            return;

        if (areaInfos[areaInspector.CurrAreaInspection] == null)
            return;

        areaInfos[areaInspector.CurrAreaInspection].mapLayer.Add(otherGrid);

        otherGrid.OnGridChange += OnOtherGridChange;
        otherGrid.OnValuesChange += OnOtherGridChange;
        otherGrid.OnFilterChange += OnOtherGridFilterChange;
    }

    private void RemoveGridData(GridData otherGrid)
    {
        if (otherGrid.patch != null && otherGrid.patch is GraphPatch)
            return;

        areaInfos[areaInspector.CurrAreaInspection].mapLayer.Remove(otherGrid);

        otherGrid.OnGridChange -= OnOtherGridChange;
        otherGrid.OnValuesChange -= OnOtherGridChange;
        otherGrid.OnFilterChange -= OnOtherGridFilterChange;
    }

    private void UpdateOutput()
	{
		if (areaInspector.CurrAreaInspection == -1)
			return;

		inspectorOutput.AreaOutput.UpdateAreaInspectorOutput(areaInfos[areaInspector.CurrAreaInspection], dataLayers);
    }

    private void UpdateAreasElements(int currIndex)
	{
		for (int i = 0; i < maxInspectionCount; ++i)
		{
			if (areaInfos[i].line == null)
				continue;

			bool currArea = (i == currIndex);
			UpdateGridsAndOutput(areaInfos[i], currArea);
			if (!currArea)
            {
                ChangeAreaLine(areaInfos[i], true);
            }
            else
            {
                ChangeAreaLine(areaInfos[i], false);
            }
		}
	}

    private void AddAreaInspection()
	{
		if (areaInspector.AreaInspectionCount == maxInspectionCount)
			return;

		++areaInspector.AreaInspectionCount;
		areaInspector.CurrAreaInspection = areaInspector.AreaInspectionCount - 1;
		++areaInspector.CreatedAreaInspectionCount;

		var areaInfo = areaInfos[areaInspector.CurrAreaInspection];

        areaInspector.CreateInspectorDeleteButton(areaInfo);

        areaInfo.inspectionIndex = areaInspector.CurrAreaInspection;

        areaInspector.CreateAreaMapLayer(areaInfo, areaInspectionMapLayerPrefab);

		removeAreaInspectionToggle.interactable = true;

        // Disable ability to create anymore inspection areas
		if (areaInspector.AreaInspectionCount == maxInspectionCount)
		{
			createAreaInspectionToggle.interactable = false;
			inspectorTool.SetAction(InspectorTool.Action.None);
		}

        areaInfo.mapViewAreaChanged = false;
	}

    private void ClearAllInspections()
    {
        foreach (var areaInfo in areaInfos)
        {
            if (areaInfo.line == null)
                continue;

            areaInspector.RemoveAreaInspectorInfoProperties(areaInfo);
            --areaInspector.AreaInspectionCount;
        }

        inspectorTool.SetAction(InspectorTool.Action.None);
        areaInspector.CurrAreaInspection = -1;
        areaInspector.CreatedAreaInspectionCount = 0;
        removeAreaInspectionToggle.interactable = false;
        createAreaInspectionToggle.interactable = true;

        // Update output
        inspectorOutput.AreaOutput.UpdateAreaInspectorOutput(null, dataLayers);
    }

    private IEnumerator WaitForLayoutToFinish()
	{
		yield return WaitFor.Frames(WaitFor.InitialFrames);
		if (areaInfos != null)
		{
			foreach (var areaInfo in areaInfos)
			{
				if (areaInfo.line == null)
					continue;

				areaInfo.mapViewAreaChanged = true;

                int coordsCount = areaInfo.coords.Count;
                for (int i = 0; i < coordsCount; ++i)
                {
                    var coordToWorldPos = mapController.GetUnitsFromCoordinates(areaInfo.coords[i]);
                    Vector3 worldPos = new Vector3(coordToWorldPos.x, coordToWorldPos.z, coordToWorldPos.y);

                    areaInfo.line.SetPosition(i, worldPos);
                }

                Vector3 firstPt = Camera.main.WorldToScreenPoint(areaInfo.line.GetPosition(0));
                areaInfo.areaInspectionDelete.UpdatePosition(firstPt);

				// Determine whether inspection delete should be shown
				bool showInspectionDel = areaInfo.areaInspectionDelete.IsRectWithinMapViewArea(firstPt) && removeAreaInspectionToggle.isOn;
				areaInfo.areaInspectionDelete.ShowInspectionDelete(showInspectionDel);
			}
		}
		waitForLayout = null;
	}
}