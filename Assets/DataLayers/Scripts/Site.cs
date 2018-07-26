// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;

public class Site
{
	public readonly string name;
	public readonly AreaBounds bounds;
	public readonly List<DataLayer> dataLayers;
	public readonly Site parent;

	public Site(string name, Site parent, AreaBounds bounds, List<DataLayer> dataLayers)
	{
		this.name = name;
		this.bounds = bounds;
		this.dataLayers = dataLayers;
		this.parent = parent;
	}

	public bool HasDataLayer(DataLayer dataLayer)
	{
		return dataLayers.Contains(dataLayer);
	}
}

public class SiteCreator
{
	public string name;
	public string parent;
	public AreaBounds bounds;
	public List<DataLayer> layers = new List<DataLayer>();
	public List<LayerSite> layerSites = new List<LayerSite>();
}
