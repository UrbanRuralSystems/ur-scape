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

public class PlanningGroup
{
    public readonly int id;
    public List<PlanningCell> cells = new List<PlanningCell>();
    public Typology typology = new Typology();
    public Dictionary<string, float> gridAttributes = new Dictionary<string, float>();
    public Dictionary<string, bool> gridNoData = new Dictionary<string, bool>();
    public Coordinate center;
	public double groupPaintedArea = 0.0;

	private readonly Planner planner;

	//
	// Public Methods
	//

	public PlanningGroup(int id, Planner planner)
    {
        this.id = id;
		this.planner = planner;
	}

    public Typology GetTypology()
    {
        return typology;
    }

    public void UpdateGridAttributes()
    {
        typology = new Typology();
        gridAttributes = new Dictionary<string, float>();
        gridNoData = new Dictionary<string, bool>();

		//double percentTypologyAreas;
		//double invGroupPaintedArea = 1.0 / groupPaintedArea;

		int i = 1;
		foreach (var cell in cells)
        {
            foreach (var pair in cell.typologyValues)
            {
				float value = pair.Value;
				double percentArea = pair.Value * cell.areaSqM * 0.01; // multiply by 0.01 because it is a percentage
				if (typology.values.ContainsKey(pair.Key))
                {
                    if (Typology.info[pair.Key].isRatio)
                    {
						//percentTypologyAreas += percentArea;
						//typology.values[pair.Key] = (float)(percentTypologyAreas * invGroupPaintedArea * 100.0);

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
					//	percentTypologyAreas = percentArea;
					//	value = (float)(percentTypologyAreas * invGroupPaintedArea * 100.0);
					//}
					typology.values.Add(pair.Key, value);
                }
			}
			++i;

			foreach (var pair in cell.gridValues)
            {
                if (gridAttributes.ContainsKey(pair.Key))
                {
                    gridAttributes[pair.Key] += cell.gridValues[pair.Key];
                    gridNoData[pair.Key] |= cell.gridValues[pair.Key] < 0;
                }
                else
                {
                    gridAttributes.Add(pair.Key, cell.gridValues[pair.Key]);
                    gridNoData.Add(pair.Key, cell.gridValues[pair.Key] < 0);
                }
            }
        }
    }

}
