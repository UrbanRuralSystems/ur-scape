// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Isaac Lu  (isaac.lu@sec.ethz.ch)
//          Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System;
using System.Collections.Generic;
using UnityEngine;

public class CorrelationTaskScheduler : TaskScheduler<CorrelationTaskInfo, NoTaskResult<CorrelationTaskInfo>> { }

public class CorrelationTaskInfo : TaskInfo<CorrelationTaskInfo, NoTaskResult<CorrelationTaskInfo>>
{
    public CorrelationTaskInfo(Func<CorrelationTaskInfo, NoTaskResult<CorrelationTaskInfo>> task) : base(task) { }

    public DataLayers dataLayers;
    public int a, b;
};

public static class SpearmanCorrelation
{
    // Specific case for testing STATA data set
    /*
    public static void SpearmanTestVersion(System.Collections.Generic.List<Observation> participants)
    {
        int N = participants.Count;
        float[] X = new float[N], Y = new float[N];

        Debug.Log("Running correlation test on " + N + " observations and " + 1 + " demographics.");
        for (int n = 0; n < N; ++n)
        {
            X[n] = (float)participants[n].parsedValues[0];
            Y[n] = (float)participants[n].parsedValues[1];
        }
        X = Rankify(X);
        Y = Rankify(Y);
        float val = PearsonCorrelation(X, Y);

        Debug.Log("Test correlation value: " + val);
    }
    //*/

    public static NoTaskResult<CorrelationTaskInfo> CorrelationCalculationTask(CorrelationTaskInfo taskInfo)
    {
        // Grab data layers
        var activeLayer = taskInfo.dataLayers.activeLayerPanels;
        var dataLayer1 = activeLayer[taskInfo.a].DataLayer;
        var dataLayer2 = activeLayer[taskInfo.b].DataLayer;

        var patch1 = dataLayer1.loadedPatchesInView[0];
        var patch2 = dataLayer2.loadedPatchesInView[0];

        // Only GridedPatch
        if (!(patch1 is GridedPatch) || !(patch2 is GridedPatch))
            return null;
        
        GridData grid1 = (patch1 as GridedPatch).grid;
        GridData grid2 = (patch2 as GridedPatch).grid;

        var values1 = grid1.values;
        var values2 = grid2.values;

        // If array lengths don't match ): data invalid
        if (!int.Equals(values1.Length, values2.Length))
            return null;

        // Calculate Correlation between the two arrays
        var coeff = PearsonCorrelation(Rankify(grid1.values), Rankify(grid2.values));

        taskInfo.dataLayers.CorrCoeffs[taskInfo.a, taskInfo.b] =
        taskInfo.dataLayers.CorrCoeffs[taskInfo.b, taskInfo.a] = coeff;

        return null;
    }

    private static float[] Rankify(float[] arr)
    {
        int n = arr.Length;
        float[] ret = new float[n];

        int r, s;
        float ai;

        for (int i = 0; i < n; ++i)
        {
            ai = arr[i];

            // Reset r and s
            r = s = 1;

            // Check before i
            for (int j = 0; j < i; ++j)
            {
                if (arr[j] < ai) ++r;
                else if (arr[j] == ai) ++s;
            }
            // Check after i
            for (int j = i + 1; j < n; ++j)
            {
                if (arr[j] < ai) ++r;
                else if (arr[j] == ai) ++s;
            }
            ret[i] = r + (s - 1) * 0.5f;
        }

        return ret;
    }

    private static float PearsonCorrelation(float[] X, float[] Y)
    {
        int n = X.Length;
        float sumX = 0, sumY = 0, sumXY = 0, sumSqX = 0, sumSqY = 0;

        for (int i = 0; i < n; ++i)
        {
            var x = X[i];
            var y = Y[i];
            sumX += x;
            sumY += y;
            sumXY += x * y;
            sumSqX += x * x;
            sumSqY += y * y;
        }
        
        return(n * sumXY - sumX * sumY) / Mathf.Sqrt((n * sumSqX - sumX * sumX) * (n * sumSqY - sumY * sumY));
    }
}
