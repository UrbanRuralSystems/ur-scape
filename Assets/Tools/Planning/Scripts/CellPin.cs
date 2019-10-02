// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public class CellPin : Pin
{
    private int index;
    public int Index { get { return index; } }

    public void Init(Coordinate coords, int index)
    {
        base.Init(coords);

        this.index = index;
        name = "Pin_" + index;
    }

    public override void UpdatePosition(MapController map)
    {
        base.UpdatePosition(map);

        var position = transform.localPosition;
        position.x = position.y = 0;
        position.z = -position.z;

        // Update line length
        var line = GetComponent<LineRenderer>();
        line.SetPosition(1, position);
    }

}
