// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class EndPt : ControlPt
{
	[Header("UI References")]
	public Sprite knobSmall;
    public Sprite knobLarge;
    public Sprite knobSolid;

    // Component References
    private Image image;
    private RectTransform rectTransform;
	private HoverHandler hoverHandler;
	private MapController mapController;

	protected override void Start()
    {
        base.Start();

        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        rectTransform.sizeDelta = new Vector2(knobSmall.texture.width * 0.5f, knobSmall.texture.height * 0.5f);

		// Add hover event
		hoverHandler = GetComponent<HoverHandler>();
		hoverHandler.OnHover += OnPointerHover;

		// Add drag and click events
		EventTrigger trigger = GetComponent<EventTrigger>();
		EventTrigger.Entry drag = new EventTrigger.Entry
		{
			eventID = EventTriggerType.Drag
		};
		drag.callback.AddListener((data) => { OnDragDelegate((PointerEventData)data); });
		trigger.triggers.Add(drag);

		EventTrigger.Entry click = new EventTrigger.Entry
		{
			eventID = EventTriggerType.PointerClick
		};
		click.callback.AddListener((data) => { OnClickDelegate((PointerEventData)data); });
		trigger.triggers.Add(click);

		mapController = ComponentManager.Instance.Get<MapController>();
	}

	protected override void UpdatePosOnDrag()
	{
		if (!inspectorTool.IsPosWithinMapViewArea(Input.mousePosition))
			return;

		Vector3 screenPos = Input.mousePosition;
		// Update position of control pts of line taking into account of screen size
		transform.localPosition = lineInfo.mapViewAreaChanged ?
								  (
									lineInfo.scaleFactor.Equals(1.0f) ? screenPos / canvas.scaleFactor :
																	    screenPos * lineInfo.scaleFactor
								  )
								  : screenPos;
		inputHandler.GetWorldPoint(screenPos, out Vector3 worldPos);

		lineInspectorPanel.UpdateLineEndPtAndMidPtFromWorldPos(LineInfo, ControlPointIndex, worldPos);
        lineInspector.UpdateInspectionDelete(LineInfo);
        lineInspectorPanel.ComputeAndUpdateLineProperties();
	}

	//
	// Public Methods
	//

	public override void UpdatePositionAndLinePtsFromCoord()
	{
        if (mapController == null)
            return;
        
		var coordToWorldPos = mapController.GetUnitsFromCoordinates(coords);
		Vector3 worldPos = new Vector3(coordToWorldPos.x, coordToWorldPos.z, coordToWorldPos.y);
		Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        
		lineInfo.line.SetPosition(controlPtIndex, worldPos);
		// Update position of control pts of line taking into account of screen size
		transform.localPosition = lineInfo.mapViewAreaChanged ?
								  (
									lineInfo.scaleFactor.Equals(1.0f) ? screenPos / canvas.scaleFactor :
																	    screenPos * lineInfo.scaleFactor
								  )
								  : screenPos;
	}

	public void KnobSmall()
    {
        image.sprite = knobSmall;
        rectTransform.sizeDelta = new Vector2(knobSmall.texture.width * 0.5f, knobSmall.texture.height * 0.5f);
    }

    public void KnobLarge()
    {
        if (lineInspector.CurrLineInspection != InspectionIndex)
            return;
        image.sprite = knobLarge;
        rectTransform.sizeDelta = new Vector2(knobLarge.texture.width * 0.5f, knobLarge.texture.height * 0.5f);
    }

    public void KnobSolid()
    {
        image.sprite = knobSolid;
        rectTransform.sizeDelta = new Vector2(knobSolid.texture.width * 0.5f, knobSolid.texture.height * 0.5f);
    }

	//
	// Private Methods
	//

	private bool IsMouseInside(RectTransform rt)
	{
		return rt.rect.Contains(rt.InverseTransformPoint(Input.mousePosition));
	}

	private void OnPointerHover(bool isHovering)
	{
		if (!isHovering && !IsMouseInside(rectTransform))
		{
			if (image.sprite == knobSolid)
				return;

			KnobSmall();
		}
		else
		{
			if (image.sprite == knobSolid)
				return;

			KnobLarge();
		}
	}

	private void OnDragDelegate(PointerEventData data)
	{
		UpdatePosOnDrag();
		UpdateCurrLineInspectionIndex();
		KnobLarge();
	}

	private void OnClickDelegate(PointerEventData data)
	{
		if (image.sprite == knobSolid)
			return;

		UpdateCurrLineInspectionIndex();
		KnobLarge();
	}
}
