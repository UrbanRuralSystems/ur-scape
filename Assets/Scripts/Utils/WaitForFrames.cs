// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using UnityEngine;

public static class WaitFor
{
    public const int InitialFrames = 5;

    public static IEnumerator Frames(int frameCount)
    {
        while (frameCount > 0)
        {
            frameCount--;
            yield return null;
        }
    }
}

public class WaitForFrames : CustomYieldInstruction
{
    private readonly int frameCount;
    private int elapsed = 0;

    public WaitForFrames(int frameCount)
    {
        this.frameCount = frameCount;
    }

    public WaitForFrames Wait()
    {
        elapsed = 0;
        return this;
    }

    public override bool keepWaiting
    {
        get
        {
            return elapsed++ < frameCount;
        }
    }
}
