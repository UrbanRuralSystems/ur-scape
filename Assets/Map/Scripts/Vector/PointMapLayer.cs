// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class PointMapLayer : VectorMapLayer
{
    protected PointData pointData;
    public PointData PointData { get => pointData; }

    protected Mesh mesh;
    protected Bounds bounds;

    protected DataBuffer renderArgs;
    protected DataBuffer valuesBuffer;
    protected DataBuffer coordsBuffer;
    protected int[,] coordsData;

    protected Distance tileCenterInMeters;
    protected Distance tileSizeInMeters;

    private Color[] categoryColors = null;
    private int highlightedIndex = -1;

    private float pointSize = 0.02f;

    private Canvas canvas;


    //
    // Unity Methods
    //

#if !USE_TEXTURE
    private void Update()
    {
        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, bounds, renderArgs.Buffer);
    }
#endif

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (pointData != null)
        {
            pointData.OnPointDataChange -= OnPointDataChange;
            pointData.OnValuesChange -= OnValuesChange;
            pointData.OnFilterChange -= OnFilterChange;
        }

        ReleaseBuffers();
	}

    //
    // Inheritance Methods
    //

    public virtual void Init(MapController map, PointData pointData)
    {
        // Deregister old events
        if (this.pointData != null)
        {
            this.pointData.OnPointDataChange -= OnPointDataChange;
            this.pointData.OnValuesChange -= OnValuesChange;
            this.pointData.OnFilterChange -= OnFilterChange;
        }

        this.pointData = pointData;

        // Register events
        pointData.OnPointDataChange += OnPointDataChange;
        pointData.OnValuesChange += OnValuesChange;
        pointData.OnFilterChange += OnFilterChange;

        InitRenderArgs();
        InitValuesBuffer();
        InitCoordsBuffer();

        canvas = GameObject.FindWithTag("Canvas").GetComponent<Canvas>();

        base.Init(map, pointData);

        // Create point mesh
        mesh = InitPointMesh();

        UpdateBounds();

        // Force an update
        if (pointData.values != null)
        {
            OnPointDataChange(pointData);
        }

        if (pointData.patch != null)
        {
            SetUserOpacity(pointData.patch.DataLayer.UserOpacity);
            SetToolOpacity(pointData.patch.DataLayer.ToolOpacity);
        }
        else
        {
            SetUserOpacity(1f);
            SetToolOpacity(1f);
        }

        //+
        if (pointData.coloring == PointData.Coloring.ReverseSingle
            || pointData.coloring == PointData.Coloring.ReverseMulti
            || pointData.coloring == PointData.Coloring.Reverse)
            SetColoringKeyword("GRADIENT_REVERSE");
    }

    public override void UpdateContent()
    {
        base.UpdateContent();

        UpdateBounds();
        UpdateShaderTransformProperties();
    }


    //
    // Public Methods
    //

    public void SetPointSize(float size)
    {
        pointSize = size;
        UpdateShaderTransformProperties();
    }

    public void SetUserOpacity(float opacity)
    {
        material.SetFloat("UserOpacity", opacity);
    }

	public void SetToolOpacity(float opacity)
	{
		material.SetFloat("ToolOpacity", opacity);
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

    public void HighlightCategory(int index)
    {
        highlightedIndex = index;
        UpdateCategoriesFilter();
    }


	//
	// Event Methods
	//

	private void OnPointDataChange(PointData pointData)
    {
        UpdateRenderArgs();

        UpdateCategories();

        OnValuesChange(pointData);
        OnFilterChange(pointData);

        UpdateCoordsBuffer();
        UpdateBounds();
        UpdateShaderTransformProperties();
    }

    protected override void OnPatchBoundsChange(PatchData patchData)
    {
        base.OnPatchBoundsChange(patchData);

        UpdateBounds();
        UpdateShaderTransformProperties();
    }

	protected void OnValuesChange(PointData pointData)
	{
		// Update min/max range
		UpdateRange();

		// Update point values
		UpdateValuesBuffer();
	}

    private void OnFilterChange(PointData pointData)
    {
        if (pointData.IsCategorized)
            UpdateCategoriesFilter();
        else
            UpdateMinMaxFilter();
    }


    //
    // Private/Protected Methods
    //

    protected Mesh InitPointMesh()
	{
        return GetComponent<MeshFilter>().sharedMesh;
    }

    private void UpdateBounds()
	{
        // Boundary surrounding the meshes we will be drawing.  Used for occlusion.
        var scale = transform.localScale;
        scale.z = scale.y; scale.y = 1;
        bounds = new Bounds(transform.position, scale);
    }

    public void UpdateRange()
    {
        // Min/max values
        float minValue, maxValue;
        if (pointData.patch == null)
        {
            minValue = pointData.minValue;
            maxValue = pointData.maxValue;
        }
        else
        {
			var layerSite = pointData.patch.SiteRecord.layerSite;
			minValue = layerSite.minValue;
			maxValue = layerSite.maxValue;
        }

		material.SetFloat("MinValue", minValue);
		material.SetFloat("InvValueRange", 1f / (maxValue - minValue));
        UpdateMaterialGamma();
    }

    protected void InitRenderArgs()
    {
        renderArgs = new DataBuffer(sizeof(uint), ComputeBufferType.IndirectArguments);
    }

    protected void UpdateRenderArgs()
    {
        // Buffer used as argument for DrawMeshInstancedIndirect
        renderArgs.Update(new uint[] {
            mesh.GetIndexCount(0),    // Number of triangle indices
            (uint)pointData.count,
            mesh.GetIndexStart(0),
            mesh.GetBaseVertex(0),
            0
        });
    }

	protected void InitValuesBuffer()
	{
        valuesBuffer = new DataBuffer(material, "Values", sizeof(float));
    }

    protected void UpdateValuesBuffer()
    {
        valuesBuffer.Update(pointData.values);
    }

    protected void InitCoordsBuffer()
    {
        coordsBuffer = new DataBuffer(material, "Coords", 2 * sizeof(int));
    }

    protected void UpdateCoordsBuffer()
    {
        int count = pointData.count;

        if (coordsData == null || coordsData.Length != count)
            coordsData = new int[count,2];

        for (int i = 0; i < count; i++)
        {
            coordsData[i, 0] = (int)(GeoCalculator.Deg2Meters * pointData.lons[i]);
            coordsData[i, 1] = (int)GeoCalculator.LatitudeToMeters(pointData.lats[i]);
        }

        coordsBuffer.Update(coordsData);
    }

    protected void UpdateShaderTransformProperties()
    {
        var minUnits = -0.5f * transform.localScale;
        var minMeters = areaCenterInMeters - areaSizeInMeters * 0.5;

        material.SetMatrix("TRS", Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one * (pointSize * canvas.scaleFactor)));
        material.SetVector("Units", new Vector4(minUnits.x, minUnits.y, map.MetersToUnits));
        material.SetInt("MinMetersX", (int)minMeters.x);
		material.SetInt("MinMetersY", (int)minMeters.y);
    }

    protected void ReleaseBuffers()
    {
        DataBuffer.Release(ref renderArgs);
        DataBuffer.Release(ref valuesBuffer);
        DataBuffer.Release(ref coordsBuffer);
    }

    public void UpdateMinMaxFilter()
    {
        // Min/max filters
        material.SetFloat("FilterMinValue", pointData.minFilter);
        material.SetFloat("FilterMaxValue", pointData.maxFilter);
    }

    protected virtual void UpdateCategoryColors()
    {
        int count = categoryColors.Length;
        for (int i = 0; i < count; i++)
        {
            categoryColors[i].a = pointData.categoryFilter.IsSetAsInt(i);
        }
        if (highlightedIndex >= 0 && pointData.categoryFilter.IsSet(highlightedIndex))
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
        if (pointData.IsCategorized && categoryColors == null)
        {
            SetColoringKeyword("CATEGORIZED");
            material.SetFloat("Categorized", 1f);

            CacheCategoryColors();
        }
    }

    private void CacheCategoryColors()
    {
        int count = pointData.categories.Length;
        if (categoryColors == null || categoryColors.Length != count)
        {
            categoryColors = new Color[count];
        }

        for (int i = 0; i < count; i++)
        {
            categoryColors[i] = pointData.categories[i].color;
        }
    }

}
