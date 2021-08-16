// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using LineInfo = LineInspector.LineInspectorInfo;

public class LineInspectionDelete : MonoBehaviour
{
    [Header("UI References")]
    public Sprite inspectionDelete;
    public Sprite inspectionDeleteSolid;
    public Button button;

    // Component References
    private Image image;
    private RectTransform rectTransform;
    private HoverHandler hoverHandler;
    private MapViewArea mapViewArea;
    private InspectorTool inspectorTool;
    private LineInfo lineInfo;
    private LineInspectorPanel lineInspectorPanel;

    private void Start()
    {
        // Get Components
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        mapViewArea = ComponentManager.Instance.Get<MapViewArea>();
        inspectorTool = ComponentManager.Instance.Get<InspectorTool>();
        lineInspectorPanel = inspectorTool.lineInspectorPanel;

        rectTransform.sizeDelta = new Vector2(inspectionDelete.texture.width * 0.5f, inspectionDelete.texture.height * 0.5f);

        // Add hover event
        hoverHandler = GetComponent<HoverHandler>();
        hoverHandler.OnHover += OnPointerHover;

        // Add click event
        EventTrigger trigger = GetComponent<EventTrigger>();
        EventTrigger.Entry click = new EventTrigger.Entry();
        click.eventID = EventTriggerType.PointerClick;
        click.callback.AddListener((data) => { lineInspectorPanel.OnRemoveLineInspection(lineInfo); });
        trigger.triggers.Add(click);
    }

    //
    // Event Methods
    //

    private void OnPointerHover(bool isHovering)
    {
        if (!isHovering && !IsMouseInside(rectTransform))
        {
            image.sprite = inspectionDelete;
        }
        else
        {
            image.sprite = inspectionDeleteSolid;
        }
    }

    //
    // Public Methods
    //

    public void ShowInspectionDelete(bool show)
    {
        gameObject.SetActive(show);
    }

    public void UpdatePosition(Vector3 midPt)
    {
        transform.localPosition = midPt;
    }

    public void SetLineInfo(LineInfo lineInfo)
    {
        this.lineInfo = lineInfo;
    }

    public bool IsRectWithinMapViewArea(Vector3 position)
    {
		if (rectTransform == null)
			return false;

        Vector3 topLeft = rectTransform.offsetMin;
        Vector3 bottomLeft = new Vector3(rectTransform.offsetMin.x, rectTransform.offsetMax.y, position.z);
        Vector3 topRight = new Vector3(rectTransform.offsetMax.x, rectTransform.offsetMin.y, position.z);
        Vector3 bottomRight = rectTransform.offsetMax;

        bool isWithinMapViewArea = mapViewArea.Contains(topLeft) && mapViewArea.Contains(bottomLeft) &&
                                   mapViewArea.Contains(topRight) && mapViewArea.Contains(bottomRight);
        return isWithinMapViewArea;
    }

    //
    // Private Methods
    //

    private bool IsMouseInside(RectTransform rt)
    {
        return rt.rect.Contains(rt.InverseTransformPoint(Input.mousePosition));
    }
}