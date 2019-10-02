// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          David Neudecker (neudecker@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;

public class Flagger : MonoBehaviour
{
	[Header("Prefabs")]
	public RectTransform flagContainerPrefab;
	public SummaryPin summaryPinPrefab;
	public GroupPin groupPinPrefab;
	public CellPin pinPrefab;
	public SummaryPanel summaryPanelPrefab;
	public SummaryPanel summaryPanelSelectedPrefab;

	[Header("Settings")]
	public int maxVisibleGroups = 5;


	// planning classes instances
	private PlanningSummary planningSummary = new PlanningSummary();
	private List<PlanningGroup> planningGroups = new List<PlanningGroup>();

	// Prefab instances
	private Transform pinContainer;
	private RectTransform flagContainer;
	private RectTransform summaryflagContainer;
	private SummaryPin summaryPin;
	private PlanningCell selectedCell;
	private int selectedGroupIndex = -1;
	private SummaryPanel summaryPanelFlag;
	private List<SummaryPanel> groupSummaries = new List<SummaryPanel>();
	private readonly List<GroupPin> groupPins = new List<GroupPin>();
	private List<string> activeTypologies = new List<string>();

	// Component references
	private MapController map;
	private PlanningOutput planningOutput;

	private static bool showFlags = true;
	private GridData grid;

	//
	// Unity Methods
	//

	private void OnDestroy()
	{
		// Detach from events
		if (map != null)
		{
			map.OnMapUpdate -= OnMapUpdate;
		}

		// Destroy containers
		if (pinContainer != null)
		{
			Destroy(pinContainer.gameObject);
			pinContainer = null;
		}
		if (flagContainer != null)
		{
			Destroy(flagContainer.gameObject);
			flagContainer = null;
		}
		if (summaryflagContainer != null)
		{
			Destroy(summaryflagContainer.gameObject);
			summaryflagContainer = null;
		}

		showFlags = true;
	}


	//
	// Events
	//

	private void OnMapUpdate()
	{
		UpdatePinPositions();
	}


	//
	// Public Methods
	//

	public void Init(GridData grid, PlanningOutput planningOutput)
	{
		this.grid = grid;
		this.planningOutput = planningOutput;

		// Find Components
		map = ComponentManager.Instance.Get<MapController>();
		var canvas = GameObject.FindWithTag("Canvas").GetComponent<Canvas>();

		// Create pin container (inside map)
		pinContainer = new GameObject("PinContainer").transform;
		pinContainer.SetParent(map.transform, false);

		// Create flag container (inside UI canvas)
		flagContainer = Instantiate(flagContainerPrefab, canvas.transform, false);
		flagContainer.name = flagContainerPrefab.name;
		flagContainer.transform.SetSiblingIndex(0);

		// Create summary flag container (inside UI canvas)
		summaryflagContainer = Instantiate(flagContainerPrefab, canvas.transform, false);
		summaryflagContainer.name = "Summary" + flagContainerPrefab.name;
		summaryflagContainer.transform.SetSiblingIndex(0);

		// Create summary pin/flag
		summaryPanelFlag = Instantiate(summaryPanelPrefab, summaryflagContainer, false);
		summaryPanelFlag.Init("SummaryPanelFlag", grid, 3);
		summaryPanelFlag.SetPlanningOutput(planningOutput);
		summaryPanelFlag.gameObject.SetActive(showFlags);
		summaryPin = Instantiate(summaryPinPrefab, pinContainer, true);
		summaryPin.Init(Coordinate.Zero, groupPins, summaryPanelFlag);

		// Attach to events
		map.OnMapUpdate += OnMapUpdate;
	}

	public void UpdateActiveTypology(List<string> activeTypologies)
	{
		this.activeTypologies = activeTypologies;

		UpdateFlags();
	}

	public void SetSelected(PlanningCell cell)
	{
		if (selectedCell != cell)
		{
			// Setup new pin
			if (cell != null)
			{
				if (planningGroups[cell.group].cells.Count <= 1)
					Deselect();

				// Show flag
				UpdateSelectedGroupSummary(cell.group);
			}
			else
			{
				Deselect();
			}

			selectedCell = cell;
		}
	}

	public void Deselect()
	{
		// Exit previously selected cell
		if (selectedCell != null)
		{
			// Hide cell flag
			selectedCell = null;

			// Hide group flag
			if (planningGroups.Count > maxVisibleGroups && selectedGroupIndex >= 0)
			{
				groupPins[selectedGroupIndex].Show(false);
				selectedGroupIndex = -1;
			}
		}
	}

	public void UpdateData(PlanningSummary summary, List<PlanningGroup> groups)
	{
		planningSummary = summary;
		planningGroups = groups;

		selectedGroupIndex = -1;

		UpdateGroupsAndSummaryPins();
		UpdateFlags();

		UpdatePinPositions();
	}

	public void RequestShowFlags(bool show)
	{
		showFlags = show;
		ShowFlags();
	}

	//
	// Private Methods
	//

	private void UpdatePinPositions()
	{
		Pin.HeadSize.x = Pin.HeadSize.y = Pin.HeadSize.z =
			(map.MetersToUnits * 0.5f + Pin.InitialHeadSize) * 0.5f;

		// Group pins
		foreach (var pin in groupPins)
		{
			pin.UpdatePosition(map);
		}

		if (planningGroups.Count > 1)
		{
			// Summary pin
			summaryPin.UpdatePosition(map);
		}
	}

	private GroupPin AddGroupPin(PlanningGroup group)
	{
		// Create summary panel
		SummaryPanel groupSummary = Instantiate(summaryPanelPrefab, flagContainer, false);
		groupSummary.SetPlanningOutput(planningOutput);
		groupSummaries.Add(groupSummary);
		groupSummary.Init(summaryPanelPrefab.name, grid, 2);
		groupSummary.gameObject.SetActive(showFlags);

		// Create pin
		var groupPin = Instantiate(groupPinPrefab, pinContainer, true);
		groupPin.name = "Group_" + group.id;
		groupPin.Init(group, groupSummary);
		groupPin.globalCount = planningGroups.Count;
		groupPin.UpdatePosition(map);
		groupPins.Add(groupPin);

		return groupPin;
	}

	private void RemoveRemainingGroups()
	{
		for (int i = planningGroups.Count; i < groupPins.Count; i++)
		{
			Destroy(groupPins[i].gameObject);
			Destroy(groupSummaries[i].gameObject);
		}

		int count = groupPins.Count - planningGroups.Count;
		groupPins.RemoveRange(planningGroups.Count, count);
		groupSummaries.RemoveRange(planningGroups.Count, count);
	}

	private void UpdateGroupsAndSummaryPins()
	{
		Coordinate center = new Coordinate();
		int oldGroupsCount = groupPins.Count;
		int newGroupsCount = planningGroups.Count;

		bool show = newGroupsCount <= maxVisibleGroups;

		// Update groups
		int count = Mathf.Min(newGroupsCount, oldGroupsCount);
		int i = 0;
		for (; i < count; i++)
		{
			var group = planningGroups[i];
			var pin = groupPins[i];

			pin.ChangeGroup(group);
			pin.globalCount = newGroupsCount; ;
			pin.Show(show);

			center.Longitude += group.center.Longitude;
			center.Latitude += group.center.Latitude;
		}

		if (oldGroupsCount > newGroupsCount)
		{
			RemoveRemainingGroups();
		}
		else
		{
			// Add remaning groups
			for (; i < newGroupsCount; i++)
			{
				var pin = AddGroupPin(planningGroups[i]);
				pin.Show(show);
				center.Longitude += pin.Coords.Longitude;
				center.Latitude += pin.Coords.Latitude;
			}
		}

		if (groupPins.Count > 0)
		{
			double invCount = 1.0 / groupPins.Count;
			center.Longitude *= invCount;
			center.Latitude *= invCount;
		}

		// Update summary pin
		if (groupPins.Count > 1)
		{
			summaryPin.ChangeCoords(center);
			summaryPin.UpdatePosition(map);

			if (oldGroupsCount <= 1)
				summaryPin.Show(true);
		}
		// if second last ground delete, update as well to delete old summary lines
		else if (groupPins.Count == 1)
		{
			summaryPin.UpdatePosition(map);
			summaryPin.Show(false);
		}

		else if (oldGroupsCount > 1)
		{
			summaryPin.Show(false);
		}
	}

	private void UpdateFlags()
	{
		if (planningGroups.Count > 1)
		{
			// Summary flag
			summaryPanelFlag.gameObject.SetActive(showFlags);
			summaryPanelFlag.UpdateGroupsSummaryPanel(planningGroups);
			summaryPanelFlag.gridNoData = planningSummary.gridNoData;
		}

		if (planningGroups.Count <= maxVisibleGroups)
		{
			// Group summaries
			for (int i = 0; i < groupSummaries.Count; ++i)
			{
				UpdateGroupSummary(groupSummaries[i], planningGroups[i]);
			}
		}
	}

	private void UpdateGroupSummary(SummaryPanel groupSummary, PlanningGroup group)
	{
		groupSummary.gameObject.SetActive(showFlags);
		groupSummary.UpdateGroupSummaryPanel(group);
		groupSummary.gridNoData = group.gridNoData;
	}

	private void UpdateSelectedGroupSummary(int group)
	{
		if (selectedGroupIndex >= 0)
			groupPins[selectedGroupIndex].Show(false);

		// Update it before showing it
		UpdateGroupSummary(groupSummaries[group], planningGroups[group]);

		groupPins[group].Show(showFlags);
		selectedGroupIndex = group;
	}

	private void ShowFlags()
	{
		if (planningGroups.Count > 1)
		{
			// Summary flag
			summaryPanelFlag.ShowAll(showFlags);
		}

		if (planningGroups.Count <= maxVisibleGroups)
		{
			// Group summaries
			for (int i = 0; i < groupSummaries.Count; ++i)
			{
				groupSummaries[i].ShowAll(showFlags);
			}
		}
	}
}
