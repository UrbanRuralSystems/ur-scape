// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections;
using System.IO;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MapCamera : UrsComponent
{
    // Bottom-left, top-left, top-right and bottom-right corners of the viewport
    private static readonly Vector3[] FullViewportPoints = {
        new Vector3(0, 0, 0),
        new Vector3(0, 1, 0),
        new Vector3(1, 1, 0),
        new Vector3(1, 0, 0),
    };
    private readonly Vector3[] viewportPoints = new Vector3[4];

    [Header("Camera")]
    public Transform pivot;

    [Header("Rotation Constraints")]
    public float MinPitchAngle = 30f;
    public float MaxPitchAngle = 90f;

    public float MinYawAngle = -135f;
    public float MaxYawAngle = 135f;

    [Header("Rotation & Zoom Scales")]
    public float HorizontalOrbitScale = 3.0f;
    public float VerticalOrbitScale = 3.0f;
    public float ZoomScale = 1f;

    [Header("Distance to Ground")]
    public float MinDistanceToTarget = 2f;

    [Header("Map")]
    [Range(2f, 15f)]
    public float MaxBoundsScale = 5f;

    [Header("Debug")]
    public bool showCameraBounds = true;

    // Reference to Camera component
    private Camera cam;
    private Transform currentPivot;

    // Reference to other components
    private MapController map;
    private MapViewArea mapViewArea;
    private InputHandler inputHandler;
	private Canvas canvas;

    // Ground point and distance to it
    private float distanceToMap = 10f;

    // Dragging
    private enum DragType
    {
        None,
        Pan,
        Orbit
    }
    private DragType dragType = DragType.None;
    private Vector3 dragWorldOrigin;

    // Ground plane (for raycasting)
    private readonly Plane plane = new Plane(Vector3.up, Vector3.zero);

	public Vector2[] BoundaryPoints { get; private set; } = new Vector2[4] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };

    private bool needToUpdateBounds = false;
    private bool needToUpdateViewport = false;
	private bool needToAdjustZoom = false;
	private Vector2 mapViewCenter = Vector2.zero;

    private int lastPixelHeight = 0;

	// Touch
	private const float Log2 = 0.30103f;
	private float initialZoom;
	private float invTouchDistance;
	private Vector2 initialTouchVector;
	private float initialCameraYaw;
	private Vector3 prevTouchCenterWorld;


    //
    // Unity Methods
    //

    private void OnEnable()
    {
        // Get camera reference
        cam = GetComponent<Camera>();
		canvas = GameObject.FindWithTag("Canvas").GetComponent<Canvas>();

		lastPixelHeight = cam.pixelHeight;

		// Double the size of the map for resolutions higher than 4K (4K = 8.85 mill pixels)
		map = FindObjectOfType<MapController>();
		if (cam.pixelWidth * cam.pixelHeight > 8850000)
		{
			map.mapScale *= 2f;
		}

		// Initialize variables
		WrapAngle(ref MinYawAngle);
        WrapAngle(ref MaxYawAngle);

        currentPivot = pivot ?? transform;

        // Initialize camera position
        UpdateDistanceToMap();

        needToUpdateBounds = true;
        needToUpdateViewport = true;
    }

    protected override void Awake()
    {
        if (Application.isPlaying)
        {
            base.Awake();
        }
    }

    private IEnumerator Start()
    {
        if (!Application.isPlaying)
            yield break;

        yield return WaitFor.Frames(WaitFor.InitialFrames);

        inputHandler = ComponentManager.Instance.Get<InputHandler>();
        mapViewArea = ComponentManager.Instance.Get<MapViewArea>();

        // Initialize Input
        if (inputHandler)
        {
            inputHandler.OnLeftMouseDragStart += OnLeftMouseDragStart;
            inputHandler.OnRightMouseDragStart += OnRightMouseDragStart;
            inputHandler.OnLeftMouseDrag += OnLeftMouseDrag;
            inputHandler.OnRightMouseDrag += OnRightMouseDrag;
            inputHandler.OnLeftMouseDragEnd += OnLeftMouseDragEnd;
            inputHandler.OnRightMouseDragEnd += OnRightMouseDragEnd;
            inputHandler.OnMouseWheel += OnMouseWheel;
        }

        // Initialize map area
        if (mapViewArea != null)
        {
            mapViewArea.OnMapViewAreaChange += OnMapViewAreaChange;
        }

        // Adjust camera position for map-view-area
        UpdateMapViewCenter();
    }

	protected override void OnDestroy()
	{
        base.OnDestroy();

		if (mapViewArea != null)
		{
			mapViewArea.OnMapViewAreaChange -= OnMapViewAreaChange;
		}
	}

	void Update()
    {
		if (Input.touchCount == 2)
		{
			Touch t1 = Input.GetTouch(0);
			Touch t2 = Input.GetTouch(1);

			var touchCenter = (t1.position + t2.position) * 0.5f;

			inputHandler.GetWorldPoint(touchCenter, out Vector3 touchCenterWorld);

			if (t1.phase == TouchPhase.Began || t2.phase == TouchPhase.Began)
			{
				initialTouchVector = t2.position - t1.position;
				initialCameraYaw = currentPivot.rotation.eulerAngles.y;

				initialZoom = map.zoom;
				invTouchDistance = 1f / (t1.position - t2.position).magnitude;
			}
			else
			{
				float distance = (t1.position - t2.position).magnitude;
				float newZoom = initialZoom + Mathf.Log10(distance * invTouchDistance) / Log2;

				var yawDiff = Angle(t2.position - t1.position, initialTouchVector) - currentPivot.rotation.eulerAngles.y + initialCameraYaw;

				map.ChangeZoom(newZoom - map.zoom, touchCenterWorld.x, touchCenterWorld.z);
				PanMap(prevTouchCenterWorld);
				Orbit(0, yawDiff);
				PanMap(-touchCenterWorld);
			}

			prevTouchCenterWorld = touchCenterWorld;
		}

#if UNITY_EDITOR
        if (needToUpdateBounds || !Application.isPlaying)
#else
        if (needToUpdateBounds)
#endif
        {
            needToUpdateBounds = false;

            if (needToUpdateViewport)
            {
				needToUpdateViewport = false;
				UpdateCameraOffset();
            }

			UpdateMapBounds(needToAdjustZoom);
			needToAdjustZoom = false;
		}
	}

	private static float Angle(Vector2 a, Vector2 b)
	{
		var an = a.normalized;
		var bn = b.normalized;
		var x = an.x * bn.x + an.y * bn.y;
		var y = an.y * bn.x - an.x * bn.y;
		return Mathf.Atan2 (y, x) * Mathf.Rad2Deg;
	}

    void OnDrawGizmos()
    {
        if (showCameraBounds)
        {
            DebugDrawCameraBounds();
        }
    }

    //
    // Inheritance Methods
    //

    public override bool HasBookmarkData()
    {
        return true;
    }

    public override void SaveToBookmark(BinaryWriter bw, string bookmarkPath)
    {
        bw.Write(currentPivot.rotation.x);
        bw.Write(currentPivot.rotation.y);
        bw.Write(currentPivot.rotation.z);
        bw.Write(currentPivot.rotation.w);

        bw.Write(distanceToMap);

        for (int i = 0; i < 4; i++)
        {
            bw.Write(BoundaryPoints[i].x);
            bw.Write(BoundaryPoints[i].y);
        }

        bw.Write(mapViewCenter.x);
        bw.Write(mapViewCenter.y);
    }

    public override void LoadFromBookmark(BinaryReader br, string bookmarkPath)
    {
        currentPivot.rotation = new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        distanceToMap = br.ReadSingle();

        for (int i = 0; i < 4; i++)
        {
			BoundaryPoints[i].x = br.ReadSingle();
			BoundaryPoints[i].y = br.ReadSingle();
        }

        mapViewCenter.x = br.ReadSingle();
        mapViewCenter.y = br.ReadSingle();

        UpdatePosition();

        needToUpdateBounds = true;
        needToUpdateViewport = true;
    }


    //
    // Public Methods
    //

    // Given an amount of pixels, calculate how many units they represent at 'distanceToMap' away from the camera
    public float PixelsToUnits(float pixels)
    {
        return pixels * 2.0f * distanceToMap * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) / cam.pixelHeight;
    }

    // Calculate how far away the camera has to be so that world-size 'units' is projected onto screen 'pixels'
    public float DistanceFromPixelsAndUnits(float pixels, float units)
    {
        return units * cam.pixelHeight / (pixels * 2.0f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
    }

    //
    // Private Methods
    //

    private void OnLeftMouseDragStart()
    {
        if (dragType == DragType.None)
        {
            dragType = DragType.Pan;
            StartPan();
        }
    }

    private void OnRightMouseDragStart()
    {
        if (dragType == DragType.None)
        {
            dragType = DragType.Orbit;
        }
    }

    private void OnLeftMouseDrag()
    {
        if (dragType == DragType.Pan)
        {
            float deltaX = inputHandler.MouseDelta.x;
            float deltaY = inputHandler.MouseDelta.y;
            if (Math.Abs(deltaX) > 0 || Math.Abs(deltaY) > 0)
            {
                Pan();
            }
        }
    }

    private void OnRightMouseDrag()
    {
        if (dragType == DragType.Orbit)
        {
            float deltaX = inputHandler.MouseDelta.x;
            float deltaY = inputHandler.MouseDelta.y;
            if (Math.Abs(deltaX) > 0 || Math.Abs(deltaY) > 0)
            {
                Orbit(deltaY * VerticalOrbitScale, deltaX * HorizontalOrbitScale);
            }
        }
    }

    private void OnLeftMouseDragEnd()
    {
        if (dragType == DragType.Pan)
        {
            dragType = DragType.None;
        }
    }

    private void OnRightMouseDragEnd()
    {
        if (dragType == DragType.Orbit)
        {
            dragType = DragType.None;
        }
    }

    private void OnMouseWheel(float delta)
    {
        if (dragType == DragType.None)
        {
            Zoom(delta);
        }
    }

    private Coroutine waitForLayout;
    private void OnMapViewAreaChange()
    {
        if (waitForLayout != null)
        {
            StopCoroutine(waitForLayout);
        }
        waitForLayout = StartCoroutine(WaitForLayoutToFinish());
    }

    private IEnumerator WaitForLayoutToFinish()
    {
        yield return WaitFor.Frames(3);
        UpdateMapViewCenter();
        waitForLayout = null;
    }

    private void UpdateMapViewCenter()
    {
		UpdateDistanceToMap();

		needToUpdateBounds = true;
        needToUpdateViewport = true;

		if (lastPixelHeight != cam.pixelHeight)
		{
			lastPixelHeight = cam.pixelHeight;
			needToAdjustZoom = true;
		}

		if (mapViewArea != null)
        {
            mapViewCenter = mapViewArea.WorldCenter();
        }
    }

    private void UpdateMapBounds(bool adjustZoom)
    {
		if (map == null)
			return;

		var west = float.MaxValue;
		var east = float.MinValue;
		var south = float.MaxValue;
		var north = float.MinValue;

		Vector3 pt;
        float maxDistance = MaxBoundsScale * distanceToMap;
        for (int i = 0; i < viewportPoints.Length; i++)
        {
			var ray = cam.ViewportPointToRay(viewportPoints[i]);
            if (plane.Raycast(ray, out float distance))
            {
                pt = ray.GetPoint(Mathf.Min(distance, maxDistance));
            }
            else
            {
                pt = ray.GetPoint(maxDistance);
			}
			BoundaryPoints[i].x = pt.x;
			BoundaryPoints[i].y = pt.z;

			west = Mathf.Min(west, BoundaryPoints[i].x);
            east = Mathf.Max(east, BoundaryPoints[i].x);
            south = Mathf.Min(south, BoundaryPoints[i].y);
            north = Mathf.Max(north, BoundaryPoints[i].y);
        }

		map.SetViewBounds(north, south, east, west, adjustZoom);
	}

    private void StartPan()
    {
        inputHandler.GetWorldPoint(Input.mousePosition, out dragWorldOrigin);
    }

    private void Pan()
    {
        if (inputHandler.GetWorldPoint(Input.mousePosition, out Vector3 point))
        {
            PanMap(dragWorldOrigin - point);
            dragWorldOrigin = point;
        }
    }

    private void PanMap(Vector3 offset)
    {
        map.MoveInUnits(offset.x, offset.z);
    }

    public void ResetRotation()
    {
        currentPivot.rotation = Quaternion.Euler(90,0,0);
        UpdatePosition();
    }

    private void Orbit(float pitch, float yaw)
    {
        Vector3 euler = currentPivot.eulerAngles + new Vector3(-pitch, yaw, 0);
        if (euler.y > 180f)
            euler.y -= 360f;

		currentPivot.rotation = Quaternion.Euler(
            Mathf.Clamp(euler.x, MinPitchAngle, MaxPitchAngle),
            Mathf.Clamp(euler.y, MinYawAngle, MaxYawAngle),
            0);

		UpdatePosition();
    }

	private void Zoom(float change)
	{
		inputHandler.GetWorldPoint(Input.mousePosition, out Vector3 zoomPoint);
		map.ChangeZoom(change, zoomPoint.x, zoomPoint.z);
	}

	private void UpdateDistanceToMap()
    {
		// Set camera distance so that MapTile.Size pixels represent 1 unit in world space
		distanceToMap = DistanceFromPixelsAndUnits(MapTile.Size, 1f / map.mapScale);
		UpdatePosition();
    }

	// This methods needs to be called when currentPivot or distanceToMap have changed
	private void UpdatePosition()
    {
        currentPivot.localPosition = currentPivot.forward * -distanceToMap;
        needToUpdateBounds = true;
    }

    private void UpdateCameraOffset()
    {
        if (mapViewArea == null)
        {
            cam.transform.localPosition = Vector3.zero;
        }
        else
        {
            var pos = cam.transform.localPosition;
            pos.x = -PixelsToUnits(mapViewCenter.x);
            pos.y = -PixelsToUnits(mapViewCenter.y);
            cam.transform.localPosition = pos;
        }

        UpdateViewportBounds();
    }

    private void UpdateViewportBounds()
    {
        if (mapViewArea == null)
        {
            for (int i = 0; i < 4; i++)
            {
                viewportPoints[i] = FullViewportPoints[i];
            }
        }
        else
        {
            mapViewArea.RectTransform.GetWorldCorners(viewportPoints);
            for (int i = 0; i < 4; i++)
            {
                viewportPoints[i] = cam.ScreenToViewportPoint(viewportPoints[i]);
            }
        }
    }

    private static void WrapAngle(ref float angle)
    {
        if (angle < -360F)
            angle += 360F;
        if (angle > 360F)
            angle -= 360F;
    }

    private void DebugDrawCameraBounds()
    {
        Gizmos.color = Color.blue;
		DebugDrawLine(BoundaryPoints[0], BoundaryPoints[1]);
		DebugDrawLine(BoundaryPoints[1], BoundaryPoints[2]);
		DebugDrawLine(BoundaryPoints[2], BoundaryPoints[3]);
		DebugDrawLine(BoundaryPoints[3], BoundaryPoints[0]);
    }

	private void DebugDrawLine(Vector2 ptA, Vector2 ptB)
	{
		Gizmos.DrawLine(new Vector3(ptA.x, 0, ptA.y), new Vector3(ptB.x, 0, ptB.y));
	}

}
