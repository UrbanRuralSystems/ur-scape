// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public static class GraphDataIO
{
	public static readonly string FileSufix = "graph";

    private static readonly LoadPatchData<GraphPatch, GraphData>[] headerLoader =
    {
        LoadCsv,
        LoadBinHeader
    };

    public static LoadPatchData<GraphPatch, GraphData> GetPatchHeaderLoader(PatchDataFormat format)
    {
        return headerLoader[(int)format];
    }

    public static IEnumerator LoadCsv(string filename, PatchDataLoadedCallback<GraphData> callback)
    {
		yield return FileRequest.GetText(filename, (sr) => PatchDataIO.ParseAsync(sr, filename, ParseCsv, callback));
	}

	private static void ParseCsv(ParseTaskData data)
	{
        GraphData graph = new GraphData();
        graph.cellSizeX = double.MaxValue;
        graph.cellSizeY = double.MaxValue;
        graph.west = GeoCalculator.MaxLongitude;
        graph.east = GeoCalculator.MinLongitude;
        graph.north = GeoCalculator.MinLongitude;
        graph.south = GeoCalculator.MaxLongitude;

		Dictionary<int, GraphNode> nodes = new Dictionary<int, GraphNode>();

		// Read/skip header
		string line = data.sr.ReadLine();

		// Read each data row at a time
		while ((line = data.sr.ReadLine()) != null)
        {
            string[] cells = line.Split(',');

			int sourceId = int.Parse(cells[1]);
            int targetId = int.Parse(cells[2]);

            if (sourceId == targetId)
                continue;

            int value = int.Parse(cells[7]);

			float distance = float.Parse(cells[0]);
            double x1 = double.Parse(cells[3]);
            double y1 = double.Parse(cells[4]);
            double x2 = double.Parse(cells[5]);
            double y2 = double.Parse(cells[6]);

			if (value >= ClassificationValue.Highway)
			{
				// Use a negative sourceId & targetId to indicate another layer            
				AddNode(graph, nodes, -sourceId, -targetId, x1, y1, x2, y2, distance, ClassificationValue.Highway);
				value -= ClassificationValue.Highway;
			}

			if (value > 0)
			{
				AddNode(graph, nodes, sourceId, targetId, x1, y1, x2, y2, distance, value);
			}
		}

		AddHalfSizeToGraph(graph);

		GridData tempGrid = new GridData();
		graph.InitGrid(tempGrid);
		graph.CreateDefaultGrid(tempGrid);
        
        graph.indexToNode.Clear();

		double kX = 1.0 / graph.cellSizeX;
		double kY = 1.0 / graph.cellSizeY;
		foreach (var node in graph.nodes)
        {
			int index = (int)((node.longitude - graph.west) * kX) + tempGrid.countX * (int)((graph.north - node.latitude) * kY);
            if (node.value == ClassificationValue.Highway)
				index = -index; // highway and other road are on the different layer, they could be overlapped
            if (!graph.indexToNode.ContainsKey(index))
               graph.indexToNode.Add(index, node);
            node.index = index;
        }

        graph.CreatePotentialNetwork(tempGrid);

		data.patch = graph;
    }

    public static IEnumerator LoadBinHeader(string filename, PatchDataLoadedCallback<GraphData> callback)
    {
#if UNITY_WEBGL
        callback(ParseBinHeader(PatchDataIO.brHeaders, filename));
        yield break;
#else
        yield return FileRequest.GetBinary(filename, (br) => callback(ParseBinHeader(br, filename)));
#endif
    }

    public static GraphData ParseBinHeader(BinaryReader br, string filename)
    {
        return ParseBinHeader(br, filename, new GraphData());
    }

    public static IEnumerator LoadBin(this GraphData graph, string filename, PatchDataLoadedCallback<GraphData> callback)
    {
		yield return FileRequest.GetBinary(filename, (br) => ParseBin(br, filename, graph));
		callback(graph);
	}

    private static GraphData ParseBinHeader(BinaryReader br, string filename, GraphData graph)
    {
		// Read header
		PatchDataIO.SkipBinVersion(br);
		PatchDataIO.ReadBinBoundsHeader(br, filename, graph);

		// Read cell sizes
        graph.cellSizeX = br.ReadDouble();
        graph.cellSizeY = br.ReadDouble();

		return graph;
    }

    private static IEnumerator ParseBin(BinaryReader br, string filename, GraphData graph)
    {
		// Read header
		ParseBinHeader(br, filename, graph);

		int count = br.ReadInt32();
        graph.indexToNode.Clear();

		uint loop = 0;
		for (int i = 0; i < count; i++)
        {
            GraphNode node = new GraphNode(br.ReadDouble(), br.ReadDouble(), br.ReadInt32());
            node.index = br.ReadInt32();
            graph.nodes.Add(node);
			graph.indexToNode.Add(node.index, node);

			if (++loop > 10000)
			{
				loop = 0;
				yield return null;
			}
		}

		while (br.BaseStream.Position < br.BaseStream.Length)
        {
            int a = br.ReadInt32();
            int b = br.ReadInt32();
            GraphNode.AddLink(graph.indexToNode[a], graph.indexToNode[b], br.ReadSingle(), br.ReadInt32());

			if (++loop > 8000)
			{
				loop = 0;
				yield return null;
			}
        }
	}

    public static void SaveBin(this GraphData graph, string filename)
    {
        using (var bw = new BinaryWriter(File.Open(filename, FileMode.Create)))
        {
			PatchDataIO.WriteBinVersion(bw);
			PatchDataIO.WriteBinBoundsHeader(bw, graph);

			bw.Write(graph.cellSizeX);
            bw.Write(graph.cellSizeY);

            int count = graph.nodes.Count;
            bw.Write(count);
            for (int n = 0; n < count; n++)
            {
                GraphNode node = graph.nodes[n];
                bw.Write(node.longitude);
                bw.Write(node.latitude);
                bw.Write(node.value);
                bw.Write(node.index);
            }

			for (int n = 0; n < count; n++)
            {
                GraphNode node = graph.nodes[n];
                for (int l = 0; l < node.links.Count; l++)
                {
                    var link = node.links[l];
                    int index = node.index;
                    int linkIndex = link.index;
                    if (linkIndex > index)
                    {
                        bw.Write(index);
                        bw.Write(linkIndex);
                        bw.Write(node.distances[l]);
                        bw.Write(node.classifications[l]);
					}
                }
            }
		}
    }

    private static GraphNode CreateNode(Dictionary<int, GraphNode> nodes, int id, double lon, double lat, int value, GraphData graph)
    {
        GraphNode node = new GraphNode(lon, lat, value);
		node.index = id;
		nodes.Add(id, node);
        graph.nodes.Add(node);

        graph.west = Math.Min(graph.west, lon);
        graph.east = Math.Max(graph.east, lon);
        graph.north = Math.Max(graph.north, lat);
        graph.south = Math.Min(graph.south, lat);

        return node;
    }

    private static void AddHalfSizeToGraph(GraphData graph)
    {
        double halfCellWidth = graph.cellSizeX * 0.5;
        double halfCellHeight = graph.cellSizeY * 0.5;

        graph.east += halfCellWidth;
        graph.west -= halfCellWidth;
        graph.north += halfCellHeight;
        graph.south -= halfCellHeight;
    }

    private static void AddNode(GraphData graph, Dictionary<int, GraphNode> nodes, int sourceId, int targetId, double x1, double y1, double x2, double y2, float distance, int value)
    {
        GraphNode sourceNode, targetNode;

        if (nodes.ContainsKey(sourceId))
        {
            if (value != nodes[sourceId].value)  //merge classifications
            {
                nodes[sourceId].value |= value;
            }
            sourceNode = nodes[sourceId];
        }
        else
        {
            sourceNode = CreateNode(nodes, sourceId, x1,y1, value, graph);
        }

        if (nodes.ContainsKey(targetId))
        {
            if (value != nodes[targetId].value)
            {
                nodes[targetId].value |= value;
            }
            targetNode = nodes[targetId];
        }
        else
        {
            targetNode = CreateNode(nodes, targetId, x2, y2, value, graph);
        }

        GraphNode.AddLink(sourceNode, targetNode, distance, value);

		// Find out smallest distance (9e-6 is ~1 meter)
		double dist = Math.Abs(targetNode.longitude - sourceNode.longitude);
		if (dist > 0.00001)
			graph.cellSizeX = Math.Min(graph.cellSizeX, dist);

		dist = Math.Abs(targetNode.latitude - sourceNode.latitude);
		if (dist > 0.00001)
			graph.cellSizeY = Math.Min(graph.cellSizeY, dist);
	}

}
