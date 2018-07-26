// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class MessageController : UrsComponent
{
    public GameObject panel; 
    public Text message;
    public ProgressBar progressBar;

    //
    // Unity Methods
    //
	
	protected override void Awake()
	{
        base.Awake();

        message.text = "";
	}

    //
    // Public Methods
    //

    public void Show(bool show)
    {
        panel.SetActive(show);
    }

    public void SetMessage(string msg)
    {
        message.text = msg;
    }

    public void SetProgress(float p)
    {
        progressBar.SetProgess(p);
    }

}
