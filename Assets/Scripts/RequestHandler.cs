// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

#if UNITY_EDITOR
#define SAFETY_CHECK
#endif

using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class RequestHandler<T> : RequestHandlerBase where T : ResourceRequest
{
    public int maxConcurrentRequests = 2;
    public int maxRequestsPerFrame = 2;

    private ResourceProvider<T> provider;

    protected LinkedList<T> pendingRequests = new LinkedList<T>();
    protected HashSet<T> runningRequests = new HashSet<T>();
    protected LinkedList<T> finishedRequests = new LinkedList<T>();
    protected int total = 0;
    private int handledRunningRequests;
    private int handledFinishedRequests;

    public override int MaxConcurrent { get { return maxConcurrentRequests; } }
    public override int PendingCount { get { return pendingRequests.Count;  } }
    public override int RunningCount { get { return Math.Max(runningRequests.Count, handledRunningRequests); } }
    public override int FinishedCount { get { return Math.Max(finishedRequests.Count, handledFinishedRequests); } }
    public override int TotalCount { get { return total; } }

    public LinkedList<T> PendingRequests { get { return pendingRequests; } }


    private void Awake()
    {
        provider = CreateResourceProvider(this);
    }

    protected abstract ResourceProvider<T> CreateResourceProvider(MonoBehaviour behaviour);

    void Update()
    {
        int maxRequests = maxConcurrentRequests - runningRequests.Count;

        handledRunningRequests = 0;
        while (pendingRequests.Count > 0 && handledRunningRequests < maxRequests)
        {
            handledRunningRequests++;

            // Get (and remove) first element in the pending requests
            T request = GetNext(pendingRequests);

            // Add it to the running requests
            StartRequest(request);
        }

        handledFinishedRequests = 0;
        while (finishedRequests.Count > 0 && handledFinishedRequests < maxRequestsPerFrame)
        {
            handledFinishedRequests++;

            // Get (and remove) first element in the finished requests
            T request = GetNext(finishedRequests);
            total--;

            request.Complete();
        }
    }

    public virtual void Add(T request)
    {
        pendingRequests.AddLast(request);
        total++;
    }

    public void Cancel(T request)
    {
        request.Cancel();

        if (pendingRequests.Remove(request))
        {
            total--;
        }
    }

    public void CancelAll()
    {
        CancelAllPending();
        CancelAllRunning();
    }

    public void CancelAllPending()
    {
        foreach (var request in pendingRequests)
        {
            request.Cancel();
        }

        total -= pendingRequests.Count;

        pendingRequests.Clear();
    }

    public void CancelAllRunning()
    {
        foreach (var request in runningRequests)
        {
            request.Cancel();
        }

        runningRequests.Clear();
    }

    public void RemovePending(LinkedListNode<T> node)
    {
        pendingRequests.Remove(node);
        total--;
    }

    private T GetNext(LinkedList<T> list)
    {
        // Get (and remove) first element in the pending requests
        var request = list.First.Value;
        list.RemoveFirst();
        return request;
    }

    private void StartRequest(T request)
    {
        request.State = RequestState.Started;
        runningRequests.Add(request);
        provider.Run(request, RequestFinished);
    }

    private void RequestFinished(T request)
    {
        runningRequests.Remove(request);
        finishedRequests.AddLast(request);
    }
}
