// Copyright (C) 2021 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class SavedLocationListItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button itemButton = default;
    [SerializeField] private Button editButton = default;
    [SerializeField] private Button deleteButton = default;
    [SerializeField] private Text entryName = default;
    [SerializeField] private Text distanceVal = default;

    // Properties
    public Button ItemButton { get { return itemButton; } }
    public Text EntryName { get { return entryName; } }
    public Text DistanceVal { get { return distanceVal; } }
    public double Lat { private set; get; }
    public double Lon { private set; get; }

    // Component References
    private ModalDialogManager dialogManager;
    private HoverHandler hoverHandler;

    private const string NewEntryName = "New Saved Entry";

    //
    // Unity Methods
    //

    private void Start()
    {
        // Get components
		var componentManager = ComponentManager.Instance;
        dialogManager = componentManager.Get<ModalDialogManager>();
        hoverHandler = GetComponent<HoverHandler>();

        // Initialize listeners
        editButton.onClick.AddListener(() => AskForSavedScaleName(LocalizationManager.Instance.Get("Rename Saved Location"), entryName));
        deleteButton.onClick.AddListener(OnDeleteClick);
        hoverHandler.OnHover += OnPointerHover;
    }

    //
    // Event Methods
    //

    private void OnDeleteClick() => Destroy(gameObject);

    private void OnPointerHover(bool isHovering)
    {
        editButton.gameObject.SetActive(isHovering);
        deleteButton.gameObject.SetActive(isHovering);
    }

    //
    // Public Methods
    //

    public void Init(int number, string unit, double lat, double lon, string label = null)
    {
        entryName.text = string.IsNullOrEmpty(label) ? LocalizationManager.Instance.Get(NewEntryName) : label;
        distanceVal.text = $"{number} {unit}";
        Lat = lat;
        Lon = lon;
    }

    //
    // Private Methods
    //

    private void AskForSavedScaleName(string title, Text name)
    {
        var popup = dialogManager.NewPopupDialog();
        popup.ShowInput(title, LocalizationManager.Instance.Get(NewEntryName));
        if (name != null)
            popup.input.text = name.text;
        popup.input.onValidateInput += GuiUtils.ValidateNameInput;
        popup.OnCloseDialog += (result) => {
            if (result.action == DialogAction.Ok)
            {
                var newSavedScaleName = popup.input.text.Trim();
                name.text = newSavedScaleName;
            }
        };
    }
}