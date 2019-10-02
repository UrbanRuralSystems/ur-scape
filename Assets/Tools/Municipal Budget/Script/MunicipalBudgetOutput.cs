// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//          Michael Joos  (joos@arch.ethz.ch)
//          Muhammad Salihin Bin Zaol-kefli

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

class BudgetItem
{
    public float value;
    public string name;
}

public class MunicipalBudgetOutput : MonoBehaviour, IOutput
{
	[Header("Prefabs")]
	public BudgetLabel textPrefab;
	public Transform container;

	[Header("UI References")]
	//public GameObject sortPanel;
	public GameObject outputMessage;
	public Toggle resultOrderToggle;
    public Text selectedArea;
    public Text selectedAreaVal;

	// Delegate 
	public delegate void OnItemHover(String name, bool hover);
	public event OnItemHover OnItemHovering;

    // Data lists & dics
    private readonly List<Transform> items = new List<Transform>();
	private Dictionary<string, List<float>> dictData;

	// Misc
	private bool orderByAplhabet = false;

    // Output
    private readonly List<BudgetItem> budgetItems = new List<BudgetItem>();
    float summary = 0;

    //
    // Public Methods
    //

    public void Start()
	{
		resultOrderToggle.onValueChanged.AddListener(OnOrderToggleChange);
	}

	public void ShowMessage(string msg)
	{
		outputMessage.SetActive(true);
		outputMessage.GetComponentInChildren<Text>().text = msg;

		// Hide the sorting panel
		//sortPanel.SetActive(false);
	}

	public void HideMessage()
	{
		outputMessage.SetActive(false);
        //sortPanel.SetActive(true);
    }

	public void RemoveData()
    {
		if (items.Count > 0)
		{
			// clear Output
			foreach (var item in items)
			{
				Destroy(item.gameObject);
			}
			items.Clear();

			GuiUtils.RebuildLayout(container);
		}
    } 

    public void SetData(Dictionary<string, List<float>> dict)
    {
		bool updateGUI = dictData == null;
        dictData = dict;
        UpdateData();

		if (updateGUI)
		{
			GuiUtils.RebuildLayout(container);
		}
	}


    //
    // Event Methods
    //

    private void OnOrderToggleChange(bool isOn)
    {
        orderByAplhabet = isOn;
        UpdateData();
    }


    //
    // Private Methods
    //

    private void UpdateData()
    {
        budgetItems.Clear();
        summary = 0;
        foreach (var pair in dictData)
        {          
            float average = pair.Value.Average();
            budgetItems.Add(new BudgetItem { name = pair.Key, value = average });
			summary += average;
		}
        
		// Order alphabetically (ascending) or by value (descending)
        if(orderByAplhabet)
			budgetItems.Sort((i1, i2) => i1.name.CompareTo(i2.name));
        else
			budgetItems.Sort((i1, i2) => i2.value.CompareTo(i1.value));

        PlotData(budgetItems, summary);
    }

    private void PlotData(IEnumerable<BudgetItem> itemlist, float summary)
    {
        RemoveData();

		// List all items and pass the value to output
		float multiplier = 100 / summary;
		foreach (var budgetItem in itemlist)
        {
            var item = Instantiate(textPrefab, container, false);
            item.name = budgetItem.name;
            item.SetName(budgetItem.name);
            item.SetValue(budgetItem.value * multiplier);
            items.Add(item.transform);

			// Add listener for highlight area in the map
			item.GetComponentInChildren<HoverHandler>().OnHover += (hover) => OnItemHoverDelegate(item, hover);
        }
    }

    private void OnItemHoverDelegate(BudgetLabel label, bool hover)
    {
		var fontStyle = hover ? FontStyle.Bold : FontStyle.Normal;
		label.valueLabel.fontStyle = label.nameLabel.fontStyle = fontStyle;

		if (OnItemHovering != null)
            OnItemHovering(label.name, hover);

        if(hover)
            UpdateSelectedAreaAndVal(label.name);
    }

	public void OutputToCSV(TextWriter csv)
    {
        float multiplier = 100 / summary;
        foreach (var budgetItem in budgetItems)
        {
            csv.WriteLine(budgetItem.name + "," + (budgetItem.value * multiplier).ToString("F2") + "%");
        }
    }

    public void ShowBudgetLabel(string name)
    {
        foreach(var item in items)
        {
            var budgetLabel = item.GetComponent<BudgetLabel>();
            if (budgetLabel)
                budgetLabel.nameLabel.fontStyle = item.name.Equals(name) ? FontStyle.Bold : FontStyle.Normal;
        }
    }

    public void UpdateSelectedAreaAndVal(string name)
    {
        BudgetItem budgetItem = budgetItems.Find(item => item.name.Equals(name));
        float multiplier = 100 / summary;
        selectedArea.text = (budgetItem != null) ? budgetItem.name : Translator.Get("None");
        selectedAreaVal.text = (budgetItem != null) ? ((budgetItem.value * multiplier).ToString("F2") + "%") : "";
    }
}
