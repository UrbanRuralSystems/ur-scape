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

public class InspectorToggle : MonoBehaviour
{
	[Header("UI References")]
	public Toggle toggle;
	public Button button;
    public Text letter;
	public InputField label;

	[Header("Images for each state")]
	public Sprite disabled;
	public Sprite toggledOn;
	public Sprite toggledOff;

    [Header("Miscellaneous")]
    public string initPlacehoderText = "Empty Inspection";

	private EventTrigger trigger = null;
	private EventTrigger.Entry enter = null;
	private EventTrigger.Entry exit = null;

	private InspectorTool inspectorTool;

	private LineInspector.LineInspectorInfo lineInfo;
	public LineInspector.LineInspectorInfo LineInfo
	{
		get { return this.lineInfo; }
		set { this.lineInfo = value; }
	}

	private AreaInspector.AreaInspectorInfo areaInfo;
	public AreaInspector.AreaInspectorInfo AreaInfo
	{
		get { return this.areaInfo; }
		set { this.areaInfo = value; }
	}

	//
	// Unity Methods
	//

	private void Start()
	{
		trigger = GetComponent<EventTrigger>();
		enter = new EventTrigger.Entry();
		enter.eventID = EventTriggerType.PointerEnter;
		exit = new EventTrigger.Entry();
		exit.eventID = EventTriggerType.PointerExit;

		inspectorTool = ComponentManager.Instance.Get<InspectorTool>();
	}

	private void OnEnable()
	{
		UpdateToggle();
	}

	//
	// Public Methods
	//

	public bool IsInteractable
	{
		set
		{
			if (!value && button.gameObject.activeSelf)
				AllowRemove(false);

			toggle.interactable = value;
			label.interactable = value;
			UpdateToggle();
		}
	}

	public void AllowRemove(bool allow)
	{
		if (!toggle.interactable)
			return;

		toggle.image.enabled = !allow;
        letter.gameObject.SetActive(!allow);
		button.gameObject.SetActive(allow);
	}

    public void ResetToggle()
    {
        IsInteractable = false;
        label.placeholder.GetComponent<Text>().text = initPlacehoderText;
        label.text = "";
        letter.text = "";
    }

	public void UpdateInspectorToggle(bool currInspector)
	{
		Color color = (!currInspector) ?
					  toggle.colors.normalColor :
					  Color.black;

		Sprite img = (toggle.IsInteractable()) ?
					 ((currInspector) ? toggledOn : toggledOff) : disabled;

		letter.color = color;
		toggle.image.sprite = img;
	}

	public void AddOnPointerEnterEvent()
	{
		enter.callback.AddListener((data) => { OnPointerEnterEventDelegate((PointerEventData)data); });
		trigger.triggers.Add(enter);
	}

	public void RemoveOnPointerEnterEvent()
	{
		trigger.triggers.Remove(enter);
		enter.callback.RemoveListener((data) => { OnPointerEnterEventDelegate((PointerEventData)data); });
	}

	public void AddOnPointerExitEvent()
	{
		exit.callback.AddListener((data) => { OnPointerExitEventDelegate((PointerEventData)data); });
		trigger.triggers.Add(exit);
	}

	public void RemoveOnPointerExitEvent()
	{
		trigger.triggers.Remove(exit);
		exit.callback.RemoveListener((data) => { OnPointerExitEventDelegate((PointerEventData)data); });
	}

	//
	// Private Methods
	//

	private void UpdateToggle()
	{
        if (toggle.interactable)
        {
            toggle.image.sprite = toggle.isOn ? toggledOn : toggledOff;
            letter.color = toggle.isOn ? Color.black : toggle.colors.normalColor;
        }
        else
        {
            toggle.image.sprite = disabled;
            letter.color = toggle.colors.normalColor;
        }
	}

	private void OnPointerEnterEventDelegate(PointerEventData data)
	{
		if (inspectorTool.InspectType == InspectorTool.InspectorType.Line)
		{
			var startPt = (lineInfo.controlPts[0] as EndPt);
			var endPt = (lineInfo.controlPts[1] as EndPt);
			var line = lineInfo.line;

			// Change endpt knobs to small
			startPt.KnobSmall();
			endPt.KnobSmall();

			// Normal line width
			line.widthMultiplier = 0.01f;
		}
		else
		{
			var line = areaInfo.line;
			// Normal line width
			line.widthMultiplier = 0.01f;
		}
	}

	private void OnPointerExitEventDelegate(PointerEventData data)
	{
		if (inspectorTool.InspectType == InspectorTool.InspectorType.Line)
		{
			var startPt = (lineInfo.controlPts[0] as EndPt);
			var endPt = (lineInfo.controlPts[1] as EndPt);
			var line = lineInfo.line;

			// Change endpt knobs to solid
			startPt.KnobSolid();
			endPt.KnobSolid();

			// Thinner line width
			line.widthMultiplier = 0.005f;
		}
		else
		{
			var line = areaInfo.line;
			// Thinner line width
			line.widthMultiplier = 0.005f;
		}
	}
}
