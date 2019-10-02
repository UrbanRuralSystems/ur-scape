// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.Collections.Generic;
using UnityEngine;

public class SummaryPanel : MonoBehaviour
{
	[Header("Prefabs")]
	public SummaryLayerInfo summaryLayerInfoPrefab;
	public DistributionBar distribBarPrefab;

	[Header("UI References")]
	public RectTransform summaryPanelContainer;
	public RectTransform layersContainer;
	public RectTransform distributionContainer;
	public Transform pin;

	private readonly Dictionary<Typology, SummaryLayerInfo> summaryLayerInfos = new Dictionary<Typology, SummaryLayerInfo>();
	private readonly Dictionary<Typology, DistributionBar> distributionBars = new Dictionary<Typology, DistributionBar>();
	private readonly Dictionary<Typology, int> typologyCounts = new Dictionary<Typology, int>();
	private readonly Dictionary<Typology, double> typologyAreas = new Dictionary<Typology, double>();
	public Dictionary<string, bool> gridNoData = new Dictionary<string, bool>();

	private static float distributionContainerWidth;
	private int totalTypologyCount = 0;
	private GridData grid;
	private PlanningOutput planningOutput;
	private Canvas canvas;


	//
	// Unity Methods
	//

	private void Start()
    {
		distributionContainerWidth = distributionContainer.rect.width;
	}

	//
	// Public Methods
	//

	public void Init(string name, GridData grid, int size)
	{
		this.name = name;
		this.grid = grid;
		pin.transform.localScale = new Vector3(size, size, size);
		canvas = GameObject.FindWithTag("Canvas").GetComponent<Canvas>();
	}

	public void SetPlanningOutput(PlanningOutput planningOutput)
	{
		this.planningOutput = planningOutput;
	}

	public void UpdateGroupSummaryPanel(PlanningGroup group)
	{
		ClearTypologyCounts();
		ClearSummaryLayerInfos();
		ClearDistributionBars();

		AddOrUpdateGroupTypologyCounts(group);
		AddOrUpdateGroupSmryLayerInfos(group);
		AddOrUpdateGroupDistribBars(group);
	}

	public void UpdateGroupsSummaryPanel(List<PlanningGroup> groups)
	{
		ClearTypologyCounts();
		ClearSummaryLayerInfos();
		ClearDistributionBars();

		// Sum all the typologies from all group
		// Before adding or updating distribution bars
		foreach (var group in groups)
		{
			AddOrUpdateGroupTypologyCounts(group);
			AddOrUpdateGroupSmryLayerInfos(group);
		}

		foreach (var group in groups)
		{
			AddOrUpdateGroupDistribBars(group);
		}
	}

	public void UpdatePosition(Vector3 worldPosition)
	{
		transform.localPosition = Camera.main.WorldToScreenPoint(worldPosition) / canvas.scaleFactor;
	}

	public void ShowPanel(bool show)
	{
		summaryPanelContainer.gameObject.SetActive(show);
	}

	public void ShowAll(bool show)
	{
		gameObject.SetActive(show);
		summaryPanelContainer.gameObject.SetActive(show);
	}

	//
	// Private Methods
	//

	private void ClearTypologyCounts()
	{
		totalTypologyCount = 0;
		typologyCounts.Clear();
	}

	private void AddOrUpdateGroupTypologyCounts(PlanningGroup group)
	{
		List<PlanningCell> cells = group.cells;
		foreach (PlanningCell cell in cells)
		{
			if (typologyCounts.ContainsKey(cell.typology))
			{
				++typologyCounts[cell.typology];
			}
			else
			{
				typologyCounts.Add(cell.typology, 1);
			}
			++totalTypologyCount;
		}
	}

	private SummaryLayerInfo NewSummaryLayerInfo(Color color, string name, double value)
	{
		SummaryLayerInfo smryLayerInfo = Instantiate(summaryLayerInfoPrefab, layersContainer);
		smryLayerInfo.Init(planningOutput);
		smryLayerInfo.SetSummaryLayerInfo(color, name, value);

		return smryLayerInfo;
	}

	private void UpdateSummaryLayerInfo(Typology typology, double value)
	{
		// Update summary layer info only if it exists
		if (summaryLayerInfos.TryGetValue(typology, out SummaryLayerInfo smryLayerInfo))
			smryLayerInfo.SetValue(value);
	}

	private void ClearSummaryLayerInfos()
	{
		for (int i = 0; i < layersContainer.transform.childCount; ++i)
		{
			Destroy(layersContainer.GetChild(i).gameObject);
		}
		summaryLayerInfos.Clear();
		typologyAreas.Clear();
	}

	private void AddOrUpdateGroupSmryLayerInfos(PlanningGroup group)
	{
		List<PlanningCell> cells = group.cells;
		foreach (PlanningCell cell in cells)
		{
			Typology typology = cell.typology;

			// Area occupied by each Typology in the group of cells
			if (typologyAreas.ContainsKey(typology))
			{
				typologyAreas[typology] += cell.areaSqM;
			}
			else
			{
				typologyAreas.Add(typology, cell.areaSqM);
			}
			group.groupPaintedArea += cell.areaSqM;

			// SummaryLayerInfo for each Typology in the group of cells
			if (summaryLayerInfos.ContainsKey(typology))
			{
				UpdateSummaryLayerInfo(typology, typologyAreas[typology]);
			}
			else
			{
				SummaryLayerInfo smryLayerInfo = NewSummaryLayerInfo(typology.color, typology.name, typologyAreas[typology]);
				summaryLayerInfos.Add(typology, smryLayerInfo);
			}
		}
	}

	private DistributionBar NewDistribBar(Color color, float percentageVal)
	{
		DistributionBar dBar = Instantiate(distribBarPrefab, distributionContainer);
		dBar.SetColor(color);
		// Set bar's width depending on percentage
		dBar.SetPercentageVal(percentageVal);
		float newWidth = percentageVal * distributionContainer.rect.width;
		dBar.SetDistribBarWidth(newWidth);

		return dBar;
	}

	private void UpdateDistributionBar(Typology typology)
	{
		if (distributionBars.TryGetValue(typology, out DistributionBar dBar))
		{
			float percentageVal = typologyCounts[typology] / (float)totalTypologyCount;
			dBar.SetPercentageVal(percentageVal);
			float newWidth = percentageVal * distributionContainerWidth;
			dBar.SetDistribBarWidth(newWidth);
		}
	}

	private void ClearDistributionBars()
	{
		for (int i = 0; i < distributionContainer.transform.childCount; ++i)
		{
			Destroy(distributionContainer.GetChild(i).gameObject);
		}
		distributionBars.Clear();
	}

	private void AddOrUpdateGroupDistribBars(PlanningGroup group)
	{
		List<PlanningCell> cells = group.cells;
		foreach (PlanningCell cell in cells)
		{
			Typology typology = cell.typology;
			if (distributionBars.ContainsKey(typology))
			{
				UpdateDistributionBar(typology);
			}
			else
			{
				DistributionBar dBar = NewDistribBar(typology.color, typologyCounts[typology] / (float)totalTypologyCount);
				distributionBars.Add(typology, dBar);
			}

			distributionContainer.gameObject.SetActive((distributionBars.Count > 1) ? true : false);
		}
	}
}
