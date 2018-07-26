// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;

public class LruCacheKV<K, V>
{
    private class Node<TK, TV>
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
        this.capacity = capacity;
        map = (capacity > 0) ?
            new Dictionary<K, Node<K, V>>(capacity) :
            new Dictionary<K, Node<K, V>>();
    }

    /// <summary>
    /// If the LRU already has that key, it returns the previous value.
    /// If the LRU is already full, it returns the oldest value.
    /// It returns null otherwise.
    /// </summary>
    public V Add(K key, V value)
    {
        V oldValue = default(V);
        if (map.ContainsKey(key))
        {
            Node<K, V> node = map[key];
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

        return oldValue;
    }

    public bool Has(K key)
    {
        return map.ContainsKey(key);
    }

    public V this[K key]
    {
        get
        {
            Node<K, V> node = map[key];
            List_MoveToHead(node);
            return node.Value;
        }
    }

    public bool TryGet(K key, out V value)
    {
        Node<K, V> node;
        if (map.TryGetValue(key, out node))
        {
            List_MoveToHead(node);
            value = node.Value;
            return true;
        }

        value = default(V);
        return false;
    }

    public void Clear()
    {
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
    }

    public V RemoveLast()
    {
        var value = tail.Value;
        map.Remove(tail.Key);
        List_Remove(tail);
        return value;
    }

    public V Remove(K key)
    {
        Node<K, V> node = map[key];
        V value = node.Value;
        map.Remove(key);
        List_Remove(node);
        return value;
    }

    public bool TryRemove(K key, out V value)
    {
        Node<K, V> node;
        if (map.TryGetValue(key, out node))
        {
            value = node.Value;
            map.Remove(key);
            List_Remove(node);
            return true;
        }
        value = default(V);
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

            // CheckIntegrity();
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

        // CheckIntegrity();
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

        // CheckIntegrity();
    }

    private void CheckIntegrity()
    {
        if (head == null || tail == null || head.Previous != tail || tail.Next != head)
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
        } while (node != head && counter < capacity);

        if (counter != map.Count)
        {
            Debug.LogError("LRU is broken!");
            return;
        }

        if (counter >= capacity)
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
        } while (node != tail && counter < capacity);

        if (counter != map.Count)
        {
            Debug.LogError("LRU is broken!");
            return;
        }

        if (counter >= capacity)
        {
            Debug.LogError("LRU is broken!");
            return;
        }
    }

}
