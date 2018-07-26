// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

#if UNITY_EDITOR
#define SAFETY_CHECK
#endif

public enum RequestState
{
    Waiting,
    Started,
    Canceled,
    Failed,
    Succeeded
}

public delegate void RequestCallback(ResourceRequest req);

public abstract class ResourceRequest
{
    public readonly string file;
    public readonly RequestCallback callback;

    public string Error;

    private RequestState state;
    public RequestState State
    {
        get { return state; }
        set
        {
            if (state != RequestState.Canceled)
                state = value;
#if SAFETY_CHECK
            else
                UnityEngine.Debug.LogWarning("This should not happen: trying to set the state of a canceled request!");
#endif
        }
    }

    public bool IsCanceled { get { return state == RequestState.Canceled; } }

    public ResourceRequest(string file, RequestCallback callback)
    {
        this.file = file;
        this.callback = callback;
        state = RequestState.Waiting;
    }

    public void Complete()
    {
        callback(this);
    }

    public void Cancel()
    {
        state = RequestState.Canceled;
    }

}
