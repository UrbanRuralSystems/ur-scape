// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;

public class LayerGroup
{
    public string name { get; private set; }

	public readonly List<DataLayer> layers = new List<DataLayer>();

    public LayerGroup(string name)
    {
		ChangeName(name);
    }

	public void ChangeName(string name)
	{
		this.name = name;
	}

	public void AddLayer(DataLayer layer)
	{
		layers.Add(layer);
	}

	public void RemoveLayer(DataLayer layer)
	{
		layers.Remove(layer);
	}

	public int IndexOf(DataLayer layer)
	{
		return layers.IndexOf(layer);
	}

	public void MoveLayerToGroup(DataLayer layer, LayerGroup otherGroup)
	{
		layer.Group.RemoveLayer(layer);
		otherGroup.AddLayer(layer);
	}

	public int MoveLayerUp(DataLayer layer)
	{
		return layers.MoveForward(layer);
	}

	public int MoveLayerDown(DataLayer layer)
	{
		return layers.MoveBack(layer);
	}

	public DataLayer GetLayer(string layerName)
	{
		foreach (var layer in layers)
		{
			if (layer.Name.EqualsIgnoreCase(layerName))
				return layer;
		}
		return null;
	}
}
