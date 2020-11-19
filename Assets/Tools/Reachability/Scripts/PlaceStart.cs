// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
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

    private Coroutine gridGeneration;
	private float[] classificationToMinutesPerMeter;
	private float travelTime;
	
	public bool SnapToNetwork { get; set; } = false;

	private readonly List<Coordinate> startPoints = new List<Coordinate>();
	public bool HasStartPoints => startPoints.Count > 0;

	private MarkerContainer markerContainer;
    private List<RoadLayer> newRoads;

    private bool isActive = false;
    public bool IsActive
    {
        get { return isActive; }
        set { isActive = value; }
    }

	public bool isMultiStart = false;

	private static readonly int[] indexToOffsetXMap = new int[8]
	{
		-1,
		0,
		+1,
		-1,
		+1,
		-1,
		0,
		+1,
	};
	private static readonly int[] indexToOffsetYMap = new int[8]
	{
		-1,
		-1,
		-1,
		0,
		0,
		+1,
		+1,
		+1,
	};


	//
	// Unity Methods
	//

	private void Awake()
    {
        inputHandler = ComponentManager.Instance.Get<InputHandler>();
		classificationToMinutesPerMeter = new float[(int)Math.Pow(2, ClassificationValue.Count - 1)];
	}

	private void OnDestroy()
	{
		StopGridGeneration();

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
		
		SetMinutesPerMeter(minutesPerMeter);
		SetTravelTime(travelTime);
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
		// Update Classification-to-MinutesPerMeter map
		classificationToMinutesPerMeter[0] = minutesPerMeter[0];
		int count = classificationToMinutesPerMeter.Length;
		int classCount = ClassificationValue.Count - 1;
		for (int i = 1; i < count; i++)
		{
			classificationToMinutesPerMeter[i] = float.MaxValue;
			for (int j = 0; j < classCount; j++)
			{
				if ((i & (1 << j)) != 0)
				{
					var val = minutesPerMeter[j + 1];
					if (val < classificationToMinutesPerMeter[i])
						classificationToMinutesPerMeter[i] = val;
				}
			}
		}
	}

	public void SetTravelTime(float time)
	{
		travelTime = time;
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

	public void StopGridGeneration()
	{
		if (gridGeneration != null)
		{
			StopCoroutine(gridGeneration);
			gridGeneration = null;
		}
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
                if (inputHandler.GetWorldPoint(Input.mousePosition, out Vector3 worldPos))
                {
					SetPoint(map.GetCoordinatesFromUnits(worldPos.x, worldPos.z));
				}
            }
        }
    }

	private void OnCancel()
	{
		Deactivate();
	}

	private void SetPoint(Coordinate coord)
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
			if (!isMultiStart)
			{
				startPoints.Clear();
				markerContainer.ClearMarkers();
			}

			startPoints.Add(coord);
			markerContainer.AddMarker(coord);

			UpdateGrid();
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

		StopGridGeneration();
		GenerateGrid();
    }

	private void ClearGrid()
	{
		// Init grid values
		GridData grid = reachabilityPatch.grid;
		grid.maxValue = travelTime;
		grid.units = "minutes";

		if (grid.minFilter != 0 || grid.maxFilter != grid.maxValue)
			reachabilityPatch.SetMinMaxFilter(0, grid.maxValue);

		// Warning: update reachability's max (for Filter Panel)
		reachabilityPatch.SiteRecord.layerSite.maxValue = reachabilityPatch.grid.maxValue;

		for (int i = grid.countX * grid.countY - 1; i >= 0; i--)
		{
			grid.values[i] = float.MaxValue;
			grid.valuesMask[i] = 0;
		}
	}

	private void InitNodesCost(GraphData graph)
	{
		int count = graph.nodes.Count;
		for (int i = 0; i < count; ++i)
			graph.nodes[i].cost = float.MaxValue;
	}

	private void PrepareGrid()
	{
		ClearGrid();
		InitNodesCost(networkPatch.graph);
	}

	private void GenerateGrid()
    {
		// Traverse all graph and fill in the grid
		gridGeneration = StartCoroutine(Traverse(10000));
    }

	private void FindClosestNode(double lon, double lat, ref int startIndex)
	{
		float minDistanceToNode = float.MaxValue;

		// Find closest node
		foreach (var node in networkPatch.graph.nodes)
		{
			float distanceToNode = (float)((node.longitude - lon) * (node.longitude - lon) + (node.latitude - lat) * (node.latitude - lat));
			if (distanceToNode < minDistanceToNode)
			{
				minDistanceToNode = distanceToNode;
				startIndex = node.index;
			}
		}
	}

	private IEnumerator Traverse(int countPerFrame)
    {
		GraphPatch graphPatch = networkPatch;
		GraphData graph = graphPatch.graph;
		GridData grid = reachabilityPatch.grid;

		// Initialize the grid values and the node costs to infinite
		PrepareGrid();

		yield return null;

		// Prepare the starting cells/nodes
		var nodes = new Queue<int>();
		var nodesSet = new HashSet<int>();
		foreach (var coord in startPoints)
		{
			// For each point find its grid index
			var startIndex = networkPatch.GetIndex(coord.Longitude, coord.Latitude);

			// Check if highway node is available and we're allowed to be on it
			if (graph.indexToNode.TryGetValue(-startIndex, out GraphNode highwayNode) &&
				classificationToMinutesPerMeter[highwayNode.classifications] > 0)
			{
				startIndex = -startIndex;
			}

			// Snap to the nearest network node?
			if (SnapToNetwork)
			{
				if (startIndex > 0 && !graph.indexToNode.ContainsKey(startIndex))
				{
					FindClosestNode(coord.Longitude, coord.Latitude, ref startIndex);
					yield return null;
				}
			}

			// Initialize the cell value and node cost
			int gridIndex = Math.Abs(startIndex);
			grid.values[gridIndex] = 0f;
			grid.valuesMask[gridIndex] = 1;
			if (graph.indexToNode.TryGetValue(startIndex, out GraphNode first))
				first.cost = 0;

			nodes.Enqueue(startIndex);
			nodesSet.Add(startIndex);
		}

		// Prepare data for off-grid computation
		GeoCalculator.GetDistanceInMeters(grid.west, grid.south, grid.east, grid.north, out double cellSizeX, out double cellSizeY);
		cellSizeX /= grid.countX;
		cellSizeY /= grid.countY;
		float distanceXY = (float)Math.Sqrt(cellSizeX * cellSizeX + cellSizeY * cellSizeY);
		float distanceX = (float)cellSizeX;
		float distanceY = (float)cellSizeY;
		int lastIndexX = grid.countX - 1;
		int lastIndexY = grid.countY - 1;

		var visitedNeighbours = new bool[8];
		var indexToOffsetMap = new int[8]
		{
			-grid.countX - 1,
			-grid.countX,
			-grid.countX + 1,
			-1,
			+1,
			grid.countX - 1,
			grid.countX,
			grid.countX + 1,
		};
		var offsetToIndexMap = new Dictionary<int, int>()
		{
			{ -grid.countX - 1, 0 },
			{ -grid.countX, 1 },
			{ -grid.countX + 1, 2 },
			{ -1, 3 },
			{ +1, 4 },
			{ grid.countX - 1, 5 },
			{ grid.countX, 6 },
			{ grid.countX + 1, 7 }
		};
		var linkDistances = new float[8]
		{
			distanceXY,
			distanceY,
			distanceXY,
			distanceX,
			distanceX,
			distanceXY,
			distanceY,
			distanceXY,
		};
		float invWalkingSpeed = classificationToMinutesPerMeter[0];


		int count = 0;
		int nodeIndex, neighbourIndex;
		var overlapedLink = new HashSet<int>();

		// Start going thru the nodes
		while (nodes.Count > 0)
        {
			nodeIndex = nodes.Dequeue();
            nodesSet.Remove(nodeIndex);

			// Check if it's a node. It could be an empty cell when going off-track
			if (graph.indexToNode.TryGetValue(nodeIndex, out GraphNode node))
			{
				nodeIndex = Math.Abs(node.index);

				overlapedLink.Clear();

				for (int k = 0; k < newRoads.Count; k++)
				{
					var newGraph = newRoads[k].graph;

					// if the node is overlapped by new road
					if (newRoads[k].graph.indexToNode.ContainsKey(nodeIndex))
					{
						var newNode = newGraph.indexToNode[nodeIndex];

						for (int i = newNode.links.Count - 1; i >= 0; i--)
						{
							neighbourIndex = Math.Abs(newNode.links[i].index);
							overlapedLink.Add(neighbourIndex);

							if (!graph.indexToNode.ContainsKey(neighbourIndex))
								neighbourIndex = -neighbourIndex;

							var neighbour = graph.indexToNode[neighbourIndex];

							// Calculate time: cost (minutes) = distance (meters) / speed (meters/minute)
							float invSpeed = classificationToMinutesPerMeter[newNode.linkClassifications[i]];
							float cost = newNode.linkDistances[i] * invSpeed + node.cost;

							if (cost < neighbour.cost && cost <= grid.maxFilter)
							{
								neighbour.cost = cost;
								grid.values[neighbourIndex] = Math.Min(cost, grid.values[neighbourIndex]);
								grid.valuesMask[neighbourIndex] = 1;

								if (!nodesSet.Contains(neighbour.index))
								{
									nodes.Enqueue(neighbour.index);
									nodesSet.Add(neighbour.index);
								}

								count++;
							}
						}
					}
				}
            
				for (int i = node.links.Count - 1; i >= 0; i--)
				{
					var neighbour = node.links[i];
					neighbourIndex = Math.Abs(neighbour.index);

					var indexOffset = neighbourIndex - nodeIndex;
					var idx = offsetToIndexMap[indexOffset];
					visitedNeighbours[idx] = true;

					if (overlapedLink.Contains(neighbourIndex))
						continue;

					// Calculate time: cost (minutes) = distance (meters) / speed (meters/minute)
					float invSpeed = classificationToMinutesPerMeter[node.linkClassifications[i]];
					float cost = node.linkDistances[i] * invSpeed + node.cost;
					if (cost < neighbour.cost && cost <= travelTime)
					{
						neighbour.cost = cost;
						// Note: a cell may have 2 nodes (e.g. primary + highway). Therefore we need to check the min value
						grid.values[neighbourIndex] = cost < grid.values[neighbourIndex]? cost : grid.values[neighbourIndex];
						grid.valuesMask[neighbourIndex] = 1;

						if (!nodesSet.Contains(neighbour.index))
						{
							nodes.Enqueue(neighbour.index);
							nodesSet.Add(neighbour.index);
						}

						count++;
					}
				}
			}

			// Calculate off-track
			if (node == null || node.classifications < ClassificationValue.HighwayLink)
			{
				int nodeY = nodeIndex / grid.countX;
				int nodeX = nodeIndex - nodeY * grid.countX;
				float nodeCost = node == null? grid.values[nodeIndex] : node.cost;

				// Check non-visited neighbours
				for (int i = 0; i < 8; ++i)
				{
					if (!visitedNeighbours[i])
					{
						int x = nodeX + indexToOffsetXMap[i];
						int y = nodeY + indexToOffsetYMap[i];
						if (x < 0 || y < 0 || x > lastIndexX || y > lastIndexY)
							continue;

						neighbourIndex = nodeIndex + indexToOffsetMap[i];

						// Calculate time: cost (minutes) = distance (meters) / speed (meters/minute)
						float cost = linkDistances[i] * invWalkingSpeed + nodeCost;
						if (cost < grid.values[neighbourIndex] && cost <= travelTime)
						{
							bool valid = false;
							if (graph.indexToNode.TryGetValue(neighbourIndex, out GraphNode neighbour))
							{
								if (neighbour.classifications < ClassificationValue.HighwayLink)
								{
									neighbour.cost = cost;
									valid = true;
								}
							}
							else
							{
								valid = true;
							}

							if (valid)
							{
								grid.values[neighbourIndex] = cost;
								grid.valuesMask[neighbourIndex] = 1;
								if (!nodesSet.Contains(neighbourIndex))
								{
									nodes.Enqueue(neighbourIndex);
									nodesSet.Add(neighbourIndex);
								}
							}

							count++;
						}
					}
					else
					{
						visitedNeighbours[i] = false;
					}
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

		gridGeneration = null;
    }

	private void CommitPatchChanges()
	{
		var grid = reachabilityPatch.grid;

		grid.UpdateDistribution();
		grid.ValuesChanged();
	}

}
