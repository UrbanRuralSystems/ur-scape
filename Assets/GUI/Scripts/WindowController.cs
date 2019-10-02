// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          David Neudecker(neudecker@arch.ethz.ch)

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
#define IS_WINDOWS
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
#define IS_OSX
#endif

using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public class WindowController : MonoBehaviour
{
	public Transform topBar;

	public Text versionLabel;
	public Button closeButton;
	public Button fullscreenButton;
	public Button minimizeButton;

#if IS_WINDOWS
	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

	[DllImport("user32.dll")]
	private static extern IntPtr GetActiveWindow();
#endif

	//
	// Unity Methods
	//

	private void Start()
	{
#if UNITY_WEBGL
		minimizeButton.onClick.AddListener(OnExitWebFullScreenClick);
		fullscreenButton.onClick.AddListener(OnExitWebFullScreenClick);
		closeButton.onClick.AddListener(OnExitWebFullScreenClick);
#else
		minimizeButton.onClick.AddListener(OnMinimizeClick);
		fullscreenButton.interactable = false;
		closeButton.onClick.AddListener(OnCloseClick);
#endif

		topBar.gameObject.SetActive(Screen.fullScreenMode != FullScreenMode.Windowed && -1 != (int)Screen.fullScreenMode);

		StartCoroutine(CheckFullScreenChange());

		versionLabel.text += "   v" + Application.version;
	}


    //
    // Event Methods
    //

    private IEnumerator CheckFullScreenChange()
	{
        var delay = new WaitForSeconds(1f);

		bool fullScreen = Screen.fullScreen;
        var mode = Screen.fullScreenMode;
		while (true)
		{
			yield return delay;
			if (fullScreen != Screen.fullScreen || mode != Screen.fullScreenMode)
			{
				fullScreen = Screen.fullScreen;
                mode = Screen.fullScreenMode;
                topBar.gameObject.SetActive(mode != FullScreenMode.Windowed && -1 != (int)Screen.fullScreenMode);
            }
		}
	}

	private void OnExitWebFullScreenClick()
	{
		OnFullscreenClick();
	}

	private void OnCloseClick()
	{
		Quit();
	}

	private void OnFullscreenClick()
	{
		topBar.gameObject.SetActive(!Screen.fullScreen);
		Screen.fullScreen = !Screen.fullScreen;
	}

	private void OnMinimizeClick()
	{
#if IS_WINDOWS
		ShowWindow(GetActiveWindow(), 2);
#else
		Screen.fullScreen = false;
#endif
	}


	//
	// Private Methods
	//

	private void Quit()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
	}
}
