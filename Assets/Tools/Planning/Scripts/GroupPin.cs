// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GroupPin : AggregationPin
{
    [Header("Setup")]
    public float baseOpacity = 50;
    public PlanningGroup group;

    public Typology maxTypology;


	//
	// Public Methods
	//

	public void Init(PlanningGroup group, SummaryPanel summaryPanel)
	{
		base.Init(group.center, summaryPanel);

		this.group = group;
		UpdateLineCount(group.cells.Count);
	}

	public void ChangeGroup(PlanningGroup group)
    {
        this.group = group;
        UpdateLineCount(group.cells.Count);

        coords = group.center;
    }

    public override void UpdatePosition(MapController map)
    {
        base.UpdatePosition(map);

        Vector3 pos = transform.position;

        // Update Lines
        int count = group.cells.Count;
		float t = (Math.Max(globalCount, count) - 5) * 0.03f;
		float alpha = Mathf.SmoothStep(1f, 0.2f, t);

		for (int i = 0; i < count; i++)
        {
			group.cells[i].UpdatePosition(map);

			var line = lines[i];
            line.SetPosition(0, pos);
			line.SetPosition(1, group.cells[i].Position);

			// Set color based on typology and transparency based on number of lines
			Color c = group.cells[i].typology.color;
			c.a = alpha;
            line.material.SetColor("_Color", c);
        }
    }

    // Find the color of the dominant typology in the group
    public Color GetGroupColor()
    {
		Dictionary<Typology, int> maxList = new Dictionary<Typology, int>();
		var list = group.cells;

        foreach (var cell in list)
        {
            if (maxList.ContainsKey(cell.typology))
            {
                ++maxList[cell.typology];
            }
            else
            {
                maxList.Add(cell.typology, 1);
            }
        }

        int max = maxList.Values.Max();
        var maxTypology = maxList.FirstOrDefault(x => x.Value == max).Key;
        return maxTypology.color;
    }
}
