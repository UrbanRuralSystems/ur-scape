// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public abstract class RequestHandlerBase : MonoBehaviour
{
    public abstract int MaxConcurrent { get; }
    public abstract int PendingCount { get; }
    public abstract int RunningCount { get;}
    public abstract int FinishedCount { get; }
    public abstract int TotalCount { get; }

}
