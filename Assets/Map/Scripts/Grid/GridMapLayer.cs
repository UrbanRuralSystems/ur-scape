// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class GridMapLayer : PatchMapLayer
{
    public enum Shape
    {
        None,
        Circle,
        Square,
    }

    public static bool ShowNoData = true;
	public static bool ManualGammaCorrection = false;

	protected GridData grid;
    protected Material material;

#if USE_TEXTURE
    protected Texture2D valuesBuffer = null;
	protected Texture2D maskBuffer = null;
	private byte[] byteArray = null;
#else
    protected ComputeBuffer valuesBuffer = null;
	public ComputeBuffer ValuesBuffer
	{
		get { return valuesBuffer; }
	}

	protected ComputeBuffer maskBuffer = null;
	public ComputeBuffer MaskBuffer
	{
		get { return maskBuffer; }
	}
#endif

    private Texture2D projection;

    public GridData Grid { get { return grid; } }

    protected Distance tileCenterInMeters;
    protected Distance tileSizeInMeters;
    private float gamma = 1;

    private const double HalfPI = Math.PI * 0.5;
    private const double Rad2Deg = 180.0 / Math.PI;
    private Color[] categoryColors = null;

	// Contours specific variables
    private int highlightedIndex = -1;
#if !USE_TEXTURE
	private bool gpuChangedValues = false;
#endif


    //
    // Unity Methods
    //

    protected virtual void OnEnable()
    {
        material = GetComponent<MeshRenderer>().material;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (grid != null)
        {
            grid.OnGridChange -= OnGridChange;
            grid.OnValuesChange -= OnGridValuesChange;
            grid.OnFilterChange -= OnGridFilterChange;
        }
        ReleaseValuesBuffer();
		ReleaseMaskBuffer();
	}

    //
    // Inheritance Methods
    //

    public virtual void Init(MapController map, GridData grid)
    {
        // Deregister old events
        if (this.grid != null)
        {
            this.grid.OnGridChange -= OnGridChange;
            this.grid.OnValuesChange -= OnGridValuesChange;
            this.grid.OnFilterChange -= OnGridFilterChange;
        }

        this.grid = grid;

        // Register events
        grid.OnGridChange += OnGridChange;
        grid.OnValuesChange += OnGridValuesChange;
        grid.OnFilterChange += OnGridFilterChange;

        base.Init(map, grid);

        // Force an update
        if (grid.values != null)
        {
            OnGridChange(grid);
        }

        SetShowNoData(ShowNoData);

        if (grid.patch != null)
        {
            SetUserOpacity(grid.patch.DataLayer.UserOpacity);
			SetToolOpacity(grid.patch.DataLayer.ToolOpacity);
		}
		else
		{
			SetUserOpacity(1f);
			SetToolOpacity(1f);
		}
    }


	//
	// Public Methods
	//

    public void SetOffset(float x, float y)
    {
        material.SetFloat("OffsetX", x);
        material.SetFloat("OffsetY", y);
    }

    public void SetCellSize(float size)
    {
        material.SetFloat("CellHalfSize", size * 0.5f);
    }

    public void SetUserOpacity(float opacity)
    {
        material.SetFloat("UserOpacity", opacity);
    }

	public void SetToolOpacity(float opacity)
	{
		material.SetFloat("ToolOpacity", opacity);
	}

    public bool IsTransectEnabled()
    {
        return material.IsKeywordEnabled("TRANSECT");
    }

	public void ShowTransect(bool show)
    {
        if (show)
            material.EnableKeyword("TRANSECT");
        else
            material.DisableKeyword("TRANSECT");
    }

    public void SetTransect(float position)
    {
        material.SetFloat("TransectPosition", position);
    }

	// A stripe marker separates 2 stripes
	// Number of stripes == Stripe markers + 1
	// The stripe values should be between grid min and max
    public void SetStripeMarkers(float[] stripeMarkers)
    {
        if (stripeMarkers == null)
        {
            SetColoringKeyword("GRADIENT");
        }
        else
        {
            int count = Mathf.Min(stripeMarkers.Length, 4);
            if (count > 0)
            {
                Vector4 vBands = Vector4.zero;
                for (int i = 0; i < count; i++)
                    vBands[i] = stripeMarkers[i];
                for (int i = count; i < 4; i++)
                    vBands[i] = grid.maxValue;

                material.SetVector("StripeMarkers", vBands);
            }

            count++;
            material.SetInt("StripesCount", count);
            SetColoringKeyword("STRIPES_" + count);
        }
    }

    public void SetStripes(int count, bool reverse = false)
    {
        count = Mathf.Clamp(count, 0, 4);
        if (count == 0)
        {
            SetColoringKeyword("GRADIENT");
        }
        else
        {
            material.SetInt("StripesCount", count);
			if (reverse)
				SetColoringKeyword("STRIPES_UNIFORM_REVERSE");
			else
				SetColoringKeyword("STRIPES_UNIFORM");
        }
    }

    private string lastUsedColoringKeyword = null;


    public void SetColoringKeyword(string keyword)
    {
        if (lastUsedColoringKeyword == keyword)
            return;

        if (lastUsedColoringKeyword != null)
        {
            material.DisableKeyword(lastUsedColoringKeyword);
            lastUsedColoringKeyword = null;
        }
        if (keyword != null)
        {
            material.EnableKeyword(keyword);
            lastUsedColoringKeyword = keyword;
        }
    }

    public void EnableFilters(bool enable)
    {
        if (enable)
            material.EnableKeyword("FILTER_DATA");
        else
            material.DisableKeyword("FILTER_DATA");
    }

    public void SetShape(Shape shape)
    {
        material.SetFloat("Shape", (float)shape);
        material.DisableKeyword("SHAPE_CIRCLE");
        material.DisableKeyword("SHAPE_SQUARE");

        if (shape == Shape.Circle)
            material.EnableKeyword("SHAPE_CIRCLE");
        else if (shape == Shape.Square)
            material.EnableKeyword("SHAPE_SQUARE");
    }

    public void SetInterpolation(bool interpolate)
    {
        if (interpolate)
            material.EnableKeyword("INTERPOLATE");
        else
            material.DisableKeyword("INTERPOLATE");
    }

    public void HighlightCategory(int index)
    {
        highlightedIndex = index;
        UpdateCategoriesFilter();
    }

    public void SetShowNoData(bool show)
    {
		// Don't allow to show "No Data" crosses for dynamically generated map layers
		if (patchData.patch == null || patchData.patch.Filename == null)
			return;

        if (show)
            material.EnableKeyword("SHOW_NODATA");
        else
            material.DisableKeyword("SHOW_NODATA");
    }

	public void SetUseMask(bool use)
	{
		if (use)
			material.EnableKeyword("USE_MASK");
		else
			material.DisableKeyword("USE_MASK");
	}

	public void SubmitGridValues()
	{
		UpdateValuesBuffer();
	}

	public void FetchGridValues()
	{
#if !USE_TEXTURE
		if (valuesBuffer != null && gpuChangedValues)
		{
			valuesBuffer.GetData(grid.values);
		}
#endif
	}

	public void SetGpuChangedValues()
	{
#if !USE_TEXTURE
		gpuChangedValues = true;
#endif
	}

	public void SetGamma(float _gamma)
	{
		gamma = Mathf.Clamp(1f / _gamma, 0.1f, 1f);
		UpdateRange();
	}


	//
	// Event Methods
	//

	private void OnGridChange(GridData grid)
    {
        UpdateCategories();

        OnGridValuesChange(grid);
        OnGridFilterChange(grid);

		int count = grid.countY + 1;
		if (projection == null || projection.width != count)
        {
            UpdateProjectionValues();
        }
    }

    protected override void OnPatchBoundsChange(PatchData patchData)
    {
        base.OnPatchBoundsChange(patchData);

        UpdateProjectionValues();
    }

	protected void OnGridValuesChange(GridData grid)
	{
		// Update CountX & CountY
		UpdateResolution();

		// Update min/max range
		UpdateRange();

		// Update grid values
		UpdateValuesBuffer();
		UpdateMaskBuffer();
	}

    private void OnGridFilterChange(GridData grid)
    {
        if (grid.IsCategorized)
            UpdateCategoriesFilter();
        else
            UpdateMinMaxFilter();
    }


    //
    // Private/Protected Methods
    //

	protected void UpdateValuesBuffer()
    {
        if (grid.values == null || grid.values.Length == 0)
        {
            ReleaseValuesBuffer();
        }
        else
        {
            if (valuesBuffer == null ||
#if USE_TEXTURE
                grid.countX != valuesBuffer.width || grid.countY != valuesBuffer.height)
#else
                grid.values.Length != valuesBuffer.count)
#endif
            {
                CreateValuesBuffer();
            }

#if USE_TEXTURE
            Buffer.BlockCopy(grid.values, 0, byteArray, 0, byteArray.Length);
            valuesBuffer.LoadRawTextureData(byteArray);
            valuesBuffer.Apply();
#else
			valuesBuffer.SetData(grid.values);
#endif
        }
    }

	protected void UpdateMaskBuffer()
	{
		if (grid.valuesMask == null || grid.valuesMask.Length == 0)
		{
			ReleaseMaskBuffer();
			SetUseMask(false);
		}
		else
		{
			SetUseMask(true);
			if (maskBuffer == null ||
#if USE_TEXTURE
                grid.countX != maskBuffer.width || grid.countY != maskBuffer.height)
#else
                grid.valuesMask.Length != maskBuffer.count)
#endif
            {
                CreateMaskBuffer();
			}

#if USE_TEXTURE
			Buffer.BlockCopy(grid.valuesMask, 0, byteArray, 0, grid.valuesMask.Length);
            maskBuffer.LoadRawTextureData(byteArray);
            maskBuffer.Apply();
#else
			maskBuffer.SetData(grid.valuesMask);
#endif
		}
	}

	private void UpdateProjectionValues()
    {
        if (grid.countY == 0)
            return;

		int count = grid.countY + 1;
		if (projection == null || projection.width != count)
        {
			CreateProjectionBuffer(count);
        }

        double min = GeoCalculator.LatitudeToNormalizedMercator(grid.south);
        double max = GeoCalculator.LatitudeToNormalizedMercator(grid.north);
        double invLatRange = (1.0 / (grid.north - grid.south));

        float[] lats = new float[count];
		double projLatInterval = (max - min) / (count - 1);
        for (int i = 0; i < count; i++)
        {
            double projLat = min + i * projLatInterval;
            double lat = (2 * Math.Atan(Math.Exp(projLat * Math.PI)) - HalfPI) * Rad2Deg;
            lats[i] = Mathf.Clamp01((float)(1 - (lat - grid.south) * invLatRange));
        }
        byte[] latBytes = new byte[lats.Length * 4];
        Buffer.BlockCopy(lats, 0, latBytes, 0, latBytes.Length);
        projection.LoadRawTextureData(latBytes);
        projection.Apply();
    }

    protected void UpdateResolution()
    {
        // Resolution
        material.SetInt("CountX", grid.countX);
        material.SetInt("CountY", grid.countY);
    }

    public void UpdateRange()
    {
        // Min/max values
        float minValue, maxValue;
        if (grid.patch == null)
        {
            minValue = grid.minValue;
            maxValue = grid.maxValue;
        }
        else
        {
			var layerSite = grid.patch.SiteRecord.layerSite;
			minValue = layerSite.minValue;
			maxValue = layerSite.maxValue;
        }

		material.SetFloat("MinValue", minValue);
		material.SetFloat("InvValueRange", Mathf.Pow(maxValue - minValue, -gamma));
		material.SetFloat("Gamma", gamma);
    }

	protected void CreateValuesBuffer()
    {
        ReleaseValuesBuffer();

        var bufferSize = grid.countX * grid.countY;

#if USE_TEXTURE
        byteArray = new byte[bufferSize * sizeof(float)];
        valuesBuffer = new Texture2D(grid.countX, grid.countY, TextureFormat.RFloat, false);
		valuesBuffer.wrapMode = TextureWrapMode.Clamp;
		valuesBuffer.filterMode = FilterMode.Point;
        material.SetTexture("Values", valuesBuffer);
#else
        valuesBuffer = new ComputeBuffer(bufferSize, 4, ComputeBufferType.Default);
        material.SetBuffer("Values", valuesBuffer);
#endif
    }

	protected void CreateMaskBuffer()
	{
		ReleaseMaskBuffer();

        // Buffer size must be multiple of 4
        var bufferSize = (grid.countX * grid.countY + 3) / 4;

#if USE_TEXTURE
		int countX = (grid.countX + 1) / 2;
		int countY = (grid.countY + 1) / 2;
		maskBuffer = new Texture2D(countX, countY, TextureFormat.RGBA32, false);
		maskBuffer.wrapMode = TextureWrapMode.Clamp;
        maskBuffer.filterMode = FilterMode.Point;
        material.SetTexture("Mask", maskBuffer);
#else
        maskBuffer = new ComputeBuffer(bufferSize, 4, ComputeBufferType.Default);
		material.SetBuffer("Mask", maskBuffer);
#endif
	}

	protected void ReleaseValuesBuffer()
    {
        if (valuesBuffer != null)
        {
#if USE_TEXTURE
            byteArray = null;
            Destroy(valuesBuffer);
#else
			valuesBuffer.Release();
#endif
			valuesBuffer = null;
        }
    }

	protected void ReleaseMaskBuffer()
	{
		if (maskBuffer != null)
		{
#if USE_TEXTURE
            Destroy(maskBuffer);
#else
			maskBuffer.Release();
#endif
			maskBuffer = null;
        }
	}

	private void CreateProjectionBuffer(int count)
    {
        ReleaseProjectionBuffer();

        projection = new Texture2D(count, 1, TextureFormat.RFloat, false);
        projection.wrapMode = TextureWrapMode.Clamp;
		projection.filterMode = FilterMode.Bilinear;
        material.SetTexture("Projection", projection);
    }

    private void ReleaseProjectionBuffer()
    {
        if (projection != null)
        {
            Destroy(projection);
            projection = null;
        }
    }

    public void UpdateMinMaxFilter()
    {
        // Min/max filters
        material.SetFloat("FilterMinValue", grid.minFilter);
        material.SetFloat("FilterMaxValue", grid.maxFilter);
    }

    protected virtual void UpdateCategoryColors()
    {
        int count = categoryColors.Length;
        for (int i = 0; i < count; i++)
        {
            categoryColors[i].a = grid.categoryFilter.IsSetAsInt(i);
        }
        if (highlightedIndex >= 0 && grid.categoryFilter.IsSet(highlightedIndex))
        {
            for (int i = 0; i < count; i++)
                categoryColors[i].a *= i == highlightedIndex ? 1f : 0f;
        }
    }

    private void UpdateCategoriesFilter()
    {
        UpdateCategoryColors();
        material.SetColorArray("CategoryColors", categoryColors);
    }

    private void UpdateCategories()
    {
        if (grid.IsCategorized && categoryColors == null)
        {
            SetColoringKeyword("CATEGORIZED");
            material.SetFloat("Categorized", 1f);

            CacheCategoryColors();
        }
    }

    private void CacheCategoryColors()
    {
        int count = grid.categories.Length;
        if (categoryColors == null || categoryColors.Length != count)
        {
            categoryColors = new Color[count];
        }

        for (int i = 0; i < count; i++)
        {
            categoryColors[i] = grid.categories[i].color;
        }
    }

}
