// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Scalebar : MonoBehaviour
{
	[Header("UI References")]
    public Button scaleBar;
    public RectTransform bar;
    public Text distanceText;
	public RectTransform saveScalePanel;
	public Button saveCurrScale;
	public Button lastSavedScale;

    [Header("Sizes")]
    public int minSize = 50;
    public int maxSize = 125;


    private MapController map;

    private float? savedScale;

    //
    // Unity Methods
    //

    private IEnumerator Start()
    {
        scaleBar.onClick.AddListener(OnScaleBarClick);
        saveCurrScale.onClick.AddListener(OnSaveCurrScaleClick);
        lastSavedScale.onClick.AddListener(OnLastSavedScaleClick);

        yield return WaitFor.Frames(WaitFor.InitialFrames);

        map = ComponentManager.Instance.Get<MapController>();
        map.OnZoomChange += OnMapZoomChange;

        UpdateScale();
    }


	//
	// Event Methods
	//

	private void OnScaleBarClick()
    {
        saveScalePanel.gameObject.SetActive(true);
        map.OnMapUpdate += OnMapUpdate;

		StartCoroutine(CheckIfPointerIsDown());
    }

    private void OnMapZoomChange(float zoom)
    {
        UpdateScale();
    }

    private void OnMapUpdate()
    {
        DisableSaveScalePanel();
    }

    private void OnSaveCurrScaleClick()
    {
        savedScale = map.zoom;
        lastSavedScale.interactable = (savedScale != null);
        CloseSaveScalePanel();
    }

    private void OnLastSavedScaleClick()
    {
        if (savedScale.HasValue)
        {
            map.SetZoom(savedScale.Value);
            CloseSaveScalePanel();
        }
    }


	//
	// Private Methods
	//

	private IEnumerator CheckIfPointerIsDown()
	{
		do
		{
			if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1))
			{
				DisableSaveScalePanel();
				break;
			}
			yield return null;
		}
		while (true);
	}

	private void UpdateScale()
    {
        float d1 = (float)GeoCalculator.PixelsToMetes(minSize, map.zoom);
        float d2 = (float)GeoCalculator.PixelsToMetes(maxSize, map.zoom);

        float d = GetNumberAndUnit(d2, out int number, out string unit);

        distanceText.text = number + unit;

        var size = bar.sizeDelta;
        size.x = Mathf.Lerp(minSize, maxSize, Mathf.InverseLerp(d1, d2, d));
        bar.sizeDelta = size;
    }

    private void DisableSaveScalePanel()
    {
        var saveCurrScaleRT = saveCurrScale.GetComponent<RectTransform>();
        var lastSavedScaleRT = lastSavedScale.GetComponent<RectTransform>();

        Vector2 inputPos0 = bar.InverseTransformPoint(Input.mousePosition);
        Vector2 inputPos1 = saveCurrScaleRT.InverseTransformPoint(Input.mousePosition);
        Vector2 inputPos2 = lastSavedScaleRT.InverseTransformPoint(Input.mousePosition);

        if (!bar.rect.Contains(inputPos0) &&
            !saveCurrScaleRT.rect.Contains(inputPos1) &&
            !lastSavedScaleRT.rect.Contains(inputPos2))
        {
            CloseSaveScalePanel();
        }
    }

    private void CloseSaveScalePanel()
    {
        saveScalePanel.gameObject.SetActive(false);
        map.OnMapUpdate -= OnMapUpdate;
    }

    private static readonly int[] PowerOfTen = { 1, 10, 100, 1000, 10000 };

    private static float GetNumberAndUnit(float value, out int number, out string unit)
    {
        int magnitude = 0;
        while (value >= 10f)
        {
            magnitude++;
            value *= 0.1f;
        }

        if (value >= 5)
            number = 5;
        else if (value >= 2)
            number = 2;
        else
            number = 1;

        if (magnitude > 2)
        {
            number *= PowerOfTen[magnitude - 3];
            value = number * 1000;
            unit = " km";
        }
        else
        {
            number *= PowerOfTen[magnitude];
            value = number;
            unit = " m";
        }
        return value;
    }

}
