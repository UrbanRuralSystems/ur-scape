// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  Michael Joos  (joos@arch.ethz.ch)
//          Muhammad Salihin Bin Zaol-kefli

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_WEBGL
using System.IO.Compression;
#endif
using UnityEngine;
using UnityEngine.UI;

public class ExportTool : Tool
{
    public const string exportSubPath = "Export";
    public const string allFilename = "All";
    public const string backgroundFilename = "Background";
    public const string outputReportFilename = "OutputReport";
    public const string fullInterfaceFilename = "FullInterface";
    public const string bgLayersCombFilename = "AllLayers";

    [Header("UI References")]
    public Toggle fullInterfaceToggle;
    public Toggle bgLayersCombToggle;
    public Toggle bgLayersSepToggle;
    public Toggle outputReportToggle;
    public Toggle pngSizeSmallToggle;
    public Toggle pngSizeMediumToggle;
    public Toggle pngSizeLargeToggle;
    public Toggle outputCSVToggle;
    public Toggle outputPDFToggle;
    public Button exportButton;
    public Scrollbar analysisProgress;
	public ToggleGroup sizeGroup;
	public ToggleGroup formatGroup;
	public Text sizeLabel;
	public Text formatLabel;

	// UI elements
	private ZoomPanel zoomPanel;
	private BackgroundsComponent backgrounds;
	private CellInspector cellInspector;
	private Toggle collapseToggleLeft;
	private Toggle collapseToggleRight;
	private Toggle collapseToggleDown;
	private Transform footer;
	private LegendPanel legendPanel;
	private Toolbox toolbox;

	private enum ImgSize
    {
        Small = 0,
        Medium = 1,
        Large = 2
    };
    ImgSize imgSize = ImgSize.Small;

	private int exportWidth = 1920;
	private int exportHeight = 1080;

	private int siblingIndex;

#if UNITY_WEBGL
	private MemoryStream zipMemStream;
	private ZipArchive zip;
#endif

	//
	// Inheritance Methods
	//

	protected override void OnComponentRegistrationFinished()
	{
		base.OnComponentRegistrationFinished();

		// Init UI elements
		zoomPanel = FindObjectOfType<ZoomPanel>();
		backgrounds = FindObjectOfType<BackgroundsComponent>();
		cellInspector = FindObjectOfType<CellInspector>();
		var centerTopContainerT = GameObject.Find("CenterTopContainer").transform;
		collapseToggleLeft = centerTopContainerT.Find("ColapseToggleLeft").GetComponent<Toggle>();
		collapseToggleRight = centerTopContainerT.Find("ColapseToggleRight").GetComponent<Toggle>();
		collapseToggleDown = centerTopContainerT.Find("ColapseToggleDown").GetComponent<Toggle>();
		footer = centerTopContainerT.Find("Footer");
		legendPanel = footer.Find("LegendPanel").GetComponent<LegendPanel>();
		toolbox = FindObjectOfType<Toolbox>();

		siblingIndex = transform.GetSiblingIndex();
	}

	protected override void OnToggleTool(bool isOn)
    {
        if (isOn)
        {
			// Reset toggles
			fullInterfaceToggle.isOn = false;
            bgLayersCombToggle.isOn = false;
            bgLayersSepToggle.isOn = false;
            outputReportToggle.isOn = false;

			// Enable default toggles
			pngSizeSmallToggle.isOn = true;
			outputCSVToggle.isOn = true;

            UpdateExportButton();
			UpdateSizeOptions();
			UpdateFormatOptions();

			// Initialize listeners
			fullInterfaceToggle.onValueChanged.AddListener(OnPngOptionsToggleChange);
            bgLayersCombToggle.onValueChanged.AddListener(OnPngOptionsToggleChange);
            bgLayersSepToggle.onValueChanged.AddListener(OnPngOptionsToggleChange);
            outputReportToggle.onValueChanged.AddListener(OnOutputReportToggleChange);
            pngSizeSmallToggle.onValueChanged.AddListener(OnPngSizeSmallToggleChange);
            pngSizeMediumToggle.onValueChanged.AddListener(OnPngSizeMediumToggleChange);
            pngSizeLargeToggle.onValueChanged.AddListener(OnPngSizeLargeToggleChange);
            outputCSVToggle.onValueChanged.AddListener(OnOutputCSVToggleChange);
            outputPDFToggle.onValueChanged.AddListener(OnOutputPDFToggleChange);
            exportButton.onClick.AddListener(OnExportButtonClick);
        }
        else
        {
            // Remove listeners
            fullInterfaceToggle.onValueChanged.RemoveListener(OnPngOptionsToggleChange);
            bgLayersCombToggle.onValueChanged.RemoveListener(OnPngOptionsToggleChange);
            bgLayersSepToggle.onValueChanged.RemoveListener(OnPngOptionsToggleChange);
            outputReportToggle.onValueChanged.RemoveListener(OnOutputReportToggleChange);
            pngSizeSmallToggle.onValueChanged.RemoveListener(OnPngSizeSmallToggleChange);
            pngSizeMediumToggle.onValueChanged.RemoveListener(OnPngSizeMediumToggleChange);
            pngSizeLargeToggle.onValueChanged.RemoveListener(OnPngSizeLargeToggleChange);
            outputCSVToggle.onValueChanged.RemoveListener(OnOutputCSVToggleChange);
            outputPDFToggle.onValueChanged.RemoveListener(OnOutputPDFToggleChange);
            exportButton.onClick.RemoveListener(OnExportButtonClick);
        }
    }

	private void UpdateSizeOptions()
	{
		var interactable = fullInterfaceToggle.isOn || bgLayersCombToggle.isOn || bgLayersSepToggle.isOn;
		var toggles = sizeGroup.GetComponentsInChildren<Toggle>();
		foreach (var toggle in toggles)
		{
			if (toggle.group == sizeGroup)
				toggle.interactable = interactable;
		}
		sizeLabel.color = interactable? toggles[0].colors.normalColor : toggles[0].colors.disabledColor;
	}

	private void UpdateFormatOptions()
	{
		var interactable = outputReportToggle.isOn;
		var toggles = formatGroup.GetComponentsInChildren<Toggle>();
		foreach (var toggle in toggles)
		{
			if (toggle.group == formatGroup)
				toggle.interactable = interactable;
		}
		formatLabel.color = interactable ? toggles[0].colors.normalColor : toggles[0].colors.disabledColor;
	}

	private void UpdateExportButton()
    {
        exportButton.interactable = fullInterfaceToggle.isOn || bgLayersCombToggle.isOn || bgLayersSepToggle.isOn || outputReportToggle.isOn;
    }

    private void OnPngOptionsToggleChange(bool isOn)
    {
		UpdateSizeOptions();
		UpdateExportButton();
	}

    private void OnOutputReportToggleChange(bool isOn)
    {
		UpdateFormatOptions();
		UpdateExportButton();
    }

    private void OnPngSizeSmallToggleChange(bool isOn)
    {
        if (isOn)
        {
            imgSize = ImgSize.Small;
            // Export Image Dimensions
            exportWidth = 1920;
            exportHeight = 1080;
        }
    }

    private void OnPngSizeMediumToggleChange(bool isOn)
    {
        if (isOn)
        {
            imgSize = ImgSize.Medium;
            // Export Image Dimensions
            exportWidth = 3840;
            exportHeight = 2160;
        }
    }

    private void OnPngSizeLargeToggleChange(bool isOn)
    {
        if (isOn)
        {
            imgSize = ImgSize.Large;
            // Export Image Dimensions
            exportWidth = 7680;
            exportHeight = 4320;
        }
    }

    private void OnOutputCSVToggleChange(bool isOn)
    {
        //+ update format variable
    }

    private void OnOutputPDFToggleChange(bool isOn)
    {
		//+ update format variable
	}

	private IEnumerator ExportImages(string exportPath)
	{
		// Get required components
		var map = ComponentManager.Instance.Get<MapController>();
		if (map == null)
		{
			Debug.LogError("Missing map component");
			yield break;
		}

		// Disable input
		var input = ComponentManager.Instance.Get<InputHandler>();
		if (input)
		{
			input.OnLeftMouseDown += NoOp;
			input.OnRightMouseDown += NoOp;
		}

		var screenshot = new ScreenshotHelper(FindObjectOfType<WindowController>(), WriteFile);

		yield return ExportImages(exportPath, screenshot);

		// Clean up
		screenshot.Destroy();

		// Restore input
		if (input)
		{
			input.OnLeftMouseDown -= NoOp;
			input.OnRightMouseDown -= NoOp;
		}

		map.RequestMapUpdate();
	}

	private IEnumerator ExportImages(string exportPath, ScreenshotHelper screenshot)
	{
		var background = map.GetLayerController<MapboxLayerController>();
		List<MapboxLayer> backgroundLayers = GetLayers(background);
		List<GridMapLayer> gridLayers = GetLayers<GridMapLayer>(map);
		List<MapLayer> toolLayers = GetLayers(map.GetLayerController<ToolLayerController>());

		int count = 0;
		if (fullInterfaceToggle.isOn)
			count++;
		if (bgLayersCombToggle.isOn)
			count++;
		if (bgLayersSepToggle.isOn)
		{
			if (backgroundLayers.Count > 0)
				count++;
			count += gridLayers.Count;
			count += toolLayers.Count;
		}

		Progress progress = new Progress(count, analysisProgress);

		// Export a screenshot of the full interface
		if (fullInterfaceToggle.isOn)
		{
			legendPanel.gameObject.SetActive(true);
			yield return new WaitForFrames(2);
			ExpandLegendItems(true);
			yield return new WaitForFrames(2);

			if (!progress.Update("Exporting full interface ..."))
				yield break;

			yield return null;

			var filename = exportPath + fullInterfaceFilename + "_" + imgSize.ToString() + ".png";
			yield return screenshot.TakeScreenshot(filename, exportWidth, exportHeight, true);

			ExpandLegendItems(false);
			legendPanel.gameObject.SetActive(false);
		}
		
		// Export a screenshot with all layers
		if (bgLayersCombToggle.isOn)
		{
			ToggleUI(false);
			yield return new WaitForFrames(2);
			ExpandLegendItems(true);
			yield return new WaitForFrames(2);

			if (!progress.Update("Exporting all layers combined ..."))
				yield break;
			yield return null;

			var filename = exportPath + bgLayersCombFilename + "_" + imgSize.ToString() + ".png";
			yield return screenshot.TakeScreenshot(filename, exportWidth, exportHeight, true);

			ExpandLegendItems(false);
			ToggleUI(true);
		}

        if (bgLayersSepToggle.isOn)
        {
			// Hide UI
			ToggleUI(false, false);
			yield return new WaitForFrames(2);
			ToggleLegendItems(false);
			yield return new WaitForFrames(2);

			// Hide all grid layers
			foreach (var layer in gridLayers)
            {
                layer.SetShape(GridMapLayer.Shape.Circle);
                layer.SetInterpolation(false);
                layer.Show(false);
            }

            bool contourOn = false;

            // Hide all tool layers
            foreach (var layer in toolLayers)
            {
                // For colour preservation when Contour Tool is active
                if (layer is ContoursMapLayer)
                    contourOn = true;

				var gridLayer = layer as GridMapLayer;
				if (gridLayer != null)
					gridLayer.SetShape(GridMapLayer.Shape.Circle);
                layer.Show(false);
            }

            // For colour preservation when Contour Tool is active
            if (contourOn)
                ComponentManager.Instance.Get<DataLayers>().ResetToolOpacity();

			bool cancelled = false;

            if (backgroundLayers.Count > 0)
            {
				if (!progress.Update("Exporting background layer ..."))
					cancelled = true;

				if (!cancelled)
				{
					yield return null;

					var filename = exportPath + backgroundFilename + "_" + imgSize.ToString() + ".png";
					yield return screenshot.TakeScreenshot(filename, exportWidth, exportHeight, true);
				}
			}

            // Hide background
            if (background)
                background.gameObject.SetActive(false);

			ToggleUI(false);
			yield return new WaitForFrames(2);
			ToggleLegendItems(false);
			yield return new WaitForFrames(2);

			// Iterate thru each individual layer and export them
			if (!cancelled && gridLayers.Count > 0)
			{
				yield return ExportLayers(gridLayers, screenshot, progress, exportPath);
				if (progress.IsCancelled)
					cancelled = true;
			}
            if (!cancelled && toolLayers.Count > 0)
			{
				yield return ExportLayers(toolLayers, screenshot, progress, exportPath);
				if (progress.IsCancelled)
					cancelled = true;
			}

			// Show background again
			if (background)
                background.gameObject.SetActive(true);

            // Show all tool layers again
            foreach (var layer in toolLayers)
            {
				var gridLayer = layer as GridMapLayer;
				if (gridLayer != null)
					gridLayer.SetShape(GridMapLayer.Shape.Circle);
                layer.Show(true);
            }

            // Show all grid layers again
            foreach (var layer in gridLayers)
            {
                layer.SetShape(GridMapLayer.Shape.Circle);
                layer.SetInterpolation(false);
                layer.Show(true);
            }

            // For colour preservation when Contour Tool is active
            if (contourOn)
                ComponentManager.Instance.Get<DataLayers>().AutoReduceToolOpacity();

			// Show UI again
			ToggleLegendItems(true);
			ToggleUI(true);
		}

		progress.Stop();
	}

	private IEnumerator ExportOutputCSV(string filename)
    {
        var activeLayerPanels = ComponentManager.Instance.Get<DataLayers>().activeLayerPanels;
        var tabPanelObj = ComponentManager.Instance.Get<Toolbox>().tabPanel.gameObject;
        int toolsCount = tabPanelObj.transform.childCount - 1;  // Exclude Add_Tab
        var outputPanelObj = ComponentManager.Instance.Get<OutputPanel>().outputContainer;

        int count = 0;
        count += activeLayerPanels.Count;

        foreach (var activeLayerPanel in activeLayerPanels)
        {
			if (activeLayerPanel.DataLayer.loadedPatchesInView[0].Data is GridData data && data.IsCategorized)
			{
				count += data.categories.Length;
			}
		}
        count += toolsCount;

        Progress progress = new Progress(count, analysisProgress);
		progress.Update("Exporting data layers ...");

		yield return null;

		var translator = LocalizationManager.Instance;
		using (var memStream = new MemoryStream())
		{
			using (var csv = new StreamWriter(memStream, System.Text.Encoding.UTF8))
			{
				csv.WriteLine(translator.Get("Active Data Layers").ToUpper());
				csv.WriteLine(translator.Get("Layer Name").ToUpper() + "," + translator.Get("Filtered Data").ToUpper() + "," + translator.Get("Units").ToUpper());

				// Data Layers info
				foreach (var activeLayerPanel in activeLayerPanels)
				{
					string layerName = activeLayerPanel.name;
					string filteredData = "";
					string units = translator.Get("N/A");

					// Filtered Data
					var data = activeLayerPanel.DataLayer.loadedPatchesInView[0].Data;
					if (data is GridData)
					{
						var gridData = data as GridData;
						if (gridData.IsCategorized)
						{
							var categories = gridData.categories;
							int catCount = (categories == null) ? 0 : categories.Length;

							if (catCount > 0)
							{
								for (int i = 0; i < catCount; i++)
								{
									if (!gridData.categoryFilter.IsSet(i))
										continue;

									filteredData += categories[i].name;

									if (i != catCount - 1)
										filteredData += ",";
								}
							}
						}
						else
						{
							float minFilterData = activeLayerPanel.DataLayer.MinFilter * activeLayerPanel.DataLayer.MaxVisibleValue;
							float maxFilterData = activeLayerPanel.DataLayer.MaxFilter * activeLayerPanel.DataLayer.MaxVisibleValue;
							filteredData = minFilterData.ToString("F2") + "-" + maxFilterData.ToString("F2");

							units = gridData.units;
						}
					}
					csv.WriteLine("{0},{1},{2}", layerName, CsvHelper.Escape(filteredData), units);
				}
				csv.WriteLine();

				if (!progress.Update("Exporting tools ..."))
					yield break;
				yield return null;

				// Tools info
				for (int i = 0; i < toolsCount; ++i)
				{
					if (!tabPanelObj.transform.GetChild(i).gameObject.activeSelf)
						continue;

					string toolName = tabPanelObj.transform.GetChild(i).gameObject.name;
					toolName = toolName.Replace("ConfigTab", "");

					int outputCount = outputPanelObj.childCount;
					for (int j = outputCount - 1; j > 0; --j)
					{						
						var outputComp = outputPanelObj.GetChild(j).GetComponent<IOutput>();
						if (outputComp != null)
						{
							if (outputComp.ToString().Contains(toolName))
							{
								string toolHeader = translator.Get((toolName + " Tool"));
								csv.WriteLine(toolHeader.ToUpper());
								outputComp.OutputToCSV(csv);
								csv.WriteLine();
							}
						}
					}
				}
			}
			WriteFile(filename, memStream.GetBuffer());
		}
		progress.Stop();
    }

    private void OnExportButtonClick()
    {
        StartCoroutine(Export());
    }

	private IEnumerator Export()
	{
		string exportPath = "";

#if !UNITY_WEBGL || UNITY_EDITOR
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
		exportPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
		exportPath += Path.DirectorySeparatorChar + "ur-scape ";
#else
		exportPath = System.Environment.CurrentDirectory + Path.DirectorySeparatorChar; // Export to where the executable is
#endif
		exportPath += exportSubPath + Path.DirectorySeparatorChar;

		// Create export directory
        Directory.CreateDirectory(exportPath);
#endif

		// Add the date as a file prefix
#if UNITY_WEBGL
		var filesPath = System.DateTime.Now.ToString("yyyyMMdd_HHmmss_");
#else
		var filesPath = exportPath + System.DateTime.Now.ToString("yyyyMMdd_HHmmss_");
#endif

#if UNITY_WEBGL
		InitializeZip();
		try
		{
#endif
			if (fullInterfaceToggle.isOn || bgLayersCombToggle.isOn || bgLayersSepToggle.isOn)
			{
				yield return ExportImages(filesPath);
			}

			if (outputReportToggle.isOn && outputCSVToggle.isOn)
			{
				yield return ExportOutputCSV(filesPath + outputReportFilename + ".csv");
			}

#if UNITY_WEBGL
		}
		finally
		{
			CloseZip(exportPath + filesPath + "Export.zip");
		}
#endif
	}

	public void WriteFile(string filename, byte[] data)
	{
#if UNITY_WEBGL
		var zipItem = zip.CreateEntry(filename);
		using (var fileMemoryStream = new MemoryStream(data))
		using (var entryStream = zipItem.Open())
		{
			fileMemoryStream.CopyTo(entryStream);
		}
#else
		File.WriteAllBytes(filename, data);
#endif
	}

#if UNITY_WEBGL
	private void InitializeZip()
	{
		zipMemStream = new MemoryStream();
		zip = new ZipArchive(zipMemStream, ZipArchiveMode.Create, true);
	}

	private void CloseZip(string filename)
	{
		if (zip != null)
		{
			zip.Dispose();
			zip = null;
		}

		if (zipMemStream != null)
		{
			var bytes = zipMemStream.ToArray();

#if UNITY_EDITOR
			File.WriteAllBytes(filename, bytes);
#else
			Web.DownloadFile(filename, bytes, bytes.Length);
#endif

			zipMemStream.Close();
			zipMemStream.Dispose();
			zipMemStream = null;
		}
	}
#endif

	private IEnumerator ExportLayers<T>(List<T> layers, ScreenshotHelper screenshot, Progress progress, string exportPath) where T : MapLayer
	{
		// Export each individual layer
		int size = layers.Count;
        for (int i = 0; i < size; ++i)
        {
			string name = layers[i].name;
			var patchLayer = layers[i] as PatchMapLayer;
			if (patchLayer != null && patchLayer.PatchData.patch != null)
				name = patchLayer.PatchData.patch.DataLayer.Name;

			if (!progress.Update("Exporting layer (" + (i + 1) + "/" + size + "): " + name + " ..."))
                yield break;

            layers[i].Show(true);
			if (layers[i] is SnapshotMapLayer)
			{
				ShowSnapshotLegendItem(i, true);
				yield return new WaitForFrames(2);
			}
			else if (!(layers[i] is ContoursMapLayer))
			{
				ShowDataLayerLegendItem(i, true);
				yield return new WaitForFrames(2);
			}

            var filename = exportPath + name + "_" + imgSize.ToString() + ".png";
            yield return screenshot.TakeScreenshot(filename, exportWidth, exportHeight, !(layers[i] is ContoursMapLayer));

			layers[i].Show(false);
			if (layers[i] is SnapshotMapLayer)
			{
				ShowSnapshotLegendItem(i, false);
				yield return new WaitForFrames(2);
			}
			else if (!(layers[i] is ContoursMapLayer))
			{
				ShowDataLayerLegendItem(i, false);
				yield return new WaitForFrames(2);
			}
		}
    }

    private List<T> GetLayers<T>(MapController map) where T : PatchMapLayer
	{
        return GetLayers(map.GetLayerController<MapLayerControllerT<T>>());
    }

    private List<T> GetLayers<T>(MapLayerControllerT<T> layerController) where T : MapLayer
	{
        List<T> layers = new List<T>();
        if (layerController)
        {
            foreach (var layer in layerController.mapLayers)
            {
                if (layer.IsVisible())
                    layers.Add(layer);
            }
        }
        return layers;
    }

    private void NoOp() { }

	private void ToggleUI(bool show, bool withLegend = true)
	{
		// Zoom panel
		zoomPanel.gameObject.SetActive(show);
		// Backgrounds
		backgrounds.gameObject.SetActive(show);
		// CellInspector
		cellInspector.gameObject.SetActive(show);
		// ColapseToggleLeft
		collapseToggleLeft.isOn = show;
		collapseToggleLeft.gameObject.SetActive(show);
		// ColapseToggleRight
		collapseToggleRight.isOn = show;
		collapseToggleRight.gameObject.SetActive(show);
		// CenterBottomContainer
		if (show)
        {
			transform.SetParent(toolbox.toolsPanel, false);
			transform.SetSiblingIndex(siblingIndex);
        }
		else
        {
			transform.SetParent(null, false);
        }
		toolbox.transform.parent.gameObject.SetActive(show);
		int exportToolChildCount = transform.childCount;
		for (int i = 0; i < exportToolChildCount; ++i)
        {
			transform.GetChild(i).gameObject.SetActive(show);
        }
		collapseToggleDown.gameObject.SetActive(show);
		// Footer
		footer.GetComponent<Image>().enabled = show;
		int footerChildCount = footer.childCount;
		for (int i = 0; i < footerChildCount; ++i)
		{
			var child = footer.GetChild(i);
			if (child.name.Contains("Panel"))
				continue;
			child.gameObject.SetActive(show);
		}
		legendPanel.gameObject.SetActive(withLegend);
	}

	private void ToggleLegendItems(bool show)
    {
		var legendItems = legendPanel.LegendItems.Values;

		foreach (var legendItem in legendItems)
		{
			legendItem.gameObject.SetActive(show);
		}
	}

	private void ShowDataLayerLegendItem(int i, bool show)
    {
		var legendItems = legendPanel.LegendItems.Values.ToArray();
		var dataLayerLegendItems = Array.FindAll(legendItems, ele => !(ele is SnapshotLegendItem));

		if (show)
		{
			dataLayerLegendItems[i].gameObject.SetActive(true);
			dataLayerLegendItems[i].GroupToggle.isOn = true;
		}
		else
		{
			dataLayerLegendItems[i].GroupToggle.isOn = false;
			dataLayerLegendItems[i].gameObject.SetActive(false);
		}
	}

	private void ShowSnapshotLegendItem(int i, bool show)
	{
		var legendItems = legendPanel.LegendItems.Values.ToArray();
		var snapshotLegendItems = Array.FindAll(legendItems, ele => ele is SnapshotLegendItem);

		if (show)
		{
			snapshotLegendItems[i - 1].gameObject.SetActive(true);
			snapshotLegendItems[i - 1].GroupToggle.isOn = true;
		}
		else
		{
			snapshotLegendItems[i - 1].GroupToggle.isOn = false;
			snapshotLegendItems[i - 1].gameObject.SetActive(false);
		}
	}

	private void ExpandLegendItems(bool show)
    {
		var legendItems = legendPanel.LegendItems.Values;

		foreach (var legendItem in legendItems)
		{
			legendItem.GroupToggle.isOn = show;
		}
	}
}