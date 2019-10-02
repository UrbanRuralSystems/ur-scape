// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Hong Liang  (hliang@student.ethz.ch)
//			Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class RoadLayer : GridMapLayer
{
	public GraphData graph;

    private int classificationIndex;

	//
	// Public Method
	//

	public void Init(GraphPatch patch, int classificationIndex, List<Coordinate> points)
    {
        graph = new GraphData();
        this.classificationIndex = classificationIndex;

        GraphData g = patch.graph;
        graph.cellSizeX = g.cellSizeX;
        graph.cellSizeY = g.cellSizeY;
        graph.west = g.west;
        graph.east = g.east;
        graph.north = g.north;
        graph.south = g.south;

        grid = new GridData(patch.grid, false);

		SetPoints(points);
	}

	public void ChangePoints(List<Coordinate> points)
	{
		SetPoints(points);
		grid.GridChanged();
	}

	private void SetPoints(List<Coordinate> points)
    {
        ClearGraph();
		CreateRoad(points);
		graph.CreateDefaultGrid(grid);
		grid.maxValue = ClassificationValue.HighwayLink;
		grid.minValue = 0;
	}

	private void ClearGraph()
    {
        graph.nodes.Clear();
        graph.indexToNode.Clear();
    }


    //
    // Private Method
    //

    private void CreateRoad(List<Coordinate> points)
    {
        // Check if all points are inside of graph
        if (!PointsInsideGraph(points, graph))
        {
            Debug.LogWarning("New road is not inside the graph");
            return;
        }

		double kX = 1.0 / graph.cellSizeX;
		double kY = 1.0 / graph.cellSizeY;

		for (int i = 0; i < points.Count - 1; i++)
        {
            int value = classificationIndex == 0? 0 : 1 << (classificationIndex - 1); 

            Coordinate coorStart = points[i];
            Coordinate coorEnd = points[i + 1];
            GraphNode startNode = new GraphNode(coorStart.Longitude, coorStart.Latitude, value);
            GraphNode endNode = new GraphNode(coorEnd.Longitude, coorEnd.Latitude, value);

            int startIndex = (int)((startNode.longitude - graph.west) * kX) + grid.countX * (int)((graph.north - startNode.latitude) * kY);
            int startRow = startIndex / grid.countX;
            int startColumn = startIndex - startRow * grid.countX;

            int endIndex = (int)((endNode.longitude - graph.west) * kX) + grid.countX * (int)((graph.north - endNode.latitude) * kY);
            int endRow = endIndex / grid.countX;
            int endColumn = endIndex - endRow * grid.countX;

            if (startIndex == endIndex)
            {
                if (i == 0)
                    AddNode(startIndex, startNode);
                continue;
            }
                
            int countX = Math.Abs(startColumn - endColumn);
            int countY = Math.Abs(startRow - endRow);

			double k;
			if (countX >= countY)
                k = (endNode.latitude - startNode.latitude) / (endNode.longitude - startNode.longitude);
            else
                k = (endNode.longitude - startNode.longitude) / (endNode.latitude - startNode.latitude);

            for (int j = 0; j <= Math.Max(countY, countX); j++)
            {
                if (j == 0) // startNode
                {
                    if (i == 0)
                    {
						if (startNode.value == ClassificationValue.Highway) // 16
						{
							// The first point is highway link
							startNode.value = ClassificationValue.HighwayLink; // 8
						}
                        AddNode(startIndex, startNode);
                        continue;
                    }
                    else
                        continue;
                }
                else if (j == Math.Max(countY, countX)) // endNode
                {
                    GraphNode lastNode = graph.nodes[graph.nodes.Count - 1];

					if ((i == points.Count - 2) && endNode.value == ClassificationValue.Highway) // 16
					{
						endNode.value = ClassificationValue.HighwayLink; // 8
					}

                    AddNode(endIndex, endNode);

					var distance = (float)GeoCalculator.GetDistanceInMeters(endNode.longitude, endNode.latitude, lastNode.longitude, lastNode.latitude);

                    GraphNode.AddLink(endNode, lastNode, distance, endNode.value);
                }
                else
                {
                    GraphNode lastNode = graph.nodes[graph.nodes.Count - 1];
                    double lon;
                    double lat;

                    if (countX >= countY)
                    {
                        if (endColumn > startColumn)
                        {
                            lon = lastNode.longitude + graph.cellSizeX;
                            lat = lastNode.latitude + k * graph.cellSizeX;
                        }
                        else
                        {
                            lon = lastNode.longitude - graph.cellSizeX;
                            lat = lastNode.latitude - k * graph.cellSizeX;
                        }
                    }
                    else
                    {
                        if (endRow < startRow)
                        {
                            lat = lastNode.latitude + graph.cellSizeY;
                            lon = lastNode.longitude + k * graph.cellSizeY;
                        }
                        else
                        {
                            lat = lastNode.latitude - graph.cellSizeY;
                            lon = lastNode.longitude - k * graph.cellSizeY;
                        }
                    }

					GraphNode newNode = new GraphNode(lon, lat, value);

					var distance = (float)GeoCalculator.GetDistanceInMeters(endNode.longitude, endNode.latitude, lastNode.longitude, lastNode.latitude);

					int index = (int)((lon - graph.west) * kX) + grid.countX * (int)((graph.north - lat) * kY);
                    AddNode(index, newNode);

                    if (i == 0 && j == 1)
                        GraphNode.AddLink(endNode, lastNode, distance, ClassificationValue.HighwayLink); // 8
					else
                        GraphNode.AddLink(newNode, lastNode, distance, value);
                }
            }
        }
    }

    private void AddNode(int index, GraphNode node)
    {
		GraphNode tempNode;
		if (graph.indexToNode.TryGetValue(index, out tempNode))
		{
			// Move it to the end of list
			graph.nodes.Remove(tempNode);
			node = tempNode;
			graph.nodes.Add(node);
		}
		else
		{
			graph.nodes.Add(node);
            graph.indexToNode.Add(index, node);
        }
    }

    private static bool PointsInsideGraph(List<Coordinate> points, GraphData graph)
    {
        foreach (var p in points)
        {
            if (!graph.IsInside(p.Longitude, p.Latitude))
                return false;
        }
        return true;
    }

}
