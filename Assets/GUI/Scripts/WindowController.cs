// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          David Neudecker(neudecker@arch.ethz.ch)

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

	[DllImport("user32.dll")]
	private static extern bool ShowWindow(IntPtr hwnd, int nCmdShow);

	[DllImport("user32.dll")]
	private static extern IntPtr GetActiveWindow();


	//
	// Unity Methods
	//

	private void Start()
	{
		fullscreenButton.onClick.AddListener(OnFullscreen);

#if UNITY_WEBGL
		closeButton.interactable = false;
		minimizeButton.interactable = false;
#else
		closeButton.onClick.AddListener(OnClose);
		minimizeButton.onClick.AddListener(OnMinimize);
#endif
		topBar.gameObject.SetActive(Screen.fullScreen);

		StartCoroutine(CheckFullScreen());

		versionLabel.text += "   v" + Application.version;
	}


	//
	// Event Methods
	//

	private static readonly IEnumerator framesToWait = WaitFor.Frames(120);
	private IEnumerator CheckFullScreen()
	{
		bool fullScreen = Screen.fullScreen;
		while (true)
		{
			yield return framesToWait;
			if (fullScreen != Screen.fullScreen)
			{
				fullScreen = Screen.fullScreen;
				topBar.gameObject.SetActive(fullScreen);
			}
		}
	}

	private void OnClose()
	{
		Quit();
	}

	private void OnFullscreen()
	{
		topBar.gameObject.SetActive(!Screen.fullScreen);
		Screen.fullScreen = !Screen.fullScreen;
	}

	private void OnMinimize()
	{
		ShowWindow(GetActiveWindow(), 2);
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
