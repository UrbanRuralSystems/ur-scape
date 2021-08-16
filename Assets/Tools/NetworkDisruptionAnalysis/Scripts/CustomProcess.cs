// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class CustomProcess
{
    private readonly ProcessStartInfo psi;
    private Process proc;
    private BackgroundWorker bw;
    private bool cancelled;

    public StreamReader Output { private set; get; }
    public StreamReader Errors { private set; get; }
    public event EventHandler OnFinished;

    public CustomProcess(string fileName, params string[] args)
    {
        var processArgs = "";
        if (args != null)
        {
            foreach (var arg in args)
            {
                processArgs += " " + arg;
            }
        }

        UnityEngine.Debug.Log(processArgs);
        psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = processArgs,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }

    public void Run()
    {
        if (proc != null)
            return;

        proc = new Process();
        proc.Exited += (_, e) =>
        {
            OnFinished?.Invoke(this, e);

            Output = null;
            Errors = null;
            proc = null;
            cancelled = false;
        };

        proc.StartInfo = psi;
        proc.EnableRaisingEvents = true;
        proc.Start();

        if (psi.RedirectStandardOutput)
            Output = proc.StandardOutput;
        if (psi.RedirectStandardError)
            Errors = proc.StandardError;

        if (bw != null)
        {
            while (bw.IsBusy)
            {
                if (bw.CancellationPending)
                {
                    // UnityEngine.Debug.Log("Cancelling...");
                    proc.Kill();
                    cancelled = true;
                    break;
                }
            }

            bw.Dispose();
            bw = null;
        }
    }

    public void RunInBG()
    {
        if (proc != null)
            return;

        bw = new BackgroundWorker();
        bw.WorkerSupportsCancellation = true;

        bw.DoWork += (_, e) =>
        {
            Run();
        };

        bw.RunWorkerAsync();
    }

    public void Cancel()
    {
        if (bw != null)
        {
            bw.CancelAsync();
        }
        else
        {
            if (proc != null)
            {
                proc.Kill();
                cancelled = true;
            }
        }
    }

    public bool IsCancelled()
    {
        return cancelled && proc.HasExited;
    }
}