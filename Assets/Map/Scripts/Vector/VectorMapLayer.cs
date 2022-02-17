// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

public abstract class VectorMapLayer : PatchMapLayer
{
	public static bool ManualGammaCorrection = false;

	public Material materialPrefab;
	protected Material material;

	protected float gamma = 1;

	protected void Awake()
	{
		material = Instantiate(materialPrefab);
	}

	public void SetColor(Color color)
	{
		material.SetColor("Tint", color);
	}

	public void SetGamma(float _gamma)
	{
		gamma = Mathf.Clamp(1f / _gamma, 0.1f, 1f);
		UpdateMaterialGamma();
	}

	protected void UpdateMaterialGamma()
	{
		material.SetFloat("Gamma", gamma);
	}

}
