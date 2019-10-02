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

public class LruCache<T> : IEnumerable 
{
	public class LruCacheEnumerator<TE> : IEnumerator
	{
		private readonly int count;
		private int index;
		private Node<TE> current;

		public LruCacheEnumerator(Node<TE> first, int count)
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
		public TE Current { get { return current.Value; } }
	}

	public class Node<NT>
    {
        public NT Value;
        public Node<NT> Next;
        public Node<NT> Previous;
    }

    private readonly Dictionary<T, Node<T>> map;
    private readonly int capacity;

    private Node<T> head;
    private Node<T> tail;

    public T First { get { return head.Value; } }
    public T Last { get { return tail.Value; } }

    public int Count { get { return map.Count; } }

    public LruCache(int capacity)
    {
        this.capacity = Math.Max(1, capacity);
		map = new Dictionary<T, Node<T>>(this.capacity);
    }

	public IEnumerator GetEnumerator()
	{
		return head == null? EmptyEnumerator.Instance : new LruCacheEnumerator<T>(head, map.Count);
	}


	/// <summary>
	/// If the LRU already has that key, it returns the previous value.
	/// If the LRU is already full, it returns the oldest value.
	/// It returns null otherwise.
	/// </summary>
	public T Add(T value)
    {
        T oldValue = default;

        if (map.TryGetValue(value, out Node<T> node))
        {
            oldValue = value;

			List_MoveToHead(node);
        }
        else if (map.Count == capacity)
        {
            // Cache reached max capacity: replace oldest element

            // Remove oldest key from map
            map.Remove(tail.Value);

            // Save oldest value for return
            oldValue = tail.Value;

            // Cycle the list
            head = head.Previous;
            tail = tail.Previous;

            // Reassign head's value
            head.Value = value;

            // Add new key/node to the map
            map.Add(value, head);
        }
        else
        {
            // Add new node at the beginning of the list
            List_AddFirst(new Node<T> { Value = value });

            // Add new key/node to the map
            map.Add(value, head);
        }

#if SAFETY_CHECK
		CheckIntegrity();
#endif
		return oldValue;
    }

    public bool Has(T value)
    {
        return map.ContainsKey(value);
    }

    public void Bump(T value)
    {
        List_MoveToHead(map[value]);

#if SAFETY_CHECK
		CheckIntegrity();
#endif
	}

	public void Clear()
    {
		if (head == null)
			return;

        Node<T> node = head;
        Node<T> next;
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

	public T RemoveLast()
    {
        var value = tail.Value;
        map.Remove(value);
        List_Remove(tail);

#if SAFETY_CHECK
		CheckIntegrity();
#endif

		return value;
    }

    public T Remove(T key)
    {
        List_Remove(map[key]);
        map.Remove(key);

#if SAFETY_CHECK
		CheckIntegrity();
#endif

		return key;
    }

    public bool TryRemove(T key)
    {
        if (map.TryGetValue(key, out Node<T> node))
        {
            List_Remove(node);
            map.Remove(key);

#if SAFETY_CHECK
			CheckIntegrity();
#endif

			return true;
        }
        return false;
    }

    private void List_MoveToHead(Node<T> node)
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

    private void List_AddFirst(Node<T> node)
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

    private void List_Remove(Node<T> node)
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

        Node<T> node = head;
        Node<T> next;
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
