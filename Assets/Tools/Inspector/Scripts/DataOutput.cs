// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DataOutput
{
    // Data lists
    public readonly Dictionary<string, InspectorOutputItemLabel> inspectorOutputItemLabels = new Dictionary<string, InspectorOutputItemLabel>();
    public Dictionary<string, List<float>> dictVals;
    public Dictionary<string, string> dictUnits;
    public Dictionary<string, Color> dictColors;
    public Dictionary<string, int> dictNoDatas;

    // Output
    public IEnumerable<InspectorOutputItem> itemslist;

    protected virtual void UpdateData(Action plotData)
    {
        List<InspectorOutputItem> inspectorOutputItems = new List<InspectorOutputItem>();

        foreach (var pair in dictVals)
        {
            float max = 0.0f;
            float min = 0.0f;
            float mean = 0.0f;
            float median = 0.0f;
            float sum = 0.0f;
            string unit = "";
            Color color = Color.black;

            if (pair.Value.Any())
            {
                max = pair.Value.Max();
                min = pair.Value.Min();
                mean = ComputeMean(pair.Value);
                median = ComputeMedian(pair.Value);
                sum = ComputeSum(pair.Value);
                unit = dictUnits.ContainsKey(pair.Key) ? dictUnits[pair.Key] : "";
                color = dictColors.ContainsKey(pair.Key) ? dictColors[pair.Key] : Color.black;
            }

            inspectorOutputItems.Add(new InspectorOutputItem
            {
                name = pair.Key,
                maxVal = max,
                minVal = min,
                meanVal = mean,
                medianVal = median,
                sumVal = sum,
                units = unit,
                dotColor = color
            });
        }

        itemslist = inspectorOutputItems;
        plotData();
    }

    protected float ComputeMedian(List<float> vals)
    {
        vals.Sort();
        double mid = (vals.Count - 1) / 2.0;
        return (vals[(int)(mid)] + vals[(int)(mid + 0.5)]) / 2;
    }

    protected float ComputeMean(List<float> vals)
    {
        // TODO: Take into account of relative values
        return vals.Average();
    }

    protected float ComputeSum(List<float> vals)
    {
        // TODO: Take into account of relative values
        return vals.Sum();
    }

    public virtual void SetData(Dictionary<string, List<float>> vals, Dictionary<string, string> units, Dictionary<string, Color> colors, Dictionary<string, int> noDataCounts, Action plotData)
    {
        dictVals = vals;
        dictUnits = units;
        dictColors = colors;
        dictNoDatas = noDataCounts;
        UpdateData(plotData);
    }

    public void AllCombinedMetrics(PropertiesAndSummaries[] ps)
    {
        foreach (var inspectorOutputItem in itemslist)
        {
            string itemName = inspectorOutputItem.name;
            InspectorOutputItemLabel inspectorOutputItemLabel = inspectorOutputItemLabels[itemName];

            List<float> allMaxVals = new List<float>();
            List<float> allMinVals = new List<float>();
            List<float> allMedianVals = new List<float>();
            List<float> allMeanVals = new List<float>();
            List<float> allSumVals = new List<float>();

            foreach (var propertyAndSummary in ps)
            {
                if (propertyAndSummary.maxVal.TryGetValue(itemName, out float maxVal))
                    allMaxVals.Add(maxVal);
                if (propertyAndSummary.minVal.TryGetValue(itemName, out float minVal))
                    allMinVals.Add(minVal);
                if (propertyAndSummary.medianVal.TryGetValue(itemName, out float medianVal))
                    allMedianVals.Add(medianVal);
                if (propertyAndSummary.meanVal.TryGetValue(itemName, out float meanVal))
                    allMeanVals.Add(meanVal);
                if (propertyAndSummary.sumVal.TryGetValue(itemName, out float sumVal))
                    allSumVals.Add(sumVal);
            }

            float max = allMaxVals.Max();
            float min = allMinVals.Min();
            float median = ComputeMedian(allMedianVals);
            float mean = ComputeMean(allMeanVals);
            float sum = ComputeSum(allSumVals);

            inspectorOutputItemLabel.SetMaxValue(max.ToString("#,##0.##"));
            inspectorOutputItemLabel.SetMedianValue(median.ToString("#,##0.##"));
            inspectorOutputItemLabel.SetMinValue(min.ToString("#,##0.##"));
            inspectorOutputItemLabel.SetMeanValue(mean.ToString("#,##0.##"));
            inspectorOutputItemLabel.SetSumValue(sum.ToString("#,##0.##"));
        }
    }

    public void SelectedMetrics(PropertiesAndSummaries currPS)
    {
        if (itemslist == null)
            return;

        foreach (var inspectorOutputItem in itemslist)
        {
            string itemName = inspectorOutputItem.name;
            InspectorOutputItemLabel inspectorOutputItemLabel = inspectorOutputItemLabels[itemName];

            inspectorOutputItemLabel.SetMaxValue(currPS.maxVal[itemName].ToString("#,##0.##"));
            inspectorOutputItemLabel.SetMedianValue(currPS.medianVal[itemName].ToString("#,##0.##"));
            inspectorOutputItemLabel.SetMinValue(currPS.minVal[itemName].ToString("#,##0.##"));
            inspectorOutputItemLabel.SetMeanValue(currPS.meanVal[itemName].ToString("#,##0.##"));
            inspectorOutputItemLabel.SetSumValue(currPS.sumVal[itemName].ToString("#,##0.##"));
        }
    }
}

public class LineDataOutput : DataOutput
{
    public void AllCombinedTotalLength(LinePropertiesAndSummaries[] linePS, LinePropertiesAndSummariesUIRef lineUIRef)
    {
        double overallTotalLength = 0.0;
        foreach (var pair in linePS)
        {
            overallTotalLength += pair.totalLength;
        }

        lineUIRef.totalLengthVal.text = overallTotalLength.ToString("#,##0.##") + " km";
    }

    public void AllCombinedNoDataLength(LinePropertiesAndSummaries[] linePS, LinePropertiesAndSummariesUIRef lineUIRef)
    {
        float overallNoDataCount = 0.0f, overallTotalDataCount = 0.0f;
        foreach (var pair in linePS)
        {
            overallNoDataCount += pair.noDataLength * pair.totalDataCount;
            overallTotalDataCount += pair.totalDataCount;
        }
        float overallNoDatalLength = overallNoDataCount / overallTotalDataCount;

        lineUIRef.noDataLengthVal.text = (overallNoDatalLength * 100).ToString("0.##") + "%";
    }
}

public class AreaDataOutput : DataOutput
{
    public void AllCombinedTotalArea(AreaPropertiesAndSummaries[] areaPS, AreaPropertiesAndSummariesUIRef areaUIRef)
    {
        double overallTotalArea = 0.0;
        foreach (var pair in areaPS)
        {
            overallTotalArea += pair.totalArea;
        }

        var suffix = "";
        float area = (float)(overallTotalArea * 0.000001);
        if (area > 1e+12)
        {
            area *= 1e-12f;
            suffix = " " + Translator.Get("trillion");
        }
        else if (area > 1e+9)
        {
            area *= 1e-9f;
            suffix = " " + Translator.Get("billion");
        }
        else if (area > 1e+6)
        {
            area *= 1e-6f;
            suffix = " " + Translator.Get("million");
        }

        areaUIRef.totalAreaVal.text = area.ToString("#,##0.##") + suffix + " km\xB2";
    }

    public void AllCombinedNoDataArea(AreaPropertiesAndSummaries[] areaPS, AreaPropertiesAndSummariesUIRef areaUIRef)
    {
        float overallNoDataCount = 0.0f, overallTotalDataCount = 0.0f;
        foreach (var pair in areaPS)
        {
            overallNoDataCount += pair.noDataArea * pair.totalDataCount;
            overallTotalDataCount += pair.totalDataCount;
        }
        float overallNoDatalArea = overallNoDataCount / overallTotalDataCount;

        areaUIRef.noDataAreaVal.text = (overallNoDatalArea * 100).ToString("0.##") + "%";
    }
}

public class ContourDataOutput : DataOutput
{
    protected override void UpdateData(Action plotData)
    {
        List<InspectorOutputItem> inspectorOutputItems = new List<InspectorOutputItem>();

        foreach (var pair in dictVals)
        {
            float max = 0.0f;
            float min = 0.0f;
            float mean = 0.0f;
            float median = 0.0f;
            float sum = 0.0f;
            string unit = "";
            Color color = Color.black;

            if (pair.Value.Any())
            {
                max = pair.Value.Max();
                min = pair.Value.Min();
                mean = ComputeMean(pair.Value);
                median = ComputeMedian(pair.Value);
                sum = ComputeSum(pair.Value);
                unit = dictUnits.ContainsKey(pair.Key) ? dictUnits[pair.Key] : "";
                color = dictColors.ContainsKey(pair.Key) ? dictColors[pair.Key] : Color.black;
            }

            inspectorOutputItems.Add(new InspectorOutputItem
            {
                name = pair.Key,
                maxVal = max,
                minVal = min,
                meanVal = mean,
                medianVal = median,
                sumVal = sum,
                units = unit,
                dotColor = color
            });
        }

        itemslist = inspectorOutputItems;
        plotData();
    }

    public override void SetData(Dictionary<string, List<float>> vals, Dictionary<string, string> units, Dictionary<string, Color> colors, Dictionary<string, int> noDataCounts, Action plotData)
    {
        dictVals = vals;
        dictUnits = units;
        dictColors = colors;
        dictNoDatas = noDataCounts;
        UpdateData(plotData);
    }
}