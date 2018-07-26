// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Text.RegularExpressions;

public class CsvHelper
{
	// CSV Regex: (?:^|,)(?=[^"]|(")?)"?((?(1)(?>[^"]+|"")+|[^,"]*))"?(?=,|$)
	public static readonly Regex regex = new Regex("(?:^|,)(?=[^\"]|(\")?)\"?((?(1)(?>[^\"]+|\"\")+|[^,\"]*))\"?(?=,|$)");
}
