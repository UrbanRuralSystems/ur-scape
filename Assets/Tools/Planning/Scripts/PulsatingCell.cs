// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public class PulsatingCell : MonoBehaviour
{
    private MapController map;
    private Coordinate coords;
    private float cellSize;

    //
    // Unity Methods
    //

    private void Awake()
    {
        map = ComponentManager.Instance.Get<MapController>();
    }

    private void OnDestroy()
    {
        map.OnZoomChange -= OnMapZoomChange;
    }


    //
    // Events
    //

    private void OnMapZoomChange(float zoom)
    {
        UpdateSize();
        UpdatePosition();
    }


    //
    // Public Methods
    //

    public void Init(float cellSize)
    {
        this.cellSize = cellSize * 1.08f;  // Expand the size a little bit
        UpdateSize();

        map.OnZoomChange += OnMapZoomChange;

        gameObject.SetActive(false);
    }

    public void SetCoords(Coordinate coords)
    {
        this.coords = coords;
        UpdatePosition();
    }

    public void Show(bool show)
    {
        gameObject.SetActive(show);
    }

    public void SetColor(Color color)
    {
        GetComponent<MeshRenderer>().material.SetColor("Tint", color);
    }


    //
    // Private Methods
    //

    private void UpdateSize()
    {
        float size = cellSize * map.MetersToUnits;
        transform.localScale = new Vector3(size, size, size);
    }

    private void UpdatePosition()
    {
        Vector3 worldPos = map.GetUnitsFromCoordinates(coords);
        worldPos.z = -0.0005f;
        transform.localPosition = worldPos;
    }

}
