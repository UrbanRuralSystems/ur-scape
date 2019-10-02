// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//          Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;

public class PlanningCell
{
    public enum IconType
    {
        Default,
        Large,
        Hidden
    }

    public readonly Coordinate coords;
    public readonly int x;
    public readonly int y;
    public readonly int index;
    public Typology typology;
	public readonly double areaSqM;
    public Dictionary<string, float> typologyValues = new Dictionary<string, float>();
    public Dictionary<string, float> gridValues = new Dictionary<string, float>();
    private Transform icon;

    public int group = 0;
    public IconType iconType;

	private Vector3 position;
	public Vector3 Position { get { return position; } }


	public PlanningCell(Typology typology, Coordinate coords, int index, int x, int y, GridData grid)
    {
        this.typology = typology;
        this.coords = coords;
        this.index = index;
        this.x = x;
        this.y = y;

		areaSqM = grid.GetCellSquareMeters(y);

		typologyValues = UpdateDataScale(typology.values);
    }

    public void UpdateTypology(Typology typology)
    {
        this.typology = typology;
        typologyValues = UpdateDataScale(typology.values);
    }

    public void UpdateGridAttributes(Dictionary<string, float> values)
    {
        gridValues = UpdateDataScale(values);
    }

    public void UpdateIconPosition(MapController map)
    {
        if (icon)
        {
            float size = map.MetersToUnits;
            icon.localScale = new Vector3(size, size, size);
            icon.localPosition = map.GetUnitsFromCoordinates(coords);
        }
    }
    
    public void SetIcon(Transform icon)
    {
        this.icon = icon;
        icon.name = typology.name;
    }

    public void ClearIcon()
    {
        if (icon != null)
        {
            Object.Destroy(icon.gameObject);
            icon = null;
        }
    }

	public virtual void UpdatePosition(MapController map)
	{
		position = map.GetUnitsFromCoordinates(coords);
		position.z = position.y;
		position.y = map.MetersToUnits;
	}


	//
	// Private Methods
	//

	private Dictionary<string, float> UpdateDataScale(Dictionary<string, float> dataToUpdate)
    {
        Dictionary<string, float> UpdatedData = new Dictionary<string, float>();

		float factor = (float)(areaSqM * 0.01); // 0.01 means dividing by 100m2 (typology values are per 100m2)

		foreach (var pair in dataToUpdate)
        {
			UpdatedData.Add(pair.Key, pair.Value * factor);
		}

        return UpdatedData;
    }
}
