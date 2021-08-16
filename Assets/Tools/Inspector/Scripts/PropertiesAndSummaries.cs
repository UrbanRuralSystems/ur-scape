// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.Collections.Generic;

public class PropertiesAndSummaries
{
    public int totalDataCount;

    public Dictionary<string, float> maxVal = new Dictionary<string, float>();
    public Dictionary<string, float> minVal = new Dictionary<string, float>();
    public Dictionary<string, float> medianVal = new Dictionary<string, float>();
    public Dictionary<string, float> meanVal = new Dictionary<string, float>();
    public Dictionary<string, float> sumVal = new Dictionary<string, float>();

    public static void AddOrUpdateMetric(Dictionary<string, float> metric, string key, float value)
    {
        if (metric.ContainsKey(key))
            metric[key] = value;
        else
            metric.Add(key, value);
    }
}

public class LinePropertiesAndSummaries : PropertiesAndSummaries
{
    public double totalLength;
    public float noDataLength;

    public static readonly int SelectedLine = 0;
    public static readonly int AllLinesCombined = 1;
    public static readonly int AllLinesCompared = 2;
    public static readonly int Correlation = 2;
}

public class AreaPropertiesAndSummaries : PropertiesAndSummaries
{
    public double totalArea;
    public float noDataArea;

    public static readonly int SelectedArea = 0;
    public static readonly int AllAreasCombined = 1;
    public static readonly int AllAreasCompared = 2;
    public static readonly int Correlation = 2;
}

public class ContourPropertiesAndSummaries : PropertiesAndSummaries
{
    public double totalArea;
    public float noDataArea;
}

public static class PropertiesAndSummariesOptions
{
    public static readonly string[] LineSummaryOptions = {
        "Selected Line"/*translatable*/,
        "All Lines Combined"/*translatable*/,
	  //"All Lines Compared"/*translatable*/
        "Correlation"/*translatable*/
	};

    public static readonly string[] LinePropertiesLabels = {
        "Line Properties"/*translatable*/,
        "Combined Properties"/*translatable*/,
        "Line Properties Compared"/*translatable*/
	};

    public static readonly string[] MetricsOptions = {
        "Maximum"/*translatable*/,
        "Median"/*translatable*/,
        "Minimum"/*translatable*/
	};

    public static readonly string[] SummaryLabels = {
        "Inspection Summary"/*translatable*/,
        "Combined Inspection Summary"/*translatable*/,
        "Inspection Summary Comparison"/*translatable*/
	};

    public static readonly string[] AreaSummaryOptions = {
        "Selected Area"/*translatable*/,
        "All Areas Combined"/*translatable*/,
	  //"All Areas Compared/*translatable*/"
        "Correlation"/*translatable*/
	};

    public static readonly string[] AreaPropertiesLabels = {
        "Area Properties"/*translatable*/,
        "Combined Properties"/*translatable*/,
        "Area Properties Compared"/*translatable*/
	};

    public static readonly string ContourPropertiesLabel = "Contour Properties"/*translatable*/;
    public static readonly string ContourSummaryLabel = "Contour Summary"/*translatable*/;
}