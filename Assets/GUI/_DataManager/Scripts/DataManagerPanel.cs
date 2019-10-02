// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DataManagerPanel : MonoBehaviour
{
	[Header("UI References")]
	public Button closeButton;

	[Header("Left Panel")]
	public Transform sitesListContainer;
	public Button deleteSiteButton;

	[Header("Middle Panel")]
	public ScrollRect layersList;
	public GameObject noDataMessage;
	public Button upButton;
	public Button downButton;
	public Button addLayerButton;
	public Button deleteLayerButton;

	[Header("Right Panel")]
	public Text infoMessage;
	public DataManagerGroupPanel groupPanel;
	public DataManagerLayerPanel layerPanel;
	public Button confirmButton;

	[Header("Prefabs")]
	public GameObject sitePrefab;
	public GameObject groupPrefab;
	public GameObject layerPrefab;
	public GameObject emptyPrefab;
	public GameObject propertyInputPrefab;
	public ImportDataWizard importDataPrefab;

	private GameObject activeSection;
	private Site activeSite;
	private DataLayer activeLayer;
	private LayerGroup activeGroup;
	private Toggle activeLayerToggle;
	private Toggle activeGroupToggle;

	private List<LayerGroup> groups;
	private readonly List<LayerGroup> visibleGroups = new List<LayerGroup>();

	private DataManager dataManager;
	private ModalDialogManager dialogManager;
	private ITranslator translator;

	private PropertiesPanel propertiesPanel = null;
	private bool updateMainUI = false;


	//
	// Unity Methods
	//

	private void Start()
	{
		// Window events
		closeButton.onClick.AddListener(OnCloseClick);

		// Left panel events
		deleteSiteButton.onClick.AddListener(OnDeleteSiteClick);

		// Middle panel events
		//addGroupButton.onClick.AddListener(OnAddGroupClick);
		addLayerButton.onClick.AddListener(OnAddLayerClick);
		deleteLayerButton.onClick.AddListener(OnDeleteLayerClick);
		upButton.onClick.AddListener(OnMoveUpClick);
		downButton.onClick.AddListener(OnMoveDownClick);

		// Right panel events
		groupPanel.OnPropertiesChanged += OnPropertiesChanged;
		layerPanel.OnPropertiesChanged += OnPropertiesChanged;
		confirmButton.onClick.AddListener(OnApplyChanges);

		groupPanel.Init(this);
		layerPanel.Init(this);

		// Component references
		var componentManager = ComponentManager.Instance;
		dataManager = componentManager.Get<DataManager>();
		dialogManager = componentManager.Get<ModalDialogManager>();
		translator = LocalizationManager.Instance;

		groups = dataManager.groups;

		InitUI();
	}


	//
	// Event Methods
	//

	private void OnSiteToggleChanged(Site site, Toggle toggle)
	{
		if (toggle.isOn)
		{
			CheckForChanges(() => SetActiveSite(site));
		}
	}

	private void OnRenameSiteClick(Site site)
	{
		CheckForChanges(() => AskToRenameSite(site));
	}

	private void OnGroupToggleChanged(LayerGroup group, Toggle toggle)
	{
		if (toggle.isOn)
		{
			CheckForChanges(() => SetActiveGroup(group, toggle));
		}
	}

	private void OnLayerToggleChanged(DataLayer layer, Toggle toggle)
	{
		if (toggle.isOn)
		{
			CheckForChanges(() => SetActiveLayer(layer, toggle));
		}
	}

	private void OnCloseClick()
	{
		CheckForChanges(Close);
	}

	private void OnAddLayerClick()
	{
		CheckForChanges(ImportData);
	}

	private void OnAddGroupClick()
	{
		CheckForChanges(AddGroup);
	}

	private void OnDeleteSiteClick()
	{
		CheckForChanges(AskToDeleteActiveSite);
	}

	private void OnDeleteLayerClick()
	{
		CheckForChanges(() =>
		{
			if (activeLayer != null)
				AskToDeleteActiveLayer();
			else if (activeGroup != null)
				AskToDeleteActiveGroup();
		});
	}

	private void OnMoveUpClick()
	{
		CheckForChanges(() => MoveActiveElement(1));
	}

	private void OnMoveDownClick()
	{
		CheckForChanges(() => MoveActiveElement(-1));
	}

	private void OnPropertiesChanged()
	{
		confirmButton.interactable = propertiesPanel != null && propertiesPanel.HasChanges();
	}

	private void OnApplyChanges()
	{
		SaveChanges();
	}


	//
	// Public Methods
	//

	public void RefreshSiteList(Site site = null)
	{
		if (site == null)
			site = activeSite;

		UpdateSiteList(site);
	}

	public void UpdateActiveGroup()
	{
		activeGroupToggle.GetComponentInChildren<Text>().text = activeGroup.name;
	}

	public void UpdateActiveLayer()
	{
		UpdateLayerToggle(activeLayerToggle, activeLayer);
	}

	public bool UpdateBackend()
	{
		try
		{
			updateMainUI = true;

			Debug.Log("Updating layer config");
			dataManager.UpdateLayerConfig();
			return true;
		}
		catch (IOException)
		{
			dialogManager.Warn(translator.Get("Make sure you don't have layers.csv open in Excel or another program"), translator.Get("Couldn't save changes"));
		}
		return false;
	}

	public void AddGroup(UnityAction<LayerGroup> callback)
	{
		AskToCreateGroup((newGroupName) => callback(dataManager.AddLayerGroup(newGroupName)));
	}


	//
	// Private Methods
	//

	private void InitUI()
	{
		confirmButton.interactable = false;

		var siteBrowser = ComponentManager.Instance.Get<SiteBrowser>();
		UpdateSiteList(siteBrowser.ActiveSite);
	}

	private void UpdateSiteList(Site selectedSite = null)
	{
		activeSite = null;

		// Clear Sites List
		for (int i = sitesListContainer.childCount - 1; i >= 0; i--)
		{
			DestroyImmediate(sitesListContainer.GetChild(i).gameObject);
		}

		// Populate Sites List
		var sites = dataManager.sites;
		if (sites.Count > 0)
		{
			Toggle selectedSiteToggle = AddToSiteList("<" + translator.Get("All") + ">", null);

			foreach (var site in sites)
			{
				var toggle = AddToSiteList(site.Name, site);
				if (site == selectedSite)
					selectedSiteToggle = toggle;
			}

			// Select the site to show its groups and layers
			if (selectedSiteToggle.isOn)
				OnSiteToggleChanged(selectedSite, selectedSiteToggle);
			else
				selectedSiteToggle.isOn = true;
		}
	}

	private void AskToRenameSite(Site site)
	{
		if (site == null)
			return;

		AskForSiteName(translator.Get("Rename Site"), site.Name, (newSiteName) => RenameSite(site, newSiteName));
	}

	private void AskForSiteName(string title, string text, UnityAction<string> callback)
	{
		var popup = dialogManager.NewPopupDialog();
		popup.ShowInput(title, translator.Get("Site Name"));
		if (text != null)
			popup.input.text = text;
		popup.input.onValidateInput += GuiUtils.ValidateNameInput;
		popup.OnCloseDialog += (result) => {
			if (result.action == DialogAction.Ok)
			{
				var newSiteName = popup.input.text.Trim();

				if (dataManager.sites.Exists((s) => s.Name.EqualsIgnoreCase(newSiteName) && !s.Name.EqualsIgnoreCase(text)))
				{
					result.shouldClose = false;
					dialogManager.Warn(translator.Get("This site already exists") + ":\n\n<b>" + newSiteName + "</b>");
				}
				else
				{
					callback?.Invoke(newSiteName);
				}
			}
		};
	}

	private void RenameSite(Site site, string newSiteName)
	{
		updateMainUI = true;

		dataManager.RenameSite(site, newSiteName);

		UpdateSiteList(activeSite);
	}

	private void UpdateLayerList(LayerGroup selectedGroup = null, DataLayer selectedLayer = null)
	{
		activeSection = null;
		activeLayer = null;
		activeGroup = null;
		activeLayerToggle = null;
		activeGroupToggle = null;

		var layersListContainer = layersList.content;
		var toggleGroup = layersListContainer.GetComponent<ToggleGroup>();
		toggleGroup.allowSwitchOff = true;

		// Clear previous sections
		for (int i = layersListContainer.childCount - 1; i >= 0; i--)
		{
			DestroyImmediate(layersListContainer.GetChild(i).gameObject);
		}

		UpdateVisibleGroups();

		if (visibleGroups.Count == 0)
		{
			noDataMessage.SetActive(true);
			ShowPropertiesPanel(null);
			return;
		}

		noDataMessage.SetActive(false);

		Toggle selectedGroupToggle = null;
		Toggle selectedLayerToggle = null;
		foreach (var group in visibleGroups)
		{
			var groupSection = Instantiate(groupPrefab, layersListContainer);
			groupSection.GetComponentInChildren<Text>().text = group.name;
			var groupToggle = groupSection.GetComponentInChildren<Toggle>();
			groupToggle.onValueChanged.AddListener((b) => OnGroupToggleChanged(group, groupToggle));
			groupToggle.group = toggleGroup;

			if (selectedGroup == group || selectedGroupToggle == null)
			{
				selectedGroupToggle = groupToggle;
				selectedLayerToggle = null;
			}

			var sectionContainer = groupSection.transform.GetChild(1);
			if (group.layers.Count == 0)
			{
				var emptyItem = Instantiate(emptyPrefab, sectionContainer);
				emptyItem.transform.GetChild(0).GetComponent<Text>().text = translator.Get("Empty group");
			}
			else
			{
				for (int j = 0; j < group.layers.Count; j++)
				{
					var layer = group.layers[j];
					if (activeSite != null && !activeSite.HasDataLayer(layer))
						continue;

					var layerItem = Instantiate(layerPrefab, sectionContainer);
					var layerToggle = layerItem.GetComponent<Toggle>();
					layerToggle.onValueChanged.AddListener((b) => OnLayerToggleChanged(layer, layerToggle));
					layerToggle.group = toggleGroup;

					if ((layer == selectedLayer || selectedLayerToggle == null) && selectedGroupToggle == groupToggle)
						selectedLayerToggle = layerToggle;

					UpdateLayerToggle(layerToggle, layer);
				}
			}
		}

		toggleGroup.allowSwitchOff = false;

		// Need to defer the group/layer selection to the next frame to wait for UI layout
		StartCoroutine(SelectGroup(selectedGroupToggle, selectedLayerToggle));
	}

	private void UpdateLayerToggle(Toggle toggle, DataLayer layer)
	{
		toggle.transform.GetChild(1).GetComponent<Image>().color = layer.Color;
		toggle.transform.GetChild(2).GetComponent<Text>().text = layer.Name;
	}

	private void UpdateVisibleGroups()
	{
		visibleGroups.Clear();

		if (activeSite != null)
		{
			var availableGroups = new HashSet<LayerGroup>();
			foreach (var group in groups)
			{
				foreach (var layer in group.layers)
				{
					if (activeSite.HasDataLayer(layer))
					{
						if (availableGroups.AddOnce(layer.Group))
							visibleGroups.Add(layer.Group);
					}
				}
			}
		}
		else
		{
			visibleGroups.AddRange(groups);
		}
	}

	private IEnumerator AdjustListScroll(float idealPosition)
	{
		yield return null;

		var groupSection = activeSection.transform.parent as RectTransform;

		var rect = groupSection.rect;
		var yPos = groupSection.localPosition.y + layersList.content.localPosition.y;
		var sectionTop = yPos + rect.yMax;
		var sectionBottom = yPos + rect.yMin;
		var listBottom = -layersList.GetComponent<RectTransform>().rect.height;

		var shift = sectionTop - idealPosition;

		sectionTop -= shift;
		sectionBottom -= shift;

		// Adjust list scroll if the section's bottom is below the list's bottom
		if (sectionBottom < listBottom)
		{
			shift += Math.Max(sectionTop + 8, sectionBottom - listBottom - 8);
		}
		// Or the section's top is above the list's top
		else if (sectionTop > 0)
		{
			shift += sectionTop + 8;
		}

		if (shift != 0)
		{
			var pos = layersList.content.localPosition;
			pos.y -= shift;
			layersList.content.localPosition = pos;
		}
	}

	private IEnumerator SelectGroup(Toggle selectedGroupToggle, Toggle selectedLayerToggle)
	{
		yield return null;

		// Select the first available group to expand it
		if (selectedGroupToggle != null)
			selectedGroupToggle.isOn = true;

		// Then select the first available layer to display its info
		if (selectedLayerToggle != null)
			selectedLayerToggle.isOn = true;
	}

	private Toggle AddToSiteList(string label, Site site)
	{
		var siteEntry = Instantiate(sitePrefab, sitesListContainer);

		siteEntry.GetComponentInChildren<Text>().text = label;

		var siteToggle = siteEntry.GetComponent<Toggle>();
		var editButton = siteEntry.GetComponentInChildren<Button>();

		siteToggle.onValueChanged.AddListener(delegate (bool isOn) {
			if (site != null)
				editButton.gameObject.SetActive(isOn);
			OnSiteToggleChanged(site, siteToggle);
		});

		siteToggle.group = sitesListContainer.GetComponent<ToggleGroup>();

		if (site == null)
		{
			Destroy(editButton.gameObject);
		}
		else
		{
			editButton.gameObject.SetActive(false);
			editButton.onClick.AddListener(() => OnRenameSiteClick(site));

			siteToggle.GetComponent<HoverHandler>().OnHover += delegate (bool hover) {
				if (site != activeSite)
					editButton.gameObject.SetActive(hover);
			};
		}

		return siteToggle;
	}

	private void ImportData()
	{
		var importDlg = dialogManager.NewDialog(importDataPrefab);
		importDlg.OnFinishImport.AddListener((site, layer) => {
			RefreshSiteList(site);
		});
	}

	private void CheckForChanges(UnityAction nextAction)
	{
		if (propertiesPanel != null && propertiesPanel.HasChanges())
		{
			AskToSaveChanges(nextAction);
		}
		else
		{
			nextAction();
		}
	}

	private void SetActiveSite(Site site)
	{
		if (site != activeSite)
		{
			activeSite = site;
			UpdateLayerList(activeGroup, activeLayer);
			UpdateListButtons();
		}
	}

	private void SetActiveGroup(LayerGroup group, Toggle toggle)
	{
		var newActiveSection = toggle.transform.parent.parent.GetChild(1).gameObject;
		if (activeSection != newActiveSection)
		{
			// Retrieve new section current position
			var groupSection = newActiveSection.transform.parent as RectTransform;
			var yPos = groupSection.localPosition.y + layersList.content.localPosition.y;
			var sectionTop = yPos + groupSection.rect.yMax;

			if (activeSection != null)
				activeSection.SetActive(false);

			activeSection = newActiveSection;
			activeSection.SetActive(true);

			GuiUtils.RebuildLayout(activeSection.transform);

			StartCoroutine(AdjustListScroll(sectionTop));
		}

		activeGroup = group;
		activeGroupToggle = toggle;
		activeLayer = null;
		activeLayerToggle = null;

		ShowGroupProperties(group);
		UpdateListButtons();
	}

	private void SetActiveLayer(DataLayer layer, Toggle toggle)
	{
		if (layer != activeLayer)
		{
			activeLayer = layer;
			activeLayerToggle = toggle;
			ShowLayerProperties(layer);
			UpdateListButtons();
		}
	}

	private void ShowPropertiesPanel(PropertiesPanel panel)
	{
		if (panel == propertiesPanel)
			return;

		if (propertiesPanel == null)
			infoMessage.gameObject.SetActive(false);
		else
			propertiesPanel.gameObject.SetActive(false);

		propertiesPanel = panel;
		confirmButton.interactable = false;

		if (propertiesPanel == null)
			infoMessage.gameObject.SetActive(true);
		else
			propertiesPanel.gameObject.SetActive(true);
	}

	private void ShowGroupProperties(LayerGroup group)
	{
		ShowPropertiesPanel(groupPanel);
		groupPanel.ShowProperties(group, activeSite);
	}

	private void ShowLayerProperties(DataLayer layer)
	{
		ShowPropertiesPanel(layerPanel);
		layerPanel.ShowProperties(layer, activeSite);
	}

	private void UpdateListButtons()
	{
		deleteSiteButton.interactable = activeSite != null;
		deleteLayerButton.interactable = (activeLayer != null || (activeGroup != null && activeSite == null));

		UpdateUpDownButtons();
	}

	private void UpdateUpDownButtons()
	{
		if (activeSite != null)
		{
			upButton.interactable = false;
			downButton.interactable = false;
			return;
		}

		int index = -1;
		int count = -1;
		if (activeLayer != null)
		{
			index = activeLayer.Group.IndexOf(activeLayer);
			count = activeLayer.Group.layers.Count;
		}
		else if (activeGroup != null)
		{
			index = groups.IndexOf(activeGroup);
			count = groups.Count;
		}
		upButton.interactable = index > 0;
		downButton.interactable = index >= 0 && index < count - 1;
	}

	private void MoveActiveElement(int move)
	{
		if (activeSite != null)
			return;

		int index = 0;
		if (activeLayer != null)
		{
			if (move == 1)
				index = activeLayer.Group.MoveLayerUp(activeLayer);
			else if (move == -1)
				index = activeLayer.Group.MoveLayerDown(activeLayer);
			else
				return;

			activeLayerToggle.transform.parent.SetSiblingIndex(index);
		}
		else if (activeGroup != null)
		{
			if (move == 1)
				index = groups.MoveForward(activeGroup);
			else if (move == -1)
				index = groups.MoveBack(activeGroup);
			else
				return;

			activeGroupToggle.transform.parent.parent.SetSiblingIndex(index);
		}

		UpdateUpDownButtons();
	}

	private void AskToDeleteActiveSite()
	{
		if (activeSite == null)
			return;
#if UNITY_WEBGL
		dialogManager.Warn(translator.Get("This feature is not available for the Web version"));
#else
		var directories = dataManager.GetDataDirectories();
		var files = activeSite.GetFiles(directories);

		string question =
			translator.Get("Are you sure you want to delete this site?") +
			"\n\n<b>" + activeSite.Name + "</b>\n\n" +
			translator.Get("The following data will be deleted") + ":\n\n" +
			translator.Get("Layers") + ": " + activeSite.layers.Count +
			"\n" +
			translator.Get("Files") + ": " + files.Count;

		dialogManager.Ask(question, () => DeleteActiveSite(files));
#endif
	}

	private void DeleteActiveSite(List<string> files)
	{
		updateMainUI = true;

		IOUtils.SafeDelete(files);
		IOUtils.DeleteDirectoryIfEmpty(activeSite.GetDirectory());

		dataManager.RemoveSite(activeSite);

		UpdateSiteList();
	}

	private void AddGroup()
	{
		AddGroup((g) => UpdateLayerList(g));
	}

	private void AskToCreateGroup(UnityAction<string> callback)
	{
		var popup = dialogManager.NewPopupDialog();
		popup.ShowInput(translator.Get("Add New Group"), translator.Get("Group name"));
		popup.input.onValidateInput += GuiUtils.ValidateNameInput;
		popup.OnCloseDialog += (result) => {
			if (result.action == DialogAction.Ok)
			{
				var newGroupName = popup.input.text.Trim();

				if (groups.Exists((g) => g.name.EqualsIgnoreCase(newGroupName)))
				{
					result.shouldClose = false;
					dialogManager.Warn(translator.Get("This group already exists") + ":\n\n" + "<b>" + newGroupName + "</b>");
				}
				else
				{
					callback?.Invoke(newGroupName);
				}
			}
		};
	}

	private void AskToDeleteActiveGroup()
	{
#if UNITY_WEBGL
		dialogManager.Warn(translator.Get("This feature is not available for the Web version"));
#else
		if (activeSite != null)
			return;

		string question = translator.Get("Are you sure you want to delete this group?");
		question += "\n<b>" + activeGroup.name + "</b>\n\n";
		if (activeGroup.layers.Count > 0)
		{
			question = translator.Get("The following layers and their data will be deleted") + ":\n";
			foreach (var layer in activeGroup.layers)
				question += layer.Name + "\n";

			dialogManager.Ask(question, DeleteActiveGroup);
		}
		else
		{
			DeleteActiveGroup();
		}
#endif
	}

	private void DeleteActiveGroup()
	{
		if (activeSite != null)
			return;

		LayerGroup selectedGroup = null;

		int index = visibleGroups.IndexOf(activeGroup);
		if (index < visibleGroups.Count - 1)
			selectedGroup = visibleGroups[index + 1];
		else if (index > 0)
			selectedGroup = visibleGroups[index - 1];

		for (int i = activeGroup.layers.Count - 1; i >= 0; i--)
		{
			DeleteLayer(activeGroup.layers[i]);
		}

		dataManager.RemoveLayerGroup(activeGroup);

		UpdateLayerList(activeGroup);
		UpdateListButtons();
	}

	private void AskToDeleteActiveLayer()
	{
#if UNITY_WEBGL
		dialogManager.Warn(translator.Get("This feature is not available for the Web version"));
#else
		string question = translator.Get("Are you sure you want to delete this layer?");
		question += "\n<b>" + activeLayer.Name + "</b>\n\n";

		var sites = activeLayer.GetSites();
		var singleSite = translator.Get("Layer data will be deleted only for the following site") + ":";
		var multipleSites = translator.Get("Layer data will be deleted for the following sites") + ":";
		bool allSites = true;
		if (activeSite == null)
		{
			question += multipleSites + "\n";
			foreach (var site in sites)
				question += site.Name + "\n";
		}
		else
		{
			allSites = sites.Count == 1;
			question += sites.Count == 1 ? singleSite : multipleSites;
			question += "\n" + activeSite.Name + "\n";
		}
		dialogManager.Ask(question, () => DeleteActiveLayer(allSites));
#endif
	}

	private void DeleteActiveLayer(bool allSites)
	{
		var layerGroup = activeLayer.Group;

		DataLayer selectedLayer = null;
		LayerGroup selectedGroup = allSites ? null : layerGroup;

		int index = layerGroup.IndexOf(activeLayer);
		if (index < layerGroup.layers.Count - 1)
		{
			if (activeSite == null)
				selectedLayer = layerGroup.layers[index + 1];
			else
			{
				int count = layerGroup.layers.Count;
				for (int i = index + 1; i < count; ++i)
				{
					if (layerGroup.layers[i].HasDataForSite(activeSite))
					{
						selectedLayer = layerGroup.layers[i];
						break;
					}
				}
			}
		}
		else if (index > 0)
		{
			if (activeSite == null)
				selectedLayer = layerGroup.layers[index - 1];
			else
			{
				int count = layerGroup.layers.Count;
				for (int i = index - 1; i >= 0; --i)
				{
					if (layerGroup.layers[i].HasDataForSite(activeSite))
					{
						selectedLayer = layerGroup.layers[i];
						break;
					}
				}
			}
		}

		DeleteLayer(activeLayer);

		if (allSites)
			layerGroup.RemoveLayer(activeLayer);

		UpdateLayerList(selectedGroup, selectedLayer);
		UpdateListButtons();
	}

	private void DeleteLayer(DataLayer layer)
	{
		// Delete layer files
		layer.DeleteFiles(activeSite.Name);

		// Remove layer reference from relevant site(s)
		if (activeSite == null)
		{
			var sites = layer.GetSites();
			foreach (var site in sites)
			{
				site.RemoveLayer(layer);
			}
		}
		else
		{
			activeSite.RemoveLayer(layer);
		}

		dataManager.ClearPatchCache();
	}

	private void AskToSaveChanges(UnityAction nextAction)
	{
		dialogManager.Ask(translator.Get("Properties were modified") + "\n\n" + translator.Get("Do you want to save changes?"),
			() =>
			{
				SaveChanges();
				nextAction?.Invoke();
			}
			, () =>
			{
				DiscardChanges();
				nextAction?.Invoke();
			});
	}

	private void DiscardChanges()
	{
		if (propertiesPanel != null)
		{
			propertiesPanel.DiscardChanges();
		}
	}

	private void SaveChanges()
	{
		if (propertiesPanel.SaveChanges())
		{
			confirmButton.interactable = false;
			updateMainUI = true;
		}
		else
		{
			dialogManager.Warn(translator.Get("And error ocurred while saving changes"), translator.Get("Error"));
		}
	}

	private void Close()
	{
		Destroy(gameObject);

		dataManager.RemoveEmptySites();

		if (updateMainUI)
		{
			// Refresh layers list in main UI
			var dataLayers = ComponentManager.Instance.Get<DataLayers>();
			dataLayers.RebuildList(groups);

			// Refresh sites list in main UI
			var siteBrowser = ComponentManager.Instance.Get<SiteBrowser>();
			siteBrowser.RebuildList();
		}
	}
}
