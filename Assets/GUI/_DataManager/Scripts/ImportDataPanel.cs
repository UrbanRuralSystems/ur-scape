// Copyright (C) 2019 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)

using Catfood.Shapefile;
using SFB;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ImportDataPanel : MonoBehaviour
{
    [Header("UI References", order = 1)]
    public GameObject propertiesPanel;
    public Text fileLabel;
    public Button openFile;
    public Text fieldLabel;
	public Dropdown fieldDropdown;
	public DropdownWithInput siteDropdown;
    public DropdownWithInput groupDropdown;
    public DropdownEx layerDropdown;
	public Toggle categorisedCheckbox;
	public Text categoriesLabel;
	public InputField categoryInput;
    public Button loadCategoriesButton;
	public DropdownEx resolutionDropdown;
	public InputField unitsInput;
	public InputField yearInput;
    public InputField monthInput;
    public InputField sourceInput;
    public Transform gridContainer;
    public Button addButton;
	public RectTransform warningIcon;
	public Text warningMessage;
	public RectTransform mask;
	public RectTransform categories;

	[Header("Prefabs")]
    public Text labelPrefab;
    public InputField inputPrefab;

    [Header("Settings")]
    public DataResolution[] resolutions = new DataResolution[]
    {
        new DataResolution{ name = "World"/*translatable*/, size = 1500, units = DataResolution.Units.Seconds },
        new DataResolution{ name = "Continent"/*translatable*/, size = 300, units = DataResolution.Units.Seconds },
        new DataResolution{ name = "Region"/*translatable*/, size = 30, units = DataResolution.Units.Seconds },
        new DataResolution{ name = "City"/*translatable*/, size = 100, units = DataResolution.Units.Meters },
        new DataResolution{ name = "Neighbourhood"/*translatable*/, size = 10, units = DataResolution.Units.Meters },
    };

	public string FullFilename { get; private set; } = null;
	public Site Site { get; private set; } = null;
	public string SiteName { get { return Site != null ? Site.Name : siteDropdown.input.text; } }
	public LayerGroup Group { get; private set; } = null;
	public string GroupName { get { return Group != null ? Group.name : groupDropdown.input.text; } }
	public DataLayer Layer { get; private set; } = null;
	public string LayerName { get { return layerDropdown.HasSelected ? layerDropdown.captionText.text : null ; } }
	public Color LayerColor { get; private set; } = Color.gray;
	public DataResolution Resolution { get { return resolutionDropdown.HasSelected ? resolutions[resolutionDropdown.value] : null; } }
	public int Level { get { return resolutionDropdown.HasSelected ? Mathf.Min(resolutionDropdown.value, 3) : 3; } }

	public string Units { get { return unitsInput.text; } }
	public int Year { get { int.TryParse(yearInput.text, out int year); return year; } }
	public int Month { get { int.TryParse(monthInput.text, out int month); return month; } }
	public bool NeedsResampling { get; private set; } = false;

	private WizardDialog wizardDlg;
    private DataManager dataManager;
	private Interpreter interpreter;
	private BasicInfo fileInfo;

	private readonly List<string> customPropertyNames = new List<string>();
	private readonly Dictionary<string, InputField> customProperties = new Dictionary<string, InputField>();
	private readonly Dictionary<string, Component> warningMessages = new Dictionary<string, Component>();
	private Component warningComponent;
	private LocalizationManager translator;

	private string IncorrectYearWarning;
	private string IncorrectMonthWarning;


	//
	// Unity Methods
	//

	private void Awake()
	{
		dataManager = ComponentManager.Instance.Get<DataManager>();
		translator = LocalizationManager.Instance;

		foreach (var res in resolutions)
		{
			res.name = translator.Get(res.name, false);
		}

		IncorrectYearWarning = translator.Get("Please enter a year between 1800 and 2500");
		IncorrectMonthWarning = translator.Get("Please enter a month value between 1 and 12");

		OnCategorisedCheckboxChanged(false);

		openFile.onClick.AddListener(OnOpenFileClick);
		addButton.onClick.AddListener(OnAddClick);

		siteDropdown.OnTextChangedWithoutValueChange += OnSiteInputChangedWithoutValueChange;
        groupDropdown.OnTextChangedWithoutValueChange += OnGroupInputChangedWithoutValueChange;
		categorisedCheckbox.onValueChanged.AddListener(OnCategorisedCheckboxChanged);
		loadCategoriesButton.onClick.AddListener(OnLoadCategoriesClicked);
        categoryInput.onValueChanged.AddListener(OnCategoryInputChanged);
        categoryInput.onEndEdit.AddListener(OnCategoryInputEndEdit);
        unitsInput.onEndEdit.AddListener(OnUnitsInputEndEdit);
		yearInput.onEndEdit.AddListener(OnYearInputEndEdit);
		monthInput.onEndEdit.AddListener(OnMonthInputEndEdit);
	}

    private void Start()
	{
		// These callbacks need to be registered in Start and not in Awake to allow dropboxes to register their callbacks first
        AddOnEndEditEvent(siteDropdown.input);
        AddOnEndEditEvent(groupDropdown.input);
		AddOnEndEditEvent(categoryInput);
        AddOnEndEditEvent(unitsInput);
		AddOnEndEditEvent(yearInput);
        AddOnEndEditEvent(monthInput);
        AddOnEndEditEvent(sourceInput);
    }

    private void AddOnEndEditEvent(InputField input)
    {
        input.onEndEdit.AddListener((text) => OnInputEndEdit(input, text));
    }

    private void OnEnable()
    {
        if (FullFilename == null)
            StartCoroutine(DelayedEnable());
    }

    private IEnumerator DelayedEnable()
    {
		yield return null;
		yield return null;
		UpdatePanelUI();
        yield return new WaitForFrames(10);
        SelectFiles();
    }


    //
    // Event Methods
    //

    private void OnOpenFileClick()
    {
        SelectFiles();
    }

    private void OnLoadFiles(string[] paths)
    {
        if (paths == null || paths.Length == 0 || string.IsNullOrWhiteSpace(paths[0]))
        {
            if (FullFilename == null)
                wizardDlg.CloseDialog(DialogAction.Cancel);
            return;
        }

		FullFilename = paths[0];
		var filename = Path.GetFileName(FullFilename);

		fileLabel.text = filename;

		interpreter = Interpreter.Get(FullFilename);
		fileInfo = interpreter.GetBasicInfo(FullFilename);

		if (fileInfo == null)
		{
			ShowWarningMessage(translator.Get("Invalid file"), openFile, true);
			return;
		}

		// Update field dropdown if file is vector data
		bool isVectorData = interpreter is ShapefileInterpreter;
		if (isVectorData)
			UpdateFieldDropdown(FullFilename);

		// Show/hide field label and dropdown depending on vector or raster data
		fieldLabel.gameObject.SetActive(isVectorData);
		fieldDropdown.gameObject.SetActive(isVectorData);

		var filename_lc = filename.ToLower();

		// Try to guess the site
		if (string.IsNullOrWhiteSpace(siteDropdown.input.text))
		{
			var sites = dataManager.sites;
			for (int i = 0; i < sites.Count; i++)
			{
				if (filename_lc.Contains(sites[i].Name.ToLower()))
				{
					siteDropdown.value = i;
					break;
				}
			}
		}

		// Try to guess the layer (and group)
		if (!layerDropdown.HasSelected)
		{
			var layer = GetLayer(filename_lc);
			if (layer == null && !string.IsNullOrWhiteSpace(fileInfo.suggestedLayerName))
				layer = GetLayer(fileInfo.suggestedLayerName.ToLower());

			if (layer != null)
			{
				groupDropdown.value = dataManager.groups.IndexOf(layer.Group);
				layerDropdown.value = layer.Group.layers.IndexOf(layer);
			}
		}

		// Suggest the resolution
		if (!resolutionDropdown.HasSelected && fileInfo.degreesPerPixel != 0)
		{
			for (int i = 0; i < resolutions.Length; i++)
			{
				if (fileInfo.degreesPerPixel.Similar(resolutions[i].ToDegrees()))
				{
					resolutionDropdown.value = i;
					break;
				}
			}
		}

		// Suggest the units
		if (string.IsNullOrWhiteSpace(unitsInput.text) && !string.IsNullOrWhiteSpace(fileInfo.suggestedUnits))
		{
			unitsInput.text = translator.Get(fileInfo.suggestedUnits);
		}

		if (fileInfo != null)
		{
			CheckBounds();
			CheckRasterSize();
			CheckResolution();
		}

		ValidateResolutionDrowndown();

		UpdateProgress();
    }

	private void OnLoadCategoryFile(string[] paths)
    {
		if (paths == null || paths.Length == 0 || string.IsNullOrWhiteSpace(paths[0]))
        {
			return;
        }

		ResetCategoryInputs();

		// Automatically fill in categories from csv file
		using (StreamReader sr = new StreamReader(paths[0]))
		{
            // Read first line for first category input
            string category = sr.ReadLine();
            categoryInput.text = category;

			// Read rest of line for rest of categories
			while (!sr.EndOfStream)
            {
                category = sr.ReadLine();
                UpdateMask(unitsInput);
                AddCategoryInput(category);
                GuiUtils.RebuildLayout(transform);
            }
            UpdateMask(unitsInput);
            AddCategoryInput();
            GuiUtils.RebuildLayout(transform);
        }
	}

	private void OnInputEndEdit(InputField input, string text)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            var next = input.FindSelectableOnRight();
            if (next == null || !next.IsInteractable())
                next = input.FindSelectableOnDown();
            if (next != null && next.IsInteractable())
                next.Select();
        }
    }

    private void OnSiteDropdownChanged(int index)
    {
		SetCurrentSite(index);
    }

    private void OnSiteInputChangedWithoutValueChange()
    {
		SetCurrentSite(siteDropdown.value);
    }

	private void OnGroupDropdownChanged(int index)
    {
        SetCurrentGroup(index);
    }

    private void OnGroupInputChangedWithoutValueChange()
    {
		SetCurrentGroup(groupDropdown.value);
    }

    private void OnLayerDropdownChanged(int index)
    {
		SetCurrentLayer(index);
    }

    private int resolutionItemIndex = 0;
    private void OnResolutionDropdownItemCreated(GameObject dropdownItem)
    {
        var resolution = resolutions[resolutionItemIndex++];
        var toggle = dropdownItem.GetComponent<Toggle>();
        toggle.interactable = CanUseResolution(resolution);
        dropdownItem.GetComponentsInChildren<Text>()[1].text = resolution.ToString();
        if (resolutionItemIndex == resolutions.Length)
			resolutionItemIndex = 0;
    }

	private void OnResolutionDropdownChanged(int index)
    {
		if (!resolutionDropdown.HasSelected)
			return;

        var res = resolutions[index];
        resolutionDropdown.captionText.text = Translator.Get(res.name, false) + "  (" + res.ToMetersString(true, true) + ")";
        UpdateProgress();
		resolutionItemIndex = 0;
	}

	private void OnCategorisedCheckboxChanged(bool isOn)
	{
		loadCategoriesButton.interactable = isOn;
		categoriesLabel.gameObject.SetActive(isOn);
		categories.gameObject.SetActive(isOn);

		var categoriesInputs = categories.GetComponentsInChildren<InputField>();
		int count = categories.childCount;

		if (count > 1)
		{
			var lastCategoryInput = categoriesInputs[count - 1];
			var placeholder = lastCategoryInput.placeholder;
			placeholder.GetComponent<LocalizedText>().text = translator.Get("New Category");
		}

		if (isOn)
			UpdateMask(unitsInput);
		else
			UpdateMask(resolutionDropdown);
		GuiUtils.RebuildLayout(transform);
	}

	private void OnLoadCategoriesClicked()
	{
		SelectCategoryFile();
	}

	private void OnCategoryInputChanged(string text)
	{
		var currTextLen = text.Length;

		// Text changed from empty
		if ((currTextLen - 1) == 0)
		{
			UpdateMask(resolutionDropdown);
			AutoAddCategoryInput();
		}

		// Delete category input when text changed to empty
		if (string.IsNullOrEmpty(text))
        {
			var categoriesInputs = categories.GetComponentsInChildren<InputField>();
			int count = categories.childCount;

			if (count > 1)
            {
				for (int i = 1; i < count; ++i)
				{
					var input = categoriesInputs[i];
					if (string.IsNullOrEmpty(input.text))
					{
						input.onEndEdit.RemoveListener(OnCategoryInputEndEdit);
						input.onValueChanged.RemoveListener(OnCategoryInputChanged);
						Destroy(input.gameObject);

						UpdateMask(categories);
						GuiUtils.RebuildLayout(categories);

						break;
					}
				}
			}
        }
	}

	private void OnCategoryInputEndEdit(string text)
	{
		UpdateProgress();
	}

	private void OnUnitsInputEndEdit(string text)
	{
		UpdateProgress();
	}

	private string incorrectYear = null;
    private void OnYearInputEndEdit(string text)
    {
        if (!string.IsNullOrWhiteSpace(text) &&
            (!int.TryParse(text, out int value) || value < 1900 || value > 2500))
        {
            if (text != incorrectYear)
            {
                incorrectYear = text;
                var dlg = ComponentManager.Instance.Get<ModalDialogManager>().NewPopupDialog();
                dlg.name = "InputYearDialog";
                dlg.ShowWarningMessage(IncorrectYearWarning);
                dlg.OnCloseDialog += (result) =>
                {
                    yearInput.selectionAnchorPosition = 0;
                    yearInput.selectionFocusPosition = yearInput.text.Length;
                    yearInput.Select();
                };
            }
        }
        else
        {
            incorrectYear = null;
        }
		ShowWarningMessage(IncorrectYearWarning, yearInput, incorrectYear != null);
		UpdateProgress();
    }

    private string incorrectMonth = null;
    private void OnMonthInputEndEdit(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (!string.IsNullOrWhiteSpace(text) &&
            (!int.TryParse(text, out int value) || value < 1 || value > 12))
        {
            if (text != incorrectMonth)
            {
                incorrectMonth = text;

                var dlg = ComponentManager.Instance.Get<ModalDialogManager>().NewPopupDialog();
                dlg.ShowWarningMessage(IncorrectMonthWarning);
                dlg.name = "InputMonthDialog";
                dlg.OnCloseDialog += (result) =>
                {
                    monthInput.selectionAnchorPosition = 0;
                    monthInput.selectionFocusPosition = monthInput.text.Length;
                    monthInput.Select();
                };
            }
        }
        else
        {
            incorrectMonth = null;
        }
		ShowWarningMessage(IncorrectMonthWarning, monthInput, incorrectMonth != null);
		UpdateProgress();
    }

    private void OnAddClick()
    {
        var dlg = ComponentManager.Instance.Get<ModalDialogManager>().NewPopupDialog();
        dlg.name = "AddPropertyDialog";
        dlg.ShowInput(translator.Get("Add New Property"), translator.Get("Type property name"));
        dlg.OnCloseDialog += (result) => {
            if (result.action == DialogAction.Ok)
            {
                if (!AddCustomProperty(dlg.InputValue))
                    result.shouldClose = false;
            }
        };
	}

    //
    // Public Methods
    //

    public void Init(WizardDialog wizardDlg)
    {
        this.wizardDlg = wizardDlg;
    }

	public List<MetadataPair> GetMetadata()
	{
		var metadata = new List<MetadataPair>();

		if (!string.IsNullOrWhiteSpace(sourceInput.text))
			metadata.Add("Source", sourceInput.text);

		foreach (var name in customPropertyNames)
		{
			var input = customProperties[name];
			if (!string.IsNullOrWhiteSpace(input.text))
				metadata.Add(name, input.text);
		}

		if (metadata.Count == 0)
			return null;

		return metadata;
	}

	public string GetOutputFilename()
	{
		var layerName = LayerName;
		var siteName = SiteName;
		var level = Level;
		var year = Year;
		var month = Month;
		var pathId = 0; //+ Missing PatchID
		var type = GridDataIO.FileSufix; //+ Don't asume it's a grid

		// Build output file name
		var patchFilename = Patch.GetFileName(layerName, level, siteName, pathId, year, month, type);
		return Path.Combine(Paths.Sites, siteName, patchFilename + "." + Patch.BIN_EXTENSION);
	}

	public string[] GetCategories()
    {
		var categoryInputs = categories.GetComponentsInChildren<InputField>();
		int length = categoryInputs.Length - 1;

		if (categorisedCheckbox.isOn && length > 0)
		{
			string[] categoryNames = new string[length];

			for (int i = 0; i < length; ++i)
			{
				categoryNames[i] = categoryInputs[i].text;
			}

			return categoryNames;
		}

		return null;
    }

	//
	// Private Methods
	//

	private void UpdateProgress()
    {
        bool canContinue = false;
        bool siteInteractable = false;
        bool groupInteractable = false;
        bool layerInteractable = false;
        bool resolutionInteractable = false;
		bool unitsInteractable = false;
		bool yearInteractable = false;
        bool monthInteractable = false;
        bool sourceInteractable = false;

        if (FullFilename == null || warningComponent == openFile)
        {
            UpdateMask(openFile);
        }
        else
        {
			siteInteractable = true;
			if (string.IsNullOrWhiteSpace(siteDropdown.input.text)
				 || warningComponent == siteDropdown)
			{
				UpdateMask(siteDropdown);
			}
			else
			{
				groupInteractable = true;
				if (string.IsNullOrWhiteSpace(groupDropdown.input.text) || warningComponent == groupDropdown)
				{
					UpdateMask(groupDropdown);
				}
				else
				{
					layerInteractable = true;
					if (!layerDropdown.HasSelected || warningComponent == layerDropdown)
					{
						UpdateMask(layerDropdown);
					}
					else
					{
						resolutionInteractable = true;
						if (!resolutionDropdown.HasSelected || warningComponent == resolutionDropdown)
						{
							UpdateMask(resolutionDropdown);
						}
						else
						{
							unitsInteractable = true;
							if (string.IsNullOrWhiteSpace(unitsInput.text) || warningComponent == unitsInput)
							{
								UpdateMask(unitsInput);
							}
							else
							{
								yearInteractable = true;
								monthInteractable = true;
								if (string.IsNullOrWhiteSpace(yearInput.text)
									|| incorrectYear != null || incorrectMonth != null
									|| warningComponent == yearInput)
								{
									UpdateMask(yearInput.transform.parent);
								}
								else
								{
									canContinue = true;
									sourceInteractable = true;
								}
							}
						}
					}
				}
			}
        }

        siteDropdown.input.interactable = siteInteractable;
        groupDropdown.input.interactable = groupInteractable;
        layerDropdown.interactable = layerDropdown.options.Count > 0 && layerInteractable;
        resolutionDropdown.interactable = layerDropdown.options.Count > 0 && resolutionInteractable;
		unitsInput.interactable = unitsInteractable;
		yearInput.interactable = yearInteractable;
        monthInput.interactable = monthInteractable;
        sourceInput.interactable = sourceInteractable;

        mask.gameObject.SetActive(!canContinue);

		var hasSameResolution = SameResolution();
		NeedsResampling = !hasSameResolution;
		wizardDlg.IsLast = true; //+ hasSameResolution;

		canContinue &= incorrectYear == null && incorrectMonth == null;

        var button = wizardDlg.IsLast ? wizardDlg.finishButton : wizardDlg.nextButton;
        button.interactable = canContinue;
    }

    private void UpdateMask(Component c)
    {
		var offset = mask.offsetMax;
		var rt = c.transform as RectTransform;
		offset.y = rt.offsetMin.y;
		mask.offsetMax = offset;
    }

    private void SelectFiles()
    {
		List<string> allExtensions = new List<string>();
		var formats = Interpreter.DataFormats;
        var extFilters = new ExtensionFilter[formats.Count + 1];
		for (int i = 0; i < formats.Count; i++)
        {
			allExtensions.AddRange(formats[i].extensions);
			extFilters[i + 1] = new ExtensionFilter(formats[i].name + " ", formats[i].extensions);
        }
		extFilters[0] = new ExtensionFilter(translator.Get("All Files") + " ", allExtensions.ToArray());

		StandaloneFileBrowser.OpenFilePanelAsync(translator.Get("Select file to import"), "", extFilters, false, OnLoadFiles);
    }

	private void SelectCategoryFile()
    {
		List<string> allExtensions = new List<string>();
		allExtensions.Add("csv");
		var formats = Interpreter.DataFormats;
		var extFilters = new ExtensionFilter[1];
		extFilters[0] = new ExtensionFilter(translator.Get("Category File") + " ", allExtensions.ToArray());

		StandaloneFileBrowser.OpenFilePanelAsync(translator.Get("Select category file"), "", extFilters, false, OnLoadCategoryFile);
	}

    private void UpdatePanelUI()
    {
        var sites = dataManager.sites;
        siteDropdown.onValueChanged.RemoveListener(OnSiteDropdownChanged);
        siteDropdown.SetOptions(sites, (item) => item.Name);
        siteDropdown.onValueChanged.AddListener(OnSiteDropdownChanged);

        var groups = dataManager.groups;
        groupDropdown.onValueChanged.RemoveListener(OnGroupDropdownChanged);
        groupDropdown.SetOptions(groups, (item) => item.name);
		groupDropdown.onValueChanged.AddListener(OnGroupDropdownChanged);

        resolutionDropdown.OnItemCreated -= OnResolutionDropdownItemCreated;
        resolutionDropdown.OnItemCreated += OnResolutionDropdownItemCreated;
        resolutionDropdown.onValueChanged.RemoveListener(OnResolutionDropdownChanged);
        resolutionDropdown.SetOptions(resolutions, (item) => Translator.Get(item.name, false));
        resolutionDropdown.onValueChanged.AddListener(OnResolutionDropdownChanged);

        UpdateLayersList();
    }

	private void SetCurrentSite(int siteIndex)
	{
		Site site = null;
		if (siteIndex >= 0 && siteIndex < dataManager.sites.Count)
		{
			site = dataManager.sites[siteIndex];
		}

		if (site != Site)
		{
			Site = site;
		}

		UpdateProgress();
	}

	private void SetCurrentGroup(int groupIndex)
    {
		LayerGroup group = null;
		if (groupIndex >= 0 && groupIndex < dataManager.groups.Count)
		{
			group = dataManager.groups[groupIndex];
		}

		if (group != Group)
        {
			Group = group;
			UpdateLayersList();
        }
        else
        {
            UpdateProgress();
        }
    }

	private void SetCurrentLayer(int layerIndex)
	{
		DataLayer layer = null;
		if (Group != null && layerIndex >= 0 && layerIndex < Group.layers.Count)
		{
			layer = Group.layers[layerIndex];
		}

		Layer = layer;
		if (layer != null)
			LayerColor = layer.Color;

		UpdateProgress();
	}

	private void UpdateLayersList()
    {
        List<DataLayer> layers = Group?.layers;
        layerDropdown.onValueChanged.RemoveListener(OnLayerDropdownChanged);
        layerDropdown.SetOptions(layers, (item) => item.Name);
        layerDropdown.onValueChanged.AddListener(OnLayerDropdownChanged);

        UpdateProgress();
    }

    private bool AddCustomProperty(string propName)
    {
        if (string.IsNullOrWhiteSpace(propName))
            return false;

        if (customProperties.ContainsKey(propName))
        {
			ComponentManager.Instance.Get<ModalDialogManager>().Warn(translator.Get("This property already exists"));
            return false;
        }

        var label = Instantiate(labelPrefab, gridContainer, false);
        label.text = propName.Trim();

        var input = Instantiate(inputPrefab, gridContainer, false);
        input.Select();

		int index = sourceInput.transform.GetSiblingIndex();
        label.transform.SetSiblingIndex(++index);
        input.transform.SetSiblingIndex(++index);

		customPropertyNames.Add(propName);
		customProperties.Add(propName, input);

        return true;
    }

	private void CheckBounds()
	{
		ShowWarningMessage(translator.Get("Invalid geolocation"), openFile, fileInfo.bounds.IsEmpty);
	}

	private void CheckRasterSize()
	{
		bool invalidRasterSize = false;
		bool largeRasterSize = false;
		if (fileInfo.isRaster)
		{
			long pixelCount = fileInfo.width * fileInfo.height;
			invalidRasterSize = fileInfo.width <= 0 || fileInfo.height <= 0;
			largeRasterSize = pixelCount > GridData.MaxValuesCount;
		}
		ShowWarningMessage(translator.Get("Invalid raster size"), openFile, invalidRasterSize);
		ShowWarningMessage(translator.Get("Raster size is too large"), openFile, largeRasterSize);
	}

	private void CheckResolution()
	{
		bool invalidResolution = fileInfo.isRaster && fileInfo.degreesPerPixel <= 0;
		ShowWarningMessage(translator.Get("Invalid raster resolution"), openFile, invalidResolution);
	}

	private void ValidateResolutionDrowndown()
	{
		if (resolutionDropdown.HasSelected &&
			!CanUseResolution(resolutions[resolutionDropdown.value]))
		{
			resolutionDropdown.Deselect();
		}
	}

	private bool CanUseResolution(DataResolution resolution)
	{
		return fileInfo != null && fileInfo.degreesPerPixel.SimilarOrSmallerThan(resolution.ToDegrees());
	}

	private bool SameResolution()
    {
		return fileInfo != null && resolutionDropdown.HasSelected && 
			fileInfo.degreesPerPixel.Similar(resolutions[resolutionDropdown.value].ToDegrees());
	}

	private void ShowWarningMessage(string msg, Component c, bool show)
	{
		if (show)
		{
			if (warningMessages.Count == 0)
			{
				warningMessage.text = msg;
				UpdateWarningTransform(c);
			}

			if (!warningMessages.ContainsKey(msg))
				warningMessages.Add(msg, c);
		}
		else if (warningMessages.Count > 0)
		{
			warningMessages.Remove(msg);
			if (warningMessages.Count == 0)
			{
				warningMessage.text = "";
				UpdateWarningTransform(null);
			}
			else if (msg.Equals(warningMessage.text))
			{
				var en = warningMessages.GetEnumerator();
				if (en.MoveNext())
				{
					var pair = en.Current;
					warningMessage.text = pair.Key;
					UpdateWarningTransform(pair.Value);
				}
			}
		}
	}

	private void UpdateWarningTransform(Component c)
	{
		warningComponent = c;
		if (warningComponent == null)
		{
			warningIcon.gameObject.SetActive(false);
		}
		else
		{
			warningIcon.gameObject.SetActive(true);

			var rt = c.transform as RectTransform;
			float top = warningIcon.rect.height * 0.5f;
			Vector3[] corners = new Vector3[4];
			rt.GetWorldCorners(corners);
			top += (corners[0].y + corners[1].y) * 0.5f;
			(mask.parent as RectTransform).GetWorldCorners(corners);
			top -= corners[1].y;

			var offsetMin = warningIcon.offsetMin;
			var offsetMax = warningIcon.offsetMax;
			offsetMax.y = top;
			offsetMin.y = offsetMax.y - warningIcon.rect.height;
			warningIcon.offsetMin = offsetMin;
			warningIcon.offsetMax = offsetMax;
		}
	}

	private DataLayer GetLayer(string filename_lc)
	{
		var groups = dataManager.groups;
		foreach (var group in groups)
		{
			foreach (var layer in group.layers)
			{
				if (filename_lc.Contains(layer.Name.ToLower()))
					return layer;
			}
		}
		return null;
	}

	private void AutoAddCategoryInput()
    {
		bool hasEmpty = false;
		var categoriesInputs = categories.GetComponentsInChildren<InputField>();

		// Check if there already exists 1 empty category input
        foreach (var input in categoriesInputs)
        {
			if (string.IsNullOrEmpty(input.text))
            {
				hasEmpty = true;
				break;
            }
        }

		// Add new category input to categories
		if (!hasEmpty)
        {
			AddCategoryInput();
		}
	}

	private void ResetCategoryInputs()
    {
		var categoriesInputs = categories.GetComponentsInChildren<InputField>();
		int length = categoriesInputs.Length;

        for (int i = 1; i < length; ++i)
        {
			var categoryInput = categoriesInputs[i];

			categoryInput.onEndEdit.RemoveAllListeners();
			categoryInput.onValueChanged.RemoveListener(OnCategoryInputChanged);

			Destroy(categoryInput.gameObject);
        }
	}

	private void AddCategoryInput(string text = null)
	{
		var catInput = Instantiate(inputPrefab, categories);
		catInput.interactable = true;
		catInput.placeholder.GetComponent<LocalizedText>().text = translator.Get("New Category");
		catInput.name = $"CategoryInput{catInput.transform.GetSiblingIndex()}";

		catInput.onValueChanged.AddListener(OnCategoryInputChanged);
		catInput.onEndEdit.AddListener(OnCategoryInputEndEdit);
		AddOnEndEditEvent(catInput);

		if (!string.IsNullOrEmpty(text))
			catInput.text = text;
	}

	private void UpdateFieldDropdown(string filename)
    {
		fieldDropdown.ClearOptions();

		Shapefile shapefile = new Shapefile(filename);
		fieldDropdown.AddOptions(new List<string>(shapefile.FieldNames));
	}
}
