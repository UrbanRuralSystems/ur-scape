// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

//#define PROFILE_TASKS

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class TaskInfo<T, R>
    where T : TaskInfo<T, R>
    where R : TaskResult<T, R>
{
    public readonly Func<T, R> task;
    public TaskInfo(Func<T, R> task) { this.task = task; }
}

public class TaskResult<T, R>
    where T : TaskInfo<T, R>
    where R : TaskResult<T, R>
{
    public readonly T taskInfo;
    public TaskResult(T taskInfo) => this.taskInfo = taskInfo;
}

public class NoTaskResult<T> : TaskResult<T, NoTaskResult<T>>
    where T : TaskInfo<T, NoTaskResult<T>>
{
    public NoTaskResult(T taskInfo) : base(taskInfo) { }
};

public class TaskScheduler<T, R>
    where T : TaskInfo<T, R>
    where R : TaskResult<T, R>
{
    private readonly List<Thread> threads = new List<Thread>();
    private readonly BlockingCollection<T> tasksQueue = new BlockingCollection<T>();
    private readonly BlockingCollection<R> resultsQueue = new BlockingCollection<R>();
    private CancellationTokenSource tasksCancellation;
    private CancellationTokenSource resultsCancellation;

    private readonly object counterLock = new object();
    private readonly object completedLock = new object();

    public volatile int resultsTime = 8; // 8ms = half of a frame time @ 60pfs
    public volatile int taskCount = 0;
    public int TaskCount => taskCount;
    private volatile int completedTasksCount = 0;
    public int CompletedTasksCount => completedTasksCount;
    public float Progress => TaskCount == 0 ? 0 : (float)completedTasksCount / TaskCount;
    public bool IsRunning => threads.Count > 0 || waitForResults != null;
    public bool runWithoutTasks = false;

    public event Action OnAllTasksFinished;

    private MonoBehaviour mb;

    public void Add(T task)
    {
        lock (counterLock)
        {
            taskCount++;
        }
        tasksQueue.Add(task);
    }

    public void Run(MonoBehaviour mb, Action<R> callback)
	{
        Run(mb, Environment.ProcessorCount - 1, callback);
    }

    public void Run(MonoBehaviour mb, int threadCount, Action<R> callback)
    {
        if (IsRunning)
        {
            Debug.LogWarning("Scheduler is already running");
            return;
        }

        tasksCancellation = new CancellationTokenSource();
        resultsCancellation = new CancellationTokenSource();

        threadCount = Mathf.Max(1, Mathf.Min(threadCount, Environment.ProcessorCount));

        // Create threads
        bool hasCallback = callback != null;
        for (int i = 0; i < threadCount; i++)
        {
            int index = i;
            var thread = new Thread(() => RunTasksThread(index, hasCallback))
            {
                Name = "RunTasksThread_" + index,
                Priority = System.Threading.ThreadPriority.Highest,
            };

            threads.Add(thread);
            thread.Start();
        }

        this.mb = mb;
        waitForResults = mb.StartCoroutine(WaitForResults(callback));
    }

    public void Stop()
    {
        StopThreads(true);

        // Stop the results coroutine
        if (waitForResults != null)
        {
            mb.StopCoroutine(waitForResults);
            waitForResults = null;
        }
    }

    public void Clear()
    {
        lock (counterLock)
        {
            // Empty the queues
            while (tasksQueue.TryTake(out var _)) ;
            while (resultsQueue.TryTake(out var _)) ;

            taskCount = 0;
        }
        lock (completedLock)
		{
            completedTasksCount = 0;
        }
    }

    private void RunTasksThread(int threadIndex, bool hasCallback)
    {
#if PROFILE_TASKS
        UnityEngine.Profiling.Profiler.BeginThreadProfiling("TaskScheduler", "Thread_" + threadIndex);
#endif

        T taskInfo = null;

        try
        {
            while (!tasksCancellation.IsCancellationRequested)
            {
                // This will block until an item is added to the queue or the cancellation token is cancelled
                taskInfo = tasksQueue.Take(tasksCancellation.Token);

                R result = taskInfo.task(taskInfo);
                if (hasCallback && result != null)
                {
                    resultsQueue.Add(result);
                }
                else
				{
                    lock (completedLock)
                    {
                        completedTasksCount++;
                    }
                }
                taskInfo = null;
            }
        }
        catch (OperationCanceledException)
        {
            // Put the task back in the queue if it didn't finish
            if (taskInfo != null)
                PutTaskBack(taskInfo);
        }
        catch (ThreadAbortException)
        {
            // Put the task back in the queue if it didn't finish
            if (taskInfo != null)
                PutTaskBack(taskInfo);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        finally
        {
            //Debug.Log("Thread " + threadIndex + " stopped");
        }

#if PROFILE_TASKS
        UnityEngine.Profiling.Profiler.EndThreadProfiling();
#endif
    }

    private readonly System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
    private Coroutine waitForResults;
    private IEnumerator WaitForResults(Action<R> callback)
    {
        while (completedTasksCount < taskCount || taskCount == 0 || runWithoutTasks)
        {
            yield return null;

            if (callback != null)
            {
                // Check if there are any results in the queue
                try
                {
                    if (resultsQueue.TryTake(out R result, 0, resultsCancellation.Token))
                    {
                        stopwatch.Restart();
                        do
                        {
                            lock (completedLock)
                            {
                                completedTasksCount++;
                            }
                            callback(result);
                        }
                        while (stopwatch.ElapsedMilliseconds < resultsTime &&
                                resultsQueue.TryTake(out result, 0, resultsCancellation.Token));

                        stopwatch.Stop();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        stopwatch.Stop();
        stopwatch.Reset();

        // Stop all the threads
        StopThreads();

        if (completedTasksCount == taskCount)
            OnAllTasksFinished?.Invoke();

        Clear();

        // Mark coroutine as null (completed)
        waitForResults = null;
    }

    private void StopThreads(bool immediate = false)
    {
        // Cancel the queues
        if (!tasksCancellation.IsCancellationRequested)
        {
            tasksCancellation.Cancel();
            resultsCancellation.Cancel();
        }

        // Stop the threads
        if (immediate)
        {
            foreach (var t in threads)
            {
                if (t != null)
                {
                    if (!t.Join(10))
                        t.Abort();
                }
            }
        }
        threads.Clear();
    }

    private void PutTaskBack(T taskInfo)
    {
        lock (counterLock)
        {
            if (taskCount > 0)
                tasksQueue.Add(taskInfo);
        }
    }
}
