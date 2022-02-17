// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections;

public class DataManagerLayerPanel : PropertiesPanel
{
	[Header("UI References")]
	public InputField layerNameInput;
	public Dropdown groupDropdown;
	public Button addGroupButton;
	public Button colorButton;

	public GameObject sitePanel;
	public Dropdown siteDropdown;
	public Button addSiteButton;
	public InputField yearInput;
	public InputField unitsInput;
	public InputField typeInput;
	public Button categoriesButton;
	public Dropdown styleDropdown;
	public Button styleButton;

	public GameObject metadataPanel;
	public Button addMetadataButton;
	public ScrollRect metadataList;
	public Transform metadataGrid;
	public GameObject noMetadataMessage;
	public GameObject multipleMetadataMessage;

	[Header("Prefabs")]
	public PaddedInputField metadataCellPrefab;
	public ColourDialog colourDialogPrefab;


	// Private Variables
	private DataLayer activeLayer;
	private readonly LayerProperties properties = new LayerProperties();

	private ITranslator translator;


	public class LayerProperties : PropertiesSet
	{
		public readonly Property<string> name = new Property<string>();
		public readonly Property<LayerGroup> group = new Property<LayerGroup>();
		public readonly Property<Color> color = new Property<Color>();

		public readonly Property<string> site = new Property<string>();
		public readonly Property<int> year = new Property<int>();
		public readonly Property<string> units = new Property<string>();
		public readonly Property<Category[]> categories = new Property<Category[]>();
		public readonly Property<GridData.Coloring> coloring = new Property<GridData.Coloring>();

		public readonly Property<List<MetadataPair>> metadata = new Property<List<MetadataPair>>();

		public LayerProperties()
		{
			Add(name);
			Add(group);
			Add(color);

			Add(site);
			Add(year);
			Add(units);
			Add(categories);
			Add(coloring);

			Add(metadata);
		}
	}

	private enum DataType
	{
		Unknown = -1,
		Continuous,
		Categorised,
		Graph
	}
	

	//
	// Unity Methods
	//

	private void Start()
	{
		translator = LocalizationManager.Instance;

		// Setup events
		properties.OnPropertiesChanged += InvokeOnPropertiesChanged;
		layerNameInput.onEndEdit.AddListener(OnLayerNameEndEdit);
		layerNameInput.onValidateInput += GuiUtils.ValidateNameInput;
		//groupDropdown.onValueChanged.AddListener(OnGroupDropdownChanged);	 // Will be set after filling the dropdown
		addGroupButton.onClick.AddListener(OnAddGroupAndSwitchLayerToGroupClick);
		colorButton.onClick.AddListener(OnColorClick);
		//siteDropdown.onValueChanged.AddListener(OnSiteDropdownChanged);  // Will be set after filling the dropdown
		addSiteButton.onClick.AddListener(OnAddSiteAndSwitchLayerToSiteClick);
		yearInput.onEndEdit.AddListener(OnYearInputEndEdit);
		unitsInput.onEndEdit.AddListener(OnUnitsInputEndEdit);
		categoriesButton.onClick.AddListener(OnEditCategoriesClick);
		//styleDropdown.onValueChanged.AddListener(OnStyleDropdownChanged);  // Will be set after filling the dropdown
		styleButton.onClick.AddListener(OnEditCustomStyleClick);
		addMetadataButton.onClick.AddListener(OnAddMetadataClick);

		// Component references
		var componentManager = ComponentManager.Instance;
		dataManager = componentManager.Get<DataManager>();

		InitStyleDropdown();
	}


	//
	// Inheritance Methods
	//

	public override bool HasChanges()
	{
		return properties.HaveChanged;
	}

	public override void DiscardChanges()
	{
		properties.Revert();
	}

	public override bool SaveChanges()
	{
		if (activeLayer == null)
			return false;

		bool updateBackend = false;
		Site activeSite = null;

		if (properties.name.HasChanged)
		{
			activeLayer.ChangeName(properties.name);
			updateBackend = true;
		}
		if (properties.group.HasChanged)
		{
			activeLayer.ChangeGroup(properties.group);
			updateBackend = true;
		}
		if (properties.color.HasChanged)
		{
			activeLayer.ChangeColor(properties.color);
			updateBackend = true;
		}

        //+
        if (properties.coloring.HasChanged)
        {
            activeLayer.UpdateColoring((int)properties.coloring.Value);
        }

        if (updateBackend)
		{
			dataManagerPanel.UpdateActiveLayer();

			if (!dataManagerPanel.UpdateBackend())
				return false;
		}

#if !UNITY_WEBGL
		if (properties.site.HasChanged ||
			properties.year.HasChanged ||
			properties.units.HasChanged ||
			properties.categories.HasChanged ||
			properties.coloring.HasChanged ||
			properties.metadata.HasChanged)
		{
			var originalSiteName = properties.site.OriginalValue;
			var site = dataManager.GetSite(originalSiteName);

			if (properties.site.HasChanged)
			{
				// Move layer to another site
				var newSite = dataManager.GetOrAddSite(properties.site);
				site.MoveLayerToSite(activeLayer, newSite);
				activeSite = site = newSite;
			}

			if (properties.year.HasChanged)
			{
				activeLayer.ChangeYear(properties.year.OriginalValue, properties.year, site);
			}

			if (properties.units.HasChanged ||
				properties.categories.HasChanged ||
				properties.coloring.HasChanged ||
				properties.metadata.HasChanged)
			{
				var directories = dataManager.GetDataDirectories();
				var binFiles = activeLayer.GetPatchFiles(directories, site.Name, Patch.BIN_EXTENSION);

				int count = binFiles.Count;
				for (int i = 0; i < count; ++i)
				{
					var file = binFiles[i];
					var newFile = file + ".new";

					try
					{
						if (File.Exists(newFile))
							File.Delete(newFile);

						IntCategory[] categories = null;
						if (properties.categories.Value != null)
							categories = properties.categories.Value.OfType<IntCategory>().ToArray();

						GridDataIO.UpdateBin(file, newFile, properties.units, properties.coloring, properties.metadata, categories);

						File.Delete(file);
						File.Move(newFile, file);
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}
				}
			}
		}
#endif
		properties.Apply();

		if (activeSite != null)
			dataManagerPanel.RefreshSiteList(activeSite);

		return true;
	}

	//
	// Event Methods
	//

	private void OnLayerNameEndEdit(string text)
	{
		var newName = text.Trim();
		if (newName.Equals(properties.name.Value))
			return;

		if (LayerNameExists(newName))
		{
			dialogManager.Warn(translator.Get("A layer already exists with this name") + ":\n\n" + newName);
			layerNameInput.text = properties.name;
			return;
		}

		var question =
			translator.Get("Changing this layer's name will affect all sites") +
			"\n\n" +
			translator.Get("Do you want to continue?");

		Ask(question, properties.name, () => SetLayerName(newName), RevertLayerName);
	}

	private void OnGroupDropdownChanged(int index)
	{
		var group = dataManager.groups[index];
		if (group == properties.group.Value)
			return;

		var question =
			translator.Get("Changing this layer's group will affect all sites") +
			"\n\n" +
			translator.Get("Do you want to continue?");

		Ask(question, properties.group, () => SetLayerGroup(group), RevertLayerGroup);
	}

	private void OnAddGroupAndSwitchLayerToGroupClick()
	{
		// This event happens when the user clicks on the '+' button inside the group dropdown in the layer properties
		// It creates a new group and then changes the layer's group to the newly created one

		dataManagerPanel.AddGroup((newGroup) => {
			UpdateGroupsDropdown(properties.group);
			groupDropdown.value = dataManager.groups.Count - 1;
		});
	}

	private void OnColorClick()
	{
		var question =
			translator.Get("Changing this layer's color will affect all sites") +
			"\n\n" +
			translator.Get("Do you want to continue?");

		Ask(question, properties.color, AskForLayerColor, null);
	}

	private void OnAddSiteAndSwitchLayerToSiteClick()
	{
		// This event happens when the user clicks on the '+' button inside the site dropdown in the layer properties
		// It creates a new site and then changes the layer's site to the newly created one

		var popup = dialogManager.NewPopupDialog();
		popup.ShowInput(translator.Get("New Site"), translator.Get("Site Name"));
		popup.OnCloseDialog += (result) => {
			if (result.action == DialogAction.Ok)
			{
				string newSiteName = popup.input.text.Trim();
				if (ValidateNewSiteName(newSiteName))
					siteDropdown.value = AddTempSiteToDropdown(newSiteName);
				else
					result.shouldClose = false;
			}
		};
	}

	private void OnSiteDropdownChanged(int value)
	{
		// Check if user selected newly created (temp) site
		if (value >= dataManager.sites.Count)
		{
			properties.site.Value = siteDropdown.options[value].text;
			return;
		}

		var newSite = dataManager.sites[value];
		if (newSite.Name == properties.site)
			return;

		if (newSite.HasDataLayer(activeLayer) && newSite.Name != properties.site.OriginalValue)
		{
			string message = Translator.Get("The selected site already has data for this layer");
			dialogManager.Warn(message);
			if (dataManager.TryGetSite(properties.site, out Site site))
				siteDropdown.SetValueWithoutNotify(dataManager.sites.IndexOf(site));
			else
				siteDropdown.SetValueWithoutNotify(dataManager.sites.IndexOf(activeSite));
		}
		else
		{
			properties.site.Value = newSite.Name;
		}
	}

	private void OnYearInputEndEdit(string text)
	{
		if (int.TryParse(text, out int year))
		{
			if (year == properties.year.Value)
				return;

			properties.year.Value = year;
		}

		yearInput.text = properties.year.Value.ToString();
	}

	private void OnUnitsInputEndEdit(string units)
	{
		if (units.Equals(properties.units.Value))
			return;

		properties.units.Value = units;
	}

	private void OnEditCategoriesClick()
	{
		Debug.LogWarning("//+ OnEditCategoriesClick");
	}

	private void OnStyleDropdownChanged(int value)
	{
		//+
		var option = styleDropdown.options[value].text.Replace(" ", "");
		bool isEnumParsed = Enum.TryParse(option, out GridData.Coloring newColoring);

		if (!isEnumParsed || newColoring == properties.coloring.Value)
			return;

		properties.coloring.Value = newColoring;
		styleButton.interactable = properties.coloring == GridData.Coloring.Custom;
    }

	private void OnEditCustomStyleClick()
	{
		Debug.LogWarning("//+ OnEditCustomStyleClick");
	}

	private void OnAddMetadataClick()
	{
		var popup = dialogManager.NewPopupDialog();
		popup.ShowInput(translator.Get("Add Metadata Field"), translator.Get("Field Name"));
		popup.OnCloseDialog += (result) => {
			if (result.action == DialogAction.Ok)
			{
				if (AddMetadata(popup.input.text.Trim()))
					StartCoroutine(DelayedScrollDownMetadataList());
				else
					result.shouldClose = false;
			}
		};
	}

	private void OnMetadataSelected(PaddedInputField input)
	{
		if (coroutine == null)
			coroutine = StartCoroutine(AdjustListScroll(input));
	}

	private void OnMetadataDeselected(PaddedInputField input)
	{
		StopCoroutine(coroutine);
		coroutine = null;
	}
	
	private Coroutine coroutine;
	private IEnumerator AdjustListScroll(PaddedInputField input)
	{
		var rt = input.transform as RectTransform;
		var listBottom = -metadataList.GetComponent<RectTransform>().rect.height;

		yield return null;

		var rect = rt.rect;
		var yPos = rt.localPosition.y + metadataList.content.localPosition.y;
		var cellTop = yPos + rect.yMax;
		var cellBottom = yPos + rect.yMin;
		AdjustListScroll(rt, cellTop, cellBottom, listBottom);

		while (true)
		{
			yield return null;

			var caretPos = input.GetCaretPosition().y;

			rect = rt.rect;
			yPos = rt.localPosition.y + metadataList.content.localPosition.y;
			caretPos += yPos + rect.yMax;
			AdjustListScroll(rt, caretPos + MetadataListPadding, caretPos - MetadataListPadding, listBottom);
		}
	}

	private const float MetadataListPadding = 20;
	private void AdjustListScroll(RectTransform rt, float top, float bottom, float listBottom)
	{
		float shift = 0;

		// Adjust list scroll if the section's bottom is below the list's bottom
		if (bottom < listBottom)
		{
			shift += Math.Max(top, bottom - listBottom);
		}
		// Or the section's top is above the list's top
		else if (top > 0)
		{
			shift += top;
		}

		if (shift != 0)
		{
			var pos = metadataList.content.localPosition;
			pos.y -= shift;
			metadataList.content.localPosition = pos;
		}
	}

	private void OnMetadataKeyEndEdit(PaddedInputField input, int index)
	{
		ChangeMetadataKey(index, input.text.Trim());
	}

	private void OnMetadataValueEndEdit(PaddedInputField input, int index)
	{
		ChangeMetadataValue(index, input.text.Trim());
	}


	//
	// Public Methods
	//

	public void ShowProperties(DataLayer layer, Site site)
	{
		activeLayer = layer;
		activeSite = site;
		if (activeLayer == null)
			return;

		properties.name.Init(activeLayer.Name);
		properties.group.Init(activeLayer.Group);
		properties.color.Init(activeLayer.Color);

		layerNameInput.text = properties.name;
		UpdateGroupsDropdown(properties.group);
		colorButton.image.color = properties.color;

		if (layer.IsTemp)
		{
			layerNameInput.readOnly = true;
			groupDropdown.interactable = false;
			addGroupButton.interactable = false;
		}
		else
		{
			layerNameInput.readOnly = false;
			groupDropdown.interactable = true;
			addGroupButton.interactable = true;
		}

		properties.site.Init();
		properties.year.Init();
		properties.units.Init();
		properties.categories.Init();
		properties.coloring.Init();
		properties.metadata.Init();

#if !UNITY_WEBGL
		if (activeSite == null || layer.IsTemp)
#endif
		{
			sitePanel.SetActive(false);
			metadataPanel.SetActive(false);
		}
#if !UNITY_WEBGL
		else
		{
			properties.site.Init(activeSite.Name);

			sitePanel.SetActive(true);
			metadataPanel.SetActive(true);

			UpdateSitesDropdown(activeSite);

			// Gather data from all the layer's patches
			List<int> years = new List<int>();
			List<string> units = new List<string>();
			List<DataType> types = new List<DataType>();
			List<GridData.Coloring> styles = new List<GridData.Coloring>();
			List<Category> categories = null;
			bool differentCategories = false;
			List<MetadataPair> metadata = null;
			bool differentMetadata = false;
			foreach (var level in layer.levels)
			{
				foreach (var layerSite in level.layerSites)
				{
					if (layerSite.Site == activeSite)
					{
						foreach (var record in layerSite.records)
						{
							var siteRecord = record.Value;
							if (!years.Contains(siteRecord.Year))
								years.Add(siteRecord.Year);

							foreach (var patch in siteRecord.patches)
							{
								var filename = patch.Filename;
								string type = Patch.GetFileNameType(filename);
								if (type.Equals(GridDataIO.FileSufix))
								{
									GridData data = patch.Data as GridData;
									if (!data.IsLoaded())
										data = QuickReadGridPatch(filename);

									if (!units.Contains(data.units))
										units.Add(data.units);
									var dataType = data.IsCategorized ? DataType.Categorised : DataType.Continuous;
									if (!types.Contains(dataType))
										types.Add(dataType);
									if (!styles.Contains(data.coloring))
										styles.Add(data.coloring);

									if (data.IsCategorized && !differentCategories)
									{
										if (categories != null)
										{
											if (!categories.SequenceEqual(data.categories))
											{
												categories.Clear();
												differentCategories = true;
											}
										}
										else if (data.categories != null)
										{
											categories = new List<Category>(data.categories);
										}
									}

									if (!differentMetadata)
									{
										if (metadata != null)
										{
											if (!metadata.SequenceEqual(data.metadata))
											{
												metadata.Clear();
												differentMetadata = true;
											}
										}
										else if (data.metadata != null)
										{
											metadata = new List<MetadataPair>(data.metadata);
										}
									}
								}
								else if (type.Equals(MultiGridDataIO.FileSufix))
								{
									MultiGridData data = patch.Data as MultiGridData;
									if (!data.IsLoaded())
										data = QuickReadMultiGridPatch(filename);

									if (!units.Contains(null))
										units.Add(null);
									if (!types.Contains(DataType.Categorised))
										types.Add(DataType.Categorised);
									if (!styles.Contains(data.coloring))
										styles.Add(data.coloring);
								}
								else if (type.Equals(GraphDataIO.FileSufix))
								{
									types.Add(DataType.Graph);
								}
								else
								{
									types.Add(DataType.Unknown);
								}
							}
						}
						break;
					}
				}
			}

			// Update Year
			if (years.Count != 1)
			{
				yearInput.interactable = false;
				yearInput.text = "";
			}
			else
			{
				properties.year.Init(years[0]);

				yearInput.interactable = true;
				yearInput.text = properties.year.Value.ToString();
			}

			// Update Units
			if (units.Count != 1)
			{
				unitsInput.interactable = false;
				unitsInput.text = "";
			}
			else
			{
				properties.units.Init(units[0]);

				unitsInput.interactable = true;
				unitsInput.text = properties.units;
			}

			// Update Type
			if (types.Count != 1)
			{
				typeInput.text = "";
				categoriesButton.interactable = false;
			}
			else
			{
				typeInput.text = types[0].ToString();

				properties.categories.Init(categories?.ToArray());
				categoriesButton.interactable = !differentCategories && categories != null && categories.Count > 0;

                //+
                UpdateStyleDropdown(types[0] == DataType.Categorised);
			}

			// Update Style
			if (styles.Count != 1)
			{
				styleButton.interactable =
				styleDropdown.interactable = false;
				styleDropdown.transform.GetChild(0).gameObject.SetActive(true);
				styleDropdown.captionText.text = "";
			}
			else
			{
				properties.coloring.Init(styles[0]);

				styleDropdown.transform.GetChild(0).gameObject.SetActive(false);
				styleDropdown.interactable = true;
				styleButton.interactable = properties.coloring == GridData.Coloring.Custom;

				styleDropdown.onValueChanged.RemoveListener(OnStyleDropdownChanged);
				//+
				// Check if properties.coloring.Value exists in styleDropdown options
				// Assign default value if not found
				int index = styleDropdown.options.FindIndex((i) => i.text.Equals(properties.coloring.Value.ToString().Replace(" ", "")));
				styleDropdown.value = index == -1 ? 0 : (int)properties.coloring.Value;
				styleDropdown.onValueChanged.AddListener(OnStyleDropdownChanged);
			}

			// Update Metadata
			if (differentMetadata)
			{
				noMetadataMessage.SetActive(false);
				multipleMetadataMessage.SetActive(true);
				addMetadataButton.interactable = false;

				ClearMetadata();
			}
			else if (metadata == null || metadata.Count == 0)
			{
				noMetadataMessage.SetActive(true);
				multipleMetadataMessage.SetActive(false);

				bool validMetadata = types.Count == 1 && (types[0] == DataType.Continuous || types[0] == DataType.Categorised);
				addMetadataButton.interactable = validMetadata;

				ClearMetadata();
			}
			else
			{
				properties.metadata.Init(metadata);

				noMetadataMessage.SetActive(false);
				multipleMetadataMessage.SetActive(false);
				addMetadataButton.interactable = true;

				UpdateMetadataList(properties.metadata);
			}
		}
#endif

	}

	//
	// Private Methods
	//

	private void InitStyleDropdown()
	{
		// Populate Style Dropdown
		List<string> options = new List<string>();
		var names = Enum.GetNames(typeof(GridData.Coloring));
		foreach (var name in names)
			options.Add(name.SplitCamelCase());

		styleDropdown.AddOptions(options);
		styleDropdown.onValueChanged.AddListener(OnStyleDropdownChanged);
	}

    //+
    private void UpdateStyleDropdown(bool isCategorised)
    {
        styleDropdown.ClearOptions();
        List<string> options = new List<string>();
        var values = Array.ConvertAll((GridData.Coloring[])Enum.GetValues(typeof(GridData.Coloring)), delegate (GridData.Coloring value) { return (int)value; });
        values = values.Distinct().ToArray();
        int length = values.Length;

        for (int i = 0; i < length; ++i)
        {
            if (isCategorised)
            {
                if (i < (int)GridData.Coloring.Reverse)
                    options.Add(((GridData.Coloring)i).ToString().SplitCamelCase());
            }
            else
            {
                if (i == (int)GridData.Coloring.Forward || i >= (int)GridData.Coloring.Custom)
                    options.Add(((GridData.Coloring)i).ToString().SplitCamelCase());
            }
        }

        styleDropdown.AddOptions(options);
        styleDropdown.onValueChanged.AddListener(OnStyleDropdownChanged);
    }

    private void UpdateGroupsDropdown(LayerGroup group)
	{
		// Clear Sites Dropdown
		groupDropdown.onValueChanged.RemoveListener(OnGroupDropdownChanged);
		groupDropdown.ClearOptions();

		// Populate Sites Dropdown
		var groups = dataManager.groups;
		if (groups.Count > 0)
		{
			List<string> options = new List<string>();
			foreach (var g in groups)
				options.Add(g.name);

			groupDropdown.AddOptions(options);

			if (group != null)
				groupDropdown.value = groups.IndexOf(group);

			groupDropdown.onValueChanged.AddListener(OnGroupDropdownChanged);
		}
	}

	private void UpdateSitesDropdown(Site site)
	{
		// Clear Sites Dropdown
		siteDropdown.onValueChanged.RemoveListener(OnSiteDropdownChanged);
		siteDropdown.ClearOptions();

		// Populate Sites Dropdown
		var sites = dataManager.sites;
		if (sites.Count > 0)
		{
			List<string> options = new List<string>();
			foreach (var s in sites)
				options.Add(s.Name);

			siteDropdown.AddOptions(options);

			if (site != null)
				siteDropdown.value = sites.IndexOf(site);

			siteDropdown.onValueChanged.AddListener(OnSiteDropdownChanged);
		}
	}

	private int AddTempSiteToDropdown(string siteName)
	{
		siteDropdown.AddOptions(new List<string> { siteName });
		return siteDropdown.options.Count - 1;
	}

	private void UpdateMetadataList(List<MetadataPair> metadata, int selectedInput = -1)
	{
		int rows = metadata.Count;
		int childCount = metadataGrid.childCount;
		for (int i = 0; i < rows; ++i)
		{
			int index = i;
			int childIndex = i * 2;

			PaddedInputField keyInput, valueInput;
			if (childIndex < childCount)
			{
				keyInput = metadataGrid.GetChild(childIndex).GetComponent<PaddedInputField>();
				valueInput = metadataGrid.GetChild(childIndex + 1).GetComponent<PaddedInputField>();
				keyInput.onEndEdit.RemoveListener(OnKeyEndEdit);
				valueInput.onEndEdit.RemoveListener(OnValueEndEdit);
				keyInput.onSelected.RemoveAllListeners();
				keyInput.onDeselected.RemoveAllListeners();
				valueInput.onSelected.RemoveAllListeners();
				valueInput.onDeselected.RemoveAllListeners();
			}
			else
			{
				keyInput = Instantiate(metadataCellPrefab, metadataGrid);
				keyInput.name = metadataCellPrefab.name + childIndex;
				valueInput = Instantiate(metadataCellPrefab, metadataGrid);
				valueInput.name = metadataCellPrefab.name + (childIndex + 1);
			}

			keyInput.text = metadata[i].Key;
			valueInput.text = metadata[i].Value;

			keyInput.onEndEdit.AddListener(OnKeyEndEdit);
			valueInput.onEndEdit.AddListener(OnValueEndEdit);
			keyInput.onSelected.AddListener(OnMetadataSelected);
			keyInput.onDeselected.AddListener(OnMetadataDeselected);
			valueInput.onSelected.AddListener(OnMetadataSelected);
			valueInput.onDeselected.AddListener(OnMetadataDeselected);

			void OnKeyEndEdit(string _) => OnMetadataKeyEndEdit(keyInput, index);
			void OnValueEndEdit(string _) => OnMetadataValueEndEdit(valueInput, index);
		}

		int totalCells = rows * 2;

		// Remove extra rows
		ClearMetadata(totalCells);

		if (selectedInput >= 0 && selectedInput < totalCells)
		{
			var selectable = metadataGrid.GetChild(selectedInput).GetComponent<Selectable>();
			if (selectable != null)
				selectable.Select();
		}
	}

	private void ClearMetadata(int fromItem = 0)
	{
		for (int i = metadataGrid.childCount - 1; i >= fromItem; --i)
		{
			Destroy(metadataGrid.GetChild(i).gameObject);
		}
	}

	private GridData QuickReadGridPatch(string filename)
	{
		var data = new GridData();
		using (var br = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
		{
			GridDataIO.ParseBinHeader(br, filename, data);
			GridDataIO.ParseBinProperties(br, filename, data);
		}
		return data;
	}

	private MultiGridData QuickReadMultiGridPatch(string filename)
	{
		var data = new MultiGridData();
		using (var br = new BinaryReader(File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
		{
			MultiGridDataIO.ParseBinHeader(br, filename, data);
			MultiGridDataIO.ParseBinProperties(br, data);
		}
		return data;
	}

	private void SetLayerName(string newName)
	{
		properties.name.Value = newName;
		layerNameInput.text = newName;
	}

	private void RevertLayerName()
	{
		layerNameInput.text = properties.name;
	}

	private void SetLayerGroup(LayerGroup group)
	{
		properties.group.Value = group;
	}

	private void RevertLayerGroup()
	{
		groupDropdown.onValueChanged.RemoveListener(OnGroupDropdownChanged);
		groupDropdown.value = dataManager.groups.IndexOf(properties.group);
		groupDropdown.onValueChanged.AddListener(OnGroupDropdownChanged);
	}

	private void AskForLayerColor()
	{
		var popup = dialogManager.NewDialog(colourDialogPrefab);
		popup.Show(properties.color);
		popup.OnCloseDialog += (result) => {
			if (result.action == DialogAction.Ok)
			{
				SetLayerColor(popup.Color);
			}
		};
	}

	private void SetLayerColor(Color color)
	{
		if (color.Equals(properties.color))
			return;

		properties.color.Value = color;
		colorButton.image.color = color;
	}

	private bool LayerNameExists(string name)
	{
		foreach (var group in dataManager.groups)
		{
			foreach (var layer in group.layers)
			{
				if (name.EqualsIgnoreCase(layer.Name) && layer != activeLayer)
					return true;
			}
		}
		return false;
	}

	private void PrepareMetadataProperty()
	{
		if (properties.metadata.Value == null)
			properties.metadata.Value = new List<MetadataPair>();
		else if (!properties.metadata.HasChanged)
			properties.metadata.Value = new List<MetadataPair>(properties.metadata.OriginalValue);
	}

	private bool AddMetadata(string key)
	{
		if (string.IsNullOrWhiteSpace(key))
			return false;

		if (properties.metadata.Value != null && properties.metadata.Value.Exists((m) => m.Key.EqualsIgnoreCase(key)))
		{
			dialogManager.Warn(translator.Get("Metadata already has this field"));
			return false;
		}

		PrepareMetadataProperty();
		properties.metadata.Value.Add(key, "");

		var lastElement = properties.metadata.Value.Count * 2 - 1;
		UpdateMetadataList(properties.metadata, lastElement);

		return true;
	}

	private void ChangeMetadataKey(int index, string key)
	{
		if (string.IsNullOrEmpty(key))
		{
			dialogManager.Ask(translator.Get("Do you want to delete this metadata field?"), () => RemoveMetadata(index));
			return;
		}

		PrepareMetadataProperty();
		properties.metadata.Value[index].Key = key;
	}

	private void ChangeMetadataValue(int index, string value)
	{
		PrepareMetadataProperty();
		properties.metadata.Value[index].Value = value;
	}

	private void RemoveMetadata(int index)
	{
		PrepareMetadataProperty();
		properties.metadata.Value.RemoveAt(index);

		UpdateMetadataList(properties.metadata);
	}

	private IEnumerator DelayedScrollDownMetadataList()
	{
		yield return null;
		yield return null;
		metadataList.normalizedPosition = Vector2.zero;
	}

	private bool ValidateNewSiteName(string siteName)
	{
		if (string.IsNullOrWhiteSpace(siteName))
			return false;

		foreach (var option in siteDropdown.options)
		{
			if (option.text == siteName)
			{
				dialogManager.Warn(translator.Get("A site already exists with this name"));
				return false;
			}
		}

		return true;
	}
}
