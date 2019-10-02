// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class ProgressDialog : MonoBehaviour
{
    public Text message;
    public Text percent;

    //
    // Unity Methods
    //
	
	protected void Awake()
	{
        message.text = "";
        percent.text = "";
    }

    //
    // Public Methods
    //

    public void Close()
    {
		Destroy(gameObject);
    }

    public void SetMessage(string msg)
    {
        message.text = msg;
    }

    public void SetProgress(float p)
    {
        percent.text = Mathf.RoundToInt(p * 100f) + "%";
    }

	public void ClearProgress()
	{
		percent.text = "";
	}

}
