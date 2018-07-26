// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class DistributionChart : MonoBehaviour
{
    private Material material;
	private Image image;
    private Color color;

#if USE_TEXTURE
    private Texture2D buffer = null;
    private byte[] byteArray = null;
#else
    private ComputeBuffer buffer = null;
#endif


    //
    // Unity Methods
    //

    private void OnDestroy()
    {
        if (image != null)
        {
            image.UnregisterDirtyMaterialCallback(OnMaterialChange);
        }
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
			ReleaseBuffer();
			material = mat;
            InitMaterial();
		}
    }

    //
    // Public Methods
    //

    public void Init(Color color)
    {
        this.color = color;

        image = GetComponent<Image>();

        material = new Material(image.material);
		material.hideFlags = HideFlags.HideAndDontSave;

		image.RegisterDirtyMaterialCallback(OnMaterialChange);
        image.material = material;

        // Note: this may not be true if the chart is inside a scrollview (or another mask)
        if (image.material == material)
        {
            InitMaterial();
        }
    }

    public void SetMinRange(float min)
    {
		material.SetFloat("MinRange", min);
	}

    public void SetMaxRange(float max)
    {
		material.SetFloat("MaxRange", max);
	}

	public void SetMinFilter(float min)
    {
        material.SetFloat("MinFilter", min);
	}

    public void SetMaxFilter(float max)
    {
        material.SetFloat("MaxFilter", max);
    }

    public void SetPower(float power)
    {
        material.SetFloat("Power", power);
    }

    public void SetData(float[] data, float max)
    {
		if (buffer == null ||
#if USE_TEXTURE
			buffer.width != data.Length
#else
			buffer.count != data.Length
#endif
			)
        {
            CreateBuffer(data.Length);
            material.SetInt("Count", data.Length);
		}

#if USE_TEXTURE
        System.Buffer.BlockCopy(data, 0, byteArray, 0, byteArray.Length);
        buffer.LoadRawTextureData(byteArray);
        buffer.Apply();
#else
        buffer.SetData(data);
#endif

		material.SetFloat("InvMaxValue", Mathf.Pow(max, -0.4f));
    }


    //
    // Private Methods
    //

    private void InitMaterial()
    {
        Rect rect = GetComponent<RectTransform>().rect;

        material.SetColor("Tint", color);
		material.SetFloat("InvHeight", 1f / rect.height);
	}

    private void CreateBuffer(int count)
    {
        ReleaseBuffer();

#if USE_TEXTURE
        byteArray = new byte[count * 4];
        buffer = new Texture2D(count, 1, TextureFormat.RFloat, false);
        buffer.filterMode = FilterMode.Point;
        material.SetTexture("Values", buffer);
#else
		buffer = new ComputeBuffer(count, sizeof(float), ComputeBufferType.Default);
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
        }
    }
}
