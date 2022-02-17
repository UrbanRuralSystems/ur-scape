// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public abstract class LegendItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] protected Image dot = default;
    [SerializeField] private Text displayedName = default;
    [SerializeField] private Button editButton = default;
    [SerializeField] private HoverHandler hoverHandler = default;
    [SerializeField] protected Toggle groupToggle = default;
    [SerializeField] protected Image arrow = default;

    // Properties
    public string LayerName { set; get; }
    public string DisplayedName { get { return displayedName.text; } }
    public Toggle GroupToggle { get { return groupToggle; } }

    // Component References
    private ModalDialogManager dialogManager;

    //
    // Unity Methods
    //

    private void OnDestroy()
    {
        GuiUtils.RebuildLayout(transform.parent.GetComponent<LayoutGroup>().transform);
        GuiUtils.RebuildLayout(transform.parent.parent.GetComponent<LayoutGroup>().transform);

        // Remove all listeners
        groupToggle.onValueChanged.RemoveAllListeners();
        hoverHandler.OnHover -= OnPointerHover;

        if (editButton != null)
            editButton.onClick.RemoveAllListeners();
    }

    //
    // Event Methods
    //

    private void OnPointerHover(bool isHovering)
    {
        if (editButton != null)
            editButton.gameObject.SetActive(isHovering);
    }

    protected abstract void OnGroupToggleChanged(bool isOn);

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

    //
    // Protected Methods
    //

    protected void Init()
    {
        // Get components
        dialogManager = ComponentManager.Instance.Get<ModalDialogManager>();

        // Initialize listeners
        if (editButton != null)
            editButton.onClick.AddListener(() => AskForItemName(LocalizationManager.Instance.Get("Rename Item Name"), displayedName));
        hoverHandler.OnHover += OnPointerHover;
        groupToggle.onValueChanged.AddListener(OnGroupToggleChanged);
        groupToggle.isOn = false;
    }

    protected IEnumerator DelayedReEnableLayout(int numOfFrames)
    {
        yield return new WaitForFrames(numOfFrames);

        GuiUtils.RebuildLayout(GetComponent<LayoutGroup>().transform);
        GuiUtils.RebuildLayout(transform.parent.GetComponent<LayoutGroup>().transform);
        GuiUtils.RebuildLayout(transform.parent.parent.GetComponent<LayoutGroup>().transform);
    }
}