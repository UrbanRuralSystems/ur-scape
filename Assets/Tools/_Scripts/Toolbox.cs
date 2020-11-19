// Copyright (C) 2020 Singapore ETH Centre, Future Cities Laboratory
// All rights reserved.
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//
// Author:  David Neudecker (neudecker@arch.ethz.ch)
//          Michael Joos  (joos@arch.ethz.ch)

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Toolbox : UrsComponent
{
	private class ToolInfo
	{
		public ToolConfig toolConfig;
		public Button button;
		public Toggle tab;
		public RectTransform panel;
	}

	public BuildConfig config;
	private readonly List<ToolInfo> tools = new List<ToolInfo>();

	[Header("UI Refecences")]
    public ToggleGroup tabPanel;
	public RectTransform toolsPanel;
	public RectTransform toolsListPanel;

	[Header("Prefabs")]
    public GameObject tabPrefab;
    public GameObject closeableTabPrefab;
    public GameObject buttonPrefab;
	public ToolMessenger messengerPrefab;

    private GameObject addTab;

    private int openedTools;

    //
    // Unity Methods
    //

    void Start()
    {
		LocalizationManager.OnReady += OnLocalizationManagerIsReady;

        // Tab for tool list panel (ADD)
        addTab = Instantiate(tabPrefab, tabPanel.transform, false);
        addTab.name = "Add_Tab";
        addTab.GetComponentInChildren<Text>().text = Translator.Get("Tools");
        var toggle = addTab.GetComponent<Toggle>();
        toggle.isOn = true;
        toggle.group = tabPanel;
        toggle.onValueChanged.AddListener(OnAddToggleChanged);

		openedTools = 0;

        var scroll = transform.GetComponentInChildren<ScrollRect>();
        scroll.content = toolsListPanel;

        // Create button and panel for each tool
        StartCoroutine(CreateToolsUI());
    }

	private IEnumerator CreateToolsUI()
    {
		yield return null;
		foreach (var tool in config.platform.tools)
        {
			if (tool.config.enabled && 
				tool.state != ToolState.Hidden)
			{
                AddTool(tool.config, tool.state == ToolState.Enabled);
            }
        }

        // Add other tools that are not in the list and have the 'autoAddToToolbox' flag set
        if (config.platform.includeOtherTools)
        {
            var toolConfigs = Resources.LoadAll<ToolConfig>("");
            foreach (var toolConfig in toolConfigs)
            {
                if (toolConfig.autoAddToToolbox &&
                    toolConfig.enabled &&
                    !tools.Exists(t => t.toolConfig == toolConfig))
                {
                    AddTool(toolConfig);
                }
            }
        }
    }


    //
    // Inheritance Methods
    //

    public override void ResetComponent()
    {
        // Close all tabs
        foreach (var tool in tools)
        {
            if (tool.tab != null)
            {
                CloseTool(tool);
            }
        }
        UpdateAddTab();
    }

    public override bool HasBookmarkData()
    {
        return false;
    }

    public override void SaveToBookmark(BinaryWriter bw, string bookmarkPath)
    {
		/*
		int activeTabIndex = 0;
        bw.Write(tools.Count);
        foreach (var tool in tools)
        {
            bool isOpen = tool.enabled && tool.tab != null;
            bw.Write(isOpen);
            if (isOpen)
            {
                int index = tool.tab.transform.GetSiblingIndex();
                bw.Write(tool.tab.transform.GetSiblingIndex());
                bw.Write(tool.name);
                if (tool.tab.isOn)
                    activeTabIndex = index;
            }
        }
        bw.Write(activeTabIndex);
		*/
	}

	public override void LoadFromBookmark(BinaryReader br, string bookmarkPath)
    {
		/*
        int count = br.ReadInt32();
        ToolInfo[] openTools = new ToolInfo[count];
        for (int i = 0; i < count; i++)
        {
            bool isOpen = br.ReadBoolean();
            if (isOpen)
            {
                int index = br.ReadInt32();
                string toolName = br.ReadString();

                foreach (var tool in tools)
                {
                    if (tool.config.enabled && tool.Name.Equals(toolName))
                    {
						openTools[index] = tool;
                        break;
                    }
                }
            }
        }
        int activeTabIndex = br.ReadInt32();

        // Open the tools from the bookmark
        for (int i = 0; i < count; i++)
        {
            if (openTools[i] != null)
            {
                OpenTool(openTools[i], i == activeTabIndex);
            }
        }

        UpdateAddTab();
		*/
	}


	//
	// Event Methods
	//

	private void OnLocalizationManagerIsReady()
	{
		LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
		OnLanguageChanged();
	}

	private void OnLanguageChanged()
	{
		var translator = LocalizationManager.Instance;
		var tab = tabPanel.transform.GetChild(tabPanel.transform.childCount - 1);
		tab.GetComponentInChildren<Text>().text = translator.Get("Tools");

		foreach (var tool in tools)
		{
			var toolName = translator.Get(tool.toolConfig.label);
			tool.button.GetComponentInChildren<Text>().text = toolName;
			if (tool.tab != null)
			{
				tool.tab.GetComponentInChildren<Text>().text = toolName;
			}
		}
	}

	private void OnAddToggleChanged(bool isOn)
    {
		toolsListPanel.gameObject.SetActive(isOn);
    }

    private void OnTabToggleChange(ToolInfo tool, bool isOn)
    {
		tool.panel.gameObject.SetActive(isOn);
        var toolComponent = tool.panel.GetComponent<Tool>();
		toolComponent.Activate(isOn);
    }

    private void OnCloseToolButtonClick(ToolInfo tool)
    {
        // Find a sibling tab to activate
        Toggle otherTab = null;
        if (tool.tab.isOn)
        {
            int index = tool.tab.transform.GetSiblingIndex();
            index += (index > 0)? -1 : 1;
            otherTab = addTab.transform.parent.GetChild(index).GetComponent<Toggle>();
        }

        CloseTool(tool, otherTab);

        UpdateAddTab();
    }

    private void OnToolButtonClick(ToolInfo tool)
    {
        OpenTool(tool, true);
        UpdateAddTab();
    }


	//
	// Public Methods
	//

	public ToolMessenger CreateMessenger(Transform parent)
	{
		var messenger = Instantiate(messengerPrefab, parent, false);
		messenger.GetComponent<RectTransform>().offsetMax = new Vector2(toolsPanel.rect.width, 0);
		return messenger;
	}

    public void AddTool(ToolConfig toolConfig)
    {
        AddTool(toolConfig, true);
    }


    //
    // Private Methods
    //

    private void AddTool(ToolConfig toolConfig, bool enabled)
    {
        var toolInfo = new ToolInfo();
        tools.Add(toolInfo);

        toolInfo.toolConfig = toolConfig;
        toolInfo.button = CreateButton(toolConfig, toolsListPanel.transform, enabled);
        if (enabled)
        {
            toolInfo.button.onClick.AddListener(delegate { OnToolButtonClick(toolInfo); });
            toolInfo.panel = CreatePanel(toolConfig);
        }
    }

    private RectTransform CreatePanel(ToolConfig config)
    {
        var toolPanel = Instantiate(config.panelPrefab, toolsPanel, false);
		toolPanel.SetActive(false);
		toolPanel.name = config.label + "Panel";
        return toolPanel.GetComponent<RectTransform>();
    }

    private Button CreateButton(ToolConfig config, Transform panel, bool enabled)
    {
        var button = Instantiate(buttonPrefab, panel, false).GetComponent<Button>();
		button.image.sprite = config.icon;
		button.interactable = enabled;
		button.GetComponentInChildren<Text>().text = Translator.Get(config.label);

		return button;
    }

    private Toggle AddTab(ToolInfo tool)
    {
        var tab = Instantiate(closeableTabPrefab, tabPanel.transform, false);
        tab.name = tool.toolConfig.name + "Tab";

        // Setup tab toggle
        var toggle = tab.GetComponent<Toggle>();
        toggle.GetComponentInChildren<Text>().text = Translator.Get(tool.toolConfig.label);
        toggle.isOn = false;
        toggle.group = tabPanel;
        toggle.onValueChanged.AddListener((isOn) => OnTabToggleChange(tool, isOn));

		// Close tool button
		var button = tab.GetComponentInChildren<Button>();
        button.onClick.AddListener(delegate { OnCloseToolButtonClick(tool); });

        GuiUtils.RebuildLayout(tab.transform);

        return toggle;
    }

    private void OpenTool(ToolInfo tool, bool activate)
    {
        // Disable the tool button
        tool.button.interactable = false;
		openedTools++;

		// Create the tab
		tool.tab = AddTab(tool);

		// Open the tool
		var toolComponent = tool.panel.GetComponent<Tool>();
        if (toolComponent != null)
        {
            toolComponent.Open(activate);
        }

        if (activate)
            tool.tab.isOn = true;

        var scroll = transform.GetComponentInChildren<ScrollRect>();
        scroll.content = tool.panel;
    }

    private void CloseTool(ToolInfo tool, Toggle otherTab = null)
    {
        // Remove listeners
        var toggle = tool.tab.GetComponent<Toggle>();
        toggle.onValueChanged.RemoveAllListeners();
        var button = tool.tab.GetComponentInChildren<Button>();
        button.onClick.RemoveAllListeners();

        // Close the tool
        var toolComponent = tool.panel.GetComponent<Tool>();
        if (toolComponent != null)
        {
            toolComponent.Close();
        }

        // Activate the new tab
        if (otherTab != null)
        {
            otherTab.isOn = true;
        }

        // Destroy the tab
        Destroy(tool.tab.gameObject);
		tool.tab = null;

        // Show the tool button
        tool.button.interactable = true;
        openedTools--;

        var scroll = transform.GetComponentInChildren<ScrollRect>();
        scroll.content = toolsListPanel;
    }

    private void UpdateAddTab()
    {
        // Force the "Add" tab to be last
        addTab.transform.SetAsLastSibling();

		// Check if there are any remaining tools in the list
		addTab.GetComponent<Toggle>().interactable = (openedTools > 0);
    }
}
