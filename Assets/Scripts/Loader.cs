// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using UnityEngine;

public class Loader : MonoBehaviour
{

    //
    // Public Methods
    //

    public static Loader Create(IEnumerator coroutine, bool autoDestroy)
    {
        Loader loader = new GameObject("Loader").AddComponent<Loader>();
        loader.Run(coroutine, autoDestroy);
        return loader;
    }

    public void Run(IEnumerator coroutine, bool autoDestroy)
    {
        StartCoroutine(Task(coroutine, autoDestroy));
    }


    //
    // Private Methods
    //

    private IEnumerator Task(IEnumerator coroutine, bool autoDestroy)
    {
        yield return coroutine;

        if (autoDestroy)
        {
            Destroy(gameObject);
        }
    }
}
