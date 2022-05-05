// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          David Neudecker (neudecker@arch.ethz.ch)  

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Planner : MonoBehaviour
{
    [Header("Prefabs")]
    public PulsatingCell pulsatingCellPrefab;
    public Flagger flaggerPrefab;
    public PlanningOutput outputPrefab;

    // Component references
    private MapController map;
    private DataLayers dataLayers;
    private InputHandler inputHandler;
    private MapViewArea mapViewArea;
    private OutputPanel outputPanel;
    private GridLayerController gridLayerController;

    // Other references
    private GridData grid;
	public GridData Grid => grid;
    private List<Typology> typologies;
    private PlanningSummary planningSummary = new PlanningSummary();

    // Prefab instances
    private PulsatingCell pulsatingCell;
    private Transform iconContainer;
    private Flagger flagger;
    private PlanningOutput planningOutputPanel;

    // List and maps
    private readonly Dictionary<string, TypologyEntry> typologiesMap = new Dictionary<string, TypologyEntry>();
    private readonly Dictionary<Typology, Material> materialsMap = new Dictionary<Typology, Material>();
    private readonly List<PlanningCell> planningCells = new List<PlanningCell>();
    private readonly List<PlanningGroup> planningGroups = new List<PlanningGroup>();
    private readonly Dictionary<int, PlanningCell> planningCellsMap = new Dictionary<int, PlanningCell>();
    public readonly Dictionary<string, float> targetValues = new Dictionary<string, float>();

    // Misc
    private DrawingTool drawingTool;
    private int currentTypologyIndex = 0;
    private const int EraseTypologyIndex = -2;
    private Distance gridCoordsToCell;
    private Distance gridCellToCoords;
    private int selectedCellIndex = -1;
	private Coroutine drawingArea;


	//
	// Unity Methods
	//

	private void Awake()
    {
        // Find Components
        map = ComponentManager.Instance.Get<MapController>();
        gridLayerController = map.GetLayerController<GridLayerController>();
        inputHandler = ComponentManager.Instance.Get<InputHandler>();
        outputPanel = ComponentManager.Instance.Get<OutputPanel>();
        dataLayers = ComponentManager.Instance.Get<DataLayers>();
        mapViewArea = ComponentManager.Instance.Get<MapViewArea>();
    }

    public void Init(TypologyLibrary typologyLibrary, GridData grid, List<Typology> typologies)
    {
        this.grid = grid;
        this.typologies = typologies;

        dataLayers.OnLayerVisibilityChange += OnLayerVisibilityChange;

        // Create typologies map
        foreach (var entry in typologyLibrary.typologies)
        {
            typologiesMap.Add(entry.name, entry);
        }

        // Cache grid transformations
        gridCoordsToCell = new Distance(grid.countX / (grid.east - grid.west), grid.countY / (grid.south - grid.north));
        gridCellToCoords = new Distance((grid.east - grid.west) / grid.countX, (grid.south - grid.north) / grid.countY);

        // Create pulsating cell (inside map)
        pulsatingCell = Instantiate(pulsatingCellPrefab, map.transform, false);
        pulsatingCell.name = pulsatingCellPrefab.name;
        pulsatingCell.Init((float)(gridCellToCoords.x * GeoCalculator.Deg2Meters));

        // Create icon container (inside map)
        iconContainer = new GameObject("Typology Icons").transform;
        iconContainer.SetParent(map.transform, false);

        // Attach to events
        map.OnMapUpdate += OnMapUpdate;

        // Update output panel
        planningOutputPanel = Instantiate(outputPrefab);
		planningOutputPanel.Init();
        planningOutputPanel.name = outputPrefab.name;
        planningOutputPanel.SetTypologies(typologies);
        planningOutputPanel.SetTargetValues(targetValues);
        SetOutput();
        planningOutputPanel.UpdateOutput();

		// Create flagger
		flagger = Instantiate(flaggerPrefab);
		flagger.name = flaggerPrefab.name;
		flagger.Init(grid, planningOutputPanel);
	}

    public void SetOutput(bool showOutputPanel = true)
    {
        outputPanel.AddPanel(showOutputPanel ? "Planning" : "Default",
                                showOutputPanel ? planningOutputPanel.transform : null);
    }

	private void Update()
    {
        // Check if mouse is within map area
        if (mapViewArea.IsMouseInside())
        {
            Vector3 worldPos;
            inputHandler.GetWorldPoint(Input.mousePosition, out worldPos);
            Coordinate coords = map.GetCoordinatesFromUnits(worldPos.x, worldPos.z);

            // Check if mouse is within site bounds
            if (grid.IsInside(coords.Longitude, coords.Latitude))
            {
                // Convert coords to column & row
                int cellX = (int)((coords.Longitude - grid.west) * gridCoordsToCell.x);
                int cellY = (int)((coords.Latitude - grid.north) * gridCoordsToCell.y);

                int index = cellX + cellY * grid.countX;

                // Convert cell center to coords
                coords.Longitude = (cellX + 0.5) * gridCellToCoords.x + grid.west;
                coords.Latitude = (cellY + 0.5) * gridCellToCoords.y + grid.north;

                if (drawingTool != null && !drawingTool.CanDraw)
                {
                    EnableDrawing();
                }

                if (index != selectedCellIndex)
                {
                    selectedCellIndex = index;
                    pulsatingCell.SetCoords(coords);

					if (drawingArea == null)
					{
						PlanningCell cell;
						if (planningCellsMap.TryGetValue(index, out cell))
						{
							flagger.SetSelected(cell);
						}
						else
						{
							flagger.Deselect();
						}
					}
				}

                return;
            }
        }

        if (drawingTool != null && drawingTool.CanDraw)
        {
            DisableDrawing();
        }
    }

    private void OnDestroy()
    {
        // Detach from events
        map.OnMapUpdate -= OnMapUpdate;
        dataLayers.OnLayerVisibilityChange -= OnLayerVisibilityChange;

		// Stop coroutines
		if (drawingArea != null)
		{
			StopCoroutine(drawingArea);
		}

        if (planningOutputPanel != null)
        {
            outputPanel.DestroyPanel("Planning");
            planningOutputPanel = null;
        }

        // Clear icons
        foreach (var cell in planningCells)
        {
            cell.ClearIcon();
        }

        // Destroy pulsating cell
        if (pulsatingCell != null)
        {
            Destroy(pulsatingCell.gameObject);
            pulsatingCell = null;
        }

        // Destroy containers
        if (iconContainer != null)
        {
            Destroy(iconContainer.gameObject);
            iconContainer = null;
        }

        if (flagger != null)
        {
            Destroy(flagger.gameObject);
            flagger = null;
        }

        // Destroy materials
        foreach (var mat in materialsMap)
        {
            Destroy(mat.Value);
        }

        // Clear list and maps
        typologiesMap.Clear();
        materialsMap.Clear();
        planningCells.Clear();
        planningCellsMap.Clear();
        planningGroups.Clear();
    }


    //
    // Events
    //

    private void OnMapUpdate()
    {
        UpdateIcons();
    }

    private void OnLayerVisibilityChange(DataLayer layer, bool visible)
    {
        UpdateCellsTypologyValues();
    }

    private void OnTypologyAttributesChanged(List<string> active)
    {
        UpdateFlagger(active);
    }

    private void OnDraw(DrawingInfo info)
    {
        if (info is BrushDrawingInfo)
        {
            var brushInfo = info as BrushDrawingInfo;
            OnDrawPoints(brushInfo.points);
        }
        else if (info is LassoDrawingInfo)
        {
            var lassoInfo = info as LassoDrawingInfo;
            OnDrawArea(lassoInfo.points, lassoInfo.min, lassoInfo.max);
        }
    }

    private void OnDrawPoints(List<Vector3> points)
    {
        bool changed = false;
        var typologyIndex = drawingTool.Erasing ? EraseTypologyIndex : currentTypologyIndex;
        foreach (var point in points)
        {
            Coordinate coords = map.GetCoordinatesFromUnits(point.x, point.z);

            // Check if mouse is within site bounds
            if (grid.IsInside(coords.Longitude, coords.Latitude))
            {
                // Convert coords to column & row
                var cellX = (int)((coords.Longitude - grid.west) * gridCoordsToCell.x);
                var cellY = (int)((coords.Latitude - grid.north) * gridCoordsToCell.y);

                // Convert cell center to coords
                coords.Longitude = (cellX + 0.5) * gridCellToCoords.x + grid.west;
                coords.Latitude = (cellY + 0.5) * gridCellToCoords.y + grid.north;

                int index = cellX + cellY * grid.countX;
                changed |= ChangeTypology(index, cellX, cellY, coords, typologyIndex);
            }
        }

        if (changed)
        {
            if (drawingTool.Erasing)
            {
                selectedCellIndex = -1;
                flagger.Deselect();
            }
            AfterTypologyChanged();
        }
    }

    private void OnDrawArea(List<Vector3> points, Vector3 min, Vector3 max)
    {
        drawingArea = StartCoroutine(DrawArea(points, min, max));
    }

    private IEnumerator DrawArea(List<Vector3> points, Vector3 min, Vector3 max)
    {
        // Get min/max coordinates
        Coordinate minCoords = map.GetCoordinatesFromUnits(min.x, min.z);
        Coordinate maxCoords = map.GetCoordinatesFromUnits(max.x, max.z);

        // Clamp min/max coordinates with map boundary
        minCoords.Longitude = Math.Max(minCoords.Longitude, grid.west);
        minCoords.Latitude = Math.Max(minCoords.Latitude, grid.south);
        maxCoords.Longitude = Math.Min(maxCoords.Longitude, grid.east);
        maxCoords.Latitude = Math.Min(maxCoords.Latitude, grid.north);

        int minX = (int)((minCoords.Longitude - grid.west) * gridCoordsToCell.x);
        int maxX = (int)((maxCoords.Longitude - grid.west) * gridCoordsToCell.x) + 1;

        int minY = (int)((maxCoords.Latitude - grid.north) * gridCoordsToCell.y);
        int maxY = (int)((minCoords.Latitude - grid.north) * gridCoordsToCell.y) + 1;

        int count = 0;
        bool changed = false;
        Coordinate coords = new Coordinate();
        var typologyIndex = drawingTool.Erasing ? EraseTypologyIndex : currentTypologyIndex;
        for (int y = minY; y < maxY; y++)
        {
			coords.Latitude = (y + 0.5) * gridCellToCoords.y + grid.north;

			for (int x = minX; x < maxX; x++)
            {
                coords.Longitude = (x + 0.5) * gridCellToCoords.x + grid.west;

                if (IsPointInPoly(map.GetUnitsFromCoordinates(coords), points))
                {
                    int index = x + y * grid.countX;
                    changed |= ChangeTypology(index, x, y, coords, typologyIndex);
                }

                if (++count % 100 == 0)
                {
					grid.ValuesChanged();
					yield return null;
                }
            }
        }
        if (changed)
        {
            AfterTypologyChanged();
        }

		drawingArea = null;
	}


    //
    // Public methods
    //

    public void SetDrawingTool(DrawingTool drawingTool)
    {
        DeactivateDrawingTool();

        this.drawingTool = drawingTool;

        ActivateDrawingTool();
    }

    public void SetTypology(int index)
    {
        currentTypologyIndex = index;
		planningOutputPanel.SetTypology(index);
	}

	public int GetTypology()
	{
		return currentTypologyIndex;
	}

    public PlanningCell FindCell(int index)
    {
        PlanningCell cell = null;
        planningCellsMap.TryGetValue(index, out cell);
        return cell;
    }

    public void UpdateFlagger(List<string> activeTypology)
    {
        flagger.UpdateActiveTypology(activeTypology);
    }

	public void ShowAllFlags(bool show)
	{
		flagger.RequestShowFlags(show);
	}

    public bool ChangeTypology(int cellIndex, int cellX, int cellY, Coordinate coords)
    {
        return ChangeTypology(cellIndex, cellX, cellY, coords, currentTypologyIndex);
    }

	public void FinishChangingTypologies()
	{
		AfterTypologyChanged();
	}


	//
	// Private Methods
	//

	private void UpdateIcons()
    {
        int count = planningCells.Count;
        for (int i = 0; i < count; i++)
        {
            planningCells[i].UpdateIconPosition(map);
        }
    }

    private void ActivateDrawingTool()
    {
        if (drawingTool != null)
        {
            drawingTool.OnDraw += OnDraw;
            drawingTool.Activate();

            if (drawingTool.Erasing)
                pulsatingCell.SetColor(Color.red);
            else
                pulsatingCell.SetColor(Color.white);
        }
    }

    private void DeactivateDrawingTool()
    {
        if (drawingTool != null)
        {
            if (drawingTool.CanDraw)
            {
                DisableDrawing();
            }

            drawingTool.OnDraw -= OnDraw;
            drawingTool.Deactivate();
        }
    }

    private void EnableDrawing()
    {
        drawingTool.CanDraw = true;
        if (drawingTool is BrushTool)
        {
            pulsatingCell.Show(true);
        }
    }

    private void DisableDrawing()
    {
        drawingTool.CanDraw = false;
        pulsatingCell.Show(false);
        flagger.Deselect();
    }

    private bool ChangeTypology(int cellIndex, int cellX, int cellY, Coordinate coords, int typologyIndex)
    {
        // +2 to skip noData and Emptyland
        float newTypologyValue = typologyIndex + 2;
        float oldTypologyValue = grid.values[cellIndex];

        if (oldTypologyValue != newTypologyValue)
        {
            grid.values[cellIndex] = newTypologyValue;

            if (newTypologyValue <= 1)
            {
                // Erase existing typology cell
                RemoveCell(cellIndex);
            }
            else if (oldTypologyValue <= 1)
            {
                // Create new typology cell
                AddCell(cellIndex, cellX, cellY, coords, typologyIndex);
            }
            else
            {
                // Replace old typology
                ReplaceCell(cellIndex, typologyIndex);
            }

            return true;
        }

        return false;
    }

    private void AfterTypologyChanged()
    {
        grid.ValuesChanged();

		UpdateGroups();
		flagger.UpdateData(planningSummary, planningGroups);
		UpdateCellsTypologyValues();
	}

    private void AddCell(int index, int x, int y, Coordinate coords, int typologyIndex)
    {
        PlanningCell cell = new PlanningCell(typologies[typologyIndex], coords, index, x, y, grid);
		cell.UpdatePosition(map);

		CreateTypologyIcon(cell);
        planningCells.Add(cell);
        planningCellsMap.Add(index, cell);
    }

    private void ReplaceCell(int index, int typologyIndex)
    {
        var cell = planningCellsMap[index];
        cell.UpdateTypology(typologies[typologyIndex]);
        cell.ClearIcon();
        CreateTypologyIcon(cell);
    }

    private void RemoveCell(int index)
    {
        var cell = planningCellsMap[index];
        cell.ClearIcon();

        planningCellsMap.Remove(index);
        planningCells.Remove(cell);
    }

    private void UpdateGroups()
    {
        planningGroups.Clear();

        if (planningCells.Count == 0)
            return;

        int i;
        int count = planningCells.Count;
        for (i = 0; i < count; i++)
        {
            planningCells[i].group = -1;
        }

        i = 0;
        int groupId = 0;

        while (i < count)
        {
            planningGroups.Add(CreateGroup(planningCells[i], groupId++));
           
            i++;
            while (i < count && planningCells[i].group != -1) i++;
        }

        planningSummary.SetGroups(planningGroups);
    } 

    private void AddNeighbor(int index, Queue<PlanningCell> queue, HashSet<PlanningCell> set)
    {
        PlanningCell cell;
        if (planningCellsMap.TryGetValue(index, out cell) && !set.Contains(cell) && cell.group == -1)
        {
            queue.Enqueue(cell);
            set.Add(cell);
        }
    }

    private PlanningGroup CreateGroup(PlanningCell cell, int groupId)
    {
        PlanningGroup group = new PlanningGroup(groupId, this);
        group.center = new Coordinate();

        Queue<PlanningCell> queue = new Queue<PlanningCell>();
        HashSet<PlanningCell> set = new HashSet<PlanningCell>();

        queue.Enqueue(cell);
        set.Add(cell);

        int count = 0;
        while (queue.Count > 0)
        {
            cell = queue.Dequeue();
            int index = cell.x + cell.y * grid.countX;

            count++;
            group.cells.Add(cell);
            group.center.Longitude += cell.coords.Longitude;
            group.center.Latitude += cell.coords.Latitude;
            cell.group = groupId;

            if (cell.x > 0)
            {
                // Left
                AddNeighbor(index - 1, queue, set);

                // Top-Left
                if (cell.y > 0)
                    AddNeighbor(index - 1 - grid.countX, queue, set);

                // Bottom-Left
                if (cell.y < grid.countY - 1)
                    AddNeighbor(index - 1 + grid.countX, queue, set);
            }

            if (cell.x < grid.countX - 1)
            {
                // Right
                AddNeighbor(index + 1, queue, set);

                // Top-Right
                if (cell.y > 0)
                    AddNeighbor(index + 1 - grid.countX, queue, set);

                // Bottom-Right
                if (cell.y < grid.countY - 1)
                    AddNeighbor(index + 1 + grid.countX, queue, set);
            }

            // Top
            if (cell.y > 0)
                AddNeighbor(index - grid.countX, queue, set);

            // Bottom
            if (cell.y < grid.countY - 1)
                AddNeighbor(index + grid.countX, queue, set);
        }

        double invCount = 1.0 / count;
        group.center.Longitude *= invCount;
        group.center.Latitude *= invCount;

        return group;
    }

    private void CreateTypologyIcon(PlanningCell cell)
    {
        TypologyEntry typologyEntry = null;
        typologiesMap.TryGetValue(cell.typology.name, out typologyEntry);
        if (typologyEntry == null || cell.iconType == PlanningCell.IconType.Hidden)
            return;

        var typologyIconPrefab = typologyEntry.icon;

        Material material;
        if (!materialsMap.TryGetValue(cell.typology, out material))
        {
            material = new Material(typologyIconPrefab.GetComponent<MeshRenderer>().sharedMaterial);
            material.color = cell.typology.color;
            material.SetFloat("_strength", typologyEntry.strength);
            materialsMap.Add(cell.typology, material);
        }

        var icon = Instantiate(typologyIconPrefab, iconContainer, true);
        icon.GetComponent<MeshRenderer>().material = material;

        cell.SetIcon(icon);
        cell.UpdateIconPosition(map);
    }

    private void UpdateCellsTypologyValues()
    {
        // Update cells first
        foreach (var cell in planningCells)
        {
            var index = cell.index;
            Dictionary<string, float> typologyValues = new Dictionary<string, float>();

            for (int i = 0; i < gridLayerController.transform.childCount; ++i)
            {
                var layer = gridLayerController.transform.GetChild(i).GetComponent<GridMapLayer>();
                var values = layer.Grid.values;
				var layerName = layer.Grid.patch.DataLayer.Name;
				if (!typologyValues.ContainsKey(layerName))
                   typologyValues.Add(layerName, values[index]);
            }
            cell.UpdateGridAttributes(typologyValues);
        }

        // Then update groups
        foreach (var group in planningGroups)
        {
            group.UpdateGridAttributes();
        }

        // Update summary last
        planningSummary.UpdateGridAttributes();

        // Update output panel after all planning attributes have been updated
        UpdateOutput();
    }

    public void UpdateOutput()
    {
        planningOutputPanel.SetSummaryValues(planningSummary.typology.values);
        planningOutputPanel.SetGridValues(planningSummary.GetAttributes());
		planningOutputPanel.UpdateOutput();
    }

    private static bool IsPointInPoly(Vector3 p, List<Vector3> poly)
    {
        bool c = false;
        int count = poly.Count;
        int j = count - 1;
        for (int i = 0; i < count; j = i++)
        {
            c ^= poly[i].z > p.y ^ poly[j].z > p.y && p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].z) / (poly[j].z - poly[i].z) + poly[i].x;
        }
        return c;
    }
}
