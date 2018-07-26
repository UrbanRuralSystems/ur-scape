// Copyright (C) 2018 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          David Neudecker  (neudecker@arch.ethz.ch)

using UnityEngine;
using UnityEngine.UI;

public class DataLayerPanel : MonoBehaviour
{
    [Header("Color Setup")]
    public float layerColorDim = 0.7f;
    public float filterIconDim = 0.6f;

    [Header("UI References")]
    public ToggleButton layerToggle;
	public ToggleButton filterToggle;
	public Image stripe;
    public Image icon;

    [Header("Sprites")]
    public Sprite categorySprite;
    public Sprite filterSprite;

    [Header("Prefabs")]
    public LayerOptionsPanel continuousPanelPrefab;
    public LayerOptionsPanel categorizedPanelPrefab;

    //Component reference
    private MapController map;

    private DataLayer dataLayer;
    private LayerOptionsPanel filterPanel;
    private bool categorizedPanel;

    // Misc
    private Color iconColor;

    public DataLayer DataLayer
    {
        get { return dataLayer; }
    }

    public LayerOptionsPanel Panel
    {
        get { return filterPanel; }
    }

	public bool IsLayerToggleOn
    {
        get { return layerToggle.isOn; }
    }

    public bool IsOptionsToggleOn
    {
        get { return filterToggle.isOn; }
    }

    //
    // Unity Methods
    //

    private void Awake()
    {
        map = ComponentManager.Instance.Get<MapController>();
        layerToggle.onValueChanged.AddListener(OnLayerToggleChanged);
    }

	//
	// Event Methods
	//

	private void OnLayerToggleChanged(bool isOn)
	{
		filterToggle.interactable = isOn;
        icon.color = iconColor * filterIconDim;

        if (isOn)
        {
            EnableHoverEvents();
            icon.color = iconColor;
        }
        else
        {
            DisableHoverEvents();
            icon.color = iconColor * filterIconDim;
        }
	}

	private void OnLayerToggleHover(bool hover)
	{
		if (hover)
			dataLayer.SetUserOpacity(1f / dataLayer.ToolOpacity);
		else
			dataLayer.SetUserOpacity(1);
	}

	private void OnPatchVisibilityChange(DataLayer dataLayer, Patch patch, bool visible)
    {
        if (visible && filterPanel != null)
        {
            bool categorizedPatch = patch.Data.IsCategorized;
            if (categorizedPatch ^ categorizedPanel)
            {
                Destroy(filterPanel.gameObject);
                CreateFilterPanel(categorizedPatch);
            }
        }
    }
	
	private void OnLevelChange(int level)
	{
		UpdateFilterIcon();
	}

	//
	// Public Methods
	//

	public void Init(DataLayer dataLayer)
    {
        this.dataLayer = dataLayer;
        gameObject.name = dataLayer.name;

        layerToggle.SetText(dataLayer.name);
        layerToggle.SetColor(dataLayer.color * layerColorDim);
		filterToggle.SetColor(dataLayer.color);
        stripe.color = dataLayer.color;
        iconColor = icon.color;
        icon.color = iconColor * filterIconDim;

        layerToggle.interactable = false;
		filterToggle.interactable = false;

        dataLayer.OnPatchVisibilityChange += OnPatchVisibilityChange;
        map.OnLevelChange += OnLevelChange;
    }

    private void EnableHoverEvents()
    {
		layerToggle.gameObject.AddComponent<HoverHandler>().OnHover += OnLayerToggleHover;
    }

	private void DisableHoverEvents()
	{
		var hoverHandler = layerToggle.gameObject.GetComponent<HoverHandler>();
		if (hoverHandler != null)
		{
			hoverHandler.OnHover -= OnLayerToggleHover;
			Destroy(hoverHandler);
		}
		dataLayer.SetUserOpacity(1);
	}

	public void UpdateFilterIcon()
    {
        bool isCategorized = false;
        var currentLevel = dataLayer.levels[map.CurrentLevel];
		if (currentLevel.layerSites.Count > 0)
        {
			isCategorized = currentLevel.layerSites[0].lastRecord.patches[0].Data.IsCategorized;
        }

        if (isCategorized)
            icon.sprite = categorySprite;
        else
            icon.sprite = filterSprite;
    } 

	// Helper function to switch from one mode to another
	public void UpdateBehaviour(bool hideInactive)
	{
		if (hideInactive)
		{
			gameObject.SetActive(layerToggle.interactable);
			EnableLayerPanel(true);
		}
		else
		{
			EnableLayerPanel(gameObject.activeSelf);
			gameObject.SetActive(true);
		}
	}

	// Enable/disable the layer button
	public void EnableLayerPanel(bool enable)
    {
        if (layerToggle.interactable != enable)
        {
            layerToggle.interactable = enable;
			filterToggle.interactable = enable && layerToggle.isOn;
        }
    }

	// Show/hide the layer panel
	public bool ShowLayerPanel(bool enable)
    {
		if (gameObject.activeSelf != enable)
        {
			// Hide the filter panel BEFORE the layer panel is hidden
			if (filterToggle.isOn && filterPanel != null && !enable)
				filterPanel.Show(false);

			gameObject.SetActive(enable);

			// Show the filter panel AFTER the layer panel is shown
			if (filterToggle.isOn && filterPanel != null && enable)
				filterPanel.Show(true);

			GuiUtils.RebuildLayout(transform);
			return true;
		}
		return false;
    }

    public void ShowOptionsPanel(bool show)
    {
        if (show)
        {
            bool categorized = false;
            if (dataLayer.loadedPatchesInView.Count > 0)
            {
                categorized = dataLayer.loadedPatchesInView[0].Data.IsCategorized;
            }

            if (filterPanel == null || categorized ^ categorizedPanel)
            {
                if (filterPanel != null)
                    Destroy(filterPanel.gameObject);

                CreateFilterPanel(categorized);
            }
            else
            {
                filterPanel.Show(true);
            }
        }
        else
        {
            filterPanel.Show(false);
        }

        GuiUtils.RebuildLayout(transform);
    }


    //
    // Private Methods
    //

    private void CreateFilterPanel(bool categorized)
    {
        categorizedPanel = categorized;

        if (categorized)
        {
            filterPanel = Instantiate(categorizedPanelPrefab, transform, false);
            icon.sprite = categorySprite;
        }
        else
        {
            filterPanel = Instantiate(continuousPanelPrefab, transform, false);
            icon.sprite = filterSprite;
        }
        filterPanel.Init(dataLayer);
    }

}

