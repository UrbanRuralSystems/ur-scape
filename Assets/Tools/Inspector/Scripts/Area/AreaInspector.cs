// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AreaInspector {

	public class AreaInspectorInfo
	{
		// public InspectorToggle uiElement;
        public AreaInspectionDelete areaInspectionDelete;
		public RectTransform areaInspection;
		public List<Coordinate> coords;
		public LineRenderer line;
        public InspectionArea inspectionArea;
        public AreaInspectionMapLayer mapLayer;
        public int inspectionIndex;
        public bool mapViewAreaChanged;
	}

	// Component references
    private InspectorTool inspectorTool;
	private MapController map;
	private ToolLayerController toolLayers;

    // Prefab references
	private ToggleButton inspectionDelPrefab;

	// Inspections count
	private int areaInspectionCount = 0;
	public int AreaInspectionCount
	{
		get { return this.areaInspectionCount; }
		set { this.areaInspectionCount = value; }
	}

	private int currAreaInspection = -1;
	public int CurrAreaInspection
	{
		get { return this.currAreaInspection; }
		set { this.currAreaInspection = value; }
	}

	private int createdAreaInspectionCount = 0;
	public int CreatedAreaInspectionCount
	{
		get { return this.createdAreaInspectionCount; }
		set { this.createdAreaInspectionCount = value; }
	}

	private List<GridData> areaInspectorGrids = new List<GridData>();
	public List<GridData> AreaInspectorGrids
	{
		get { return this.areaInspectorGrids; }
		set { this.areaInspectorGrids = value; }
	}

	//
	// Public Methods
	//

	public void Init(ToolLayerController toolLayers, ToggleButton inspectionDelPrefab)
	{
        inspectorTool = ComponentManager.Instance.Get<InspectorTool>();
		map = ComponentManager.Instance.Get<MapController>();
		this.toolLayers = toolLayers;

		this.inspectionDelPrefab = inspectionDelPrefab;
	}

	public void RemoveAreaInspectorInfoProperties(AreaInspectorInfo areaInfo)
	{
        // Remove map layer
		if (areaInfo.mapLayer != null)
		{
			toolLayers.Remove(areaInfo.mapLayer);
			if (areaInfo.mapLayer.grids.Count > 0)
			{
				areaInspectorGrids.AddRange(areaInfo.mapLayer.grids);
				areaInfo.mapLayer.Clear();
			}
			GameObject.Destroy(areaInfo.mapLayer.gameObject);
			areaInfo.mapLayer = null;
		}

        // Remove area inspection delete
        if (areaInfo.areaInspectionDelete)
        {
            GameObject.Destroy(areaInfo.areaInspectionDelete.gameObject);
            areaInfo.areaInspectionDelete = null;
        }

		areaInfo.coords.Clear();

		// Remove line renderer
		if (areaInfo.line)
		{
			GameObject.Destroy(areaInfo.line.gameObject);
			areaInfo.line = null;
		}

		// Remove area inspection
		if (areaInfo.areaInspection != null)
		{
			GameObject.Destroy(areaInfo.areaInspection.gameObject);
			areaInfo.areaInspection = null;
		}

        areaInfo.mapViewAreaChanged = false;
	}

	public void DeleteAllAreaInspection(AreaInspectorInfo[] areaInfos)
	{
		foreach (var areaInfo in areaInfos)
		{
			RemoveAreaInspectorInfoProperties(areaInfo);
		}
		areaInspectionCount = 0;
		currAreaInspection = -1;
	}

    public void ShowAreaLines(bool show, AreaInspectorInfo[] areaInfos)
	{
        if (areaInfos == null)
            return;

		foreach (var areaInfo in areaInfos)
		{
			if (areaInfo.line != null)
				areaInfo.line.gameObject.SetActive(show);

            if (areaInfo.areaInspectionDelete != null)
                areaInfo.areaInspectionDelete.gameObject.SetActive(show && inspectorTool.areaInspectorPanel.removeAreaInspectionToggle.isOn);
		}
	}

	public void CreateAreaInspection(AreaInspectorInfo areaInfo, int count, RectTransform areaInspectionPrefab, RectTransform inspectorContainer)
	{
		if (areaInfo.areaInspection == null)
		{
			areaInfo.areaInspection = GameObject.Instantiate(areaInspectionPrefab);
			areaInfo.areaInspection.SetParent(inspectorContainer);
			areaInfo.areaInspection.name = "InspectionArea" + count.ToString();
		}
	}

	public void CreateArea(AreaInspectorInfo areaInfo, int count, LineRenderer areaPrefab, List<Vector3> points)
	{
		// Init line prefab
		if (areaInfo.line == null)
		{
			areaInfo.line = GameObject.Instantiate(areaPrefab);
			areaInfo.line.transform.SetParent(areaInfo.areaInspection);
			areaInfo.line.name = "Area" + count.ToString();
            areaInfo.inspectionArea = areaInfo.line.GetComponent<InspectionArea>();
            areaInfo.inspectionArea.SetAreaInfo(areaInfo);

			// Create shape of area and store coordinates
			int length = points.Count;
			for (int i = 0; i < length; ++i)
			{
				areaInfo.line.positionCount++;
				Vector3 worldPos = points[i];
				areaInfo.line.SetPosition(i, points[i]);

				Coordinate coord = map.GetCoordinatesFromUnits(worldPos.x, worldPos.z);
				areaInfo.coords.Add(coord);
			}
		}
	}

    public void CreateInspectorDeleteButton(AreaInspectorInfo areaInfo)
    {
        Vector3 firstPt = Camera.main.WorldToScreenPoint(areaInfo.line.GetPosition(0));
        ToggleButton inspectionDelTB = GameObject.Instantiate(inspectionDelPrefab, firstPt, Quaternion.identity);
        areaInfo.areaInspectionDelete = inspectionDelTB.GetComponent<AreaInspectionDelete>();

        areaInfo.areaInspectionDelete.transform.SetParent(areaInfo.areaInspection);
        areaInfo.areaInspectionDelete.transform.name = "InspectionDel" + createdAreaInspectionCount.ToString();

        areaInfo.areaInspectionDelete.SetAreaInfo(areaInfo);
    }

    public void CreateAreaMapLayer(AreaInspectorInfo areaInfo, AreaInspectionMapLayer areaInspectionMapLayerPrefab)
	{
		areaInfo.mapLayer = toolLayers.CreateGridMapLayer(areaInspectionMapLayerPrefab, "AreaInspectionLayer" + createdAreaInspectionCount.ToString());
		areaInfo.mapLayer.Init(areaInfo.coords);
	}
}
