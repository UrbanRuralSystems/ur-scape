// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using SFB;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ImportDataPanel : MonoBehaviour
{
    [Header("UI References", order = 1)]
    public GameObject propertiesPanel;
    public Text filenameLabel;
    public Button openFile;
	public RectTransform warningIcon;
	public Text warningMessage;
	public RectTransform mask;
	public RectTransform categories;

	[Header("Prefabs")]
    public Text labelPrefab;
    public InputField inputPrefab;

    [Header("Settings")]

	public string saveFilePath = "SelectedFolderPath.txt";

	//
	// Unity Methods
	//

	private void Awake()
	{
		// translator = LocalizationManager.Instance;
		openFile.onClick.AddListener(OnOpenFileClick);
	}

    private void OnOpenFileClick()
    {
        // Open folder panel
        var paths = StandaloneFileBrowser.OpenFolderPanel("Select Folder", "", false);
        
        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string selectedFolderPath = paths[0];
            Debug.Log("Selected Folder: " + selectedFolderPath);

            // Save the folder path to a .txt file
            SavePathToFile(selectedFolderPath);
        }
    }

	private void SavePathToFile(string path)
    {
        // Save the folder path to the specified .txt file
        File.WriteAllText(Application.persistentDataPath + "/" + saveFilePath, path);
        Debug.Log("Folder path saved to: " + Application.persistentDataPath + "/" + saveFilePath);
    }

/*
	private void ShowWarningMessage(string msg, Component c, bool show)
	{
		if (show)
		{
			if (warningMessages.Count == 0)
			{
				warningMessage.text = msg;
				UpdateWarningTransform(c);
			}

			if (!warningMessages.ContainsKey(msg))
				warningMessages.Add(msg, c);
		}
		else if (warningMessages.Count > 0)
		{
			warningMessages.Remove(msg);
			if (warningMessages.Count == 0)
			{
				warningMessage.text = "";
				UpdateWarningTransform(null);
			}
			else if (msg.Equals(warningMessage.text))
			{
				var en = warningMessages.GetEnumerator();
				if (en.MoveNext())
				{
					var pair = en.Current;
					warningMessage.text = pair.Key;
					UpdateWarningTransform(pair.Value);
				}
			}
		}
	}

	private void UpdateWarningTransform(Component c)
	{
		warningComponent = c;
		if (warningComponent == null)
		{
			warningIcon.gameObject.SetActive(false);
		}
		else
		{
			warningIcon.gameObject.SetActive(true);

			var rt = c.transform as RectTransform;
			float top = warningIcon.rect.height * 0.5f;
			Vector3[] corners = new Vector3[4];
			rt.GetWorldCorners(corners);
			top += (corners[0].y + corners[1].y) * 0.5f;
			(mask.parent as RectTransform).GetWorldCorners(corners);
			top -= corners[1].y;

			var offsetMin = warningIcon.offsetMin;
			var offsetMax = warningIcon.offsetMax;
			offsetMax.y = top;
			offsetMin.y = offsetMax.y - warningIcon.rect.height;
			warningIcon.offsetMin = offsetMin;
			warningIcon.offsetMax = offsetMax;
		}
	}
*/
}
