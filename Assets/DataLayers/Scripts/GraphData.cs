// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
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
using System.Collections.Generic;
using UnityEngine;

public enum ClassificationIndex
{
	None = 0,
	Other = 1,
	Secondary = 2,
	Primary = 3,
	HighwayLink = 4,
	Highway = 5,

	Count = 6,
}

public static class ClassificationValue
{
	public const int None = 0;
	public const int Other = 1;
	public const int Secondary = 2;
	public const int Primary = 4;
	public const int HighwayLink = 8;
	public const int Highway = 16;

	public const int Count = 6;
}

public static class ClassificationMap
{
	public static readonly int[][] ToValues = InitToValuesArray();
	private static int[][] InitToValuesArray()
	{
		int lastClassificationIndex = (int)ClassificationIndex.Count - 1;
		int[][] map = new int[(int)Math.Pow(2, lastClassificationIndex)][];
		map[0] = new int[] { 0 };

		int count = map.Length;
		for (int i = 1; i < count; i++)
		{
			var arr = map[i] = new int[CountBits(i)];

			int index = 0;
			for (int j = 0; j < lastClassificationIndex; j++)
			{
				if ((i & (1 << j)) != 0)
					arr[index++] = (1 << j);
			}
		}
		return map;
	}

	private static int CountBits(int value)
	{
		int count = 0;
		while (value != 0)
		{
			count++;
			value &= value - 1;
		}
		return count;
	}
}


public class GraphNode
{
    public readonly double longitude;
    public readonly double latitude;
	public int classifications;

	public readonly List<GraphNode> links = new List<GraphNode>();
    public readonly List<float> linkDistances = new List<float>();
    public readonly List<int> linkClassifications = new List<int>();

    public float cost;	// cost is dynamically calculated based on the start point(s)
    public int index;	// the index to the cell in the GridData

	public GraphNode(double longitude, double latitude, int classifications, int index)
	{
		this.longitude = longitude;
		this.latitude = latitude;
		this.classifications = classifications;
		this.index = index;
	}

	public static void AddLink(GraphNode nodeA, GraphNode nodeB, float distance, int classification)
    {
		if (classification == ClassificationValue.None)
		{
			AddLinkData(nodeA, nodeB, distance, classification);
			AddLinkData(nodeB, nodeA, distance, classification);
		}
		else
		{
			// Seperate links with different classification
			var classValues = ClassificationMap.ToValues[classification];
			int count = classValues.Length;
			for (int i = 0; i < count; i++)
			{
				AddLinkData(nodeA, nodeB, distance, classValues[i]);
				AddLinkData(nodeB, nodeA, distance, classValues[i]);
			}
		}
	}

    public static void RemoveLink(GraphNode nodeA, GraphNode nodeB, int classification)
    {
		for (int i = nodeA.links.Count - 1; i >= 0; --i)
        {
            if (nodeA.links[i] == nodeB && nodeA.linkClassifications[i] == classification)
            {
                nodeA.links.RemoveAt(i);
                nodeA.linkDistances.RemoveAt(i);
                nodeA.linkClassifications.RemoveAt(i);
				break;
            }
        }

		for (int i = nodeB.links.Count - 1; i >= 0; --i)
		{
			if (nodeB.links[i] == nodeA && nodeB.linkClassifications[i] == classification)
			{
				nodeB.links.RemoveAt(i);
				nodeB.linkDistances.RemoveAt(i);
				nodeB.linkClassifications.RemoveAt(i);
				break;
			}
		}
	}

	public void RemoveLink(int index)
	{
		var nodeB = links[index];
		int classification = linkClassifications[index];

		links.RemoveAt(index);
		linkDistances.RemoveAt(index);
		linkClassifications.RemoveAt(index);

		for (int i = nodeB.links.Count - 1; i >= 0; --i)
		{
			if (nodeB.links[i] == this && nodeB.linkClassifications[i] == classification)
			{
				nodeB.links.RemoveAt(i);
				nodeB.linkDistances.RemoveAt(i);
				nodeB.linkClassifications.RemoveAt(i);
				break;
			}
		}
	}

	private static void AddLinkData(GraphNode node, GraphNode link, float distance, int classification)
    {
#if SAFETY_CHECK
		if (node == link || node.index == link.index)
		{
			Debug.LogError("A node can't have a link to itself!  Node: " + node.index);
			return;
		}
#endif
		for (int i = 0; i < node.links.Count; i++)
		{
			if (node.links[i] == link && node.linkClassifications[i] == classification)
			{
#if SAFETY_CHECK
				// Duplicate links should be avoided. Old csv files may have duplicate links due to bugs in the export tool
				//+	Debug.LogWarning("Duplicate link found between node " + node.index + " and " + link.index + " with classification: " + classification);
#endif
				node.linkDistances[i] = distance < node.linkDistances[i] ? distance : node.linkDistances[i];
				return;
			}
		}

		node.links.Add(link);
		node.linkDistances.Add(distance);
		node.linkClassifications.Add(classification);
    }
}

public class GraphData : PatchData
{
	public readonly List<GraphNode> nodes = new List<GraphNode>();

	public double cellSizeX;
	public double cellSizeY;

	public readonly Dictionary<int, GraphNode> indexToNode = new Dictionary<int, GraphNode>();

	public override bool IsLoaded()
	{
		return true;
	}

	public override void UnloadData()
	{
		nodes.Clear();
		indexToNode.Clear();
	}

	public void InitGrid(GridData grid)
	{
		grid.countX = (int)Math.Round((east - west) / cellSizeX);
		grid.countY = (int)Math.Round((north - south) / cellSizeY);

		grid.north = north;
		grid.east = east;
		grid.south = south;
		grid.west = west;
	}

	public void CreateDefaultGrid(GridData grid)
	{
		grid.InitGridValues(false);

		grid.minValue = float.MaxValue;
		grid.maxValue = float.MinValue;

		for (int i = nodes.Count - 1; i >= 0; i--)
		{
			var node = nodes[i];
			int classification = node.classifications;
			int index = Math.Abs(node.index);

			int gridValue = classification + (int)grid.values[index];
			grid.values[index] = gridValue;
			grid.minValue = gridValue < grid.minValue ? gridValue : grid.minValue;
			grid.maxValue = gridValue > grid.maxValue ? gridValue : grid.maxValue;
		}

		grid.minFilter = grid.minValue;
		grid.maxFilter = grid.maxValue;
	}

}
