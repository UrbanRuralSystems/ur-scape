// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine.UI;

public class Progress
{
	private readonly float interval;
	private float progress = 0;
	private int index = 0;
	private Scrollbar progressBar = null;

	public bool IsCancelled => index == 0 && progress > 0;

	public Progress(int count, Scrollbar bar = null)
	{
		progressBar = bar;
		interval = 1f / count;

		progressBar.gameObject.SetActive(true);
	}

	public bool Update(string msg)
	{
#if UNITY_EDITOR1
			if (!Next())
				return false;
			if (!DisplayEditorProgressBar(msg, progress))
			{
				Stop();
				return false;
			}
			return true;
#else
		return Next();
#endif
	}

#if UNITY_EDITOR1
		public static bool DisplayEditorProgressBar(string msg, float progress)
		{
			if (UnityEditor.EditorUtility.DisplayCancelableProgressBar("Export", msg + "  " + (int)(progress * 100) + " %", progress))
			{
				UnityEditor.EditorUtility.ClearProgressBar();
				return false;
			}
			return true;
		}
#endif

	public bool Next()
	{
		progress = ++index * interval;
		if (progress > 1)
		{
			Stop();
			return false;
		}

		if (!progressBar.gameObject.activeSelf)
			progressBar.gameObject.SetActive(true);

		progressBar.size = progress;
		return true;
	}

	public void Stop()
	{
		progressBar.size = 0;
		index = 0;
		progressBar.gameObject.SetActive(false);

#if UNITY_EDITOR1
			UnityEditor.EditorUtility.ClearProgressBar();
#endif
	}
}
