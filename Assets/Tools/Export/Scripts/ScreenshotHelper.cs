// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class ScreenshotHelper
{
	public readonly Camera camera;
	public readonly Canvas canvas;
	private RenderTexture rt;
	private Texture2D screenShot;

	private readonly WindowController wndController;
	private readonly UnityAction<string, byte[]> writeFile;

	public ScreenshotHelper(WindowController wndController, UnityAction<string, byte[]> writeFileCallback)
	{
		this.wndController = wndController;
		writeFile = writeFileCallback;

		camera = Camera.main;
		canvas = GameObject.FindWithTag("Canvas").GetComponent<Canvas>();
	}

	public void Destroy()
	{
		RenderTexture.active = null;
		if (rt != null)
		{
			Object.DestroyImmediate(rt);
			rt = null;
		}
		if (screenShot != null)
		{
			Object.DestroyImmediate(screenShot);
			screenShot = null;
		}
	}

	private void CreateRenderTexture(int exportWidth, int exportHeight)
	{
		if ((rt == null) || (rt.width != exportWidth) || (rt.height != exportHeight))
		{
			if (rt != null)
				Object.DestroyImmediate(rt);

			rt = new RenderTexture(exportWidth, exportHeight, 0);
		}
	}

	private void TakeScreenshot(int x, int y, int width, int height)
	{
		RenderTexture.active = rt;
		if ((screenShot == null) || (screenShot.width != width) || (screenShot.height != height))
		{
			if (screenShot != null)
				Object.DestroyImmediate(screenShot);

			screenShot = new Texture2D(width, height, TextureFormat.RGBA32, false);
		}
		screenShot.ReadPixels(new Rect(x, y, width, height), 0, 0);
		screenShot.Apply();
		RenderTexture.active = null;
	}

	public IEnumerator TakeScreenshot(string filename, int exportWidth, int exportHeight, bool withUI)
	{
		if (withUI)
		{
			yield return new WaitForEndOfFrame();

			int scale = exportWidth / Screen.width;

			var t = ScreenCapture.CaptureScreenshotAsTexture(scale);
			var t2 = new Texture2D(t.width, t.height, TextureFormat.RGB24, false);
			t2.SetPixels(t.GetPixels());
			t2.Apply();
			writeFile(filename, t2.EncodeToPNG());
			Object.DestroyImmediate(t);
			Object.DestroyImmediate(t2);
		}
		else
		{
			var mapRect = ComponentManager.Instance.Get<MapViewArea>().Rect;
			var topBar = wndController.topBar as RectTransform;
			bool topBarActive = topBar.gameObject.activeSelf;

			// Compute canvas to export resolution ratio
			var canvasRect = canvas.GetComponent<RectTransform>().rect;
			float canvasWidthRatio = canvasRect.width / exportWidth;
			float canvasHeightRatio = canvasRect.height / exportHeight;
			float mapCameraWidth = mapRect.width / canvasWidthRatio;
			float mapCameraHeight = mapRect.height / canvasHeightRatio;

			// Scale render texture for cropped portion of screen will be exported to export resolution
			float exportMapWidthRatio = exportWidth / mapCameraWidth;
			float exportMapHeightRatio = exportHeight / mapCameraHeight;

			int scaledExportWidth = (int)(exportWidth * exportMapWidthRatio);
			int scaledExportHeight = (int)(exportHeight * exportMapHeightRatio);

			CreateRenderTexture(scaledExportWidth, scaledExportHeight);

			float offsetX = (scaledExportWidth - exportWidth) * 0.5f;
			float offsetY = topBarActive ? topBar.rect.height / canvasHeightRatio : 0.0f;
			if (!SystemInfo.graphicsUVStartsAtTop ||
                SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Metal)
				offsetY = scaledExportHeight - exportHeight - offsetY;

			camera.targetTexture = rt;
			camera.Render();
			camera.targetTexture = null;

			TakeScreenshot((int)offsetX, (int)offsetY, exportWidth, exportHeight);

			writeFile(filename, screenShot.EncodeToPNG());
		}
	}
}
