// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
public class PaddedInputField :	InputField
{
	[SerializeField] private RectOffset m_Padding = new RectOffset();
	private float m_PreferredWidth = -1;
	private float m_PreferredHeight = -1;
	private bool updatePadding;

	public class SelectionEvent : UnityEvent<PaddedInputField> { }

	public SelectionEvent onSelected = new SelectionEvent();
	public SelectionEvent onDeselected = new SelectionEvent();

	//
	// Unity Methods
	//

	protected override void OnEnable()
	{
		base.OnEnable();

		onValueChanged.RemoveListener(OnValueChanged);
		if (textComponent != null)
			onValueChanged.AddListener(OnValueChanged);
		UpdateTextPadding();
	}

	protected override void OnDisable()
	{
		base.OnDisable();
		onValueChanged.RemoveListener(OnValueChanged);
	}

#if UNITY_EDITOR
	protected override void OnValidate()
	{
		base.OnValidate();

		// This code covers the following cases:
		//  - when the user has changed the text in the inspector
		//  - when the user has changed the padding in the inspector

		updatePadding = true;
		RebuildLayout();
	}
#endif

	//
	// Overrides
	//

	public override float minWidth { get { return m_Padding.horizontal; } }
	public override float minHeight { get { return m_Padding.vertical; } }
	public override float preferredWidth { get { return m_PreferredWidth; } }
	public override float preferredHeight { get { return m_PreferredHeight; } }

	public override void CalculateLayoutInputHorizontal()
	{
		base.CalculateLayoutInputHorizontal();
		m_PreferredWidth = base.preferredWidth + m_Padding.horizontal;
		UpdateTextPadding();
	}

	public override void CalculateLayoutInputVertical()
	{
		base.CalculateLayoutInputVertical();
		m_PreferredHeight = base.preferredHeight + m_Padding.vertical;
	}

	public override void OnSelect(BaseEventData eventData)
	{
		base.OnSelect(eventData);
		onSelected?.Invoke(this);
	}

	public override void OnDeselect(BaseEventData eventData)
	{
		base.OnDeselect(eventData);
		onDeselected?.Invoke(this);
	}


	//
	// Private Methods
	//

	private void UpdateTextPadding()
	{
		if (updatePadding && m_TextComponent != null)
		{
			var rt = m_TextComponent.rectTransform;
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMax = new Vector2(-m_Padding.right, -m_Padding.top);
			rt.offsetMin = new Vector2(m_Padding.left, m_Padding.bottom);
		}
		updatePadding = false;
	}

	private void OnValueChanged(string s)
	{
		// This event happens when the user changes the text at runtime (via code or keyboard input), not in the inspector
		if (base.preferredHeight != textComponent.rectTransform.rect.height)
		{
			RebuildLayout();
		}
	}

	public Vector2 GetCaretPosition()
	{
		return GetCharacterPosition(caretPosition);
	}

	public Vector2 GetCharacterPosition(int charIndex)
	{
		var textGen = cachedInputTextGenerator;

		var rt = m_TextComponent.rectTransform;
		Vector2 extents = m_TextComponent.rectTransform.rect.size;
		textGen.Populate(text, m_TextComponent.GetGenerationSettings(extents));

		int indexOfTextQuad = charIndex * 4;
		Vector2 pos =(textGen.verts[indexOfTextQuad].position +
			textGen.verts[indexOfTextQuad + 1].position +
			textGen.verts[indexOfTextQuad + 2].position +
			textGen.verts[indexOfTextQuad + 3].position) * 0.25f;

		pos += (Vector2)rt.localPosition;

		return pos;
	}

	private void RebuildLayout()
	{
		if (!IsActive())
			return;
		LayoutRebuilder.MarkLayoutForRebuild(transform as RectTransform);
	}

}
