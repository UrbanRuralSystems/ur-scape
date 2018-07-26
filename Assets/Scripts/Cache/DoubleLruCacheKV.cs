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

public abstract class DoubleLruCacheKV<K, V> : DoubleLruCache where V : class
{
    public int maxSize = 100;

    private LruCacheKV<K, V> usedCache;
    private LruCacheKV<K, V> unusedCache;

    public override int MaxSize { get { return maxSize; } }
    public override int UsedCount { get { return usedCache.Count; } }
    public override int UnusedCount { get { return unusedCache.Count; } }


    //
    // Unity Methods
    //

    void Awake()
    {
        usedCache = new LruCacheKV<K, V>(maxSize);
        unusedCache = new LruCacheKV<K, V>(maxSize);
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
        // old: the value that was just kicked out of the cache
        V old = usedCache.Add(key, value);

        // Check if the cache kicked out an old value
        if (old == null)
        {
            // Cache didn't have this key and has therefore added a new entry
            if (usedCache.Count + unusedCache.Count > maxSize)
            {
                // The total size has exceeded maxSize: remove oldest entry from unusedCache
                OnRemovedFromCache(unusedCache.RemoveLast());
            }
        }
        else if (old != value)
        {
            // Either the cache already had this id and 'old' is the previous value,
            // or the cache was full and kicked the oldest value out. Either way, remove it.
            OnRemovedFromCache(old);
        }
    }

    public V Get(K key)
    {
        // Check if the key exists in the used region first, then in the unused
        if (usedCache.Has(key))
        {
            return usedCache[key];
        }
        else if (unusedCache.Has(key))
        {
            // Move the entry from unused to used region
            V value = unusedCache.Remove(key);
            usedCache.Add(key, value);
            return value;
        }

        return null;
    }

    public void NotUsed(K key)
    {
        if (usedCache.Has(key))
        {
            unusedCache.Add(key, usedCache.Remove(key));
        }
    }

    public void Remove(K key)
    {
        // Check if the key exists in the used region first, then in the unused
        if (usedCache.Has(key))
        {
            OnRemovedFromCache(usedCache.Remove(key));
        }
        else if (unusedCache.Has(key))
        {
            OnRemovedFromCache(unusedCache.Remove(key));
        }
#if SAFETY_CHECK
        else
        {
            Debug.LogWarning("Couldn't remove entry with key " + key);
        }
#endif
    }

    protected abstract void OnRemovedFromCache(V value);

}
