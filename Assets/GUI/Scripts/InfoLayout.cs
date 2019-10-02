// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[Serializable]
public class ColumnLayoutInfo
{
    public int minWidth;
    public int maxWidth;
	[Tooltip("Extra space added on the right side of the column")]
	public float spacing;
	[Tooltip("Expand this column if there is available space")]
	public bool expandToFill = false;
	[Tooltip("Use either item's preferred width (true) or item's current width (false)")]
	public bool itemPreferredWidth;
	[Tooltip("Use either item's preferred height (true) or item's current height (false)")]
	public bool itemPreferredHeight;
	[Tooltip("Expand item's width to match the max width in the column")]
	public bool expandItemWidth;
	[Tooltip("Expand item's height to match the max height in the row")]
	public bool expandItemHeight;
}

public class InfoLayout : LayoutGroup
{
	[SerializeField]
	protected Vector2 m_Spacing = Vector2.zero;
	public Vector2 spacing { get { return m_Spacing; } set { SetProperty(ref m_Spacing, value); } }

    [SerializeField]
    protected ColumnLayoutInfo[] m_ColumnInfo = { new ColumnLayoutInfo(), new ColumnLayoutInfo() };
    public ColumnLayoutInfo[] columnInfo { get { return m_ColumnInfo; } set { SetProperty(ref m_ColumnInfo, value); } }

    [SerializeField]
	protected bool m_ExpandChildWidth = false;
	public bool expandChildWidth { get { return m_ExpandChildWidth; } set { SetProperty(ref m_ExpandChildWidth, value); } }

    [SerializeField]
    protected bool m_ExpandChildHeight = false;
    public bool expandChildHeight { get { return m_ExpandChildHeight; } set { SetProperty(ref m_ExpandChildHeight, value); } }

    private int columns;
	private int rows;
	private float[] columnWidths = new float[0];
	private float[] rowHeights = new float[0];
	private float[] columnPos = new float[0];
	private float[] rowPos = new float[0];
	private float totalColumnWidth;
	private float totalSpacing;
	private float totalRowHeight;

	public event UnityAction OnLayoutChange;

    private void InitializeLayout()
	{
		var columnsCount = columnInfo.Length;
		columns = Mathf.Min(columnsCount, rectChildren.Count);
		rows = Mathf.CeilToInt(rectChildren.Count / (float)columnsCount);

		if (columns != columnWidths.Length)
		{
			columnWidths = new float[columns];
			columnPos = new float[columns];
		}

        int flexibleColumns = 0;
        for (int col = 0; col < columns; col++)
        {
            columnWidths[col] = columnInfo[col].minWidth;
            if (columnInfo[col].expandToFill)
                flexibleColumns++;
        }

		if (rows != rowHeights.Length)
		{
			rowHeights = new float[rows];
			rowPos = new float[rows];
		}
		else
		{
			Array.Clear(rowHeights, 0, rowHeights.Length);
		}

		for (int cell = 0; cell < rectChildren.Count; cell++)
		{
			int col = cell % columns;

			var child = rectChildren[cell];

			// Calculate child width
			var width = GetWidth(child, col);

			// Clamp child width if necessary
			if (columnInfo[col].maxWidth != 0 && width > columnInfo[col].maxWidth)
			{
				width = columnInfo[col].maxWidth;
				SetChildAlongAxis(child, 0, 0, width);
			}

			columnWidths[col] = Mathf.Max(columnWidths[col], width);
		}

		float pos = padding.left;
		totalColumnWidth = 0;
		totalSpacing = 0;
		for (int col = 0; col < columns; col++)
		{
			totalColumnWidth += columnWidths[col];
			columnPos[col] = pos;
			pos += columnWidths[col] + spacing.x + columnInfo[col].spacing;
			totalSpacing += spacing.x + columnInfo[col].spacing;
		}
		if (columns > 1)
			totalSpacing -= spacing.x + columnInfo[columns - 1].spacing;

		// Fill the remaining column space (when container's width is wider than all columns)
		var rt = transform as RectTransform;

        float totalPreferred = padding.horizontal + totalColumnWidth + totalSpacing;
        float remainingWidth = rt.rect.width - totalPreferred;
        if (remainingWidth != 0 && flexibleColumns > 0)
        {
			float increment = remainingWidth / flexibleColumns;
            float offset = 0;
            for (int col = 0; col < columns; col++)
            {
                columnPos[col] += offset;
				if (columnInfo[col].expandToFill)
				{
					columnWidths[col] += increment;
					offset += increment;
				}
            }

            totalColumnWidth += remainingWidth;
        }
	}

	private void InitializeVerticalLayout()
	{
		for (int cell = 0; cell < rectChildren.Count; cell++)
		{
			int col = cell % columns;
			int row = cell / columns;

			var child = rectChildren[cell];

			// Calculate child height AFTER calculating width
			var height = GetHeight(child, col);

			rowHeights[row] = Mathf.Max(rowHeights[row], height);
		}

		float pos = padding.top;
		totalRowHeight = 0;
		for (int row = 0; row < rows; row++)
		{
			totalRowHeight += rowHeights[row];
			rowPos[row] = pos;
			pos += rowHeights[row] + spacing.y;
		}
	}

	public override void CalculateLayoutInputHorizontal()
	{
		base.CalculateLayoutInputHorizontal();

		InitializeLayout();

		float totalMin = padding.horizontal;
		float totalPreferred = totalMin + totalColumnWidth + totalSpacing;
		SetLayoutInputForAxis(totalMin, totalPreferred, -1, 0);
	}

	public override void CalculateLayoutInputVertical()
	{
		InitializeVerticalLayout();

		float totalMin = padding.vertical;
		float totalPreferred = totalMin + totalRowHeight + spacing.y * (rows - 1);
		SetLayoutInputForAxis(totalMin, totalPreferred, -1, 1);
	}

	public override void SetLayoutHorizontal()
	{
		SetCellsAlongAxis(0);
	}

	public override void SetLayoutVertical()
	{
		SetCellsAlongAxis(1);

		if (OnLayoutChange != null)
			OnLayoutChange();
	}

	private void SetCellsAlongAxis(int axis)
	{
		if (axis == 0)	// Horizontal
		{
			for (int i = 0; i < rectChildren.Count; i++)
			{
				var child = rectChildren[i];
				int col = i % columns;
				float pos = columnPos[col];
				if (m_ExpandChildWidth || columnInfo[col].expandItemWidth)
                {
                    SetChildAlongAxis(child, axis, pos, columnWidths[col]);
                }
                else
                {
					var width = GetWidth(child, col);
                    if (width < columnWidths[col])
                    {
                        switch (childAlignment)
                        {
                            case TextAnchor.UpperCenter:
                            case TextAnchor.MiddleCenter:
                            case TextAnchor.LowerCenter:
                                pos += (columnWidths[col] - width) * 0.5f;
                                break;

                            case TextAnchor.UpperRight:
                            case TextAnchor.MiddleRight:
                            case TextAnchor.LowerRight:
                                pos += columnWidths[col] - width;
                                break;
                        }
                    }
					else
					{
						width = Mathf.Min(width, columnWidths[col]);
					}

					if (columnInfo[col].itemPreferredWidth || width < child.rect.width)
						SetChildAlongAxis(child, axis, pos, width);
					else
						SetChildAlongAxis(child, axis, pos);
                }
            }
		}
		else	// Vertical
		{
			for (int i = 0; i < rectChildren.Count; i++)
			{
				var child = rectChildren[i];
				int col = i % columns;
				int row = i / columns;
				float pos = rowPos[row];
                if (m_ExpandChildHeight || columnInfo[col].expandItemHeight)
                {
                    SetChildAlongAxis(child, axis, pos, rowHeights[row]);
                }
                else
                {
					var height = GetHeight(child, col);
					if (height < rowHeights[row])
                    {
                        switch (childAlignment)
                        {
                            case TextAnchor.MiddleLeft:
                            case TextAnchor.MiddleCenter:
                            case TextAnchor.MiddleRight:
                                pos += (rowHeights[row] - height) * 0.5f;
                                break;

                            case TextAnchor.LowerLeft:
                            case TextAnchor.LowerCenter:
                            case TextAnchor.LowerRight:
                                pos += rowHeights[row] - height;
                                break;
                        }
                    }

					if (columnInfo[col].itemPreferredHeight)
						SetChildAlongAxis(child, axis, pos, height);
					else
						SetChildAlongAxis(child, axis, pos);
                }
			}
		}
	}

	private float GetWidth(RectTransform item, int column)
	{
		return columnInfo[column].itemPreferredWidth ?
			LayoutUtility.GetPreferredWidth(item) :
			item.rect.width;
	}

	private float GetHeight(RectTransform item, int column)
	{
		return columnInfo[column].itemPreferredHeight ?
			LayoutUtility.GetPreferredHeight(item) :
			item.rect.height;
	}

}
