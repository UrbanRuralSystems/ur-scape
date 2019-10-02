// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Hong Liang  (hliang@student.ethz.ch)
//			Michael Joos  (joos@arch.ethz.ch)

#if UNITY_EDITOR
#define SAFETY_CHECK
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaceStart : MonoBehaviour
{
	[Header("Prefabs")]
	public MarkerContainer markerContainerPrefab;

    // Component references
    private InputHandler inputHandler;
    protected MapController map;

    // Misc
    private GraphPatch networkPatch;
    public GraphPatch NetworkPatch
	{
        get { return networkPatch; }
        set { networkPatch =  value; }
    }
    private GridPatch reachabilityPatch;
    public GridPatch ReachabilityPatch
	{
		get { return reachabilityPatch; }
		set { reachabilityPatch = value; }
    }

    private Dictionary<int, Coroutine> gridGenerations = new Dictionary<int, Coroutine>();
    private float[] minutesPerMeter;	// Inverse of speed
    private float travelTime;

	private List<Coordinate> startPoints = new List<Coordinate>();
	public bool HasStartPoints
	{
		get { return startPoints.Count > 0; }
	}

	private MarkerContainer markerContainer;
    private List<RoadLayer> newRoads;

    private bool isActive = false;
    public bool IsActive
    {
        get { return isActive; }
        set { isActive = value; }
    }

	public bool isMultiStart = false;


	//
	// Unity Methods
	//

	private void Awake()
    {
        inputHandler = ComponentManager.Instance.Get<InputHandler>();
	}

	private void OnDestroy()
	{
		if (markerContainer != null)
		{
			Destroy(markerContainer.gameObject);
			markerContainer = null;
		}
	}


	//
	// Public Method
	//

	public void Init(GraphPatch networkPatch, GridPatch reachabilityPatch, float[] minutesPerMeter, float travelTime)
    {
        this.networkPatch = networkPatch;
        this.reachabilityPatch = reachabilityPatch;
        this.map = ComponentManager.Instance.Get<MapController>();
        this.minutesPerMeter = minutesPerMeter;
		this.travelTime = travelTime;
	}

	public void Activate()
	{
		isActive = true;

		inputHandler.OnLeftMouseUp += OnSetPoint;
        inputHandler.OnRightMouseUp += OnCancel;

		if (markerContainer == null)
		{
			markerContainer = Instantiate(markerContainerPrefab, map.transform, false);
			markerContainer.Init();
		}
	}

	public void Deactivate()
    {
        if (isActive)
        {
            inputHandler.OnLeftMouseUp -= OnSetPoint;
            inputHandler.OnRightMouseUp -= OnCancel;
            isActive = false;
        }    
    }

    public void SetMinutesPerMeter(float[] minutesPerMeter)
    {
        this.minutesPerMeter = minutesPerMeter;
    }

    public void SetNewRoads(List<RoadLayer> newRoads)
    {
        this.newRoads = newRoads;
    }

	public void Clear()
	{
		ClearGrid();
		CommitPatchChanges();
	}

	public void SetTravelTime(float time)
	{
		travelTime = time;
	}

	public void StopGridGenerations()
	{
		foreach (var generation in gridGenerations.Values)
		{
			StopCoroutine(generation);
		}
		gridGenerations.Clear();
	}


	//
	// Private Method
	//

	private void OnSetPoint()
    {
        if (!inputHandler.IsDraggingLeft && !inputHandler.IsDraggingRight)
        {
            if (reachabilityPatch == null)
            {
                Debug.LogError("No available site to set the start point");
            }
            else
            {
                Vector3 worldPos;
                if (inputHandler.GetWorldPoint(Input.mousePosition, out worldPos))
                {
					bool isFirstPoint = !isMultiStart || startPoints.Count == 0;
					SetPoint(map.GetCoordinatesFromUnits(worldPos.x, worldPos.z), isFirstPoint);
				}
            }
        }
    }

	private void OnCancel()
	{
		Deactivate();
	}

	private void SetPoint(Coordinate coord, bool isFirstPoint)
    {
#if SAFETY_CHECK
		if (networkPatch == null || reachabilityPatch == null)
        {
            Debug.LogWarning("Network layer or reachability layer is not available");
            return;
        }
#endif
		if (networkPatch.graph.IsInside(coord.Longitude, coord.Latitude))
        {
			if (isFirstPoint)
			{
				startPoints.Clear();
				markerContainer.ClearMarkers();

				StopGridGenerations();
				PrepareGrid();
			}

			startPoints.Add(coord);
			markerContainer.AddMarker(coord);

			GenerateGrid(coord.Longitude, coord.Latitude, startPoints.Count - 1);
		}
    }

    public void UpdateGrid()
    {
#if SAFETY_CHECK
		if (networkPatch == null || reachabilityPatch == null)
        {
            Debug.LogWarning("Network layer or reachability layer is not available");
            return;
        }
#endif

		StopGridGenerations();
		PrepareGrid();

		for (int i = 0; i < startPoints.Count; i++)
        {
            GenerateGrid(startPoints[i].Longitude, startPoints[i].Latitude, i);
        }
    }

	private void ClearGrid()
	{
		// Init grid values
		GridData grid = reachabilityPatch.grid;
		grid.maxValue = travelTime;
		grid.units = "minutes";

		if (grid.minFilter != 0 || grid.maxFilter != grid.maxValue)
			reachabilityPatch.SetMinMaxFilter(0, grid.maxValue);

		for (int i = grid.countX * grid.countY - 1; i >= 0; i--)
		{
			grid.values[i] = float.MaxValue;
			grid.valuesMask[i] = 0;
		}
	}

	private void InitNodesCost(GraphData graph)
	{
		foreach (var node in graph.nodes)
		{
			node.cost = float.MaxValue;
		}
	}

	private void PrepareGrid()
	{
		ClearGrid();
		InitNodesCost(networkPatch.graph);
	}

	private void GenerateGrid(double lon, double lat, int id)
    {
        GraphNode startNode = null;
        float minDistanceToNode = float.MaxValue;

        var gridMapLayer = reachabilityPatch.GetMapLayer() as GridMapLayer;

		var gridIndex = networkPatch.GetIndex(lon, lat);
		if (!networkPatch.graph.indexToNode.TryGetValue(gridIndex, out startNode))
		{
			// Find closest node
			foreach (var node in networkPatch.graph.nodes)
			{
				float distanceToNode = (float)((node.longitude - lon) * (node.longitude - lon) + (node.latitude - lat) * (node.latitude - lat));
				if (distanceToNode < minDistanceToNode)
				{
					minDistanceToNode = distanceToNode;
					startNode = node;
				}
			}
		}
		startNode.cost = 0;

        // Traverse all graph and fill in the grid
		gridGenerations.Add(id, StartCoroutine(Traverse(startNode, 1000, gridMapLayer, id)));
    }

    private IEnumerator Traverse(GraphNode first, int countPerFrame, GridMapLayer gridMapLayer, int id)
    {
		GraphPatch graphPatch = networkPatch;
		GraphData graph = graphPatch.graph;
        GridData grid = reachabilityPatch.grid;

        Queue<GraphNode> nodes = new Queue<GraphNode>();
        HashSet<GraphNode> nodesSet = new HashSet<GraphNode>();

        nodes.Enqueue(first);
        nodesSet.Add(first);

        int count = 0;
        float maxValue = 0f;

        int index = graphPatch.GetIndex(first);
        grid.values[index] = 0f;
		grid.valuesMask[index] = 1;

        while (nodes.Count > 0)
        {
            GraphNode node = nodes.Dequeue();
            nodesSet.Remove(node);

            index = graphPatch.GetIndex(node);

            HashSet<int> overlapedLink = new HashSet<int>();

            for (int k = 0; k < newRoads.Count; k++)
            {
                GraphData newGraph = newRoads[k].graph;
				// if the node is overlapped by new road
				if (newRoads[k].graph.indexToNode.ContainsKey(index))
				{
					GraphNode newNode = newGraph.indexToNode[index];

                    for (int i = newNode.links.Count - 1; i >= 0; i--)
                    {
                        index = graphPatch.GetIndex(newNode.links[i]); // neighbour index
                        overlapedLink.Add(index);

                        if (!graph.indexToNode.ContainsKey(index))
                            index = -index;

                        GraphNode neighbour = graph.indexToNode[index];

                        float invSpeed = GetMinutesPerMeter(newNode.classifications[i]);
                        float cost = newNode.distances[i] * invSpeed + node.cost;
                        if (cost < neighbour.cost && cost <= grid.maxFilter)
                        {
                            neighbour.cost = cost;
                            maxValue = Mathf.Max(maxValue, cost);

                            index = graphPatch.GetIndex(neighbour);
                            grid.values[index] = Math.Min(cost, grid.values[index]);
							grid.valuesMask[index] = 1;

                            if (!nodesSet.Contains(neighbour))
                            {
                                nodes.Enqueue(neighbour);
                                nodesSet.Add(neighbour);
                            }

                            count++;
                        }
                    }
                }
            }
            
            for (int i = node.links.Count - 1; i >= 0; i--)
            {
                GraphNode neighbour = node.links[i];

                index = graphPatch.GetIndex(neighbour);

                if (overlapedLink.Contains(index))
                    continue;

				float invSpeed = GetMinutesPerMeter(node.classifications[i]);

                // Calculate time: cost (minutes) = distance (meters) / speed (meters/minute)
                float cost = node.distances[i] * invSpeed + node.cost;
                if (cost < neighbour.cost && cost <= grid.maxFilter)
                {
                    neighbour.cost = cost;
                    maxValue = Mathf.Max(maxValue, cost);

                    index = graphPatch.GetIndex(neighbour);
                    grid.values[index] = Math.Min(cost, grid.values[index]);
					grid.valuesMask[index] = 1;

                    if (!nodesSet.Contains(neighbour))
                    {
                        nodes.Enqueue(neighbour);
                        nodesSet.Add(neighbour);
                    }

                    count++;
                }
            }

            if (count > countPerFrame)
            {
                count = 0;
                grid.ValuesChanged();
				yield return null;
            }
        }

        yield return null;

		CommitPatchChanges();

		gridGenerations.Remove(id);
    }

	private float GetMinutesPerMeter(int classificationValue)
	{
		if (classificationValue == ClassificationValue.None) // no network
			return minutesPerMeter[0];

		for (int j = ClassificationIndex.HighwayLink; j > 0; --j)
		{
			if (classificationValue >= (1 << (j - 1)))
			{
				return minutesPerMeter[j];
			}
		}

		return 0;
	}

	private void CommitPatchChanges()
	{
		var grid = reachabilityPatch.grid;

		// Warning: update reachability's max (for Filter Panel)
		reachabilityPatch.SiteRecord.layerSite.maxValue = grid.maxValue;

		grid.maxValue = grid.maxFilter;
		grid.UpdateDistribution(true);
		grid.ValuesChanged();
	}

}
