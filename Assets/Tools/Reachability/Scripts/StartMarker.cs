// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public class StartMarker : MonoBehaviour
{
    protected Coordinate coords;
	private static readonly Quaternion kRotation = Quaternion.Euler(-90, 0, 0);

	public void Init(Coordinate coords)
    {
        this.coords = coords;
	}

	public void Show(bool show)
    {
        gameObject.SetActive(show);
    }

	public void UpdatePosition(MapController map, Quaternion cameraRotation)
    {
        var position = map.GetUnitsFromCoordinates(coords);
        transform.localPosition = position;
		transform.localRotation = kRotation * cameraRotation;
	}
}

