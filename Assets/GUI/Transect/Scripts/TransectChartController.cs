// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//			Muhammad Salihin Bin Zaol-kefli

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LineInfo = LineInspector.LineInspectorInfo;

public class TransectChartController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    public RectTransform chartsContainer;
	public RectTransform customChartsContainer;
	public RectTransform highlight;

    [Header("Prefabs")]
	public TransectChart gridChartPrefab;
	public TransectChart categoryChartPrefab;

	public delegate void OnTransectHighlightChangeDelegate(float percentage);
    public event OnTransectHighlightChangeDelegate OnTransectHighlightChange;

    private RectTransform rt;
    private float invRectWidth;

    private Dictionary<GridData, TransectChart> layerCharts = new Dictionary<GridData, TransectChart>();
	private Dictionary<GridData, TransectChart> customCharts = new Dictionary<GridData, TransectChart>();
	private float locator = 0.5f;
    private bool updateHighlight;
    private float highlightHalfWidth = 0;

    private float lastMousePosition = -1;
    private float highlightPosition = 0;

	private MapController map;
    private GridLayerController gridLayerController;
    private Material material;
    private InspectorTool inspectorTool;

	private LineInfo lineInfo = null;

	//
	// Unity Methods
	//

	private IEnumerator Start()
    {
        // Copy material to avoid serializing width/height changes
        var image = GetComponent<Image>();
        material = new Material(image.material);

        image.RegisterDirtyMaterialCallback(OnMaterialChange);
        image.material = material;

        rt = GetComponent<RectTransform>();
        invRectWidth = 1f / rt.rect.width;

        UpdateTransectSize();

        highlightHalfWidth = highlight.rect.width * 0.5f;

		// Wait InitialFrames frame for UI to finish layout, so that charts have the right size for the shader
		yield return WaitFor.Frames(WaitFor.InitialFrames);

        var transectLocator = ComponentManager.Instance.GetOrNull<TransectLocator>();
        if (transectLocator != null)
        {
			transectLocator.OnLocatorChange += OnLocatorChange;
        }

        map = ComponentManager.Instance.Get<MapController>();
        inspectorTool = ComponentManager.Instance.Get<InspectorTool>();
        if (map != null)
        {
            gridLayerController = map.GetLayerController<GridLayerController>();
            if (gridLayerController != null)
            {
                gridLayerController.OnShowGrid += OnShowGrid;

                foreach (var mapLayer in gridLayerController.mapLayers)
                {
					if (layerCharts.ContainsKey(mapLayer.Grid))
						continue;

                    Add(mapLayer.Grid, mapLayer.Grid.patch.DataLayer.Color, layerCharts, chartsContainer);
					if (lineInfo != null)
						UpdateGridData(mapLayer.Grid);
				}
            }
        }

		var siteBrowser = ComponentManager.Instance.Get<SiteBrowser>();
		siteBrowser.OnBeforeActiveSiteChange += OnBeforeActiveSiteChange;
	}

    private void OnRectTransformDimensionsChange()
    {
        if (material != null)
        {
            UpdateTransectSize();
        }
    }

    private void Update()
    {
        if (updateHighlight)
        {
            if (Input.mousePosition.x != lastMousePosition)
            {
                lastMousePosition = Input.mousePosition.x;

                highlightPosition = rt.InverseTransformPoint(Input.mousePosition).x * invRectWidth + rt.pivot.x;

                var pos = highlight.position;
                pos.x = Input.mousePosition.x - highlightHalfWidth;
                highlight.position = pos;

                if (OnTransectHighlightChange != null)
                    OnTransectHighlightChange(highlightPosition);
            }
        }
    }

    protected void OnDestroy()
    {
        var image = GetComponent<Image>();
        if (image != null)
        {
            image.UnregisterDirtyMaterialCallback(OnMaterialChange);
        }

        if (gridLayerController != null)
        {
            gridLayerController.OnShowGrid -= OnShowGrid;
        }

		foreach (var chart in customCharts.Values)
		{
			chart.Destroy();
		}
    }

    //
    // Inheritance Methods
    //

    public void OnPointerEnter(PointerEventData eventData)
    {
        updateHighlight = true;
        highlight.gameObject.SetActive(updateHighlight);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        updateHighlight = false;
        highlight.gameObject.SetActive(updateHighlight);
    }

    public void ShowHighlight(bool show)
    {
        highlight.gameObject.SetActive(show);
    }

    public void UpdateHighlightPos(float percent)
    {
        var pos = highlight.anchoredPosition;
        pos.x = percent * rt.sizeDelta.x;
        highlight.anchoredPosition = pos;
    }

    //
    // Event Methods
    //

    private void OnMaterialChange()
    {
        var mat = GetComponent<Image>().materialForRendering;
        if (mat != material)
        {
            material = mat;
            UpdateTransectSize();
        }
    }

    private void OnLocatorChange(float locator)
    {
        this.locator = locator;

        foreach (var chart in layerCharts.Values)
        {
            chart.SetLocator(locator);
        }
		foreach (var chart in customCharts.Values)
		{
			chart.SetLocator(locator);
		}
	}

	private void OnShowGrid(GridMapLayer mapLayer, bool show)
    {
        if (show)
        {
			if(lineInfo != null)
			{
				Add(mapLayer.Grid, mapLayer.Grid.patch.DataLayer.Color, layerCharts, chartsContainer);
				UpdateGridData(mapLayer.Grid);
			}
        }
        else
        {
            Remove(mapLayer.Grid, layerCharts);
        }
    }

	private void OnOtherGridFilterChange(GridData grid)
	{
		if (lineInfo != null)
			UpdateGridData(grid);
	}

	private void OnBeforeActiveSiteChange(Site nextSite, Site previousSite)
	{
		foreach (var mapLayer in gridLayerController.mapLayers)
		{
			Remove(mapLayer.Grid, layerCharts);
		}
	}

	//
	// Public Methods
	//

	public void AddCustomGrid(GridData grid, Color color, TransectChart prefab = null)
	{
		var chart = Add(grid, color, customCharts, customChartsContainer, prefab);
		chart.transform.SetAsLastSibling();
	}

	public void AddGrid(GridData otherGrid, Color color)
	{
		if (!layerCharts.ContainsKey(otherGrid))
		{
			Add(otherGrid, color, layerCharts, chartsContainer);
		}
	}

    public void RemoveCustomGrid(GridData grid)
	{
		Remove(grid, customCharts);
	}

	public void UpdateGridData(GridData otherGrid)
	{
		layerCharts.TryGetValue(otherGrid, out TransectChart chart);
		if(chart == null)
		{
			Add(otherGrid, otherGrid.patch.DataLayer.Color, layerCharts, chartsContainer);
			chart = layerCharts[otherGrid];
		}
        
        if (inspectorTool != null)
        {
            if (inspectorTool.InspectType == InspectorTool.InspectorType.Line)
                chart.SetLineInfo(lineInfo);
        }
	}

	public void SetLineInfo(LineInfo lineInfo)
	{
		this.lineInfo = lineInfo;
	}

	//
	// Private Methods
	//

	private TransectChart Add(GridData grid, Color color, Dictionary<GridData, TransectChart> charts, Transform container, TransectChart prefab = null)
    {
		if (charts.ContainsKey(grid))
		{
			Debug.LogError("Grid already added to transect charts");
			return charts[grid];
		}

		if (prefab == null)
		{
			prefab = grid.IsCategorized ? categoryChartPrefab : gridChartPrefab;
		}

		TransectChart chart = Instantiate(prefab, container, false);
        chart.Init(grid, color);

		grid.OnFilterChange += OnOtherGridFilterChange;
		if (lineInfo != null)
			chart.SetLineInfo(lineInfo);
		else
			chart.SetLocator(locator);

		charts.Add(grid, chart);

		return chart;
    }

    private void Remove(GridData grid, Dictionary<GridData, TransectChart> charts)
    {
        if (charts.ContainsKey(grid))
        {
			grid.OnFilterChange -= OnOtherGridFilterChange;
			DestroyImmediate(charts[grid].gameObject);
            charts.Remove(grid);
        }
    }

    private void UpdateTransectSize()
    {
        var rect = GetComponent<RectTransform>().rect;
        material.SetFloat("Width", rect.width);
        material.SetFloat("Height", rect.height);
    }

}
