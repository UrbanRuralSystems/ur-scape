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

using UnityEngine;

public abstract class DoubleLruCacheT<T> : DoubleLruCache where T : class
{
    public int maxSize = 100;

    private LruCache<T> usedCache;
    private LruCache<T> unusedCache;

    public override int MaxSize { get { return maxSize; } }
    public override int UsedCount { get { return usedCache.Count; } }
    public override int UnusedCount { get { return unusedCache.Count; } }


    //
    // Unity Methods
    //

    void Awake()
    {
        usedCache = new LruCache<T>(maxSize);
        unusedCache = new LruCache<T>(maxSize);
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
        T old = usedCache.Add(obj);

        // Check if the cache kicked out an old object
        if (old == null)
        {
            // Cache didn't have this key and has therefore added a new entry
            if (usedCache.Count + unusedCache.Count > maxSize)
            {
                // The total size has exceeded maxSize: remove oldest entry from unusedCache
                OnRemovedFromCache(unusedCache.RemoveLast());
            }
        }
        else if (old != obj)
        {
            // Either the cache already had this id and 'old' is the previous value,
            // or the cache was full and kicked the oldest value out. Either way, remove it.
            OnRemovedFromCache(old);
        }
    }

    public void Bump(T obj)
    {
        // Check if the key exists in the used region first, then in the unused
        if (usedCache.Has(obj))
        {
            usedCache.Bump(obj);
        }
        else if (unusedCache.Has(obj))
        {
            // Move the entry from unused to used region
            usedCache.Add(unusedCache.Remove(obj));
        }
    }

    public void NotUsed(T obj)
    {
        if (usedCache.Has(obj))
        {
            unusedCache.Add(usedCache.Remove(obj));
        }
    }

    public void Remove(T obj)
    {
        // Check if the object exists in the used region first, then in the unused
        if (usedCache.TryRemove(obj))
        {
            OnRemovedFromCache(obj);
        }
        else if (unusedCache.TryRemove(obj))
        {
            OnRemovedFromCache(obj);
        }
#if SAFETY_CHECK
        else
        {
            Debug.LogWarning("Couldn't remove object " + obj);
        }
#endif
    }

    protected abstract void OnRemovedFromCache(T obj);

}
