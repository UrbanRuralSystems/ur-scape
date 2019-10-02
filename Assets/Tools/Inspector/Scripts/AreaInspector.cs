// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AreaInspector {

	public class AreaInspectorInfo
	{
		public InspectorToggle uiElement;
		public RectTransform areaInspection;
		public List<Coordinate> coords;
		public LineRenderer line;
	}

	// Component references
	private MapController map;

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

	public void Init(ToolLayerController toolLayers)
	{
		//inputHandler = ComponentManager.Instance.Get<InputHandler>();
		map = ComponentManager.Instance.Get<MapController>();
		//this.toolLayers = toolLayers;
	}

	public void RemoveAreaInspectorInfoProperties(AreaInspectorInfo areaInfo)
	{
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

	public void UpdateAreaInspectorToggleProperties(AreaInspectorInfo areaInfo, string newAreaInspectionPrefix)
	{
		areaInfo.uiElement.IsInteractable = true;
		areaInfo.uiElement.toggle.isOn = true;
		areaInfo.uiElement.label.placeholder.GetComponent<Text>().text = newAreaInspectionPrefix + " " + createdAreaInspectionCount.ToString();
		areaInfo.uiElement.letter.GetComponent<Text>().text = createdAreaInspectionCount.ToString();
		areaInfo.uiElement.letter.gameObject.SetActive(true);
		areaInfo.uiElement.AreaInfo = areaInfo;
	}

	public void UpdateAreaInspectorToggle(AreaInspectorInfo areaInfo, bool currArea)
	{
		var inspectorToggle = areaInfo.uiElement;
		inspectorToggle.UpdateInspectorToggle(currArea);
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

	public void CreateArea(AreaInspectorInfo areaInfo, int count, LineRenderer linePrefab, List<Vector3> points)
	{
		// Init line prefab
		if (areaInfo.line == null)
		{
			areaInfo.line = GameObject.Instantiate(linePrefab);
			areaInfo.line.transform.SetParent(areaInfo.areaInspection);
			areaInfo.line.name = "Area" + count.ToString();

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

	public void UpdatePositionAndLinePts(AreaInspectorInfo[] areaInfos)
	{
		foreach (var areaInfo in areaInfos)
		{
			if (areaInfo.line == null)
				return;

			int length = areaInfo.line.positionCount;
			for (int i = 0; i < length; ++i)
			{
				var coordToWorldPos = map.GetUnitsFromCoordinates(areaInfo.coords[i]);
				var worldPos = new Vector3(coordToWorldPos.x, coordToWorldPos.z, coordToWorldPos.y);
				areaInfo.line.SetPosition(i, worldPos);
			}
		}
	}

	public void ShowAreaLines(bool show, AreaInspectorInfo[] areaInfos)
	{
		foreach (var areaInfo in areaInfos)
		{
			if (areaInfo.line != null)
				areaInfo.line.gameObject.SetActive(show);
		}
	}
}
