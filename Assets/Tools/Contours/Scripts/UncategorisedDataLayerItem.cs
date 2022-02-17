// Copyright (C) 2022 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System;
using UnityEngine;
using UnityEngine.UI;

public class UncategorisedDataLayerItem : MonoBehaviour
{
	[Header("UI References")]
    [SerializeField] protected Image dot = default;
    [SerializeField] private Text displayedName = default;
    [SerializeField] private Button editButton = default;
    [SerializeField] private HoverHandler hoverHandler = default;
    [SerializeField] private Text filterRange = default;

    //[Header("Prefabs")]

    // Properties
    public string LayerName { set; get; }

    // Component References
    private ModalDialogManager dialogManager;

    //
    // Unity Methods
    //

    private void Start()
    {
        // Get components
        dialogManager = ComponentManager.Instance.Get<ModalDialogManager>();

        // Initialize listeners
        editButton.onClick.AddListener(() => AskForItemName(LocalizationManager.Instance.Get("Rename Item Name"), displayedName));
        hoverHandler.OnHover += OnPointerHover;
    }

    private void OnDestroy()
    {
        GuiUtils.RebuildLayout(transform.parent.GetComponent<LayoutGroup>().transform);
        GuiUtils.RebuildLayout(transform.parent.parent.GetComponent<LayoutGroup>().transform);

        // Remove all listeners
        hoverHandler.OnHover -= OnPointerHover;
        editButton.onClick.RemoveAllListeners();
    }

    //
    // Event Methods
    //

    private void OnPointerHover(bool isHovering)
    {
        editButton.gameObject.SetActive(isHovering);
    }

    //
    // Public Methods
    //

    public void SetDotColor(Color color)
    {
        dot.color = color;
    }

    public void SetDisplayName(string nameString)
    {
        displayedName.text = nameString;
    }

    public void SetFilterRange(float min, float max, string units)
	{
        filterRange.text = $"{FormatValue(min)} - {FormatValue(max)} {units}";
	}

    //
    // Private Methods
    //

    private void AskForItemName(string title, Text name)
    {
        var popup = dialogManager.NewPopupDialog();
        popup.ShowInput(title, LocalizationManager.Instance.Get(LayerName, false));
        if (name != null)
            popup.input.text = name.text;
        popup.input.onValidateInput += GuiUtils.ValidateNameInput;
        popup.OnCloseDialog += (result) => {
            if (result.action == DialogAction.Ok)
            {
                var newItemName = popup.input.text.Trim();
                name.text = newItemName;
            }
        };
    }

    private string FormatValue(float value)
    {
        var absValue = Math.Abs(value);
        if (absValue >= 1e+12)
            return ((int)(value * 1e-12)).ToString("0 T");
        else if (absValue >= 1e+11)
            return ((int)(value * 1e-9)).ToString("0 B");
        else if (absValue >= 1e+9)
            return ((int)(value * 1e-9)).ToString("0 B");
        else if (absValue >= 1e+8)
            return ((int)(value * 1e-6)).ToString("0 M");
        else if (absValue >= 1e+6)
            return ((int)(value * 1e-6)).ToString("0 M");
        else if (absValue >= 1e+5)
            return ((int)(value * 1e-3)).ToString("0 K");
        else if (absValue >= 1e+3)
            return ((int)(value * 1e-3)).ToString("0 K");
        else if (absValue < 0.0001f)
            return "0";
        else if (absValue < 0.1f)
            return ((int)value).ToString();
        else
        {
            return ((int)value).ToString();
        }
    }

}