// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Scalebar : MonoBehaviour
{
    [Serializable]
    private class SavedLocationScaleIcon
    {
        [SerializeField] private Image image = default;
        [SerializeField] private HoverHandler hoverHandler = default;
        [SerializeField] private Color colorOnHover = default;
        [SerializeField] private Color colorOffHover = default;

        public HoverHandler HoverHandlr { get { return hoverHandler; } }

        public void ToggleImageColor(bool onHover)
        {
            image.color = onHover ? colorOnHover : colorOffHover;
        }
    }

    [Header("UI References")]
	[SerializeField] private SavedLocationScaleIcon savedLocationScaleIcon = default;
	public Image saveScaleIconBG;
    public RectTransform bar;
    public InputField distanceInput;
    public Text unitText;
	public ScalePanel scalePanel;

    [Header("Sizes")]
    public int minSize = 50;
    public int maxSize = 125;

    private Canvas canvas;
    private MapController map;

    private ClickHandler clickHandler;
    private Color saveScaleIconBGColor;
    private bool panelEnabled;
    private bool firstClicked;

    //
    // Unity Methods
    //

    private void Awake()
    {
        saveScaleIconBGColor = saveScaleIconBG.color;
        clickHandler = distanceInput.GetComponent<ClickHandler>();
    }

    private IEnumerator Start()
    {
        distanceInput.onEndEdit.AddListener(OnDistanceInputEndEdit);
        savedLocationScaleIcon.HoverHandlr.OnHover += OnPointerHover;
        clickHandler.OnClick += OnDistanceInputClick;

        yield return WaitFor.Frames(WaitFor.InitialFrames);

        map = ComponentManager.Instance.Get<MapController>();
        map.OnZoomChange += OnMapZoomChange;

        canvas = GameObject.FindWithTag("Canvas").GetComponent<Canvas>();

        UpdateScale();
        StartCoroutine(scalePanel.Init(this));
    }

    //
    // Event Methods
    //

    private void OnDistanceInputClick(bool isLeft)
    {
        if (!firstClicked)
        {
            distanceInput.text += "000";
            firstClicked = true;
        }
        unitText.text = "m";
    }

    private void OnDistanceInputEndEdit(string inputText)
    {
        firstClicked = false;
        UpdateScale(inputText);
    }

    private void OnPointerHover(bool isHovering)
    {
        if (isHovering)
        {
            savedLocationScaleIcon.ToggleImageColor(true);
            ToggleScalePanel(true);
        }
        else
        {
            if (panelEnabled)
                StartCoroutine(CheckIfPointerIsOutsidePanel());
        }
    }

    private void OnMapZoomChange(float zoom)
    {
        UpdateScale();
    }

    //
    // Public Methods
    //

    public void ToggleScalePanel(bool isOn)
    {
        saveScaleIconBGColor.a = isOn ? 1.0f : 0.0f;
        saveScaleIconBG.color = saveScaleIconBGColor;
        scalePanel.gameObject.SetActive(isOn);
        panelEnabled = isOn;
    }

    public double GetMapZoom(int pixels, float number)
    {
        return Math.Log(pixels * canvas.scaleFactor * GeoCalculator.InitialResolution / number) / Math.Log(2);
    }

    public void GetScalebarDistanceParams(out float d1, out float d2, out float d, out int number, out string unit)
    {
        d1 = (float)GeoCalculator.PixelsToMetes(minSize, canvas.scaleFactor, map.zoom);
        d2 = (float)GeoCalculator.PixelsToMetes(maxSize, canvas.scaleFactor, map.zoom);

        d = GetNumberAndUnit(d2, out number, out unit);
    }

    //
    // Private Methods
    //

    private IEnumerator CheckIfPointerIsOutsidePanel()
    {
        do
        {
            if (scalePanel.IsMouseOutsideScalePanel())
            {
                savedLocationScaleIcon.ToggleImageColor(false);
                ToggleScalePanel(false);
                break;
            }
            yield return null;
        }
        while (true);
    }

    private void UpdateScale(string inputText = null)
    {
        // Get new map zoom from distance input
        if (!string.IsNullOrEmpty(inputText) && !string.IsNullOrWhiteSpace(inputText))
        {
            int distance = int.Parse(inputText);
            map.SetZoom((float)GetMapZoom(maxSize, distance));
        }

        // Update scale bar and distance
        GetScalebarDistanceParams(out float d1, out float d2, out float d, out int number, out string unit);

        distanceInput.text = $"{number}";
        unitText.text = unit;

        var size = bar.sizeDelta;
        size.x = Mathf.Lerp(minSize, maxSize, Mathf.InverseLerp(d1, d2, d));
        bar.sizeDelta = size;
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
            unit = "km";
        }
        else
        {
            number *= PowerOfTen[magnitude];
            value = number;
            unit = "m";
        }
        return value;
    }
}
