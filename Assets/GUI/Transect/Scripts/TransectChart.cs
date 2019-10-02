// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker  (neudecker@arch.ethz.ch)
//          Michael Joos  (joos@arch.ethz.ch)
//			Muhammad Salihin Bin Zaol-kefli

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using LineInfo = LineInspector.LineInspectorInfo;

public class TransectChart : MonoBehaviour
{
    private Material material;
    private Image image;
    private float locator;
	private GridData grid;
    private Color color;

#if USE_TEXTURE
    private Texture2D buffer = null;
    private byte[] byteArray = null;
#else
    private ComputeBuffer buffer = null;
#endif
    private int bufferSize = 0;
    private float[] dataToSend;

    float lineAlpha = 1f;
    float fillAlpha = 1f;

    public GridData Grid { get { return grid; } }


	//
	// Unity Methods
	//

	private void OnDestroy()
    {
		image.UnregisterDirtyMaterialCallback(OnMaterialChange);
        grid.OnGridChange -= OnGridChange;
		grid.OnValuesChange -= OnGridValuesChange;
		ReleaseBuffer();
    }

	//
	// Event Methods
	//

	private void OnMaterialChange()
	{
		var mat = image.materialForRendering;
		if (mat != material)
		{
			material = mat;
			InitMaterial();

			if (buffer != null)
			{
				ReleaseBuffer();
				UpdateChart();
			}
		}
	}

	private void OnGridChange(GridData gd)
	{
		UpdateResolution();
		UpdateChart();
	}

	private void OnGridValuesChange(GridData gd)
	{
        UpdateChart();
	}

	//
	// Public Methods
	//

	public void Init(GridData grid, Color color)
    {
        this.color = color;
        this.grid = grid;
        grid.OnGridChange += OnGridChange;
		grid.OnValuesChange += OnGridValuesChange;

		image = GetComponent<Image>();

		material = new Material(image.material);
        if (!grid.IsCategorized)
        {
            lineAlpha = material.GetColor("Line").a;
			fillAlpha = material.GetColor("Fill").a;
        }

        image.RegisterDirtyMaterialCallback(OnMaterialChange);
        image.material = material;

        // Note: this may not be true if the transect is inside a scrollview (or another mask)
        if (image.material == material)
        {
            InitMaterial();
        }

		UpdateResolution();
	}

	public void Destroy()
	{
		image.UnregisterDirtyMaterialCallback(OnMaterialChange);
		grid.OnGridChange -= OnGridChange;
		grid.OnValuesChange -= OnGridValuesChange;
		ReleaseBuffer();
		Destroy(gameObject);
	}

    public void SetLocator(float locator)
    {
        if (this.locator != locator)
        {
            this.locator = locator;
            UpdateChart();
        }
    }

	public void SetLineInfo(LineInfo lineInfo)
	{
		UpdateLineInspetorChart(lineInfo);
	}

    //
    // Private Methods
    //

    private void InitMaterial()
    {
        Rect rect = GetComponent<RectTransform>().rect;

        material.SetColor("Line", new Color(color.r, color.g, color.b, lineAlpha));
        material.SetColor("Fill", new Color(color.r, color.g, color.b, fillAlpha));
        material.SetFloat("Width", rect.width);
        material.SetFloat("Height", rect.height);

        if (grid.IsCategorized)
        {
            int count = grid.categories.Length;
            Color[] colors = new Color[count];
            for (int i = 0; i < count; i++)
            {
                colors[i] = grid.categories[i].color;
            }
            material.SetColorArray("CategoryColors", colors);
            material.SetFloat("InvCountMinusOne", count > 0 ? 1f / (count - 1) : 0f);
        }
    }

	private void UpdateResolution()
	{
		if (dataToSend == null || dataToSend.Length != grid.countX)
		{
			dataToSend = new float[grid.countX];
		}
	}

	private void UpdateChart()
    {
        int columnSize = grid.countX;
        int locatorStart = Mathf.RoundToInt(grid.countY * locator) * grid.countX;
        locatorStart = Mathf.Clamp(locatorStart, 0, grid.values.Length - columnSize);

        if (columnSize >= dataToSend.Length)
            UpdateResolution();

        if (grid.IsCategorized)
        {
            for (int i = 0; i < columnSize; ++i)
            {
                dataToSend[i] = grid.values[locatorStart++];
            }
        }
        else
        {
            float minValue = grid.minValue;
            float invRange = 1f / (grid.maxValue - grid.minValue);

            for (int i = 0; i < columnSize; ++i)
            {
				dataToSend[i] = (grid.values[locatorStart++] - minValue) * invRange;
            }
        }

        SetData(dataToSend);
    }

	private void UpdateLineInspetorChart(LineInfo lineInfo)
	{
		if (lineInfo != null)
		{
			GridData lineGrid = lineInfo.mapLayer.Grid;

			// Initizlize values
			int length = lineInfo.mapLayer.numOfSamples;
			if (dataToSend == null || dataToSend.Length != length)
				dataToSend = new float[length];

			// Set all values to 0
			for (int i = 0; i < length; ++i)
			{
				dataToSend[i] = 0;
			}

			// Calculate values
			double thisDegreesPerCellX = (lineGrid.east - lineGrid.west) / lineGrid.countX;
			double thisDegreesPerCellY = (lineGrid.south - lineGrid.north) / lineGrid.countY;
			double thisCellsPerDegreeX = 1.0 / thisDegreesPerCellX;
			double thisCellsPerDegreeY = 1.0 / thisDegreesPerCellY;

			var patchCellsPerDegreeX = grid.countX / (grid.east - grid.west);
			var patchCellsPerDegreeY = grid.countY / (grid.south - grid.north);

			double scaleX = patchCellsPerDegreeX * thisDegreesPerCellX;
			double scaleY = patchCellsPerDegreeY * thisDegreesPerCellY;

			double offsetX = (lineGrid.west - grid.west) * patchCellsPerDegreeX + 0.5 * scaleX;
			double offsetY = (lineGrid.north - grid.north) * patchCellsPerDegreeY + 0.5 * scaleY;

			Coordinate coorStart = lineInfo.coords[0];
			Coordinate coorEnd = lineInfo.coords[1];

			// The smaller range of longitude and latitude
			double startLon = (coorStart.Longitude > grid.west) ? coorStart.Longitude : grid.west;
			double startLat = (coorStart.Latitude < grid.north) ? coorStart.Latitude : grid.north;
			double endLon = (coorEnd.Longitude < grid.east) ? coorEnd.Longitude : grid.east;
			double endLat = (coorEnd.Latitude > grid.south) ? coorEnd.Latitude : grid.south;

			// Precompute values to be used in loop
			double diffX = endLon - startLon;
			double diffY = endLat - startLat;
			double diffXperSample = diffX / length;
			double diffYperSample = diffY / length;

			double srcX = (startLon - lineGrid.west) * thisCellsPerDegreeX + 0.5;
			double srcY = (startLat - lineGrid.north) * thisCellsPerDegreeY + 0.5;

			double pX = offsetX + srcX * scaleX;
			double pY = offsetY + srcY * scaleY;

			double srcXStep = diffXperSample * thisCellsPerDegreeX;
			double srcYStep = diffYperSample * thisCellsPerDegreeY;

			double srcXStepScaleX = srcXStep * scaleX;
			double srcYStepScaleY = srcYStep * scaleY;

			if (grid.IsCategorized)
			{
				if (grid.values != null)
				{
					for (int j = 0; j < lineInfo.mapLayer.numOfSamples; ++j)
					{
						if (grid.IsInside(startLon, startLat))
						{
							int patchIndex = (int)pY * grid.countX + (int)pX;
							if (patchIndex >= 0 && patchIndex < grid.values.Length)
							{
								int value = (int)grid.values[patchIndex];
								byte mask = grid.valuesMask == null ? (byte)1 : grid.valuesMask[patchIndex];
								float valToAdd = mask == 1 ? grid.categoryFilter.IsSetAsInt(value) : 0;
								dataToSend[j] = valToAdd;
							}
						}

						startLon += diffXperSample;
						startLat += diffYperSample;

						srcX += srcXStep;
						srcY += srcYStep;

						pX += srcXStepScaleX;
						pY += srcYStepScaleY;
					}
				}
			}
			else
			{
				float gridMin = grid.minValue;
				float invRange = 1f / (grid.maxValue - grid.minValue);
				if (lineInfo.mapLayer.useFilters)
				{
					if (grid.values != null)
					{
						for (int j = 0; j < length; ++j)
						{
							if (grid.IsInside(startLon, startLat))
							{
								int patchIndex = (int)pY * grid.countX + (int)pX;
								if (patchIndex >= 0 && patchIndex < grid.values.Length)
								{
									int value = (int)grid.values[patchIndex];
									byte mask = grid.valuesMask == null ? (byte)1 : grid.valuesMask[patchIndex];
									if (mask == 1 && value >= grid.minFilter && value <= grid.maxFilter)
									{
										float valToAdd = (value - gridMin) * invRange;
										dataToSend[j] = valToAdd;
									}
								}
							}

							startLon += diffXperSample;
							startLat += diffYperSample;

							srcX += srcXStep;
							srcY += srcYStep;

							pX += srcXStepScaleX;
							pY += srcYStepScaleY;
						}
					}
				}
				else
				{
					if (grid.values != null)
					{
						for (int j = 0; j < lineInfo.mapLayer.numOfSamples; ++j)
						{
							if (grid.IsInside(startLon, startLat))
							{
								int patchIndex = (int)pY * grid.countX + (int)pX;
								if (patchIndex >= 0 && patchIndex < grid.values.Length)
								{
									byte mask = grid.valuesMask == null ? (byte)1 : grid.valuesMask[patchIndex];
									if (mask == 1)
									{
										float valToAdd = (grid.values[patchIndex] - gridMin);
										dataToSend[j] = valToAdd;
									}
								}
							}

							startLon += diffXperSample;
							startLat += diffYperSample;

							srcX += srcXStep;
							srcY += srcYStep;

							pX += srcXStepScaleX;
							pY += srcYStepScaleY;
						}
					}
				}
			}
		}
		else
		{
			// Set all values to 0
			for (int i = 0; i < dataToSend.Length; ++i)
			{
				dataToSend[i] = 0;
			}
		}

		if(dataToSend.Length > 0)
			SetData(dataToSend);
	}

	private void SetData(float[] data)
    {
        if (buffer == null || data.Length != bufferSize)
        {
			CreateBuffer(data.Length);
		}

#if USE_TEXTURE
        System.Buffer.BlockCopy(data, 0, byteArray, 0, byteArray.Length);
        buffer.LoadRawTextureData(byteArray);
        buffer.Apply();
#else
        buffer.SetData(data);
#endif
        if(material)
            material.SetInt("Count", data.Length - 1);
    }

	private void CreateBuffer(int count)
	{
		ReleaseBuffer();

		bufferSize = count;
#if USE_TEXTURE
        byteArray = new byte[count * 4];
        buffer = new Texture2D(bufferSize, 1, TextureFormat.RFloat, false);
        buffer.filterMode = FilterMode.Point;
        material.SetTexture("Values", buffer);
#else
		buffer = new ComputeBuffer(bufferSize, sizeof(float), ComputeBufferType.Default);
		material.SetBuffer("Values", buffer);
#endif
	}

	private void ReleaseBuffer()
    {
        if (buffer != null)
        {
#if USE_TEXTURE
            byteArray = null;
            Destroy(buffer);
#else
            buffer.Release();
#endif
            buffer = null;
            bufferSize = 0;
        }
    }
}
