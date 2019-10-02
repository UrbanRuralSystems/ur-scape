// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)


using UnityEngine;
using UnityEngine.UI;

public class LoadingComponent : UrsComponent
{
    public Scrollbar scrollBar;
    public Text label;

    private CanvasGroup cg;
    private float alphaChange = 0f;
    private float alpha = 0f;

    //
    // Unity Methods
    //

    protected override void Awake()
    {
        base.Awake();

        cg = GetComponent<CanvasGroup>();

        cg.alpha = alpha = 0f;
        gameObject.SetActive(false);

		LocalizationManager.WaitAndRun(InitLocalization);
	}

	private void Update()
    {
        if (alphaChange > 0 && cg.alpha < 1f)
        {
            alpha += alphaChange * Time.deltaTime;
            if (alpha >= 1f)
            {
                alpha = 1f;
                alphaChange = 0;
            }
            cg.alpha = Mathf.Pow(alpha, 4);
        }
        else if (alphaChange < 0 && cg.alpha > 0f)
        {
            alpha += alphaChange * Time.deltaTime;
            if (alpha <= 0)
            {
                gameObject.SetActive(false);
                return;
            }
            cg.alpha = Mathf.Pow(alpha, 4);
        }
    }


	//
	// Public Methods
	//

	public void Show(bool show)
    {
        if (show)
        {
            cg.alpha = alpha = 0f;
            gameObject.SetActive(true);
        }
        alphaChange = show? 3f : -2f;
    }

    public void SetLabel(string text)
    {
        label.text = text;
    }

    public void SetProgress(float p)
    {
        scrollBar.size = p;
    }


	//
	// Private Methods
	//

	private void InitLocalization()
	{
		LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
	}

	private void OnLanguageChanged()
	{
		label.text = Translator.Get("Loading") + " ...";
	}

}
