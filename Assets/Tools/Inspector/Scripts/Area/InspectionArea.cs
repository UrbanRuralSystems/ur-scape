// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;

using AreaInfo = AreaInspector.AreaInspectorInfo;

public class InspectionArea : MonoBehaviour
{
    // Component References
    private InputHandler inputHandler;
    private InspectorTool inspectorTool;
    private MapController mapController;
    private AreaInfo areaInfo;
    private AreaInspectorPanel areaInspectorPanel;
    private AreaInspector areaInspector;

    //
    // Unity Methods
    //

    private void Start()
    {
        inputHandler = ComponentManager.Instance.Get<InputHandler>();
        inspectorTool = ComponentManager.Instance.Get<InspectorTool>();
        mapController = ComponentManager.Instance.Get<MapController>();

        areaInspectorPanel = inspectorTool.areaInspectorPanel;
        areaInspector = areaInspectorPanel.areaInspector;
    }

    private void Update()
    {
        // Incomplete area or in delete or draw mode
        if (areaInspectorPanel.removeAreaInspectionToggle.isOn ||
            areaInspectorPanel.createAreaInspectionToggle.isOn ||
            areaInspector.CurrAreaInspection == -1)
            return;

        var mousePosition = Input.mousePosition;
        inputHandler.GetWorldPoint(mousePosition, out Vector3 point);
        bool isCursorInPoly = areaInfo.mapLayer.IsPointInPoly(mapController.GetCoordinatesFromUnits(point.x, point.z), areaInfo.mapLayer.Points);

        if (areaInfo != areaInspectorPanel.areaInfos[areaInspector.CurrAreaInspection])
        {
            if (inspectorTool.IsPosWithinMapViewArea(mousePosition) && isCursorInPoly)
            {
                areaInspectorPanel.ChangeAreaLine(areaInfo, false);
                if (inputHandler.IsLeftMouseDown)
                    SelectArea();
            }
            else
                areaInspectorPanel.ChangeAreaLine(areaInfo, true);
        }
    }

    //
    // Public Methods
    //

    public void SetAreaInfo(AreaInfo areaInfo)
    {
        this.areaInfo = areaInfo;
    }

    //
    // Private Methods
    //

    private void SelectArea()
    {
        areaInspectorPanel.SetCurrInspection(areaInfo.inspectionIndex);
		areaInspectorPanel.ComputeAndUpdateAreaProperties();
    }
}