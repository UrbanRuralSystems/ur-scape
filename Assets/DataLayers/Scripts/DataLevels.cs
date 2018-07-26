// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

[System.Serializable]
public class DataLevel
{
    public string name;
    public float minZoom;
    public float maxZoom;
}

[CreateAssetMenu(menuName = "URS/DataLevels")]
public class DataLevels : ScriptableObject
{

    public DataLevel[] levels;

	private void OnEnable()
	{
		if (levels != null && levels.Length != DataLayer.MaxLevels)
		{
			Debug.LogError(name + "'s levels don't match DataLayer's MaxLevels (" + DataLayer.MaxLevels + ")");
		}
	}

	public int GetDataLevelIndex(float zoom)
    {
        for (int i = levels.Length - 1; i >= 0; i--)
        {
            if (zoom >= levels[i].minZoom &&
                zoom <= levels[i].maxZoom)
            {
                return i;
            }
        }

        Debug.LogError("Trying to get level index with out-of-bounds zoom value: " + zoom);
        return (zoom < levels[0].minZoom)? 0 : levels.Length - 1;
    }

    public DataLevel GetDataLevel(float zoom)
    {
        for (int i = levels.Length - 1; i >= 0; i--)
        {
            if (zoom >= levels[i].minZoom &&
                zoom <= levels[i].maxZoom)
            {
                return levels[i];
            }
        }
        return null;
    }

}
