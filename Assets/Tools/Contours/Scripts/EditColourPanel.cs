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

public class EditColourPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button closeButton = default;
    [SerializeField] private RectTransform container = default;

    [Header("Prefabs")]
    [SerializeField] private SnapshotColor snapshotColourPrefab = default;
    [SerializeField] private ColourDialog colourDialogPrefab = default;

    private ContoursTool contoursTool;
    private ModalDialogManager dialogManager;
    private LocalizationManager translator;

    //
    // Unity Methods
    //

    private void Awake()
    {
        // Window events
        closeButton.onClick.AddListener(OnCloseClick);

        // Component references
        var componentManager = ComponentManager.Instance;
        contoursTool = componentManager.Get<ContoursTool>();
        dialogManager = componentManager.Get<ModalDialogManager>();
        translator = LocalizationManager.Instance;

        //translator.OnLanguageChanged += OnLanguageChanged;
    }

    //
    // Event Methods
    //

    private void OnCloseClick()
    {
        if (contoursTool)
            contoursTool.editToggle.isOn = false;

        gameObject.SetActive(false);
    }

    private void OnColorClick(ContoursTool.Snapshot snapshot = null, SnapshotColor snapshotColour = null)
    {
        var popup = dialogManager.NewDialog(colourDialogPrefab);

        var colourImage = snapshotColour.ColourImage;
        if (colourImage != null)
            popup.Show(colourImage.color);

        popup.OnCloseDialog += (result) =>
        {
            if (result.action == DialogAction.Ok)
            {
                // Assign snapshot color in panel
                if (colourImage != null)
                    colourImage.color = popup.Color;

                if (snapshot != null)
                {
                    // Assign snapshot toggle and contour color
                    var toggleButton = snapshot.uiTransform.GetComponentInChildren<Toggle>();
                    var deleteButton = snapshot.uiTransform.GetChild(1).GetComponent<Button>();

                    SetColorsToSnapshot(snapshot.mapLayer, toggleButton, deleteButton, popup.Color);
                    contoursTool.InfoPanel.UpdateSnapshotColour(snapshot.id, popup.Color);
                    if (contoursTool.LegendPanl.LegendItems.TryGetValue(snapshot.id, out LegendItem item))
                    {
                        item.SetDotColor(popup.Color);
                    }
                }
            }
        };
    }

    private void OnLanguageChanged()
    {
        int count = container.childCount;
        var snapshots = contoursTool.Snapshots;

        for (int i = 0; i < count; ++i)
        {
            var snapshot = snapshots[i];
            var snapshotColour = container.GetChild(i).GetComponent<SnapshotColor>();

            snapshotColour.SnapshotName.text = translator.Get(snapshot.name);
        }
    }

    //
    // Public Methods
    //

    public void AddSnapshotColour(int index, Color color)
    {
        var snapshots = contoursTool.Snapshots;

        var snapshotColour = Instantiate(snapshotColourPrefab, container, false);
        var snapshot = snapshots[index];

        snapshotColour.SnapshotName.text = snapshot.name;
        snapshotColour.ColourImage.color = color;
        snapshotColour.EditButton.onClick.AddListener(() => OnColorClick(snapshot, snapshotColour));
    }

    public void RemoveSnapshotColour(int index)
    {
        var snapshotColour = container.GetChild(index).GetComponent<SnapshotColor>();

        snapshotColour.EditButton.onClick.RemoveListener(() => OnColorClick());
        Destroy(snapshotColour.gameObject);
    }

    public void RenameEntry(int index, string titleLabel)
    {
        var snapshotColour = container.GetChild(index).GetComponent<SnapshotColor>();

        snapshotColour.SnapshotName.text = titleLabel;
    }

    //
    // Private Methods
    //

    private void SetColorsToSnapshot(GridMapLayer mapLayer, Toggle toggle, Button deleteButton, Color color)
    {
        toggle.image.color = color;
        toggle.GetComponent<ToggleTint>().colorOff = color;
        deleteButton.image.color = color;

        // set color for mapLayer
        mapLayer.SetColor(color);
    }
}