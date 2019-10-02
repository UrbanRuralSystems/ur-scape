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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LruCacheKV<K, V> : IEnumerable
{
	public class LruCacheEnumerator<TK, TV> : IEnumerator
	{
		private readonly int count;
		private int index;
		private Node<TK, TV> current;

		public LruCacheEnumerator(Node<TK, TV> first, int count)
		{
			this.count = count;
			index = -1;
			current = first.Previous;
		}

		public bool MoveNext()
		{
			current = current.Next;
			return ++index < count;
		}

		public void Reset() { index = -1; }
		object IEnumerator.Current { get { return current.Value; } }
		public TV Current { get { return current.Value; } }
	}

	public class Node<TK, TV>
    {
        public TK Key;
        public TV Value;
        public Node<TK, TV> Next;
        public Node<TK, TV> Previous;
    }

    private readonly Dictionary<K, Node<K, V>> map;
    private readonly int capacity;

    private Node<K, V> head;
    private Node<K, V> tail;

    public V First { get { return head.Value; } }
    public V Last  { get { return tail.Value; } }

    public int Count { get { return map.Count; } }

    public LruCacheKV(int capacity)
    {
		this.capacity = Math.Max(1, capacity);
        map = new Dictionary<K, Node<K, V>>(this.capacity);
    }

	public IEnumerator GetEnumerator()
	{
		return head == null ? EmptyEnumerator.Instance : new LruCacheEnumerator<K, V>(head, map.Count);
	}


	/// <summary>
	/// If the LRU already has that key, it returns the previous value.
	/// If the LRU is already full, it returns the oldest value.
	/// It returns null otherwise.
	/// </summary>
	public V Add(K key, V value)
    {
        V oldValue = default;

        if (map.TryGetValue(key, out Node<K, V> node))
        {
            oldValue = node.Value;

            node.Value = value;
            List_MoveToHead(node);
        }
        else if (map.Count == capacity)
        {
            // Cache reached max capacity: replace oldest element's key/value

            // Remove oldest key from map
            map.Remove(tail.Key);

            // Save oldest value for return
            oldValue = tail.Value;

            // Cycle the list
            head = head.Previous;
            tail = tail.Previous;

            // Reassign head's key/value
            head.Key = key;
            head.Value = value;

            // Add new key/node to the map
            map.Add(key, head);
        }
        else
        {
            // Add new node at the beginning of the list
            List_AddFirst(new Node<K, V> { Key = key, Value = value });

            // Add new key/node to the map
            map.Add(key, head);
        }

#if SAFETY_CHECK
		CheckIntegrity();
#endif
		return oldValue;
    }

    public bool Has(K key)
    {
        return map.ContainsKey(key);
    }

	public void Bump(K key)
	{
		List_MoveToHead(map[key]);

#if SAFETY_CHECK
		CheckIntegrity();
#endif
	}

	public V this[K key]
    {
        get
        {
            Node<K, V> node = map[key];
            List_MoveToHead(node);

#if SAFETY_CHECK
		CheckIntegrity();
#endif
			return node.Value;
        }
    }

    public bool TryGet(K key, out V value)
    {
		if (map.TryGetValue(key, out Node<K, V> node))
		{
			List_MoveToHead(node);
			value = node.Value;

#if SAFETY_CHECK
		CheckIntegrity();
#endif
			return true;
		}

		value = default;
        return false;
    }

    public void Clear()
    {
		if (head == null)
			return;

        Node<K, V> node = head;
        Node<K, V> next;
        do
        {
            next = node.Next;
            node.Previous = node.Next = null;
            node = next;
        } while (node != head);

        head = null;
        tail = null;

		map.Clear();

#if SAFETY_CHECK
		CheckIntegrity();
#endif
	}

	public V RemoveLast()
    {
        var value = tail.Value;
        map.Remove(tail.Key);
        List_Remove(tail);

#if SAFETY_CHECK
		CheckIntegrity();
#endif
		return value;
    }

    public V Remove(K key)
    {
        Node<K, V> node = map[key];
        V value = node.Value;
        map.Remove(key);
        List_Remove(node);

#if SAFETY_CHECK
		CheckIntegrity();
#endif
		return value;
    }

    public bool TryRemove(K key, out V value)
    {
		if (map.TryGetValue(key, out Node<K, V> node))
		{
			value = node.Value;
			map.Remove(key);
			List_Remove(node);

#if SAFETY_CHECK
		CheckIntegrity();
#endif
			return true;
		}
		value = default;
        return false;
    }

    private void List_MoveToHead(Node<K, V> node)
    {
        if (node != head)
        {
            // Remove node from list
            node.Previous.Next = node.Next;
            node.Next.Previous = node.Previous;
            if (node == tail)
                tail = tail.Previous;

            // Insert it a the beginning of the list
            node.Next = head;
            head.Previous = node;
            head = node;
            head.Previous = tail;
            tail.Next = head;
        }
    }

    private void List_AddFirst(Node<K, V> node)
    {
        if (head == null)
        {
            // Add the first node in the list
            tail = head = node;
            head.Next = tail;
            head.Previous = tail;
        }
        else
        {
            node.Next = head;
            head.Previous = node;
            head = node;
            head.Previous = tail;
            tail.Next = head;
        }
    }

    private void List_Remove(Node<K, V> node)
    {
        if (node == head && node == tail)
        {
            head = null;
            tail = null;
        }
        else
        {
            node.Previous.Next = node.Next;
            node.Next.Previous = node.Previous;

            if (node == head)
                head = head.Next;
            else if (node == tail)
                tail = tail.Previous;
        }
        node.Previous = null;
        node.Next = null;
    }

    private void CheckIntegrity()
    {
		if (head == null && tail == null && map.Count == 0)
			return;

		if (head.Previous != tail || tail.Next != head)
		{
            Debug.LogError("LRU is broken!");
            return;
        }

        Node<K, V> node = head;
        Node<K, V> next;
        int counter = 0;
        do
        {
            next = node.Next;
            if (next == null)
            {
                Debug.LogError("LRU is broken!");
                return;
            }
            node = next;
            counter++;
        } while (node != head && counter <= capacity);

        if (counter != map.Count)
        {
            Debug.LogError("LRU is broken!");
            return;
        }

        if (counter > capacity)
        {
            Debug.LogError("LRU is broken!");
            return;
        }

        node = tail;
        counter = 0;
        do
        {
            next = node.Previous;
            if (next == null)
            {
                Debug.LogError("LRU is broken!");
                return;
            }
            node = next;
            counter++;
        } while (node != tail && counter <= capacity);

        if (counter != map.Count)
        {
            Debug.LogError("LRU is broken!");
            return;
        }

        if (counter > capacity)
        {
            Debug.LogError("LRU is broken!");
            return;
        }
    }

}
