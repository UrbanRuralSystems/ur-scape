// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          David Neudecker (neudecker@arch.ethz.ch)
//          Muhammad Salihin Bin Zaol-kefli

using System.Collections.Generic;

public class PlanningSummary
{
    private List<PlanningGroup> groups = new List<PlanningGroup>();
    public readonly Typology typology = new Typology();
    public readonly Dictionary<string, bool> gridNoData = new Dictionary<string, bool>();
    private readonly Dictionary<string, float> gridAttributes = new Dictionary<string, float>();
    
    public Dictionary<string, float> GetAttributes()
    {
        return gridAttributes;
    }

    public Dictionary<string, float> GetTypologyValues()
    {
        return typology.values;
    }

    public void SetGroups(List<PlanningGroup> groups)
    {
        this.groups = groups;
    }

    public void UpdateGridAttributes()
    {
        typology.Clear();
        gridAttributes.Clear();
        gridNoData.Clear();

		//double percentGroupAreas = 0.0, groupsPaintedArea = 0.0;
		int i = 1;
		foreach (var group in groups)
        {
            foreach (var pair in group.typology.values)
            {
				double percent = (double)pair.Value * 0.01;
				double percentArea = percent * group.groupPaintedArea;
				float value = pair.Value;

				if (typology.values.ContainsKey(pair.Key))
				{
					if (Typology.info[pair.Key].isRatio)
					{
						//percentGroupAreas += percentArea;
						//groupsPaintedArea += group.groupPaintedArea;
						//typology.values[pair.Key] = (float)(percentGroupAreas / groupsPaintedArea) * 100.0f;

						// Compute Average
						if (i > 2)
							typology.values[pair.Key] *= (i - 1);

						typology.values[pair.Key] += value;

						if (i > 1)
							typology.values[pair.Key] /= i;
					}
					else
					{
						typology.values[pair.Key] += value;
					}
				}
				else
				{
					//if (Typology.info[pair.Key].isRatio)
					//{
					//	percentGroupAreas = percentArea;
					//	groupsPaintedArea += group.groupPaintedArea;
					//	value = (float)(percentGroupAreas / groupsPaintedArea) * 100.0f;
					//}
					typology.values.Add(pair.Key, value);
				}
			}
			++i;

			foreach (var pair in group.gridAttributes)
            {
                if (gridAttributes.ContainsKey(pair.Key))
                {
                    gridAttributes[pair.Key] += group.gridAttributes[pair.Key];
                }
                else
                {
                    gridAttributes.Add(pair.Key, group.gridAttributes[pair.Key]);
                }
            }

            foreach (var pair in group.gridNoData)
            {
                if (gridNoData.ContainsKey(pair.Key))
                {
                    gridNoData[pair.Key] |= group.gridNoData[pair.Key] ;
                }
                else
                {
                    gridNoData.Add(pair.Key, group.gridNoData[pair.Key] );
                }
            }
        }
    }

}
