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

public abstract class SingleLruCacheKV<K, V> : SingleLruCache where V : class
{
    public int maxSize = 100;

    private LruCacheKV<K, V> cache;

    public override int MaxSize { get { return maxSize; } }
    public override int Count { get { return cache.Count; } }


    //
    // Unity Methods
    //

    void Awake()
    {
        cache = new LruCacheKV<K, V>(maxSize);
    }

    //
    // Public Methods
    //

    public void Add(K key, V value)
    {
#if SAFETY_CHECK
        if (value == null)
        {
            Debug.LogError("Key " + key + " is trying to add a null to the cache!");
            return;
        }
#endif
        // old: the object that was just kicked out of the cache
        V old = cache.Add(key, value);

        // Check if the cache kicked out an old object
        if (old != null && old != value)
        {
            // The cache was full and kicked the oldest value out.
            OnPushedOutFromCache(old);
        }

#if SAFETY_CHECK
        if (old == value)
        {
            Debug.LogWarning("This shouldn't happen: key " + key + " was already in the cache!");
            return;
        }
#endif
    }

    public bool Has(K key)
    {
        return cache.Has(key);
    }

    public V Get(K key)
    {
#if SAFETY_CHECK
        // Check if the object exists in the cache
        if (!cache.Has(key))
        {
            Debug.LogWarning("Couldn't find object with key: " + key);
            return null;
        }
#endif
        return cache[key];
    }

    public V Remove(K key)
    {
#if SAFETY_CHECK
        // Check if the object exists in the cache
        if (!cache.Has(key))
        {
            Debug.LogWarning("Couldn't remove object with key: " + key);
            return null;
        }
#endif
        return cache.Remove(key);
    }

    public bool TryRemove(K key, out V value)
    {
        // Check if the object exists in the cache
        return cache.TryRemove(key, out value);
    }

    protected abstract void OnPushedOutFromCache(V value);

}
