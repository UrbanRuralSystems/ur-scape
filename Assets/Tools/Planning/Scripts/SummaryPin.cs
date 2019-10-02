// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;

public class SummaryPin : AggregationPin
{
    private List<GroupPin> groupPins;


    //
    // Public Methods
    //

	public void Init(Coordinate coords, List<GroupPin> groupPins, SummaryPanel summaryPanel)
	{
		base.Init(coords, summaryPanel);

		this.groupPins = groupPins;
		this.name = "SummaryPin";

		Show(false);
	}

	public override void UpdatePosition(MapController map)
    {
        base.UpdatePosition(map);

        // if only one group left, delete all lines (not keep one)
        int count = groupPins.Count>1? groupPins.Count:0;
        UpdateLineCount(count);

        Vector3 pos = transform.position;
        
		// Update Lines
        for (int i = 0; i < count; i++)
        {
            var line = lines[i];
            line.SetPosition(0, pos);
            line.SetPosition(1, groupPins[i].transform.position);
            
			// Set color based on typology and transparency based on number of lines
			Color c = groupPins[i].GetGroupColor();
			line.material.SetColor("_Color", c);
        }    
    }
}
