// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class GraticulePanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Toggle metersToggle = default;
    [SerializeField] private Toggle degreesToggle = default;
    [SerializeField] private Toggle lockedToggle = default;
    [SerializeField] private Toggle showGridToggle = default;
    [SerializeField] private InputField intervalX = default;
    [SerializeField] private InputField intervalY = default;
    [SerializeField] private RectTransform neckRT = default;
    [SerializeField] private RectTransform panelRT = default;

    //[Header("Prefabs")]

    public Toggle DegreesToggle { get { return degreesToggle; } }
    public bool IsDegrees { private set; get; }
    public Toggle LockedToggle { get { return lockedToggle; } }
    public Toggle ShowGridToggle { get { return showGridToggle; } }
    public InputField IntervalX { get { return intervalX; } }
    public InputField IntervalY { get { return intervalY; } }

    public float? PrevMetersX { set; get; }
    public float? PrevMetersY { set; get; }
    public float? PrevDegreesX { set; get; }
    public float? PrevDegreesY { set; get; }

    //
    // Unity Methods
    //

    private void Awake()
    {
        degreesToggle.onValueChanged.AddListener(OnDegreesToggleChanged);
        gameObject.SetActive(false);
    }

    //
    // Event Methods
    //

    private void OnDegreesToggleChanged(bool isOn)
    {
        IsDegrees = isOn;

        if (IsDegrees)
        {
            // Store previous meters values
            PrevMetersX = float.Parse(intervalX.text);
            PrevMetersY = float.Parse(intervalY.text);

            // Retore previous degrees values
            if (PrevDegreesX.HasValue)
                SetIntervalX(PrevDegreesX.Value);

            if (PrevDegreesY.HasValue)
                SetIntervalY(PrevDegreesY.Value);
        }
        else
        {
            // Store previous degrees values
            PrevDegreesX = float.Parse(intervalX.text);
            PrevDegreesY = float.Parse(intervalY.text);

            // Restore previous meters values
            if (PrevMetersX.HasValue)
                SetIntervalX(PrevMetersX.Value);

            if (PrevMetersY.HasValue)
                SetIntervalY(PrevMetersY.Value);
        }
    }

    //
    // Public Methods
    //

    public void SetIntervalX(float interval)
    {
        intervalX.text = $"{interval}";
    }

    public void SetIntervalY(float interval)
    {
        intervalY.text = $"{interval}";
    }

    public void GetPrevIntervals(out float? intervalX, out float? intervalY)
    {
        // Retore previous interval values
        intervalX = IsDegrees ? PrevDegreesX : PrevMetersX;
        intervalY = IsDegrees ? PrevDegreesY : PrevMetersY;
    }

    public bool IsMousePosOutsideGraticulePanel()
    {
        var metersToggleRT = metersToggle.GetComponent<RectTransform>();
        var degreesToggleRT = degreesToggle.GetComponent<RectTransform>();
        var showGridToggleRT = showGridToggle.GetComponent<RectTransform>();
        var lockedToggleRT = lockedToggle.GetComponent<RectTransform>();
        var intervalXRT = intervalX.GetComponent<RectTransform>();
        var intervalYRT = intervalY.GetComponent<RectTransform>();

        Vector2 inputPos0 = metersToggleRT.InverseTransformPoint(Input.mousePosition);
        Vector2 inputPos1 = degreesToggleRT.InverseTransformPoint(Input.mousePosition);
        Vector2 inputPos2 = showGridToggleRT.InverseTransformPoint(Input.mousePosition);
        Vector2 inputPos3 = lockedToggleRT.InverseTransformPoint(Input.mousePosition);
        Vector2 inputPos4 = intervalXRT.InverseTransformPoint(Input.mousePosition);
        Vector2 inputPos5 = intervalYRT.InverseTransformPoint(Input.mousePosition);
        Vector2 inputPos6 = neckRT.InverseTransformPoint(Input.mousePosition);
        Vector2 inputPos7 = panelRT.InverseTransformPoint(Input.mousePosition);

        return !metersToggleRT.rect.Contains(inputPos0) &&
                !degreesToggleRT.rect.Contains(inputPos1) &&
                !showGridToggleRT.rect.Contains(inputPos2) &&
                !lockedToggleRT.rect.Contains(inputPos3) &&
                !intervalXRT.rect.Contains(inputPos4) &&
                !intervalYRT.rect.Contains(inputPos5) &&
                !neckRT.rect.Contains(inputPos6) &&
                !panelRT.rect.Contains(inputPos7);
    }

    //
    // Private Methods
    //

}