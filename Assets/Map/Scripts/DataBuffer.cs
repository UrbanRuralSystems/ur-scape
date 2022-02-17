// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using UnityEngine;

public class DataBuffer
{
#if USE_TEXTURE
    protected Texture2D buffer = null;
	private byte[] byteArray = null;
#else
    protected ComputeBuffer buffer = null;
	public ComputeBuffer Buffer { get=> buffer; }
#endif

    protected readonly int stride;
    protected readonly ComputeBufferType type;
    protected readonly Material material;
    protected readonly string propName;

    public DataBuffer(int stride, ComputeBufferType type = ComputeBufferType.Default)
    {
        this.stride = stride;
        this.type = type;
        this.material = null;
        this.propName = null;
    }

    public DataBuffer(Material material, string propName, int stride, ComputeBufferType type = ComputeBufferType.Default)
	{
        this.material = material;
        this.propName = propName;
        this.stride = stride;
        this.type = type;
    }

    public void Create(int count)
    {
        ReleaseBuffer();

#if USE_TEXTURE
        byteArray = new byte[count * stride];
        buffer = new Texture2D(count, 1, TextureFormat.RFloat, false);
		buffer.wrapMode = TextureWrapMode.Clamp;
		buffer.filterMode = FilterMode.Point;
        if (material)
            material.SetTexture(propName, buffer);
#else
        buffer = new ComputeBuffer(count, stride, type);
        if (material)
            material.SetBuffer(propName, buffer);
#endif
    }

    public void Update(Array data)
    {
        if (data == null || data.Length == 0)
        {
            ReleaseBuffer();
        }
        else
        {
            if (buffer == null ||
#if USE_TEXTURE
                data.Length != buffer.width)
#else
                data.Length != buffer.count)
#endif
            {
                Create(data.Length);
            }

#if USE_TEXTURE
            Buffer.BlockCopy(data, 0, byteArray, 0, byteArray.Length);
            buffer.LoadRawTextureData(byteArray);
            buffer.Apply();
#else
            buffer.SetData(data);
#endif
        }
    }

    private void ReleaseBuffer()
    {
        if (buffer != null)
        {
#if USE_TEXTURE
            byteArray = null;
            UnityEngine.Object.Destroy(buffer);
#else
            buffer.Release();
#endif
            buffer = null;
        }
    }

    public static void Release(ref DataBuffer dataBuffer)
    {
        if (dataBuffer != null)
        {
            dataBuffer.ReleaseBuffer();
            dataBuffer = null;
        }
    }

}
