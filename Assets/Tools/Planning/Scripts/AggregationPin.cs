// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;

public class AggregationPin : Pin
{
    [Header("Prefabs")]
    public LineRenderer linePrefab;

	[HideInInspector]
	public int globalCount;

	protected SummaryPanel summaryPanel;
    protected List<LineRenderer> lines = new List<LineRenderer>();


    //
    // Unity Methods
    //

    private void OnDestroy()
    {
		if (summaryPanel != null)
		{
			Destroy(summaryPanel.gameObject);
			summaryPanel = null;
		}
	}


    //
    // Public Methods
    //

	public void Init(Coordinate coords, SummaryPanel summaryPanel)
	{
		base.Init(coords);

		this.summaryPanel = summaryPanel;
	}

	public void ChangeCoords(Coordinate coords)
    {
        this.coords = coords;
    }

    public override void Show(bool show)
    {
        base.Show(show);
		summaryPanel.ShowAll(show);
    }

    public override void UpdatePosition(MapController map)
    {
        base.UpdatePosition(map);
		summaryPanel.UpdatePosition(transform.position);
	}

    protected void UpdateLineCount(int count)
    {
        if (count < lines.Count)
        {
            // Remove extra lines
            int i = lines.Count - 1;
            do
            {
                Destroy(lines[i].gameObject);
            } while (i-- > count);
            lines.RemoveRange(count, lines.Count - count);
        }
        else
        {
            // Create new lines
            for (int i = count - lines.Count; i > 0; i--)
            {
                lines.Add(Instantiate(linePrefab, transform, false));
            }
        }
    }
}
