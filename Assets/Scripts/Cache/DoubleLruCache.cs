// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public abstract class DoubleLruCache : MonoBehaviour
{
    public abstract int MaxSize { get; }
    public abstract int UsedCount { get; }
    public abstract int UnusedCount { get; }
}