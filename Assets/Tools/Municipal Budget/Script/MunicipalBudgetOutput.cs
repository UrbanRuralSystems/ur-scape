// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//          Michael Joos  (joos@arch.ethz.ch)
//          Muhammad Salihin Bin Zaol-kefli

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MunicipalBudgetOutput : MonoBehaviour, IOutput
{
	[Header("Prefabs")]
	public BudgetLabel textPrefab;
	public Transform container;

    [Header("UI References")]
    public GameObject areasContainer;
    public Text selectedArea;
    public Text selectedAreaVal;
    public Toggle resultOrderToggle;
    public Text outputMessage;

    // Delegate 
    public delegate void OnItemHover(string name, bool hover);
	public event OnItemHover OnItemHovering;

    // Data lists & dics
    private readonly List<Transform> areas = new List<Transform>();
    private BudgetData budgetData;

    // Misc
    private bool orderByAplhabet = false;
    private bool useNoData = false;


    //
    // Public Methods
    //

    public void Start()
	{
		resultOrderToggle.onValueChanged.AddListener(OnOrderToggleChange);
	}

	public void ShowMessage(string msg)
	{
		outputMessage.gameObject.SetActive(true);
		outputMessage.text = msg;
	}

	public void HideMessage()
	{
		outputMessage.gameObject.SetActive(false);
    }

    public void ShowAreas(bool show)
    {
        areasContainer.SetActive(show);
    }

    public void ClearAreas()
    {
		if (areas.Count > 0)
		{
			foreach (var area in areas)
				Destroy(area.gameObject);
            areas.Clear();
		}
    }

    public void SetData(BudgetData budgetData)
    {
        bool updateGUI = budgetData == null;
        this.budgetData = budgetData;
        UpdateData();

        if (updateGUI)
        {
            GuiUtils.RebuildLayout(container);
        }
    }

    public void SetNoDataUse(bool isOn)
    {
        useNoData = isOn;
        UpdateData();
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
        var budgetItems = budgetData.BudgetItems;
    
		// Order alphabetically (ascending) or by value (descending)
        if (orderByAplhabet)
			budgetItems.Sort((i1, i2) => i1.name.CompareTo(i2.name));
        else
			budgetItems.Sort((i1, i2) => i2.value.CompareTo(i1.value));

        PlotData(budgetItems, budgetData.Summary);
    }

    private void PlotData(IEnumerable<BudgetItem> budgetItems, float summary)
    {
        // List all items and pass the value to output
        int index = 0;
		float multiplier = 100 / summary;
		foreach (var budgetItem in budgetItems)
        {
            BudgetLabel area;
            if (index < areas.Count)
            {
                area = areas[index].GetComponent<BudgetLabel>();
            }
            else
            {
                area = Instantiate(textPrefab, container, false);
                areas.Add(area.transform);

                // Add listener for highlight area in the map
                area.GetComponentInChildren<HoverHandler>().OnHover += (hover) => OnItemHoverDelegate(area, hover);
            }

            area.name = budgetItem.name;
            area.SetName(budgetItem.name);

            // if user set exclude no data show only n/a
            if (useNoData && budgetItem.isMasked)
            {
                area.SetValue("n/a");
            }
            else
            {
                var value = budgetItem.value * multiplier;
                // value = budgetItem.value; // For testing. Display values instead of percentages
                area.SetValue(value.ToString("F2") + " %");
            }

            index++;
        }

        if (index < areas.Count)
            RemoveRemainingAreas(index);
    }

    private void RemoveRemainingAreas(int index)
    {
        for (int i = areas.Count - 1; i >= index; i--)
        {
            Destroy(areas[i].gameObject);
            areas.RemoveAt(i);
        }
    }

    private void OnItemHoverDelegate(BudgetLabel label, bool hover)
    {
		var fontStyle = hover ? FontStyle.Bold : FontStyle.Normal;
		label.valueLabel.fontStyle = label.nameLabel.fontStyle = fontStyle;

        OnItemHovering?.Invoke(label.name, hover);

        if (hover)
            UpdateSelectedAreaAndVal(label.name);
    }

	public void OutputToCSV(TextWriter csv)
    {
        float multiplier = 100 / budgetData.Summary;
        foreach (var budgetItem in budgetData.BudgetItems)
        {
            csv.WriteLine(budgetItem.name + "," + (budgetItem.value * multiplier).ToString("F2") + "%");
        }
    }

    public void ShowBudgetLabel(string name)
    {
        foreach(var area in areas)
        {
            var budgetLabel = area.GetComponent<BudgetLabel>();
            if (budgetLabel)
                budgetLabel.nameLabel.fontStyle = area.name.Equals(name) ? FontStyle.Bold : FontStyle.Normal;
        }
    }

    public void UpdateSelectedAreaAndVal(string name)
    {
        BudgetItem budgetItem = budgetData.BudgetItems.Find(item => item.name.Equals(name));
        float multiplier = 100 / budgetData.Summary;
        selectedArea.text = (budgetItem != null) ? budgetItem.name : Translator.Get("None");
        selectedAreaVal.text = (budgetItem != null) ? ((budgetItem.value * multiplier).ToString("F2") + "%") : "";
    }
}
