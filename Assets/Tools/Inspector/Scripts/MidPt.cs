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
public class MidPt : ControlPt
{
	private Color offColor;
	private Vector3 oldInputPos = Vector3.zero;
	private Vector3 diff0 = Vector3.zero;
	private Vector3 diff1 = Vector3.zero;

	// Component References
	private Image image;
    private RectTransform rectTransform;

	protected override void Start()
    {
        base.Start();

        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        rectTransform.sizeDelta = new Vector2(image.sprite.texture.width * 0.5f, image.sprite.texture.height * 0.5f);

        offColor = new Color(image.color.r, image.color.g, image.color.b, 0.0f);
		image.color = offColor;

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
			eventID = EventTriggerType.BeginDrag
		};
		click.callback.AddListener((data) => { OnClickDelegate((PointerEventData)data); });
		trigger.triggers.Add(click);
	}

	protected override void UpdatePosOnDrag()
	{
		Vector3 inputMousePos = Input.mousePosition;
		Vector3 newStart = inputMousePos + diff0;
		Vector3 newEnd = inputMousePos + diff1;

		// Update position of control pts of line taking into account of screen size
		LineInfo.controlPtsTB[0].transform.localPosition = LineInfo.mapViewAreaChanged ?
														   (
															!lineInfo.scaleFactor.Equals(1.0f) ? newStart * lineInfo.scaleFactor :
																								 newStart / canvas.scaleFactor
														   )
														   : newStart;
		LineInfo.controlPtsTB[1].transform.localPosition = LineInfo.mapViewAreaChanged ?
														   (
															!lineInfo.scaleFactor.Equals(1.0f) ? newEnd * lineInfo.scaleFactor :
																								 newEnd / canvas.scaleFactor
														   )
														   : newEnd;

		LineInfo.inspectionDelete.transform.localPosition = (newStart + newEnd) * 0.5f;

		bool show = inspectorTool.IsPosWithinMapViewArea(newStart);
		LineInfo.controlPtsTB[0].gameObject.SetActive(show);
		show = inspectorTool.IsPosWithinMapViewArea(newEnd);
		LineInfo.controlPtsTB[1].gameObject.SetActive(show);

		// Update position of line
		inputHandler.GetWorldPoint(newStart, out Vector3 newStartWorldPos);
		inputHandler.GetWorldPoint(newEnd, out Vector3 newEndWorldPos);

		lineInspectorPanel.UpdateLineEndPtAndMidPtFromWorldPos(LineInfo, 0, newStartWorldPos);
        lineInspectorPanel.UpdateLineEndPtAndMidPtFromWorldPos(LineInfo, 1, newEndWorldPos);
		lineInspector.UpdateInspectionDelete(LineInfo);
	}
	
	//
	// Private Methods
	//

	private void OnDragDelegate(PointerEventData _)
	{
        if (!inspectorTool.IsPosWithinMapViewArea(Input.mousePosition))
            return;

		UpdatePosOnDrag();
		UpdateCurrLineInspectionIndex();
    }

    private void OnClickDelegate(PointerEventData _)
	{
		oldInputPos = Input.mousePosition;
		diff0 = Camera.main.WorldToScreenPoint(lineInfo.line.GetPosition(0)) - oldInputPos;
		diff1 = Camera.main.WorldToScreenPoint(lineInfo.line.GetPosition(1)) - oldInputPos;
	}
}
