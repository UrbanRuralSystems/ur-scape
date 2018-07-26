// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

public delegate void ProviderCallback<T>(T req) where T : ResourceRequest;

public interface ResourceProvider<T> where T : ResourceRequest
{
    void Run(T request, ProviderCallback<T> callback);
}
