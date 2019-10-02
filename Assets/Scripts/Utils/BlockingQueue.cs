// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using System.Threading;

public class BlockingQueue<T>
{
	private int count = 0;
	private Queue<T> queue = new Queue<T>();

	public void Enqueue(T data)
	{
		lock (queue)
		{
			queue.Enqueue(data);
			count++;
			Monitor.Pulse(queue);
		}
	}

	public T Dequeue()
	{
		lock (queue)
		{
			while (count <= 0) Monitor.Wait(queue);
			count--;
			return queue.Dequeue();
		}
	}
}
