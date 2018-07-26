// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Connection between ContoursMapLayer and OutputGroup
public class ContoursGroup
{
    public OutputGroup group;
    public ContoursMapLayer layer;
    public bool updatable; // e.g. snapshots are not updatable
    public bool selectable; // for layer analysis: when user is selecting contour areas 
}

public class ContoursOutput : MonoBehaviour
{
    [Header("Prefabs")]
    public OutputGroup outputGroubPrefab;

    [Header("UI References")]
    public Transform outputContainer;
    public GameObject outputMessage;

    // Component References
    private Dictionary<string , ContoursGroup> contoursGroups = new Dictionary<string, ContoursGroup>();

    public void AddGroup(string name, ContoursMapLayer layer, bool updatable, bool selectable)
    {
		// Hide "no data layers" message
		outputMessage.SetActive(false);

		// Check if group is already in output
		ContoursGroup contoursGroup;
        if (!contoursGroups.TryGetValue(name, out contoursGroup))
        {
            OutputGroup outputGroup = Instantiate(outputGroubPrefab, outputContainer, false);
            outputGroup.Init(name);
            contoursGroup = new ContoursGroup();
            contoursGroup.group = outputGroup;
            contoursGroup.layer = layer;
            contoursGroup.updatable = updatable;
            contoursGroup.selectable = selectable;
            contoursGroups.Add(name, contoursGroup);
        }
        UpdateGroup(name);
    }

    public void UpdateGroup(string name)
    {
        ContoursGroup contoursGroup;
        if (contoursGroups.TryGetValue(name, out contoursGroup))
        {
            UpdateGridValues(contoursGroup);         
        }

        GuiUtils.RebuildLayout(outputContainer);
    }

    public void Grid_OnFilterChange()
    {
        UpdateFilter();
    }

    private Coroutine delayedCoroutine;
    private float lastUpdateTime;
    private void UpdateFilter()
    {
        lastUpdateTime = Time.time + updateInterval;
        if (delayedCoroutine == null)
        {
            delayedCoroutine = StartCoroutine(DelayedFilterUpdate());
        }
    }

    private static readonly float updateInterval = 0.25f;
    private static readonly WaitForSeconds updateWait = new WaitForSeconds(updateInterval);
    private IEnumerator DelayedFilterUpdate()
    {
        do
        {
            yield return updateWait;
            UpdateAllContourGroups();
        }
        while (Time.time < lastUpdateTime);
        delayedCoroutine = null;
    }

    private void UpdateAllContourGroups()
    {
        foreach (var pair in contoursGroups)
        {
            if (pair.Value.updatable)
            {
                if (pair.Value.layer.grids.Count > 0)
                    pair.Value.layer.FetchGridValues();

                UpdateGridValues(pair.Value);
            }          
        }
    }

    private void UpdateGridValues(ContoursGroup contoursGroup)
    {
        // Delete all items to create actual new items based on grid
        contoursGroup.group.DeleteAllItems();

		int cellCount = contoursGroup.layer.GetContoursCount(contoursGroup.selectable);

		// Always create this item with area
		float hectars = cellCount * contoursGroup.layer.GetContoursSquareMeters() * 0.0001f; // convert m2 to hectars
		contoursGroup.group.UpdateItem("Area in Ha", hectars);
       
        // Create average items for each layer
        float cellDivider = 1f / cellCount;
        foreach (var grid in contoursGroup.layer.grids)
        {
            if (!grid.IsCategorized) // Add average only if is not categorized
            {
                float gridValue = contoursGroup.layer.GetGridContouredData(grid, contoursGroup.selectable);
                contoursGroup.group.UpdateItem(grid.patch.dataLayer.name + " - Avg " + grid.units, gridValue * cellDivider);
            }
        }  
    }

    public void RemoveGroup(string name)
    {
		ContoursGroup removeGroup;
		if (!contoursGroups.TryGetValue(name, out removeGroup))
		{
			Debug.LogWarning("Snapshot " + name + " could not be removed: not found");
			return;
		}

		Destroy(removeGroup.group.gameObject);
		contoursGroups.Remove(name);
	}

    public void RenameGroup(string oldName, string newName)
    {
        ContoursGroup renameGroup;
		if (!contoursGroups.TryGetValue(oldName, out renameGroup))
		{
			Debug.LogWarning("Snapshot " + oldName + " could not be renamed: not found");
			return;
		}

		renameGroup.group.SetGroupName(newName);
        contoursGroups.Add(newName, renameGroup);
        contoursGroups.Remove(oldName);
    }
}
