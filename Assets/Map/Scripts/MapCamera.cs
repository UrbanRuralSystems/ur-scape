// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
// Summary: 

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class MapCamera : UrsComponent
{
	public event UnityAction OnResolutionChanged;

	public enum MovementType
    {
        MoveCamera,
        MoveMap
    }

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

    [Header("Movement")]
    public MovementType movementType;

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
    public float InitialDistanceToTarget = 20f;
    public float MinDistanceToTarget = 2f;

    [Header("Map")]
    public float mapScale = 1f;
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

    // Ground point and distance to it
    private Vector3 target;
    private float distanceToTarget = 10f;
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

    private Vector3[] groundPoints;

    // Frustrum boundaries (Unity units)
    private float west = 0;
    private float east = 0;
    private float south = 0;
    private float north = 0;

    private bool needToUpdateBounds = false;
    private bool needToUpdateViewport = false;
    private Vector2 mapViewCenter;

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

        // Double the size of the map for resolutions higher than 4K (4K = 8.85 mill pixels)
        if (cam.pixelWidth * cam.pixelHeight > 8850000)
        {
            mapScale = 2f;
        }

        // Initialize variables
        WrapAngle(ref MinYawAngle);
        WrapAngle(ref MaxYawAngle);

        currentPivot = pivot == null ? transform : pivot;

        groundPoints = new Vector3[4] { Vector3.zero, Vector3.zero, Vector3.zero, Vector3.zero };

        distanceToMap = InitialDistanceToTarget;

        if (movementType == MovementType.MoveMap)
        {
            // Set camera distance so that MapTile.Size pixels represent 1 unit in world space
            distanceToMap = DistanceFromPixelsAndUnits(MapTile.Size, 1f / mapScale);
        }

        // Initialize camera position
        SetDistanceToTarget(distanceToMap);

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

        map = ComponentManager.Instance.Get<MapController>();
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
        // Check if resolution has changed
        if (lastPixelHeight != cam.pixelHeight)
        {
            lastPixelHeight = cam.pixelHeight;

            if (movementType == MovementType.MoveMap)
            {
                // Set camera distance so that MapTile.Size pixels represent 1 unit in world space
                distanceToMap = DistanceFromPixelsAndUnits(MapTile.Size, 1f / mapScale);
                UpdatePosition();
            }

            needToUpdateBounds = true;
            needToUpdateViewport = true;

			if (OnResolutionChanged != null)
				OnResolutionChanged();
		}

		if (Input.touchCount == 2)
		{
			Touch t1 = Input.GetTouch(0);
			Touch t2 = Input.GetTouch(1);

			var touchCenter = (t1.position + t2.position) * 0.5f;

			Vector3 touchCenterWorld;
			inputHandler.GetWorldPoint(touchCenter, out touchCenterWorld);

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

				PanMap(prevTouchCenterWorld);
				Zoom(newZoom - map.zoom);
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
                UpdateCameraOffset();
                needToUpdateViewport = false;
            }

            UpdateCameraBounds();

            if (map)
            {
                map.SetViewBounds(north, south, east, west);
            }
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

        bw.Write(target.x);
        bw.Write(target.y);
        bw.Write(target.z);

        bw.Write(distanceToTarget);
        bw.Write(distanceToMap);

        for (int i = 0; i < 4; i++)
        {
            bw.Write(groundPoints[i].x);
            bw.Write(groundPoints[i].y);
            bw.Write(groundPoints[i].z);
        }

        bw.Write(west);
        bw.Write(east);
        bw.Write(south);
        bw.Write(north);

        bw.Write(mapViewCenter.x);
        bw.Write(mapViewCenter.y);

        bw.Write(lastPixelHeight);
    }

    public override void LoadFromBookmark(BinaryReader br, string bookmarkPath)
    {
        currentPivot.rotation = new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());

        target.x = br.ReadSingle();
        target.y = br.ReadSingle();
        target.z = br.ReadSingle();

        distanceToTarget = br.ReadSingle();
        distanceToMap = br.ReadSingle();

        for (int i = 0; i < 4; i++)
        {
            groundPoints[i].x = br.ReadSingle();
            groundPoints[i].y = br.ReadSingle();
            groundPoints[i].z = br.ReadSingle();
        }

        west = br.ReadSingle();
        south = br.ReadSingle();
        south = br.ReadSingle();
        north = br.ReadSingle();

        mapViewCenter.x = br.ReadSingle();
        mapViewCenter.y = br.ReadSingle();

        lastPixelHeight = br.ReadInt32();

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
        needToUpdateBounds = true;
        needToUpdateViewport = true;

        if (mapViewArea != null)
        {
            var rt = mapViewArea.GetComponent<RectTransform>();
            mapViewCenter = rt.TransformPoint(rt.rect.center);
            mapViewCenter.x -= Screen.width * 0.5f;
            mapViewCenter.y -= Screen.height * 0.5f;
        }
    }

    private void UpdateCameraBounds()
    {
        west = float.MaxValue;
        east = float.MinValue;
        south = float.MaxValue;
        north = float.MinValue;

        Ray ray;
        float distance;
        Vector3 viewportPoint = Vector3.zero;

        float maxDistance = MaxBoundsScale * distanceToMap;
        for (int i = 0; i < viewportPoints.Length; i++)
        {
            ray = cam.ViewportPointToRay(viewportPoints[i]);
            if (plane.Raycast(ray, out distance))
            {
                groundPoints[i] = ray.GetPoint(Mathf.Min(distance, maxDistance));
                groundPoints[i].y = 0;
            }
            else
            {
                groundPoints[i] = ray.GetPoint(maxDistance);
                groundPoints[i].y = 0;
            }

            west = Mathf.Min(west, groundPoints[i].x);
            east = Mathf.Max(east, groundPoints[i].x);
            south = Mathf.Min(south, groundPoints[i].z);
            north = Mathf.Max(north, groundPoints[i].z);
        }
    }

    private void StartPan()
    {
        inputHandler.GetWorldPoint(Input.mousePosition, out dragWorldOrigin);
    }

    private void Pan()
    {
        Vector3 point;
        if (inputHandler.GetWorldPoint(Input.mousePosition, out point))
        {
            switch (movementType)
            {
                case MovementType.MoveCamera:
                    PanCamera(dragWorldOrigin - point);
                    break;
                case MovementType.MoveMap:
                    PanMap(dragWorldOrigin - point);
                    dragWorldOrigin = point;
                    break;
            }
        }
    }

    private void PanCamera(Vector3 offset)
    {
        target += offset;
        UpdatePosition();
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

        Quaternion quat = Quaternion.Euler(
            Mathf.Clamp(euler.x, MinPitchAngle, MaxPitchAngle),
            Mathf.Clamp(euler.y, MinYawAngle, MaxYawAngle),
            0);
        currentPivot.rotation = quat;
        UpdatePosition();
    }

    private void Zoom(float change)
    {
        switch (movementType)
        {
            case MovementType.MoveCamera:
                SetDistanceToTarget(Mathf.Max(MinDistanceToTarget, distanceToTarget - change * ZoomScale * distanceToTarget));
                break;
            case MovementType.MoveMap:
                Vector3 zoomPoint;
                inputHandler.GetWorldPoint(Input.mousePosition, out zoomPoint);
                map.ChangeZoom(change, zoomPoint.x, zoomPoint.z);
                break;
        }
    }

    private void SetDistanceToTarget(float distance)
    {
        distanceToTarget = distance;
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        switch (movementType)
        {
            case MovementType.MoveCamera:
                currentPivot.localPosition = target - currentPivot.forward * distanceToTarget;
                break;
            case MovementType.MoveMap:
                currentPivot.localPosition = currentPivot.forward * -distanceToMap;
                break;
        }
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
            (mapViewArea.transform as RectTransform).GetWorldCorners(viewportPoints);
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
        Gizmos.DrawLine(groundPoints[0], groundPoints[1]);
        Gizmos.DrawLine(groundPoints[1], groundPoints[2]);
        Gizmos.DrawLine(groundPoints[2], groundPoints[3]);
        Gizmos.DrawLine(groundPoints[3], groundPoints[0]);
    }

}
