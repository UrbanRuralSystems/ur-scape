// Copyright (C) 2022 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Muhammad Salihin Bin Zaol-kefli  (mzaolkefli@ethz.ch)

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CategorisedDataLayerItem : MonoBehaviour
{
	[Header("UI References")]
    [SerializeField] protected Image dot = default;
    [SerializeField] private Text displayedName = default;
    [SerializeField] private Button editButton = default;
    [SerializeField] private HoverHandler hoverHandler = default;
    [SerializeField] protected Toggle groupToggle = default;
    [SerializeField] protected Image arrow = default;
    [SerializeField] private RectTransform categoriesInfo = default;

    [Header("Prefabs")]
    [SerializeField] private Text categoryLabel = default;

    private readonly Dictionary<string, Text> categoryLabels = new Dictionary<string, Text>();

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
        groupToggle.onValueChanged.AddListener(OnGroupToggleChanged);
        groupToggle.isOn = false;
    }

    private void OnDestroy()
    {
        GuiUtils.RebuildLayout(transform.parent.GetComponent<LayoutGroup>().transform);
        GuiUtils.RebuildLayout(transform.parent.parent.GetComponent<LayoutGroup>().transform);

        // Remove all listeners
        groupToggle.onValueChanged.RemoveAllListeners();
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

    protected void OnGroupToggleChanged(bool isOn)
	{
        arrow.rectTransform.eulerAngles = new Vector3(0.0f, 0.0f, isOn ? 0.0f : -90.0f);
        categoriesInfo.gameObject.SetActive(isOn);
        StartCoroutine(DelayedReEnableLayout(2));
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

    public void AddCategory(string name)
    {
        if (!categoryLabels.ContainsKey(name))
        {
            var item = Instantiate(categoryLabel, categoriesInfo, false);

            item.name = item.text = name;

            categoryLabels.Add(name, item);

            if (categoryLabels.Count > 0)
            {
                groupToggle.gameObject.SetActive(true);
            }
            else
            {
                groupToggle.isOn = false;
                groupToggle.gameObject.SetActive(false);
            }

            StartCoroutine(DelayedReEnableLayout(2));
        }
    }

    public void RemoveCategory(string name)
    {
        if (categoryLabels.TryGetValue(name, out Text item))
        {
            Destroy(item.gameObject);
            categoryLabels.Remove(name);

            if (categoryLabels.Count > 0)
            {
                groupToggle.gameObject.SetActive(true);
            }
            else
            {
                groupToggle.isOn = false;
                groupToggle.gameObject.SetActive(false);
            }

            StartCoroutine(DelayedReEnableLayout(2));
        }
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

    private IEnumerator DelayedReEnableLayout(int numOfFrames)
    {
        yield return new WaitForFrames(numOfFrames);

        GuiUtils.RebuildLayout(GetComponent<LayoutGroup>().transform);
        GuiUtils.RebuildLayout(transform.parent.GetComponent<LayoutGroup>().transform);
        GuiUtils.RebuildLayout(transform.parent.parent.GetComponent<LayoutGroup>().transform);
    }

}