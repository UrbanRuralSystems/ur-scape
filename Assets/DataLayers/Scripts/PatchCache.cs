// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)


public class PatchCache : SingleLruCacheT<Patch>
{
    protected override void OnPushedOutFromCache(Patch patch)
    {
        patch.UnloadData();
    }
}
