// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;
using UnityEngine;

public static class ClassificationIndex
{
	public const int None = 0;
	public const int Other = 1;
	public const int Secondary = 2;
	public const int Primary = 3;
	public const int HighwayLink = 4;
	public const int Highway = 5;

	public const int Count = 6;
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

public class GraphNode
{
    public readonly double longitude;
    public readonly double latitude;
    
    public readonly List<GraphNode> links = new List<GraphNode>();
    public readonly List<float> distances = new List<float>();
    public readonly List<int> classifications = new List<int>();

	public int value;
    public float cost;
    public int index;

    public GraphNode(double longitude, double latitude, int value)
    {
        this.value = value;
        this.longitude = longitude;
        this.latitude = latitude;
    }

    public GraphNode(double longitude, double latitude)
    {
        this.longitude = longitude;
        this.latitude = latitude;
    }

    public static void AddLink(GraphNode nodeA, GraphNode nodeB, float distance, int classification)
    {
        if (classification == ClassificationValue.None)
        {
            if (!nodeA.links.Contains(nodeB))
            {
                AddLinkData(nodeA, nodeB, distance, classification);
            }
            if (!nodeB.links.Contains(nodeA))
            {
                AddLinkData(nodeB, nodeA, distance, classification);
            }
        }

        for (int i = 0; i < ClassificationIndex.Highway; i++) // seperate links with different classification
        {
			int classificationMask = 1 << i;

			if ((classification & classificationMask) > 0)
            {
                if (nodeA.links.Count == 0)
                {
                    AddLinkData(nodeA, nodeB, distance, classificationMask);
                }
                else
                {
                    for (int j = 0; j < nodeA.links.Count; j++)
                    {
                        if (nodeA.links[j].Equals(nodeB) && nodeA.classifications[j] == classificationMask)
                            break;
                        if (j == nodeA.links.Count - 1)
                        {
                            AddLinkData(nodeA, nodeB, distance, classificationMask);
                            break;
                        }
                    }
                }

                if (nodeB.links.Count == 0)
                {
                    AddLinkData(nodeB, nodeA, distance, classificationMask);
                }
                else
                {
                    for (int j = 0; j < nodeB.links.Count; j++)
                    {
                        if (nodeB.links[j].Equals(nodeA) && nodeB.classifications[j] == classificationMask)
                            break;
                        if (j == nodeB.links.Count - 1)
                        {
                            AddLinkData(nodeB, nodeA, distance, classificationMask);
                            break;
                        }
                    }
                }                
            }
        }            
    }

    public static void RemoveLink(GraphNode nodeA, GraphNode nodeB, int classification)
    {
        for (int i = 0; i < nodeA.links.Count; i++)
        {
            if (nodeA.links[i].Equals(nodeB) && nodeA.classifications[i] == classification)
            {
                nodeA.links.RemoveAt(i);
                nodeA.distances.RemoveAt(i);
                nodeA.classifications.RemoveAt(i);
            }
        }

        for (int i = 0; i < nodeB.links.Count; i++)
        {
            if (nodeB.links[i].Equals(nodeA) && nodeB.classifications[i] == classification)
            {
                nodeB.links.RemoveAt(i);
                nodeB.distances.RemoveAt(i);
                nodeB.classifications.RemoveAt(i);
            }
        }
    }

    private static void AddLinkData(GraphNode node, GraphNode link, float distance, int classification)
    {
        node.links.Add(link);
        node.distances.Add(distance);
        node.classifications.Add(classification);
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

		double kX = 1.0 / cellSizeX;
		double kY = 1.0 / cellSizeY;

		for (int i = nodes.Count - 1; i >= 0; i--)
		{
			var node = nodes[i];
			int index = (int)((node.longitude - west) * kX) + grid.countX * (int)((north - node.latitude) * kY);
			int value = node.value;
			int indexCopy = index;

			if (value == ClassificationValue.Highway)	// 16
			{
				// not distinguish highway and highwaylink
				value = ClassificationValue.HighwayLink;	// 8
				// highway and other road are on the different layer, they could be overlapped
				indexCopy = -index;
			}

			node.index = indexCopy;

			int gridValue = (int)grid.values[index];

			gridValue = Math.Max(value, gridValue);
			grid.values[index] = gridValue;
			grid.minValue = Mathf.Min(grid.minValue, gridValue);
			grid.maxValue = Mathf.Max(grid.maxValue, gridValue);
		}

		grid.minFilter = grid.minValue;
		grid.maxFilter = grid.maxValue;
	}

	public void CreatePotentialNetwork(GridData grid)
	{
		int existingNodeCount = nodes.Count;

		for (int i = grid.countX * grid.countY - 1; i >= 0; i--)
		{
			if (indexToNode.ContainsKey(i) && indexToNode.ContainsKey(-i)) // there are both highway and highway link
			{
				if (indexToNode[i].value >= ClassificationValue.HighwayLink)	// 8
					CreateHighwayLink(i);
			}

			if (grid.values[i] == ClassificationValue.None)	// 0
			{
				// Create new nodes
				int row = i / grid.countX;
				int column = i - row * grid.countX;
				double lonX = column * cellSizeX + west;
				double latY = north - row * cellSizeY;

				GraphNode newNode = new GraphNode(lonX, latY, 0);
				newNode.index = i;
				nodes.Add(newNode);
				indexToNode.Add(i, newNode);
			}
		}

		double x, y;
		GeoCalculator.GetDistanceInMeters(west, south, east, north, out x, out y);
		x /= grid.countX;
		y /= grid.countY;
		float distanceXY = (float)Math.Pow(x * x + y * y, 0.5);
		float distanceX = (float)x;
		float distanceY = (float)y;

		// create links for each new node
		int nodeCount = nodes.Count;
		for (int i = existingNodeCount; i < nodeCount; i++)
		{
			GraphNode node = nodes[i];
			if (node.value == 0)
			{
				int thisIndex = node.index;
				int row = thisIndex / grid.countX;
				int column = thisIndex - row * grid.countX;
				if (row > 0)
				{
					int upIndex = thisIndex - grid.countX;
					if (indexToNode.ContainsKey(upIndex))
						GraphNode.AddLink(node, indexToNode[upIndex], distanceY, ClassificationValue.None);
				}

				if (column > 0) // not on the left edge
				{
					int leftIndex = thisIndex - 1;
					if (indexToNode.ContainsKey(leftIndex))
						GraphNode.AddLink(node, indexToNode[leftIndex], distanceX, ClassificationValue.None);
				}

				if (row < grid.countY - 1) // not on the bottom
				{
					int downIndex = thisIndex + grid.countX;
					if (indexToNode.ContainsKey(downIndex))
						GraphNode.AddLink(node, indexToNode[downIndex], distanceY, ClassificationValue.None);
				}

				if (column < grid.countX - 1)
				{
					int rightIndex = thisIndex + 1;
					if (indexToNode.ContainsKey(rightIndex))
						GraphNode.AddLink(node, indexToNode[rightIndex], distanceX, ClassificationValue.None);
				}

				if (row > 0 && column > 0)
				{
					int upleftIndex = thisIndex - grid.countX - 1;
					if (indexToNode.ContainsKey(upleftIndex))
						GraphNode.AddLink(node, indexToNode[upleftIndex], distanceXY, ClassificationValue.None);
				}

				if (row > 0 && column < grid.countX - 1)
				{
					int uprightIndex = thisIndex - grid.countX + 1;
					if (indexToNode.ContainsKey(uprightIndex))
						GraphNode.AddLink(node, indexToNode[uprightIndex], distanceXY, ClassificationValue.None);
				}

				if (row < grid.countY - 1 && column > 0)
				{
					int downleftIndex = thisIndex + grid.countX - 1;
					if (indexToNode.ContainsKey(downleftIndex))
						GraphNode.AddLink(node, indexToNode[downleftIndex], distanceXY, ClassificationValue.None);
				}

				if (row < grid.countY - 1 && column < grid.countX - 1)
				{
					int downrightIndex = thisIndex + grid.countX + 1;
					if (indexToNode.ContainsKey(downrightIndex))
						GraphNode.AddLink(node, indexToNode[downrightIndex], distanceXY, ClassificationValue.None);
				}
			}
		}
	}

	private void CreateHighwayLink(int index)
	{
		GraphNode highwayNode = indexToNode[-index];
		GraphNode linkNode = indexToNode[index];

		for (int i = 0; i < linkNode.links.Count; i++)
		{
			if (linkNode.classifications[i] == ClassificationValue.HighwayLink)	// 8
			{
				GraphNode.AddLink(highwayNode, linkNode.links[i], linkNode.distances[i], ClassificationValue.HighwayLink); // 8
				GraphNode.RemoveLink(linkNode, linkNode.links[i], ClassificationValue.HighwayLink); // 8
			}
		}
	}

}
