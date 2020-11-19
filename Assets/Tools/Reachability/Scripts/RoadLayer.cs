// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
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

    private int classification;

	//
	// Public Method
	//

	public void Init(GraphPatch patch, int classification, List<Coordinate> points)
    {
        graph = new GraphData();
        this.classification = classification;

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
            Coordinate coorStart = points[i];
            Coordinate coorEnd = points[i + 1];

            int startIndex = (int)((coorStart.Longitude - graph.west) * kX) + grid.countX * (int)((graph.north - coorStart.Latitude) * kY);
            int endIndex = (int)((coorEnd.Longitude - graph.west) * kX) + grid.countX * (int)((graph.north - coorEnd.Latitude) * kY);

            var startNode = new GraphNode(coorStart.Longitude, coorStart.Latitude, classification, startIndex);
            var endNode = new GraphNode(coorEnd.Longitude, coorEnd.Latitude, classification, endIndex);

            int startRow = startIndex / grid.countX;
            int startColumn = startIndex - startRow * grid.countX;

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
						if (startNode.classifications == ClassificationValue.Highway) // 16
						{
							// The first point is highway link
							startNode.classifications = ClassificationValue.HighwayLink; // 8
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

					if ((i == points.Count - 2) && endNode.classifications == ClassificationValue.Highway) // 16
					{
						endNode.classifications = ClassificationValue.HighwayLink; // 8
					}

                    AddNode(endIndex, endNode);

					var distance = (float)GeoCalculator.GetDistanceInMeters(endNode.longitude, endNode.latitude, lastNode.longitude, lastNode.latitude);

                    GraphNode.AddLink(endNode, lastNode, distance, endNode.classifications);
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

                    int index = (int)((lon - graph.west) * kX) + grid.countX * (int)((graph.north - lat) * kY);
                    var newNode = new GraphNode(lon, lat, classification, index);

					var distance = (float)GeoCalculator.GetDistanceInMeters(endNode.longitude, endNode.latitude, lastNode.longitude, lastNode.latitude);

                    AddNode(index, newNode);

                    if (i == 0 && j == 1)
                        GraphNode.AddLink(endNode, lastNode, distance, ClassificationValue.HighwayLink); // 8
					else
                        GraphNode.AddLink(newNode, lastNode, distance, classification);
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
