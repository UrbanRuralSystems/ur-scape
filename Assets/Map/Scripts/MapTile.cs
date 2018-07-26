// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

#if UNITY_EDITOR
#define SAFETY_CHECK
#endif

using UnityEngine;

public class MapTile : MonoBehaviour
{
    public const int Size = 512;
    public const float InvSize = 1f / Size;

    protected MapTileId tileId;
    private MeshRenderer meshRenderer;

    public MapTileId Id
    {
        get { return tileId; }
    }

    public int ZoomLevel
    {
        get { return tileId.Z; }
    }

    //
    // Unity Methods
    //


    //
    // Public Methods
    //

    public static MapTile Create(MapTileId tileId, Transform parent)
    {
        // Create empty GameObject
        GameObject tileObj = new GameObject();
        tileObj.transform.SetParent(parent, false);

        // Add components
        MeshFilter meshFilter = tileObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = tileObj.AddComponent<MeshRenderer>();
        MapTile tile = tileObj.AddComponent<MapTile>();

        // Create mesh
        Mesh mesh = meshFilter.mesh;
        mesh.vertices = new Vector3[] {
            new Vector3 (0f, 0f, 0.0f),
            new Vector3 (1f, 0f, 0.0f),
            new Vector3 (1f, -1f, 0.0f),
            new Vector3 (0f, -1f, 0.0f),
        };

        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };

        mesh.normals = new Vector3[] {
            Vector3.up,
            Vector3.up,
            Vector3.up,
            Vector3.up
        };

        mesh.uv = new Vector2[] {
            new Vector2 (0f, 1f),
            new Vector2 (1f, 1f),
            new Vector2 (1f, 0f),
            new Vector2 (0f, 0f)
        };

        // Setup mesh renderer
        meshRenderer.material = new Material(Shader.Find("Unlit/Texture"));
        meshRenderer.enabled = false;

        // Store a reference to the mesh renderer
        tile.meshRenderer = meshRenderer;

        tile.Init(tileId);

        return tile;
    }

    public void Init(MapTileId tileId)
    {
        this.tileId = tileId;
    }

    public bool IsVisible()
    {
        return meshRenderer.enabled && meshRenderer.material.mainTexture != null;
    }

    public void Show()
    {
        meshRenderer.enabled = true;
    }

    public void Hide()
    {
        meshRenderer.enabled = false;
        meshRenderer.material.mainTexture = null;
    }

    public void UpdateTile(MapTileId anchor, float anchorScale, Vector2 offset, float tileScale, float zOffset = 0f)
    {
        int zoomLevelDiff = anchor.Z - tileId.Z;
        float toMapZoomLevel = Mathf.Pow(2, Mathf.Ceil(zoomLevelDiff));

        transform.localPosition = new Vector3(
            (tileId.X * toMapZoomLevel - anchor.X) * anchorScale + offset.x,
            (tileId.Y * toMapZoomLevel - anchor.Y) * -anchorScale + offset.y,
            zoomLevelDiff * 0.00001f + zOffset // A little difference in height to allow higher zoom levels to be on top
        );

        transform.localScale = new Vector3(tileScale, tileScale, 1);
    }

    public void SetTexture(Texture texture)
    {
        Material material = meshRenderer.material;
#if SAFETY_CHECK
        if (texture == null)
        {
            Debug.LogError("Trying to assign null texture to tile " + tileId);
            return;
        }
        if (material.mainTexture != null)
        {
            Debug.LogWarning("Reassigning raster data on tile " + tileId);
        }
#endif
        material.mainTexture = texture;
        material.mainTexture.wrapMode = TextureWrapMode.Clamp;
    }

    public Texture GetTexture()
    {
        return meshRenderer.material.mainTexture;
    }


    //
    // Private Methods
    //

}
