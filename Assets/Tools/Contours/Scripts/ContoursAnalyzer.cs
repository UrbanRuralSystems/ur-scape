// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContoursAnalyzer : MonoBehaviour
{
	private static readonly float updateInterval = 0.15f;
	private static readonly WaitForSeconds updateWait = new WaitForSeconds(updateInterval);
	private float lastUpdateTime;
	private Coroutine coroutine;

	public delegate void ProgressCallback(float progress);

	private void OnDestroy()
	{
		Stop();
	}

	public void Stop()
    {
        if (coroutine != null)
        {
            StopCoroutine(coroutine);
            coroutine = null;
        }
    }

    public void Analyze(ContoursMapLayer layer, ProgressCallback callback)
    {
		if (Time.time >= lastUpdateTime)
		{
			// Stop the coroutine (if there is) because analysis would have already started
			Stop();
		}

		lastUpdateTime = Time.time + updateInterval - 0.01f;
		if (coroutine == null)
		{
			coroutine = StartCoroutine(DelayedAnalyze(layer, callback));
		}
    }

	private IEnumerator DelayedAnalyze(ContoursMapLayer layer, ProgressCallback callback)
	{
		do
		{
			yield return updateWait;
		}
		while (Time.time < lastUpdateTime);

		layer.FetchGridValues();
		yield return Analyze(layer.Grid, 1000, callback);
	}

	private IEnumerator Analyze(GridData contours, int countPerFrame, ProgressCallback callback)
    {
        Queue<int> cellQueue = new Queue<int>();
        HashSet<int> cellSet = new HashSet<int>();

        int xmo = contours.countX - 1;
        int ymo = contours.countY - 1;

        int siteIndex = 1;
        int scanIndex = 0;
        int processed = 0;
        int count = contours.values.Length;

        do
        {
            while (scanIndex < count && contours.values[scanIndex] != 1)
            {
                scanIndex++;

				if (++processed > countPerFrame)
				{
					processed = 0;
					callback((float)scanIndex / count);
					yield return null;
				}
			}

			if (scanIndex < count)
            {
                siteIndex++;

                cellQueue.Enqueue(scanIndex);
                cellSet.Add(scanIndex);

                while (cellQueue.Count > 0)
                {
                    int index = cellQueue.Dequeue();
                    cellSet.Remove(index);

                    contours.values[index] = siteIndex;

                    int y = index / contours.countX;
                    int x = index % contours.countX;

                    // Up, up-left, up-right
                    if (x > 0)
                    {
                        int idx = index - 1;
                        AddCell(contours, cellQueue, cellSet, idx);
                        if (y > 0)
                        {
                            AddCell(contours, cellQueue, cellSet, idx - contours.countX);
                        }
                        if (y < ymo)
                        {
                            AddCell(contours, cellQueue, cellSet, idx + contours.countX);
                        }
                    }
                    // Down, down-left, down-right
                    if (x < xmo)
                    {
                        int idx = index + 1;
                        AddCell(contours, cellQueue, cellSet, idx);
                        if (y > 0)
                        {
                            AddCell(contours, cellQueue, cellSet, idx - contours.countX);
                        }
                        if (y < ymo)
                        {
                            AddCell(contours, cellQueue, cellSet, idx + contours.countX);
                        }
                    }
                    // Left, right
                    if (y > 0)
                    {
                        AddCell(contours, cellQueue, cellSet, index - contours.countX);
                    }
                    if (y < ymo)
                    {
                        AddCell(contours, cellQueue, cellSet, index + contours.countX);
                    }

                    if (++processed > countPerFrame)
                    {
                        processed = 0;
                        callback((float)scanIndex / count);
                        yield return null;
                    }
                }
            }
        }
        while (++scanIndex < count);

        coroutine = null;

		if (callback != null)
        {
            callback(1);
        }
    }

    private static void AddCell(GridData contours, Queue<int> cellQueue, HashSet<int> cellSet, int index)
    {
        if (contours.values[index] == 1 && !cellSet.Contains(index))
        {
            cellQueue.Enqueue(index);
            cellSet.Add(index);
        }
    }
}
