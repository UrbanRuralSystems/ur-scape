// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextItem
{
    public Transform item;
    public Text itemLabel;
    public Text itemValue;

}

public class OutputGroup : CollapsibleList
{
    [Header("Prefabs")]
    public Transform itemPrefab;

	private readonly Dictionary<string, TextItem> textItems = new Dictionary<string, TextItem>();


    //
    // Public Methods
    //

    private void AddItem(string itemLabel, float value)
    {
        TextItem textItem = new TextItem();
        var item = Instantiate(itemPrefab, container, false);
        textItem.item = item;
        textItem.itemLabel = item.GetChild(0).GetComponent<Text>();
        textItem.itemValue = item.transform.GetChild(1).GetComponent<Text>();
        textItem.itemLabel.text = itemLabel;
		UpdateValue(textItem, value);

		textItems.Add(itemLabel, textItem);

        GuiUtils.RebuildLayout(container);
    }

    public void UpdateItem(string name, float value)
    {
        TextItem textItem = null;
        if (textItems.TryGetValue(name, out textItem))
        {
			UpdateValue(textItem, value);
        }
        else
        {
            AddItem(name, value);
        }
    }

    public void DeleteAllItems()
    {
        foreach (var pair in textItems)
        {
            Destroy(pair.Value.item.gameObject);
        }
        textItems.Clear();

        GuiUtils.RebuildLayout(container);
    }

	private void UpdateValue(TextItem textItem, float value)
	{
		var absValue = System.Math.Abs(value);
		if (absValue > 1000)
			textItem.itemValue.text = value.ToString("N0");
		else if (absValue > 10)
			textItem.itemValue.text = value.ToString("0.##");
		else
			textItem.itemValue.text = value.ToString("0.####");
	}
}
