// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;

public class LayerGroup
{
    public string name;
    public List<DataLayer> layers = new List<DataLayer>();

    public LayerGroup(string name)
    {
        this.name = name;
    }
}
