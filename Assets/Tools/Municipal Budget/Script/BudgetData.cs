// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//			Michael Joos  (joos@arch.ethz.ch)

using System.Collections.Generic;

public class BudgetItem
{
	public float value;
	public string name;
	public bool isMasked;
}

public class BudgetData
{
	public List<BudgetItem> BudgetItems { get; } = new List<BudgetItem>();
	private float summary;
	public float Summary => summary;

	private readonly Dictionary<int, float> idToValueList = new Dictionary<int, float>();
	private readonly Dictionary<int, byte> idToMaskList = new Dictionary<int, byte>();
	private readonly Dictionary<int, float> idToDivisors = new Dictionary<int, float>();


	public BudgetData(float[] values, byte[] masks, float[] divisors, MunicipalityData data)
	{
		Update(values, masks, divisors, data);
	}

	//+ This guy is the bottleneck (+100ms). Try to optimize
	public void Update(float[] values, byte[] masks, float[] divisors, MunicipalityData data)
	{
		// Add all cells to list based on municipality
		for (int i = 0; i < values.Length; ++i)
		{
			if (data.ids[i] < 0)
				continue;

			var id = data.ids[i];
			if (!idToValueList.ContainsKey(id))
			{
				idToValueList.Add(id, values[i]);
				idToMaskList.Add(id, masks[i]);
				idToDivisors.Add(id, divisors[i]);
			}
			else
			{
				idToValueList[id] += values[i];
				idToMaskList[id] &= masks[i];
				idToDivisors[id] += divisors[i];
			}
		}

		// Translate BudgetData to budget Items
		summary = 0;
		int index = 0;
		int count = BudgetItems.Count;
		BudgetItem budgetItem;
		foreach (var pair in idToValueList)
		{
			float totalValue = pair.Value / idToDivisors[pair.Key];
			var name = data.idToName[pair.Key];
			if (index < count)
			{
				budgetItem = BudgetItems[index];
				budgetItem.name = name;
				budgetItem.value = totalValue;
			}
			else
			{
				budgetItem = new BudgetItem { name = name, value = totalValue };
				BudgetItems.Add(budgetItem);
				count++;
			}
			budgetItem.isMasked = idToMaskList[pair.Key] == 0;
			summary += totalValue;
			index++;
		}

		// Remove remaining items
		if (idToValueList.Count < count)
			BudgetItems.RemoveRange(idToValueList.Count, count - idToValueList.Count);

		idToValueList.Clear();
		idToMaskList.Clear();
		idToDivisors.Clear();
	}
}
