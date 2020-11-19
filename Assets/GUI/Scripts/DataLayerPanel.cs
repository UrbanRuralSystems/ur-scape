// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
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
	[Header("UI References")]
    public Toggle layerToggle;
	public Toggle filterToggle;
	public Button infoButton;
	public Text label;
	public Image dot;
	public Image stripe;
	public Image filterIcon;

    [Header("Prefabs")]
    public LayerOptionsPanel continuousPanelPrefab;
    public LayerOptionsPanel categorizedPanelPrefab;

    private DataLayer dataLayer;
    private LayerOptionsPanel filterPanel;
    private bool categorizedPanel;

	private static InfoPanel infoPanel;
	private static PatchBoundaryLayerController patchBoundaries;

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

    public bool IsFilterToggleOn
    {
        get { return filterToggle.isOn; }
    }
	

	//
	// Unity Methods
	//

	private void Awake()
    {
        layerToggle.onValueChanged.AddListener(OnLayerToggleChanged);
		infoButton.onClick.AddListener(OnInfoButtonClick);
		infoButton.GetComponent<HoverHandler>().OnHover += OnInfoButtonHover;

		if (infoPanel == null)
		{
			var componentManager = ComponentManager.Instance;

			infoPanel = componentManager.Get<InfoPanel>();
			patchBoundaries = componentManager.Get<MapController>().GetLayerController<PatchBoundaryLayerController>();
		}
	}


	//
	// Event Methods
	//

	private void OnLayerToggleChanged(bool isOn)
	{
		filterToggle.gameObject.SetActive(isOn);
		infoButton.gameObject.SetActive(isOn);

		stripe.gameObject.SetActive(isOn);

		if (isOn)
        {
            EnableHoverEvents();
		}
		else
        {
            DisableHoverEvents();
		}
	}

	private void OnLayerToggleHover(bool hover)
	{
		if (hover || dataLayer.ToolOpacity == 0)
			dataLayer.SetUserOpacity(1f / dataLayer.ToolOpacity);
		else
			dataLayer.SetUserOpacity(1f);

		if (!hover || dataLayer.loadedPatchesInView.Count <= 1)
			patchBoundaries.HighlightBoundary(dataLayer, hover);
	}

	private void OnPatchVisibilityChange(DataLayer dataLayer, Patch patch, bool visible)
    {
        if (visible && filterPanel != null)
        {
			if (patch.Data is GridData)
			{
				bool categorizedPatch = (patch.Data as GridData).IsCategorized;
				if (categorizedPatch ^ categorizedPanel)
				{
					Destroy(filterPanel.gameObject);
					filterPanel = null;

					if (IsFilterToggleOn)
						CreateFilterPanel(categorizedPatch);
				}
			}
		}
    }
	
	private void OnInfoButtonClick()
	{
		infoPanel.ShowData(dataLayer, infoButton.GetComponent<RectTransform>());
	}

	private void OnInfoButtonHover(bool isHovering)
	{
		if (isHovering)
		{
			infoPanel.ShowData(dataLayer, infoButton.GetComponent<RectTransform>());
		}
		else
		{
			if (!infoPanel.IsMouseInside())
			{
				infoPanel.Hide();
			}
		}
	}


	//
	// Public Methods
	//

	public void Init(DataLayer dataLayer)
    {
        this.dataLayer = dataLayer;

		UpdateNameAndColor();

		layerToggle.interactable = false;
		filterToggle.gameObject.SetActive(false);
		infoButton.gameObject.SetActive(false);

		dataLayer.OnPatchVisibilityChange += OnPatchVisibilityChange;
    }

	public void UpdateNameAndColor()
	{
		gameObject.name = dataLayer.Name;
		label.text = dataLayer.Name;
		dot.color = dataLayer.Color;
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
			var showToggles = enable && layerToggle.isOn;
			filterToggle.gameObject.SetActive(showToggles);
			infoButton.gameObject.SetActive(showToggles);
		}
    }

	// Show/hide the layer panel
	public bool ShowLayerPanel(bool enable)
    {
		if (gameObject.activeSelf != enable)
        {
            // Hide the filter panel BEFORE the layer panel is hidden
            if (IsFilterToggleOn && filterPanel != null && !enable)
				filterPanel.Show(false);

			gameObject.SetActive(enable);

			// Show the filter panel AFTER the layer panel is shown
			if (IsLayerToggleOn && IsFilterToggleOn && filterPanel != null && enable)
				filterPanel.Show(true);

            GuiUtils.RebuildLayout(transform);
			return true;
		}
        return false;
    }

    public void ShowFilterPanel(bool show)
    {
		if (show)
        {
            bool categorized = false;
            if (dataLayer.loadedPatchesInView.Count > 0)
            {
				var data = dataLayer.loadedPatchesInView[0].Data;
				if (data is GridData)
				{
					categorized = (data as GridData).IsCategorized;
				}
				else if (data is MultiGridData)
				{
					categorized = true;
				}
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
        }
        else
        {
            filterPanel = Instantiate(continuousPanelPrefab, transform, false);
        }
        filterPanel.Init(dataLayer);
    }

	private void EnableHoverEvents()
	{
		layerToggle.GetComponent<HoverHandler>().OnHover += OnLayerToggleHover;
		if ((layerToggle.transform as RectTransform).IsMouseInside())
			OnLayerToggleHover(true);
	}

	private void DisableHoverEvents()
	{
		layerToggle.GetComponent<HoverHandler>().OnHover -= OnLayerToggleHover;
		dataLayer.SetUserOpacity(1);
	}

}

