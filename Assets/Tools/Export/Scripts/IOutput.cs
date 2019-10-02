// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli

using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

public interface IOutput
{
    void OutputToCSV(TextWriter csv);
}
