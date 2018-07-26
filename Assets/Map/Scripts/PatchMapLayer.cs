// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class PatchMapLayer : AreaMapLayer
{
    protected PatchData patchData;
    public PatchData PatchData { get { return patchData; } }


    //
    // Unity Methods
    //

    protected virtual void OnDestroy()
    {
        if (patchData != null)
        {
            patchData.OnBoundsChange -= OnPatchBoundsChange;
        }
    }


    //
    // Inheritance Methods
    //

    public virtual void Init(MapController map, PatchData patchData)
    {
        base.Init(map, patchData.north, patchData.east, patchData.south, patchData.west);

        // Deregister old events
        if (this.patchData != null)
        {
            this.patchData.OnBoundsChange -= OnPatchBoundsChange;
        }

        this.patchData = patchData;

        // Register events
        patchData.OnBoundsChange += OnPatchBoundsChange;
    }


    //
    // Private/Protected Methods
    //

    protected virtual void OnPatchBoundsChange(PatchData patchData)
    {
        UpdateAreaCenterAndSize(patchData.north, patchData.east, patchData.south, patchData.west);
    }

}
