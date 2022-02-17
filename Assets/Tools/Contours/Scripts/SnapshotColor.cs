// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SnapshotColor : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text snapshotName = default;
    [SerializeField] private Image colourImage = default;
    [SerializeField] private Button editButton = default;

    public Text SnapshotName { get { return snapshotName; } }
    public Image ColourImage { get { return colourImage; } }
    public Button EditButton { get { return editButton; } }

    //
    // Unity Methods
    //

    private void Start()
    {
        StartCoroutine(DelayedReEnableLayout(2));
    }

	//
	// Event Methods
	//

	

    //
    // Public Methods
    //



    //
    // Private Methods
    //

    private IEnumerator DelayedReEnableLayout(int numOfFrames)
    {
        yield return new WaitForFrames(numOfFrames);

		GuiUtils.RebuildLayout(transform.parent.GetComponent<LayoutGroup>().transform);
	}
}