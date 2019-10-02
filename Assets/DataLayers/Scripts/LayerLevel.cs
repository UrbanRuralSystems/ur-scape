// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;

public class LayerLevel
{
	public readonly List<LayerSite> layerSites = new List<LayerSite>();

	public LayerSite AddSite(Site site, Patch patch)
	{
		var layerSite = new LayerSite(site, patch);
		layerSites.Add(layerSite);
		return layerSite;
	}
}