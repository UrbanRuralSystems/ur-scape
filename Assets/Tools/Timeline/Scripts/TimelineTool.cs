// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class TimelineTool : Tool
{
	private static readonly WaitForSeconds delay = new WaitForSeconds(0.05f);

	private class LayerData
	{
		public DataLayer layer;
		public List<int> years = new List<int>();
	}

	private class TimelineData
	{
		public List<LayerData> layers = new List<LayerData>();
		public List<int> years = new List<int>();
		public Dictionary<int, int> yearToIndex = new Dictionary<int, int>();

		public void Reset()
		{
			layers.Clear();
			years.Clear();
			yearToIndex.Clear();
		}
	}

	[Header("UI Reference")]
	public Text warningText;
    public SliderEx slider;
	public Image chart;
	public RectTransform layersContainer;
	public RectTransform yearsContainer;

	[Header("Prefabs")]
	public RectTransform layerPrefab;
	public ToggleButton yearPrefab;
	public RectTransform yearSeparatorPrefab;
	public Toggle circlePrefab;

	// reference
	private DataManager dataManager;
	private DataLayers dataLayers;
	private Material material;

	private int selectedYear = -1;
	private bool isChangingYear;
	private List<ToggleButton> yearToggles = new List<ToggleButton>();
	private int containerMaxOffset;
	private bool updateVisiblePatches;

	private readonly TimelineData data = new TimelineData();
	private readonly List<DataLayer> activeLayers = new List<DataLayer>();

	//
	// Unity Methods
	//

	protected override void Awake()
	{
		base.Awake();

		// Copy material to avoid serializing width/height changes
		material = new Material(chart.material);

		chart.RegisterDirtyMaterialCallback(OnMaterialChange);
		chart.material = material;

		containerMaxOffset = (int)layersContainer.offsetMax.y;

		UpdateChartSize();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		if (chart != null)
		{
			chart.UnregisterDirtyMaterialCallback(OnMaterialChange);
		}

		if (dataLayers != null)
		{
			dataLayers.OnLayerVisibilityChange -= OnDataLayerVisibilityChange;
		}
	}

	private void OnRectTransformDimensionsChange()
	{
		if (material != null)
		{
			UpdateChartSize();
		}
	}

	protected override void OnToggleTool(bool isOn)
    {
        if (isOn)
        {
			// Cache references
			dataManager = ComponentManager.Instance.Get<DataManager>();
			dataLayers = ComponentManager.Instance.Get<DataLayers>();

			// Enable listeners
			dataLayers.OnLayerVisibilityChange += OnDataLayerVisibilityChange;
			slider.onValueChanged.AddListener(OnSliderChange);
			map.OnBoundsChange += OnMapBoundsUpdate;

			foreach (var group in dataManager.groups)
			{
				foreach (var layer in group.layers)
				{
					if (dataLayers.IsLayerActive(layer))
					{
						activeLayers.Add(layer);
						layer.OnPatchVisibilityChange += OnPatchVisibilityChange;
					}
				}
			}

			CheckVisibleData();
        }
        else
        {
			// Disable listeners
			dataLayers.OnLayerVisibilityChange -= OnDataLayerVisibilityChange;
			slider.onValueChanged.RemoveAllListeners();
			map.OnBoundsChange -= OnMapBoundsUpdate;

			foreach (var layer in activeLayers)
			{
				layer.OnPatchVisibilityChange -= OnPatchVisibilityChange;
			}

			activeLayers.Clear();

			ResetLayersYear();

			selectedYear = -1;
		}
    }

	//
	// Event Methods
	//

	private void OnMaterialChange()
	{
		var mat = chart.materialForRendering;
		if (mat != material)
		{
			material = mat;
			UpdateChartSize();
		}
	}

	private void OnMapBoundsUpdate()
	{
		updateVisiblePatches = true;
	}

	private void OnDataLayerVisibilityChange(DataLayer layer, bool visible)
	{
		if (visible)
		{
			activeLayers.Add(layer);
			layer.OnPatchVisibilityChange += OnPatchVisibilityChange;
		}
		else
		{
			activeLayers.Remove(layer);
			layer.OnPatchVisibilityChange -= OnPatchVisibilityChange;
			layer.visibleYear = -1;
		}

		CheckVisibleData();
	}

	private void OnPatchVisibilityChange(DataLayer dataLayer, Patch patch, bool visible)
	{
		if (updateVisiblePatches)
		{
			DelayedCheckVisibleData();
		}
	}

	private void OnSliderChange(float value)
    {
		if (data.years.Count > 0)
			OnYearChange((int)value);
    }

	private void OnSlide(int index)
	{
		OnYearChange(index);
	}

	private void OnYearToggleChange(int index, bool isOn)
	{
		if (isOn)
			OnYearChange(index);

		yearToggles[index].GetComponentInChildren<Text>().fontStyle = isOn ? FontStyle.Bold : FontStyle.Normal;
	}

	private void OnYearChange(int index)
	{
		if (isChangingYear)
			return;

		isChangingYear = true;

		selectedYear = data.years[index];

		slider.value = index;

		var yearToggle = yearToggles[index];
		yearToggle.isOn = true;

		int layerCount = data.layers.Count;
		for (int i = 0; i < layerCount; i++)
		{
			var layerData = data.layers[i];
			var dotContainer = layersContainer.GetChild(i).GetChild(0);

			// Check if this layer has data for this year
			int yearIndex = FindNearestYear(layerData, dotContainer);
			var toggle = dotContainer.GetChild(yearIndex).GetComponent<Toggle>();
			if (!toggle.isOn)
			{
				toggle.isOn = true;
			}
		}

		isChangingYear = false;
	}

	private void OnCircleChange(Toggle toggle, DataLayer layer, int year)
	{
		if (toggle.isOn)
		{
			if (year == -1)
			{
				layer.HideLoadedPatches();
			}

			layer.visibleYear = year;
			layer.UpdatePatches(dataManager.ActiveSite, map.CurrentLevel, map.MapCoordBounds);
		}
	}

	//
	// Private Methods
	//

	private Coroutine delayedCheckVisibleData;
	private void DelayedCheckVisibleData()
	{
		if (delayedCheckVisibleData == null)
		{
			delayedCheckVisibleData = StartCoroutine(_DelayedCheckVisibleData());
		}
	}

	private IEnumerator _DelayedCheckVisibleData()
	{
		yield return delay;
		CheckVisibleData();
		delayedCheckVisibleData = null;
	}

	private void CheckVisibleData()
	{
		data.Reset();

		HashSet<int> years = new HashSet<int>();

		int levelIndex = map.CurrentLevel;
		var bounds = map.MapCoordBounds;

		foreach (var layer in activeLayers)
		{
			LayerData layerData = null;

			var level = layer.levels[levelIndex];
			foreach (var site in level.layerSites)
			{
				if (site.records.Count < 2)
					continue;

				foreach (var record in site.records)
				{
					foreach (var patch in record.Value.patches)
					{
						if (patch.IsVisible() || patch.Data.Intersects(bounds.west, bounds.east, bounds.north, bounds.south))
						{
							int year = record.Key;
							if (layerData == null)
							{
								layerData = new LayerData() { layer = layer };
								data.layers.Add(layerData);
							}

							if (!layerData.years.Contains(year))
							{
								layerData.years.Add(year);

								if (!years.Contains(year))
								{
									years.Add(year);
									data.years.Add(year);
								}
							}
							break;
						}
					}
				}
			}

			if (layerData != null)
			{
				layerData.years.Sort();
				if (layerData.layer.visibleYear == -1)
				{
					int index = selectedYear == -1? layerData.years.Count - 1 : FindNearestYear(layerData);
					layerData.layer.visibleYear = layerData.years[index];
				}
			}
		}

		data.years.Sort();
		int count = data.years.Count;
		for (int i  = 0; i < count; i++)
		{
			data.yearToIndex.Add(data.years[i], i);
		}

		updateVisiblePatches = false;

		// Need to remove and re-add listener because changing the slider count to a smaller number causes a OnSliderChange event
		slider.onValueChanged.RemoveAllListeners();
		slider.ChangeCount(data.years.Count);
		slider.onValueChanged.AddListener(OnSliderChange);

		UpdateChart();
	}
	
	private void UpdateChart()
	{
		for (int i = yearsContainer.childCount - 1; i >= 0; i--)
		{
			Destroy(yearsContainer.GetChild(i).gameObject);
		}
		for (int i = layersContainer.childCount - 1; i >= 0; i--)
		{
			Destroy(layersContainer.GetChild(i).gameObject);
		}
		yearToggles.Clear();

		if (data.layers == null || data.layers.Count == 0)
		{
			warningText.gameObject.SetActive(true);
			return;
		}
		warningText.gameObject.SetActive(false);


		int yearCount = data.years.Count;
		float layerLineWidth = layersContainer.rect.width + layerPrefab.GetChild(0).GetComponent<RectTransform>().sizeDelta.x;
		int circleDistance = (int)(layerLineWidth / yearCount);
		int circleOffset = (int)(0.5f * layerLineWidth / yearCount);

		foreach (var layerData in data.layers)
		{
			// Create a timeline layer
			var layer = layerData.layer;
			var timelineLayer = Instantiate(layerPrefab, layersContainer, false);
			timelineLayer.GetChild(1).GetComponent<Text>().text = layer.Name;

			// Create a circle for earch available year
			var circleContainer = timelineLayer.transform.GetChild(0);
			var circleGroup = circleContainer.GetComponent<ToggleGroup>();
			int currentYear = layer.visibleYear == -1? layerData.years[layerData.years.Count - 1] : layer.visibleYear;
			foreach (var year in layerData.years)
			{
				var circle = Instantiate(circlePrefab, circleContainer, false);
				circle.group = circleGroup;
				circle.image.color = layer.Color;
				circle.graphic.color = layer.Color;
				var rt = circle.GetComponent<RectTransform>();
				var pos = rt.anchoredPosition;
				pos.x = circleOffset + circleDistance * data.yearToIndex[year];
				rt.anchoredPosition = pos;

				if (year == currentYear)
				{
					circle.isOn = true;
				}

				int y = year;
				circle.onValueChanged.AddListener((isOn) => OnCircleChange(circle, layer, y));
			}
		}

		var o = yearsContainer.offsetMax;
		o.y = containerMaxOffset - (int)(0.5f * layersContainer.rect.height / data.layers.Count);
		yearsContainer.offsetMax = o;

		// Create the year toggles
		var yearsGroup = yearsContainer.GetComponent<ToggleGroup>();
		for (int i = 0; i < yearCount; i++)
		{
			var year = data.years[i];

			var newYear = Instantiate(yearPrefab, yearsContainer, false);
			newYear.group = yearsGroup;
			newYear.GetComponentInChildren<Text>().text = year.ToString();

			var rt = newYear.GetComponent<RectTransform>();
			rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, circleDistance);
			var pos = rt.anchoredPosition;
			pos.x = i * circleDistance;
			rt.anchoredPosition = pos;

			int index = i;
			newYear.onValueChanged.AddListener((isOn) => OnYearToggleChange(index, isOn));
			newYear.gameObject.AddComponent<SlideHandler>().OnSlide += () => OnSlide(index);

			var background = newYear.transform.GetChild(1).GetComponent<RectTransform>();
			if (i > 0 && year != data.years[i - 1] + 1)
			{
				var offset = background.offsetMin;
				offset.x = 5;
				background.offsetMin = offset;
			}

			if (i + 1 < yearCount && year != data.years[i + 1] - 1)
			{
				var offset = background.offsetMax;
				offset.x = -5;
				background.offsetMax = offset;
			}

			yearToggles.Add(newYear);
		}
	}


	private void UpdateChartSize()
	{
		var rect = GetComponent<RectTransform>().rect;
		material.SetFloat("Width", rect.width);
		material.SetFloat("Height", rect.height);
	}

	private void ResetLayersYear()
	{
		var site = dataManager.ActiveSite;
		foreach (var group in dataManager.groups)
		{
			foreach (var layer in group.layers)
			{
				layer.visibleYear = -1;
				if (dataLayers.IsLayerActive(layer))
				{
					layer.HideLoadedPatches();
					layer.UpdatePatches(site, map.CurrentLevel, map.MapCoordBounds);
				}
			}
		}
	}

	private int FindNearestYear(LayerData layerData, Transform dotContainer = null)
	{
		int dotIndex = layerData.years.IndexOf(selectedYear);
		if (dotIndex >= 0)
			return dotIndex;

		// If not, then find the nearest/newest year
		int yearCount = layerData.years.Count;
		for (int i = 0; i < yearCount; i++)
		{
			int layerYear = layerData.years[i];
			if (layerYear > selectedYear)
			{
				if (dotContainer == null)
					return i;

				if (i > 0 && dotContainer.GetChild(i - 1).GetComponent<Toggle>().isOn)
					return i - 1;

				return i;
			}
		}

		return layerData.years.Count - 1;
	}
}

public class SlideHandler : MonoBehaviour, IPointerEnterHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
{
	public delegate void OnDragDelegate();
	public event OnDragDelegate OnSlide;

	private static bool isDragging;

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (isDragging && OnSlide != null)
			OnSlide();
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		isDragging = true;
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		isDragging = false;
	}

	public void OnDrag(PointerEventData eventData) { }

}
