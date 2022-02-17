// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class SavedScaleListItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button itemButton = default;
    [SerializeField] private Button deleteButton = default;
    [SerializeField] private Text distanceVal = default;

    // Properties
    public Button ItemButton { get { return itemButton; } }
    public Text DistanceVal { get { return distanceVal; } }

    // Component References
    private HoverHandler hoverHandler;

    //
    // Unity Methods
    //

    private void Start()
    {
        hoverHandler = GetComponent<HoverHandler>();

        // Initialize listeners
        deleteButton.onClick.AddListener(OnDeleteClick);
        hoverHandler.OnHover += OnPointerHover;
    }

    //
    // Event Methods
    //

    private void OnDeleteClick() => Destroy(gameObject);

    private void OnPointerHover(bool isHovering)
    {
        deleteButton.gameObject.SetActive(isHovering);
    }

    //
    // Public Methods
    //

    public void Init(int number, string unit)
    {
        distanceVal.text = $"{number} {unit}";
    }

    //
    // Private Methods
    //



}