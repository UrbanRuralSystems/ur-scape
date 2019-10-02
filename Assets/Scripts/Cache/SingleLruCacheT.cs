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

public abstract class SingleLruCacheT<T> : SingleLruCache where T : class
{
    public int maxSize = 100;

    private LruCache<T> cache;

    public override int MaxSize { get { return maxSize; } }
    public override int Count { get { return cache.Count; } }


    //
    // Unity Methods
    //

    void Awake()
    {
        cache = new LruCache<T>(maxSize);
    }

    //
    // Public Methods
    //

    public void Add(T obj)
    {
#if SAFETY_CHECK
        if (obj == null)
        {
            Debug.LogError("Trying to add a null object to the cache!");
            return;
        }
#endif
        // old: the object that was just kicked out of the cache
        T old = cache.Add(obj);

        // Check if the cache kicked out an old object
        if (old != null && old != obj)
        {
            // The cache was full and kicked the oldest value out.
            OnPushedOutFromCache(old);
        }
    }

    public bool Has(T obj)
    {
        return cache.Has(obj);
    }

    public void Remove(T obj)
    {
#if SAFETY_CHECK
        // Check if the object exists in the cache
        if (!cache.Has(obj))
        {
            Debug.LogWarning("Couldn't remove object " + obj);
            return;
        }
#endif
        cache.Remove(obj);
    }

    public bool TryRemove(T obj)
    {
        return cache.TryRemove(obj);
    }

	public void Clear()
	{
		foreach (T obj in cache)
		{
			OnPushedOutFromCache(obj);
		}
		cache.Clear();
	}

    protected abstract void OnPushedOutFromCache(T obj);

}
