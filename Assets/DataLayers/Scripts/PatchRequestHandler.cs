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

using UnityEngine;

public class PatchRequest : ResourceRequest
{
    public readonly Patch patch;

    public PatchRequest(Patch patch, RequestCallback callback) : base(patch.Filename, callback)
    {
        this.patch = patch;
    }
}

public class PatchRequestHandler : RequestHandler<PatchRequest>
{
    public PatchCache cache;

    protected override ResourceProvider<PatchRequest> CreateResourceProvider(MonoBehaviour behaviour)
    {
        return new PatchProvider(behaviour, cache);
    }
}

public class PatchProvider : ResourceProvider<PatchRequest>
{
    private MonoBehaviour behaviour;

#if SAFETY_CHECK
    private PatchCache cache;
#endif

    public PatchProvider(MonoBehaviour behaviour, PatchCache cache)
    {
        this.behaviour = behaviour;
#if SAFETY_CHECK
        this.cache = cache;
#endif
    }

    public void Run(PatchRequest request, ProviderCallback<PatchRequest> callback)
    {
#if SAFETY_CHECK
        // Try finding it in the cache
        if (cache.TryRemove(request.patch))
        {
            Debug.LogError("This shouldn't happen: requested patch is already in the cache: " + request.file);
            request.State = RequestState.Succeeded;
            callback(request);
            return;
        }
#endif

        // Get it from the file/url
        behaviour.StartCoroutine(request.patch.LoadData((p) => OnPatchLoaded(request, callback)));
    }

    private void OnPatchLoaded(PatchRequest request, ProviderCallback<PatchRequest> callback)
    {
        if (!request.IsCanceled)
        {
            request.State = RequestState.Succeeded;
        }

        callback(request);
    }
}
