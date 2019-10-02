// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Events;

public class MunicipalityData
{
    public readonly int[] ids;
    public readonly Dictionary<int, string> idToName;

	public MunicipalityData(int[] list, Dictionary<int, string> map)
	{
		ids = list;
		idToName = map;
	}
}

public static class MunicipalityIO
{
    public static IEnumerator Load(string filename, UnityAction<MunicipalityData> callback, UnityAction errCallback)
    {
        yield return FileRequest.GetText(filename, (sr) => callback(Parse(sr)), errCallback);
    }

    private static MunicipalityData Parse(StreamReader sr)
    {
		List<int> ids = new List<int>();
		Dictionary<int, string> idToName = new Dictionary<int, string>();

		int id = 0;
		string line = sr.ReadLine();
        while ((line = sr.ReadLine()) != null)
        {
            string[] cells = line.Split(',');
            if (cells[0].EqualsIgnoreCase("CATEGORIES"))
            {
				while ((line = sr.ReadLine()) != null)
				{
					cells = line.Split(',');
					if (cells[0].EqualsIgnoreCase("VALUE"))
					{
						while ((line = sr.ReadLine()) != null)
						{
							cells = line.Split(',');
							if (int.TryParse(cells[0], out id))
								ids.Add(id);
						}
						break;
					}
					if (int.TryParse(cells[1], out id))
						idToName.Add(id, cells[0]);
				}
            }
        }

		return new MunicipalityData(ids.ToArray(), idToName);
	}

}

