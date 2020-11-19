// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Text.RegularExpressions;

public static class CsvHelper
{
	public const char Delimiter = ',';

	// CSV Regex: (?<=^|,)(?=[^"]|(")?)"?((?(1)(?>[^"]+|"")+|[^,"]*))"?(?=,|$)
	public static readonly Regex regex = new Regex("(?<=^|,)(?=[^\"]|(\")?)\"?((?(1)(?>[^\"]+|\"\")+|[^,\"]*))\"?(?=,|$)");

	public static readonly char[] CsvTokens = new[] { '\"', ',', '\n', '\r' };

	public static string Escape(string str)
	{
		if (str.IndexOfAny(CsvTokens) >= 0)
			return "\"" + str.Replace("\"", "\"\"") + "\"";
		return str;
	}

	public static string Unescape(string str)
	{
		return str.Replace("\"\"", "\"");
	}
}

public static class TsvHelper
{
	public const char Delimiter = '\t';

	// CSV Regex: (?<=^|\t)(?=[^"]|(")?)"?((?(1)(?>[^"]+|"")+|[^\t"]*))"?(?=\t|$)
	public static readonly Regex regex = new Regex("(?<=^|\\t)(?=[^\"]|(\")?)\"?((?(1)(?>[^\"]+|\"\")+|[^\\t\"]*))\"?(?=\\t|$)");

	public static readonly char[] TsvTokens = new[] { '\"', '\t', '\n', '\r' };

	public static string Escape(string str)
	{
		if (str.IndexOfAny(TsvTokens) >= 0)
			return "\"" + str.Replace("\"", "\"\"") + "\"";
		return str;
	}

	public static string Unescape(string str)
	{
		return str.Replace("\"\"", "\"");
	}
}
