// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)


using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

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
        var highwaySet = new HashSet<int>();
        var highwayList = new List<int>();
        var cultureInfo = CultureInfo.InvariantCulture;

		// Read/skip header
		data.sr.ReadLine();

        // Read each data row at a time
        string line;
        while ((line = data.sr.ReadLine()) != null)
        {
            string[] cells = line.Split(',');

			int sourceId = int.Parse(cells[1]);
            int targetId = int.Parse(cells[2]);

            if (sourceId == targetId)
                continue;

            int classification = int.Parse(cells[7]);
            
			float distance = float.Parse(cells[0], cultureInfo);
            double x1 = double.Parse(cells[3], cultureInfo);
            double y1 = double.Parse(cells[4], cultureInfo);
            double x2 = double.Parse(cells[5], cultureInfo);
            double y2 = double.Parse(cells[6], cultureInfo);

			if (classification >= ClassificationValue.Highway)
			{
				// Use a negative sourceId & targetId to indicate another layer            
				AddLink(graph, nodes, -sourceId, -targetId, x1, y1, x2, y2, distance, ClassificationValue.Highway);
                classification -= ClassificationValue.Highway;
            }

            if (classification > 0)
			{
                AddLink(graph, nodes, sourceId, targetId, x1, y1, x2, y2, distance, classification);
			}
		}

        AddHalfSizeToGraph(graph);

        // Once the graph is fully loaded, each node's index will be updated
        graph.indexToNode.Clear();

        double kX = 1.0 / graph.cellSizeX;
        double kY = 1.0 / graph.cellSizeY;
        int countX = (int)Math.Round((graph.east - graph.west) * kX);
        for (int i = graph.nodes.Count - 1; i >= 0; i--)
        {
            var node = graph.nodes[i];
            int index = (int)((node.longitude - graph.west) * kX) + countX * (int)((graph.north - node.latitude) * kY);

            // Highway and other road are on the different layer, they could be overlapped
            if (node.classifications == ClassificationValue.Highway)  // 16
            {
                if (!highwaySet.Contains(index))
                {
                    highwaySet.Add(index);
                    highwayList.Add(index);
                }
                index = -index;
            }

            if (!graph.indexToNode.ContainsKey(index))
                graph.indexToNode.Add(index, node);

            node.index = index;
        }

        // Connect Highways to the other roads with 'HighwayLink' links
        foreach (var idx in highwayList)
        {
            // Does it have both Highway and HighwayLink?
            if (graph.indexToNode.TryGetValue(idx, out GraphNode linkNode) &&
                linkNode.classifications >= ClassificationValue.HighwayLink)
            {
                // Connect highwayNode to linkNode's neighbours (only if the links are HighwayLink)
                var highwayNode = graph.indexToNode[-idx];
                for (int i = linkNode.links.Count - 1; i >= 0; --i)
                {
                    if (linkNode.linkClassifications[i] == ClassificationValue.HighwayLink)
                    {
                        GraphNode.AddLink(highwayNode, linkNode.links[i], linkNode.linkDistances[i], ClassificationValue.HighwayLink);
                        GraphNode.RemoveLink(linkNode, linkNode.links[i], ClassificationValue.HighwayLink);
                    }
                }

                if (linkNode.links.Count > 0)
				{
                    // Remove the 'HighwayLink' classification from the linkNode
                    linkNode.classifications &= ~ClassificationValue.HighwayLink;
                }
                else
                {
                    // Remove the linkNode if all of its links were removed
                    graph.nodes.Remove(linkNode);
                    graph.indexToNode.Remove(idx);
                }
            }
        }

        RemoveExtraLongLinks(graph);

        var grid = new GridData();
        graph.InitGrid(grid);
        graph.CreateDefaultGrid(grid);

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
            GraphNode node = new GraphNode(br.ReadDouble(), br.ReadDouble(), br.ReadInt32(), br.ReadInt32());
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

        RemoveExtraLongLinks(graph);
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
                bw.Write(node.classifications);
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
                        bw.Write(node.linkDistances[l]);
                        bw.Write(node.linkClassifications[l]);
					}
                }
            }
		}
    }

    private static GraphNode CreateNode(Dictionary<int, GraphNode> nodes, int id, double lon, double lat, int classification, GraphData graph)
    {
        GraphNode node = new GraphNode(lon, lat, classification, id);
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

    private static void AddLink(GraphData graph, Dictionary<int, GraphNode> nodes, int sourceId, int targetId, double x1, double y1, double x2, double y2, float distance, int classification)
    {
        if (nodes.TryGetValue(sourceId, out GraphNode sourceNode))
            sourceNode.classifications |= classification;   // Merge classifications
        else
            sourceNode = CreateNode(nodes, sourceId, x1, y1, classification, graph);

        if (nodes.TryGetValue(targetId, out GraphNode targetNode))
            targetNode.classifications |= classification;   // Merge classifications
        else
            targetNode = CreateNode(nodes, targetId, x2, y2, classification, graph);

        GraphNode.AddLink(sourceNode, targetNode, distance, classification);

		// Find out smallest distance (9e-6 is ~1 meter)
		double dist = Math.Abs(x2 - x1);
		if (dist > 0.00001)
			graph.cellSizeX = Math.Min(graph.cellSizeX, dist);

		dist = Math.Abs(y2 - y1);
		if (dist > 0.00001)
			graph.cellSizeY = Math.Min(graph.cellSizeY, dist);
	}

    private static void RemoveExtraLongLinks(GraphData graph)
    {
        int countX = (int)Math.Round((graph.east - graph.west) / graph.cellSizeX);

        var ne = -countX - 1;
        var n = -countX;
        var nw = -countX + 1;
        var e = -1;
        var w = +1;
        var se = countX - 1;
        var s = countX;
        var sw = countX + 1;

        var emptyNodes = new List<GraphNode>();
        int removedLinks = 0;
        int removedNodes = 0;

        for (int i = graph.nodes.Count - 1; i >= 0; --i)
        {
            var node = graph.nodes[i];
            if (node.links.Count == 0)
            {
                removedNodes++;
                graph.nodes.RemoveAt(i);
                graph.indexToNode.Remove(node.index);
            }
            else
            {
                var nodeIndex = Math.Abs(node.index);
                for (var j = node.links.Count - 1; j >= 0; --j)
                {
                    var neighbour = node.links[j];
                    var indexOffset = Math.Abs(neighbour.index) - nodeIndex;
                    if (indexOffset != ne &&
                        indexOffset != n &&
                        indexOffset != nw &&
                        indexOffset != e &&
                        indexOffset != w &&
                        indexOffset != se &&
                        indexOffset != s &&
                        indexOffset != sw)
                    {
                        removedLinks++;
                        node.RemoveLink(j);
                        if (neighbour.links.Count == 0)
                            emptyNodes.Add(neighbour);
                    }
                }
                if (node.links.Count == 0)
                {
                    removedNodes++;
                    graph.nodes.RemoveAt(i);
                    graph.indexToNode.Remove(node.index);
                }
            }
        }
        
        removedNodes += emptyNodes.Count;

        foreach (var node in emptyNodes)
        {
            if (graph.indexToNode.Remove(node.index))
                graph.nodes.Remove(node);
        }

        if (removedLinks != 0 || removedNodes != 0)
		{
            Debug.LogWarning("Removed " + removedLinks + " extra long links. Removed " + removedNodes + " nodes");
        }
    }
}
