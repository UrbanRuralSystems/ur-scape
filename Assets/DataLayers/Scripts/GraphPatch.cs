// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using System.Collections;

public class GraphPatch : GridedPatch
{
    public readonly GraphData graph;

    protected GraphPatch(DataLayer dataLayer, string name, int level, int year, GraphData graph, string filename)
        : base(dataLayer, name, level, year, new GridData(), filename)
    {
        this.graph = graph;
        graph.patch = this;

        graph.InitGrid(grid);
    }

    //
    // Public Methods
    //

    public static GraphPatch Create<D>(DataLayer dataLayer, string site, int level, int year, D data, string filename) where D: GraphData
    {
        return new GraphPatch(dataLayer, site, level, year, data, filename);
    }

    public override IEnumerator LoadData(PatchLoadedCallback callback)
    {
		bool loaded = false;
		yield return graph.LoadBin(Filename, (g) => loaded = true);

		if (loaded)
		{
			callback(this);
		}
    }

    public override void UnloadData()
    {
        graph.UnloadData();
		grid.UnloadData();
	}

    public override void Save(string filename, PatchDataFormat format)
    {
        switch (format)
        {
            case PatchDataFormat.BIN:
                graph.SaveBin(filename);
				Filename = filename;
                break;
            default:
                Debug.LogError("Not implemented");
                break;
        }
    }
    
    public int GetIndex(GraphNode node)
    {
        return (int)((node.longitude - graph.west) / graph.cellSizeX) + grid.countX * (int)((graph.north - node.latitude) / graph.cellSizeY);
    }

	public int GetIndex(double lon, double lat)
	{
		return (int)((lon - graph.west) / graph.cellSizeX) + grid.countX * (int)((graph.north - lat) / graph.cellSizeY);
	}

	public void CreateDefaultGrid()
	{
		graph.CreateDefaultGrid(grid);
	}
	
}
